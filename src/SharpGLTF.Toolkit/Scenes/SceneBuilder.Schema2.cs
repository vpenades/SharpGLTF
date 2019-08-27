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
    /// Helper class to create a Schema2.Scene from a Scene Builder
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

        public void AddScene(Scene dstScene, SceneBuilder srcScene, bool useStridedBuffers = true)
        {
            // gather all unique MeshBuilders

            var srcMeshes = srcScene.Instances
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
                var val = dstScene.LogicalParent.CreateMaterial(mg.Key);
                foreach (var key in mg) _Materials[key] = val;
            }

            // create a Schema2.Mesh for every MeshBuilder.

            var dstMeshes = dstScene.LogicalParent.CreateMeshes(mat => _Materials[mat], useStridedBuffers,  srcMeshes);

            for (int i = 0; i < srcMeshes.Length; ++i)
            {
                _Meshes[srcMeshes[i]] = dstMeshes[i];
            }

            // TODO: here we could check that every dstMesh has been correctly created.

            // gather all NodeBuilder unique armatures

            var armatures = srcScene.Instances
                .Select(item => item.Content?.GetArmatureAsset())
                .Where(item => item != null)
                .Select(item => item.Root)
                .Distinct()
                .ToList();

            // create Schema2.Node trees for every armature

            foreach (var armature in armatures)
            {
                CreateArmature(dstScene,  armature);
            }

            // process instances

            var schema2Instances = srcScene
                .Instances
                .OfType<IOperator<Scene>>();

            foreach (var inst in schema2Instances)
            {
                inst.Setup(dstScene, this);
            }
        }

        /// <summary>
        /// Recursively converts all the <see cref="NodeBuilder"/> instances into <see cref="Schema2.Node"/> instances.
        /// </summary>
        /// <param name="container">The target <see cref="Schema2.Scene"/> or <see cref="Schema2.Node"/>.</param>
        /// <param name="srcNode">The source <see cref="NodeBuilder"/> instance.</param>
        private void CreateArmature(IVisualNodeContainer container, NodeBuilder srcNode)
        {
            var dstNode = container.CreateNode(srcNode.Name);
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

            foreach (var c in srcNode.VisualChildren) CreateArmature(dstNode, c);
        }

        public void SetMorphAnimation(Node dstNode, Animations.AnimatableProperty<Transforms.SparseWeight8> animation)
        {
            if (animation == null) return;

            var dstMesh = dstNode.Mesh;
            dstMesh.SetMorphWeights(default);

            foreach (var t in animation.Tracks) dstNode.WithMorphingAnimation(t.Key, t.Value);
        }

        #endregion

        #region types

        public interface IOperator<T>
        {
            void Setup(T dst, Schema2SceneBuilder context);
        }

        #endregion
    }

    public partial class SceneBuilder
    {
        #region from SceneBuilder to Schema2

        /// <summary>
        /// Converts this <see cref="SceneBuilder"/> instance into a <see cref="ModelRoot"/> instance.
        /// </summary>
        /// <param name="useStridedBuffers">True to generate strided vertex buffers whenever possible.</param>
        /// <returns>A new <see cref="ModelRoot"/> instance.</returns>
        public ModelRoot ToSchema2(bool useStridedBuffers = true)
        {
            var dstModel = ModelRoot.CreateModel();

            var dstScene = dstModel.UseScene(0);

            var context = new Schema2SceneBuilder();
            context.AddScene(dstScene, this, useStridedBuffers);

            return dstModel;
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
                CopyToNodeBuilder(dstArmature, srcArmature, dstNodes);
            }

            // TODO: we must also process the armatures of every skin, in case the joints are outside the scene.

            var dstScene = new SceneBuilder();

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
                    var dstInst = dstScene.AddMesh(dstMesh, dstNode);

                    CopyMorphingAnimation(dstInst, srcInstance);
                }
                else
                {
                    var joints = new (NodeBuilder, Matrix4x4)[srcInstance.Skin.JointsCount];

                    for (int i = 0; i < joints.Length; ++i)
                    {
                        var j = srcInstance.Skin.GetJoint(i);
                        joints[i] = (dstNodes[j.Item1], j.Item2);
                    }

                    var dstInst = dstScene.AddSkinnedMesh(dstMesh, joints);

                    CopyMorphingAnimation(dstInst, srcInstance);
                }
            }
        }

        private static void _AddCameraInstances(SceneBuilder dstScene, IReadOnlyDictionary<Node, NodeBuilder> dstNodes, IReadOnlyList<Node> srcInstances)
        {
            if (srcInstances.Count == 0) return;

            foreach (var srcInstance in srcInstances)
            {
                var dstNode = dstNodes[srcInstance];
                var cam = srcInstance.Camera;

                if (cam.Settings is CameraPerspective perspective)
                {
                    dstScene.AddPerspectiveCamera(dstNode, perspective.AspectRatio, perspective.VerticalFOV, perspective.ZNear, perspective.ZFar);
                }

                if (cam.Settings is CameraOrthographic orthographic)
                {
                    dstScene.AddOrthographicCamera(dstNode, orthographic.XMag, orthographic.YMag, orthographic.ZNear, orthographic.ZFar);
                }
            }
        }

        private static void CopyToNodeBuilder(NodeBuilder dstNode, Node srcNode, IDictionary<Node, NodeBuilder> nodeMapping)
        {
            Guard.NotNull(srcNode, nameof(srcNode));
            Guard.NotNull(dstNode, nameof(dstNode));

            dstNode.Name = srcNode.Name;
            dstNode.LocalTransform = srcNode.LocalTransform;

            CopyTransformAnimation(dstNode, srcNode);

            if (nodeMapping == null) return;

            nodeMapping[srcNode] = dstNode;

            foreach (var srcChild in srcNode.VisualChildren)
            {
                var dstChild = dstNode.CreateNode();
                CopyToNodeBuilder(dstChild, srcChild, nodeMapping);
            }
        }

        private static void CopyTransformAnimation(NodeBuilder dstNode, Node srcNode)
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

        private static void CopyMorphingAnimation(InstanceBuilder dstInst, Node srcNode)
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
