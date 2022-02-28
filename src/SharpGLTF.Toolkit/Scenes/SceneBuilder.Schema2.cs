using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

using SharpGLTF.Schema2;

using MESHBUILDER = SharpGLTF.Geometry.IMeshBuilder<SharpGLTF.Materials.MaterialBuilder>;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Defines configurable options for converting <see cref="SceneBuilder"/> to <see cref="ModelRoot"/>
    /// </summary>
    /// <remarks>
    /// Used by <see cref="SceneBuilder.ToGltf2(SceneBuilderSchema2Settings)"/>
    /// </remarks>
    public struct SceneBuilderSchema2Settings
    {
        public static SceneBuilderSchema2Settings Default => new SceneBuilderSchema2Settings
        {
            UseStridedBuffers = true,
            CompactVertexWeights = false,
            GpuMeshInstancingMinCount = int.MaxValue
        };

        public static SceneBuilderSchema2Settings WithGpuInstancing => new SceneBuilderSchema2Settings
        {
            UseStridedBuffers = true,
            CompactVertexWeights = false,
            GpuMeshInstancingMinCount = 3
        };

        /// <summary>
        /// When true, meshes will be created using strided vertices when possible.
        /// </summary>
        /// <remarks>
        /// this option is not taken into account by meshes with morph targets.
        /// </remarks>
        public bool UseStridedBuffers { get; set; }

        /// <summary>
        /// if meshes have Skin Weights, defines the output vertex element format:<br/>
        /// - True: Short<br/>
        /// - False: Float<br/>
        /// </summary>
        public bool CompactVertexWeights { get; set; }

        /// <summary>
        /// determines the mínimum number mesh instances required to enable Gpu mesh instancing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Set to <see cref="int.MaxValue"/> to disable gpu instancing.
        /// </para>
        /// <para>
        /// If set to a small value like 10, any mesh instance collection smaller than this will be instantiated
        /// using individual nodes, otherwise it will use Gpu Instancing extension.
        /// </para>
        /// </remarks>
        public int GpuMeshInstancingMinCount { get; set; }
    }

    public partial class SceneBuilder : IConvertibleToGltf2
    {
        #region from SceneBuilder to Schema2

        /// <summary>
        /// Converts this <see cref="SceneBuilder"/> instance into a <see cref="ModelRoot"/> instance.
        /// </summary>
        /// <returns>A new <see cref="ModelRoot"/> instance.</returns>
        public ModelRoot ToGltf2()
        {
            return ToGltf2(new[] { this }, SceneBuilderSchema2Settings.Default);
        }

        /// <summary>
        /// Converts this <see cref="SceneBuilder"/> instance into a <see cref="ModelRoot"/> instance.
        /// </summary>
        /// <param name="settings">Conversion settings.</param>
        /// <returns>A new <see cref="ModelRoot"/> instance.</returns>
        public ModelRoot ToGltf2(SceneBuilderSchema2Settings settings)
        {
            return ToGltf2(new[] { this }, settings);
        }

        /// <summary>
        /// Converts a collection of <see cref="SceneBuilder"/> instances to a single <see cref="ModelRoot"/> instance.
        /// </summary>
        /// <param name="srcScenes">A collection of scenes</param>
        /// <param name="settings">Conversion settings.</param>
        /// <returns>A new <see cref="ModelRoot"/> instance.</returns>
        public static ModelRoot ToGltf2(IEnumerable<SceneBuilder> srcScenes, SceneBuilderSchema2Settings settings)
        {
            Guard.NotNull(srcScenes, nameof(srcScenes));

            var context = new Schema2SceneBuilder();
            context.GpuMeshInstancingMinCount = settings.GpuMeshInstancingMinCount;

            var dstModel = ModelRoot.CreateModel();
            context.AddGeometryResources(dstModel, srcScenes, settings);

            foreach (var srcScene in srcScenes)
            {
                var dstScene = dstModel.UseScene(dstModel.LogicalScenes.Count);
                srcScene.TryCopyNameAndExtrasTo(dstScene);

                context.AddScene(dstScene, srcScene);
            }

            dstModel.DefaultScene = dstModel.LogicalScenes[0];

            return dstModel;
        }

        #endregion

        #region from Schema2 to SceneBuilder

        public static SceneBuilder[] CreateFrom(ModelRoot model)
        {
            return model == null
                ? Array.Empty<SceneBuilder>()
                : CreateFrom(model.LogicalScenes).ToArray();
        }

        public static SceneBuilder CreateFrom(Scene srcScene)
        {
            if (srcScene == null) return null;

            // gather shared mesh instances

            var dstMeshIntances = _GatherMeshInstances(Node.Flatten(srcScene));

            return _CreateFrom(srcScene, dstMeshIntances);
        }

        public static IEnumerable<SceneBuilder> CreateFrom(IEnumerable<Scene> srcScenes)
        {
            if (srcScenes == null) yield break;

            // gather shared mesh instances

            var dstMeshIntances = _GatherMeshInstances(srcScenes.Distinct().SelectMany(s => Node.Flatten(s)));

            // process each scene

            foreach (var srcScene in srcScenes)
            {
                yield return _CreateFrom(srcScene, dstMeshIntances);
            }
        }

        private static SceneBuilder _CreateFrom(Scene srcScene, IReadOnlyDictionary<Node, MESHBUILDER> dstInstances)
        {
            // Process armatures

            var dstNodes = new Dictionary<Node, NodeBuilder>();

            foreach (var srcArmature in srcScene.VisualChildren)
            {
                var dstArmature = new NodeBuilder();
                _CopyToNodeBuilder(dstArmature, srcArmature, dstNodes);
            }

            // TODO: we must also process the armatures of every skin, in case the joints are outside the scene.

            var dstScene = new SceneBuilder();

            dstScene.SetNameAndExtrasFrom(srcScene);

            _AddMeshInstances(dstScene, Node.Flatten(srcScene), dstNodes, dstInstances);

            // process cameras
            var srcCameraInstances = Node.Flatten(srcScene)
                .Where(item => item.Camera != null)
                .ToList();

            _AddCameraInstances(dstScene, dstNodes, srcCameraInstances);

            var srcLightInstances = Node.Flatten(srcScene)
                .Where(item => item.PunctualLight != null)
                .ToList();

            _AddLightInstances(dstScene, dstNodes, srcCameraInstances);

            #if DEBUG
            dstScene._VerifyConversion(srcScene);
            #endif

            return dstScene;
        }

        private static IReadOnlyDictionary<Node, MESHBUILDER> _GatherMeshInstances(IEnumerable<Node> srcNodes)
        {
            // filter all the nodes with meshes

            var srcInstances = srcNodes
                .Where(item => item.Mesh != null);

            // create a dictionary of shared Mesh => MeshBuilder pairs.

            var srcMeshes = srcInstances
                .Select(item => item.Mesh)
                .Distinct()
                .ToDictionary(item => item, item => item.ToMeshBuilder());

            // return a Node => MeshBuilder dictionary.

            return srcInstances
                .ToDictionary(item => item, item => srcMeshes[item.Mesh]);
        }

        private static void _AddMeshInstances(SceneBuilder dstScene, IEnumerable<Node> srcNodes, IReadOnlyDictionary<Node, NodeBuilder> nodesDict, IReadOnlyDictionary<Node, MESHBUILDER> meshesDict)
        {
            foreach (var srcNode in srcNodes)
            {
                if (!meshesDict.TryGetValue(srcNode, out var dstMesh)) continue; // nothing to do.

                if (srcNode.Skin == null)
                {
                    // rigid mesh instance

                    var dstNode = nodesDict[srcNode];

                    var gpuInstancing = srcNode.GetGpuInstancing();

                    if (gpuInstancing == null)
                    {
                        var dstInstance = dstScene.AddRigidMesh(dstMesh, dstNode);

                        _CopyMorphingAnimation(dstInstance, srcNode);
                    }
                    else
                    {
                        // use gpu instancing extension

                        foreach (var xinst in gpuInstancing.LocalTransforms)
                        {
                            var dstInstance = dstScene.AddRigidMesh(dstMesh, dstNode, xinst);

                            // if we add morphing to the mesh, all meshes would morph simultaneously??
                            _CopyMorphingAnimation(dstInstance, srcNode);
                        }
                    }
                }
                else
                {
                    // skinned mesh instance

                    var joints = new (NodeBuilder, Matrix4x4)[srcNode.Skin.JointsCount];

                    for (int i = 0; i < joints.Length; ++i)
                    {
                        var (j, ibm) = srcNode.Skin.GetJoint(i);
                        joints[i] = (nodesDict[j], ibm);
                    }

                    var dstInst = dstScene.AddSkinnedMesh(dstMesh, joints);

                    _CopyMorphingAnimation(dstInst, srcNode);
                }
            }
        }

        private static void _AddCameraInstances(SceneBuilder dstScene, IReadOnlyDictionary<Node, NodeBuilder> dstNodes, IReadOnlyList<Node> srcInstances)
        {
            if (srcInstances.Count == 0) return;

            foreach (var srcInstance in srcInstances)
            {
                var dstNode = dstNodes[srcInstance];
                var srcCam = srcInstance.Camera;
                if (srcCam == null) continue;

                CameraBuilder dstCam = null;

                if (srcCam.Settings is CameraPerspective perspective) dstCam = new CameraBuilder.Perspective(perspective);
                if (srcCam.Settings is CameraOrthographic orthographic) dstCam = new CameraBuilder.Orthographic(orthographic);

                if (dstCam != null)
                {
                    dstCam.SetNameAndExtrasFrom(srcCam);
                    dstScene.AddCamera(dstCam, dstNode);
                }
            }
        }

        private static void _AddLightInstances(SceneBuilder dstScene, IReadOnlyDictionary<Node, NodeBuilder> dstNodes, IReadOnlyList<Node> srcInstances)
        {
            if (srcInstances.Count == 0) return;

            foreach (var srcInstance in srcInstances)
            {
                var dstNode = dstNodes[srcInstance];
                var srcLight = srcInstance.PunctualLight;
                if (srcLight == null) continue;

                LightBuilder dstLight = null;
                if (srcLight.LightType == PunctualLightType.Directional) dstLight = new LightBuilder.Directional(srcLight);
                if (srcLight.LightType == PunctualLightType.Point) dstLight = new LightBuilder.Point(srcLight);
                if (srcLight.LightType == PunctualLightType.Spot) dstLight = new LightBuilder.Spot(srcLight);

                if (dstLight != null)
                {
                    dstLight.SetNameAndExtrasFrom(srcInstance);
                    dstScene.AddLight(dstLight, dstNode);
                }
            }
        }

        private static void _CopyToNodeBuilder(NodeBuilder dstNode, Node srcNode, IDictionary<Node, NodeBuilder> nodeMapping)
        {
            Guard.NotNull(srcNode, nameof(srcNode));
            Guard.NotNull(dstNode, nameof(dstNode));

            dstNode.SetNameAndExtrasFrom(srcNode);

            dstNode.LocalTransform = srcNode.LocalTransform;

            _CopyTransformAnimation(dstNode, srcNode);

            if (nodeMapping == null) return;

            nodeMapping[srcNode] = dstNode;

            foreach (var srcChild in srcNode.VisualChildren)
            {
                var dstChild = dstNode.CreateNode();
                _CopyToNodeBuilder(dstChild, srcChild, nodeMapping);
            }
        }

        private static void _CopyTransformAnimation(NodeBuilder dstNode, Node srcNode)
        {
            foreach (var anim in srcNode.LogicalParent.LogicalAnimations)
            {
                var name = anim.Name;
                if (string.IsNullOrWhiteSpace(name)) name = anim.LogicalIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);

                var curves = srcNode.GetCurveSamplers(anim);

                if (curves.Scale != null) dstNode.UseScale(name).SetCurve(curves.Scale);
                if (curves.Rotation != null) dstNode.UseRotation(name).SetCurve(curves.Rotation);
                if (curves.Translation != null) dstNode.UseTranslation(name).SetCurve(curves.Translation);
            }
        }

        private static void _CopyMorphingAnimation(InstanceBuilder dstInst, Node srcNode)
        {
            bool hasDefaultMorphing = false;

            var defMorphWeights = srcNode.GetMorphWeights();
            if (defMorphWeights != null && defMorphWeights.Count > 0)
            {
                dstInst.Content.UseMorphing().SetValue(defMorphWeights.ToArray());
                hasDefaultMorphing = true;
            }

            if (!hasDefaultMorphing) return;

            foreach (var anim in srcNode.LogicalParent.LogicalAnimations)
            {
                var name = anim.Name;
                if (string.IsNullOrWhiteSpace(name)) name = anim.LogicalIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);

                var curves = srcNode.GetCurveSamplers(anim);

                var srcMorphing = curves.GetMorphingSampler<ArraySegment<float>>();
                if (srcMorphing != null)
                {
                    var dstMorphing = dstInst.Content.UseMorphing(name);
                    dstMorphing.SetCurve(srcMorphing);

                    _VerifyCurveConversion(srcMorphing, dstMorphing, (a, b) => a.AsSpan().SequenceEqual(b));
                }
            }
        }

        #endregion

        #region utilities

        internal static void _VerifyCurveConversion<T>(IAnimationSampler<T> a, Animations.IConvertibleCurve<T> b, Func<T, T, bool> equalityComparer)
        {
            if (a.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE)
            {
                if (b.MaxDegree != 3) throw new ArgumentException("MaxDegree must be 3", nameof(b));

                var bb = b.ToSplineCurve();

                foreach (var (ak, av) in a.GetCubicKeys())
                {
                    if (!bb.TryGetValue(ak, out var bv)) throw new ArgumentException($"Missing key {ak}", nameof(b));

                    if (!equalityComparer(av.TangentIn, bv.TangentIn)) throw new ArgumentException("Conversion failed.", nameof(b));
                    if (!equalityComparer(av.Value, bv.Value)) throw new ArgumentException("Conversion failed.", nameof(b));
                    if (!equalityComparer(av.TangentOut, bv.TangentOut)) throw new ArgumentException("Conversion failed.", nameof(b));
                }
            }
            else if (a.InterpolationMode == AnimationInterpolationMode.LINEAR)
            {
                if (b.MaxDegree != 1) throw new ArgumentException(nameof(b.MaxDegree));

                var bb = b.ToLinearCurve();

                foreach (var (ak, av) in a.GetLinearKeys())
                {
                    if (!bb.TryGetValue(ak, out var bv)) throw new ArgumentException($"Missing key {ak}", nameof(b));
                    if (!equalityComparer(av, bv)) throw new ArgumentException("Conversion failed.", nameof(b));
                }
            }

            if (a.InterpolationMode == AnimationInterpolationMode.STEP)
            {
                if (b.MaxDegree != 0) throw new ArgumentException(nameof(b.MaxDegree));

                var bb = b.ToStepCurve();

                foreach (var (ak, av) in a.GetLinearKeys())
                {
                    if (!bb.TryGetValue(ak, out var bv)) throw new ArgumentException($"Missing key {ak}", nameof(b));
                    if (!equalityComparer(av, bv)) throw new ArgumentException("Conversion failed.", nameof(b));
                }
            }
        }

        internal void _VerifyConversion(Scene gltfScene)
        {
            // renderable instances

            var renderableInstCount = this.Instances
                .Select(item => item.Content.GetGeometryAsset())
                .Where(item => !Geometry.MeshBuilderToolkit.IsEmpty(item))
                .Count();

            // check if we have created the same amount of instances defined in the SceneBuilder.

            var renderableGltfCount = Node.Flatten(gltfScene)
                .Where(item => item.Mesh != null)
                .Sum(item => item.GetGpuInstancing()?.Count ?? 1);

            if (renderableInstCount != renderableGltfCount)
            {
                throw new InvalidOperationException($"Expected {this.Instances.Count}, but found {renderableGltfCount}");
            }

            // create a viewer to compare against

            var gltfViewOptions = new Runtime.RuntimeOptions();
            gltfViewOptions.IsolateMemory = false;
            gltfViewOptions.GpuMeshInstancing = Runtime.MeshInstancing.Enabled;

            var gltfView = Runtime.SceneTemplate
                .Create(gltfScene, gltfViewOptions)
                .CreateInstance();

            var renderableViewCount = gltfView.Sum(item => item.InstanceCount);

            if (renderableInstCount != renderableViewCount)
            {
                throw new InvalidOperationException($"Expected {this.Instances.Count}, but found {renderableViewCount}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper class to create a Schema2.Scene from one or multiple <see cref="SceneBuilder"/> instances.
    /// </summary>
    class Schema2SceneBuilder
    {
        #region data

        private readonly Dictionary<Materials.MaterialBuilder, Material> _Materials = new Dictionary<Materials.MaterialBuilder, Material>();

        private readonly Dictionary<MESHBUILDER, Mesh> _Meshes = new Dictionary<MESHBUILDER, Mesh>();

        private readonly Dictionary<NodeBuilder, Node> _Nodes = new Dictionary<NodeBuilder, Node>();

        #endregion

        #region settings
        public int GpuMeshInstancingMinCount { get; set; }

        #endregion

        #region API

        public Mesh GetMesh(MESHBUILDER key) { return key == null ? null : _Meshes.TryGetValue(key, out Mesh val) ? val : null; }

        public Node GetNode(NodeBuilder key) { return key == null ? null : _Nodes.TryGetValue(key, out Node val) ? val : null; }

        public static bool HasContent(Node node, bool checkTransform = true)
        {
            if (checkTransform && node.LocalMatrix != Matrix4x4.Identity) return true;

            if (node.VisualChildren.Any()) return true;

            if (node.Mesh != null) return true;
            if (node.Skin != null) return true;
            if (node.Camera != null) return true;
            if (node.PunctualLight != null) return true;
            if (node.GetGpuInstancing() != null) return true;

            return false;
        }

        public void AddGeometryResources(ModelRoot root, IEnumerable<SceneBuilder> srcScenes, SceneBuilderSchema2Settings settings)
        {
            // gather all unique MeshBuilders

            var srcMeshes = srcScenes
                .SelectMany(item => item.Instances)
                .Select(item => item.Content?.GetGeometryAsset())
                .Where(item => !Geometry.MeshBuilderToolkit.IsEmpty(item))
                .Distinct()
                .ToArray();

            // gather all unique MaterialBuilders

            var materialGroups = srcMeshes
                .SelectMany(item => item.Primitives)
                .Where(item => !Geometry.MeshBuilderToolkit.IsEmpty(item))
                .Select(item => item.Material)
                .Distinct()
                .ToList()
                // group by equal content, to reduce material splitting whenever possible.
                .GroupBy(item => item, Materials.MaterialBuilder.ContentComparer);

            // create a Schema2.Material for every MaterialBuilder.

            foreach (var mg in materialGroups)
            {
                var val = root.CreateMaterial(mg.Key);
                foreach (var key in mg) _Materials[key] = val;
            }

            // create a Schema2.Mesh for every MeshBuilder.

            var dstMeshes = root.CreateMeshes(mat => _Materials[mat], settings, srcMeshes);

            for (int i = 0; i < srcMeshes.Length; ++i)
            {
                _Meshes[srcMeshes[i]] = dstMeshes[i];
            }

            // TODO: here we could check that every dstMesh has been correctly created.
        }

        private void AddArmatureResources(IEnumerable<SceneBuilder> srcScenes, Func<Node> nodeFactory)
        {
            // ALIGNMENT ISSUE:
            // the toolkit builder is designed in a way that every instance can reuse the same node many times, even from different scenes.
            // so the only way to handle this is to forcefully recreate all the nodes on every scene.

            // gather all NodeBuilder unique armatures

            var armatures = srcScenes
                .SelectMany(item => item.Instances)
                .Select(item => item.Content?.GetArmatureRoot())
                .Where(item => item != null)
                .Select(item => item.Root)
                .Distinct()
                .ToList();

            // create Schema2.Node trees for every armature

            foreach (var armature in armatures)
            {
                CreateArmature(armature, nodeFactory);
            }
        }

        private void CreateArmature(NodeBuilder srcNode, Func<Node> nodeFactory)
        {
            var dstNode = nodeFactory();

            srcNode.TryCopyNameAndExtrasTo(dstNode);

            _Nodes[srcNode] = dstNode;

            if (srcNode.HasAnimations)
            {
                dstNode.LocalTransform = srcNode.LocalTransform.GetDecomposed();

                // Copies all the animations to the target node.
                if (srcNode.Scale != null) foreach (var t in srcNode.Scale.Tracks) dstNode.WithScaleAnimation(t.Key, t.Value);
                if (srcNode.Rotation != null) foreach (var t in srcNode.Rotation.Tracks) dstNode.WithRotationAnimation(t.Key, t.Value);
                if (srcNode.Translation != null) foreach (var t in srcNode.Translation.Tracks) dstNode.WithTranslationAnimation(t.Key, t.Value);
            }
            else
            {
                dstNode.LocalTransform = srcNode.LocalTransform;
            }

            foreach (var c in srcNode.VisualChildren) CreateArmature(c, () => dstNode.CreateNode());
        }

        public static void SetMorphAnimation(Node dstNode, Animations.AnimatableProperty<Transforms.SparseWeight8> animation)
        {
            Guard.NotNull(dstNode, nameof(dstNode));
            Guard.NotNull(dstNode.Mesh, nameof(dstNode.Mesh), "call after IOperator.ApplyTo");

            if (animation == null) return;

            var dstMesh = dstNode.Mesh;

            dstMesh.SetMorphWeights(animation.Value);

            foreach (var t in animation.Tracks) dstNode.WithMorphingAnimation(t.Key, t.Value);
        }

        public static void SetMorphAnimation(Node dstNode, Animations.AnimatableProperty<ArraySegment<float>> animation)
        {
            Guard.NotNull(dstNode, nameof(dstNode));
            Guard.NotNull(dstNode.Mesh, nameof(dstNode.Mesh), "call after IOperator.ApplyTo");

            if (animation == null) return;

            var dstMesh = dstNode.Mesh;

            dstMesh.SetMorphWeights(animation.Value);

            foreach (var t in animation.Tracks) dstNode.WithMorphingAnimation(t.Key, t.Value);
        }

        public void AddScene(Scene dstScene, SceneBuilder srcScene)
        {
            _Nodes.Clear();
            AddArmatureResources(new[] { srcScene }, () => dstScene.CreateNode());

            // gather single operators (RigidTransformer and SkinnedTransformer)

            var srcSingleOperators = srcScene
                .Instances
                .Select(item => item.Content)
                .Where(item => !Geometry.MeshBuilderToolkit.IsEmpty(item.GetGeometryAsset()))
                .OfType<IOperator<Scene>>();

            // gather multi operators (Fixed Transformer)

            var srcChildren = srcScene
                .Instances
                .Select(item => item.Content)
                .Where(item => !Geometry.MeshBuilderToolkit.IsEmpty(item.GetGeometryAsset()))
                .OfType<FixedTransformer>();

            var srcMultiOperators = _MeshInstancing.CreateFrom(srcChildren, this.GpuMeshInstancingMinCount);

            // apply operators

            var srcOperators = srcSingleOperators.Concat(srcMultiOperators);

            foreach (var op in srcOperators)
            {
                op.ApplyTo(dstScene, this);
            }

            #if DEBUG
            srcScene._VerifyConversion(dstScene);
            #endif
        }

        #endregion

        #region nested types

        /// <summary>
        /// Represents an object that can operate on a target object.
        /// </summary>
        /// <typeparam name="T">
        /// The target type.
        /// This is usually <see cref="Scene"/> or <see cref="Node"/>.
        /// </typeparam>
        public interface IOperator<T>
        {
            void ApplyTo(T target, Schema2SceneBuilder context);
        }

        #endregion
    }
}
