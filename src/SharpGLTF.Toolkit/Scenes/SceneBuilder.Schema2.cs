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
    /// Helper class to create a Schema2.Scene from one or multiple <see cref="SceneBuilder"/> instances.
    /// </summary>
    class Schema2SceneBuilder
    {
        #region data

        private readonly Dictionary<Materials.MaterialBuilder, Material> _Materials = new Dictionary<Materials.MaterialBuilder, Material>();

        private readonly Dictionary<MESHBUILDER, Mesh> _Meshes = new Dictionary<MESHBUILDER, Mesh>();

        private readonly Dictionary<NodeBuilder, Node> _Nodes = new Dictionary<NodeBuilder, Node>();

        #endregion

        #region API

        public Mesh GetMesh(MESHBUILDER key) { return key == null ? null : _Meshes.TryGetValue(key, out Mesh val) ? val : null; }

        public Node GetNode(NodeBuilder key) { return key == null ? null : _Nodes.TryGetValue(key, out Node val) ? val : null; }

        public void AddGeometryResources(ModelRoot root, IEnumerable<SceneBuilder> srcScenes, SceneBuilderSchema2Settings settings)
        {
            // gather all unique MeshBuilders

            var srcMeshes = srcScenes
                .SelectMany(item => item.Instances)
                .Select(item => item.Content?.GetGeometryAsset())
                .Where(item => item != null)
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

        private void AddArmatureResources(Func<string, Node> nodeFactory, IEnumerable<SceneBuilder> srcScenes)
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
                CreateArmature(nodeFactory, armature);
            }
        }

        private void CreateArmature(Func<string, Node> nodeFactory, NodeBuilder srcNode)
        {
            var dstNode = nodeFactory(srcNode.Name);
            _Nodes[srcNode] = dstNode;

            if (srcNode.HasAnimations)
            {
                dstNode.LocalTransform = srcNode.LocalTransform;

                // Copies all the animations to the target node.
                if (srcNode.Scale != null) foreach (var t in srcNode.Scale.Tracks) dstNode.WithScaleAnimation(t.Key, t.Value);
                if (srcNode.Rotation != null) foreach (var t in srcNode.Rotation.Tracks) dstNode.WithRotationAnimation(t.Key, t.Value);
                if (srcNode.Translation != null) foreach (var t in srcNode.Translation.Tracks) dstNode.WithTranslationAnimation(t.Key, t.Value);
            }
            else
            {
                dstNode.LocalMatrix = srcNode.LocalMatrix;
            }

            foreach (var c in srcNode.VisualChildren) CreateArmature(dstNode.CreateNode, c);
        }

        public static void SetMorphAnimation(Node dstNode, Animations.AnimatableProperty<Transforms.SparseWeight8> animation)
        {
            if (animation == null) return;

            var dstMesh = dstNode.Mesh;

            dstMesh.SetMorphWeights(animation.Value);

            foreach (var t in animation.Tracks) dstNode.WithMorphingAnimation(t.Key, t.Value);
        }

        public void AddScene(Scene dstScene, SceneBuilder srcScene)
        {
            _Nodes.Clear();
            AddArmatureResources(dstScene.CreateNode, new[] { srcScene });

            var schema2Instances = srcScene
                .Instances
                .OfType<IOperator<Scene>>();

            foreach (var inst in schema2Instances)
            {
                inst.Setup(dstScene, this);
            }
        }

        #endregion

        #region types

        public interface IOperator<T>
        {
            void Setup(T dst, Schema2SceneBuilder context);
        }

        #endregion
    }

    public struct SceneBuilderSchema2Settings
    {
        public static SceneBuilderSchema2Settings Default => new SceneBuilderSchema2Settings
        {
            UseStridedBuffers = true,
            CompactVertexWeights = false
        };

        public bool UseStridedBuffers;

        public bool CompactVertexWeights;
    }

    public partial class SceneBuilder : IConvertibleToGltf2
    {
        #region from SceneBuilder to Schema2

        /// <summary>
        /// Convertes a collection of <see cref="SceneBuilder"/> instances to a single <see cref="ModelRoot"/> instance.
        /// </summary>
        /// <param name="srcScenes">A collection of scenes</param>
        /// <param name="settings">Conversion settings.</param>
        /// <returns>A new <see cref="ModelRoot"/> instance.</returns>
        public static ModelRoot ToSchema2(IEnumerable<SceneBuilder> srcScenes, SceneBuilderSchema2Settings settings)
        {
            Guard.NotNull(srcScenes, nameof(srcScenes));

            var context = new Schema2SceneBuilder();

            var dstModel = ModelRoot.CreateModel();
            context.AddGeometryResources(dstModel, srcScenes, settings);

            foreach (var srcScene in srcScenes)
            {
                var dstScene = dstModel.UseScene(dstModel.LogicalScenes.Count);

                dstScene.Name = srcScene.Name;

                context.AddScene(dstScene, srcScene);
            }

            return dstModel;
        }

        /// <summary>
        /// Converts this <see cref="SceneBuilder"/> instance into a <see cref="ModelRoot"/> instance.
        /// </summary>
        /// <param name="settings">Conversion settings.</param>
        /// <returns>A new <see cref="ModelRoot"/> instance.</returns>
        public ModelRoot ToGltf2(SceneBuilderSchema2Settings settings)
        {
            var context = new Schema2SceneBuilder();

            var dstModel = ModelRoot.CreateModel();
            context.AddGeometryResources(dstModel, new[] { this }, settings);

            var dstScene = dstModel.UseScene(0);

            dstScene.Name = this.Name;

            context.AddScene(dstScene, this);

            dstModel.DefaultScene = dstScene;

            return dstModel;
        }

        public ModelRoot ToGltf2()
        {
            return ToGltf2(SceneBuilderSchema2Settings.Default);
        }

        #endregion

        #region from Schema2 to SceneBuilder

        internal static SceneBuilder CreateFrom(Scene srcScene)
        {
            if (srcScene == null) return null;

            // Process armatures

            var dstNodes = new Dictionary<Node, NodeBuilder>();

            foreach (var srcArmature in srcScene.VisualChildren)
            {
                var dstArmature = new NodeBuilder();
                _CopyToNodeBuilder(dstArmature, srcArmature, dstNodes);
            }

            // TODO: we must also process the armatures of every skin, in case the joints are outside the scene.

            var dstScene = new SceneBuilder();

            dstScene.Name = srcScene.Name;

            // process mesh instances
            var srcMeshInstances = Node.Flatten(srcScene)
                .Where(item => item.Mesh != null)
                .ToList();

            _AddMeshInstances(dstScene, dstNodes, srcMeshInstances);

            // process cameras
            var srcCameraInstances = Node.Flatten(srcScene)
                .Where(item => item.Camera != null)
                .ToList();

            _AddCameraInstances(dstScene, dstNodes, srcCameraInstances);

            var srcLightInstances = Node.Flatten(srcScene)
                .Where(item => item.PunctualLight != null)
                .ToList();

            _AddLightInstances(dstScene, dstNodes, srcCameraInstances);

            return dstScene;
        }

        private static void _AddMeshInstances(SceneBuilder dstScene, IReadOnlyDictionary<Node, NodeBuilder> dstNodes, IReadOnlyList<Node> srcInstances)
        {
            var dstMeshes = srcInstances
                            .Select(item => item.Mesh)
                            .Distinct()
                            .ToDictionary(item => item, item => item.ToMeshBuilder());

            foreach (var srcInstance in srcInstances)
            {
                var dstMesh = dstMeshes[srcInstance.Mesh];

                if (srcInstance.Skin == null)
                {
                    var dstNode = dstNodes[srcInstance];
                    var dstInst = dstScene.AddRigidMesh(dstMesh, dstNode);

                    _CopyMorphingAnimation(dstInst, srcInstance);
                }
                else
                {
                    var joints = new (NodeBuilder, Matrix4x4)[srcInstance.Skin.JointsCount];

                    for (int i = 0; i < joints.Length; ++i)
                    {
                        var (j, ibm) = srcInstance.Skin.GetJoint(i);
                        joints[i] = (dstNodes[j], ibm);
                    }

                    var dstInst = dstScene.AddSkinnedMesh(dstMesh, joints);

                    _CopyMorphingAnimation(dstInst, srcInstance);
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

                if (dstCam != null) dstScene.AddCamera(dstCam, dstNode);
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

                if (dstLight != null) dstScene.AddLight(dstLight, dstNode);
            }
        }

        private static void _CopyToNodeBuilder(NodeBuilder dstNode, Node srcNode, IDictionary<Node, NodeBuilder> nodeMapping)
        {
            Guard.NotNull(srcNode, nameof(srcNode));
            Guard.NotNull(dstNode, nameof(dstNode));

            dstNode.Name = srcNode.Name;
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

                var scaAnim = anim.FindScaleSampler(srcNode)?.CreateCurveSampler();
                if (scaAnim != null) dstNode.UseScale(name).SetCurve(scaAnim);

                var rotAnim = anim.FindRotationSampler(srcNode)?.CreateCurveSampler();
                if (rotAnim != null) dstNode.UseRotation(name).SetCurve(rotAnim);

                var traAnim = anim.FindTranslationSampler(srcNode)?.CreateCurveSampler();
                if (traAnim != null) dstNode.UseTranslation(name).SetCurve(traAnim);
            }
        }

        private static void _CopyMorphingAnimation(InstanceBuilder dstInst, Node srcNode)
        {
            foreach (var anim in srcNode.LogicalParent.LogicalAnimations)
            {
                var name = anim.Name;
                if (string.IsNullOrWhiteSpace(name)) name = anim.LogicalIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);

                var mrpAnim = anim.FindSparseMorphSampler(srcNode)?.CreateCurveSampler();
                if (mrpAnim != null) dstInst.Content.UseMorphing(name).SetCurve(mrpAnim);
            }
        }

        #endregion
    }
}
