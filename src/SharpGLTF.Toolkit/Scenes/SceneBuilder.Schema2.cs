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

        public void AddScene(Scene dstScene, SceneBuilder srcScene)
        {
            // gather all MaterialBuilder unique instances

            var materialGroups = srcScene.Instances
                .Select(item => item.Content?.GetGeometryAsset())
                .Where(item => item != null)
                .SelectMany(item => item.Primitives)
                .Select(item => item.Material)
                .Where(item => item != null)
                .Distinct()
                .ToList()
                // group by equal content, to reduce material splitting whenever possible.
                .GroupBy(item => item, Materials.MaterialBuilder.ContentComparer);

            foreach (var mg in materialGroups)
            {
                var val = dstScene.LogicalParent.CreateMaterial(mg.Key);

                foreach (var key in mg)
                {
                    _Materials[key] = val;
                }
            }

            // gather all MeshBuilder unique instances
            // and group them by their vertex attribute layout.

            var meshGroups = srcScene.Instances
            .Select(item => item.Content?.GetGeometryAsset())
            .Where(item => item != null)
            .Distinct()
            .ToList()
            .GroupBy(item => item.GetType());

            // create Schema2.Mesh collections for every gathered group.

            foreach (var meshGroup in meshGroups)
            {
                var meshArray = meshGroup.ToArray();

                var meshDst = dstScene.LogicalParent.CreateMeshes(mat => _Materials[mat], meshArray);

                for (int i = 0; i < meshArray.Length; ++i)
                {
                    _Meshes[meshArray[i]] = meshDst[i];
                }
            }

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

            foreach (var c in srcNode.Children) CreateArmature(dstNode, c);
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
        /// <returns>A new <see cref="ModelRoot"/> instance.</returns>
        public ModelRoot ToSchema2()
        {
            var dstModel = ModelRoot.CreateModel();

            var dstScene = dstModel.UseScene(0);

            var context = new Schema2SceneBuilder();
            context.AddScene(dstScene, this);

            return dstModel;
        }

        #endregion

        #region from Schema2 to SceneBuilder

        internal static SceneBuilder CreateFrom(Scene srcScene)
        {
            if (srcScene == null) return null;

            var dstNodes = new Dictionary<Node, NodeBuilder>();

            foreach (var srcArmature in srcScene.VisualChildren)
            {
                var dstArmature = new NodeBuilder();
                CopyToNodeBuilder(srcArmature, dstArmature, dstNodes);
            }

            // TODO: we must also process the armatures of every skin, in case the joints are outside the scene.

            var dstScene = new SceneBuilder();

            // process meshes
            var srcMeshInstances = Node.Flatten(srcScene).Where(item => item.Mesh != null).ToList();
            _AddMeshInstances(dstScene, dstNodes, srcMeshInstances);

            // process cameras
            var srcCameraInstances = Node.Flatten(srcScene).Where(item => item.Camera != null).ToList();
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
                    dstScene.AddMesh(dstMesh, dstNode);
                }
                else
                {
                    var joints = new (NodeBuilder, Matrix4x4)[srcInstance.Skin.JointsCount];

                    for (int i = 0; i < joints.Length; ++i)
                    {
                        var j = srcInstance.Skin.GetJoint(i);
                        joints[i] = (dstNodes[j.Item1], j.Item2);
                    }

                    dstScene.AddSkinnedMesh(dstMesh, joints);
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

        private static void CopyToNodeBuilder(Node srcNode, NodeBuilder dstNode, IDictionary<Node, NodeBuilder> nodeMapping)
        {
            Guard.NotNull(srcNode, nameof(srcNode));
            Guard.NotNull(dstNode, nameof(dstNode));

            dstNode.Name = srcNode.Name;
            dstNode.LocalTransform = srcNode.LocalTransform;

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

            if (nodeMapping == null) return;

            nodeMapping[srcNode] = dstNode;

            foreach (var srcChild in srcNode.VisualChildren)
            {
                var dstChild = dstNode.CreateNode();
                CopyToNodeBuilder(srcChild, dstChild, nodeMapping);
            }
        }

        #endregion
    }
}
