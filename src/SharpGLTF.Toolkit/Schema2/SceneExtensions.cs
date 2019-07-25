using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Schema2
{
    public static partial class Schema2Toolkit
    {
        #region fluent creation

        public static Node WithLocalTransform(this Node node, Transforms.AffineTransform xform)
        {
            Guard.NotNull(node, nameof(node));

            node.LocalTransform = xform;
            return node;
        }

        public static Node WithLocalTranslation(this Node node, Vector3 translation)
        {
            Guard.NotNull(node, nameof(node));

            var xform = node.LocalTransform;
            xform.Translation = translation;
            node.LocalTransform = xform;

            return node;
        }

        public static Node WithLocalRotation(this Node node, Quaternion rotation)
        {
            Guard.NotNull(node, nameof(node));

            var xform = node.LocalTransform;
            xform.Rotation = rotation;
            node.LocalTransform = xform;

            return node;
        }

        public static Node WithLocalScale(this Node node, Vector3 scale)
        {
            Guard.NotNull(node, nameof(node));

            var xform = node.LocalTransform;
            xform.Scale = scale;
            node.LocalTransform = xform;

            return node;
        }

        public static Node WithMesh(this Node node, Mesh mesh)
        {
            Guard.NotNull(node, nameof(node));

            node.Mesh = mesh;
            return node;
        }

        public static Node WithSkin(this Node node, Skin skin)
        {
            Guard.NotNull(node, nameof(node));

            node.Skin = skin;
            return node;
        }

        public static Node WithSkinBinding(this Node node, Matrix4x4 meshPoseTransform, params Node[] joints)
        {
            Guard.NotNull(node, nameof(node));

            var skin = node.LogicalParent.CreateSkin();
            skin.BindJoints(meshPoseTransform, joints);

            node.Skin = skin;
            return node;
        }

        public static Node WithSkinBinding(this Node node, params (Node, Matrix4x4)[] joints)
        {
            Guard.NotNull(node, nameof(node));

            var skin = node.LogicalParent.CreateSkin();
            skin.BindJoints(joints);

            node.Skin = skin;
            return node;
        }

        public static Node WithSkinnedMesh(this Node node, Mesh mesh, Matrix4x4 meshPoseTransform, params Node[] joints)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNull(mesh, nameof(mesh));
            Guard.NotNull(joints, nameof(joints));
            Guard.MustShareLogicalParent(node, mesh, nameof(mesh));

            foreach (var j in joints) Guard.MustShareLogicalParent(node, j, nameof(joints));

            // TODO: the joints must be visible in the visual tree that contains node.

            return node
                .WithMesh(mesh)
                .WithSkinBinding(meshPoseTransform, joints);
        }

        public static Node WithSkinnedMesh(this Node node, Mesh mesh, params (Node, Matrix4x4)[] joints)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNull(mesh, nameof(mesh));
            Guard.NotNull(joints, nameof(joints));
            Guard.MustShareLogicalParent(node, mesh, nameof(mesh));

            foreach (var j in joints) Guard.MustShareLogicalParent(node, j.Item1, nameof(joints));

            // TODO: the joints must be visible in the visual tree that contains node.

            return node
                .WithMesh(mesh)
                .WithSkinBinding(joints);
        }

        public static Node WithPerspectiveCamera(this Node node, float? aspectRatio, float fovy, float znear, float zfar = float.PositiveInfinity)
        {
            var camera = node.LogicalParent.CreateCamera();
            camera.SetPerspectiveMode(aspectRatio, fovy, znear, zfar);

            node.Camera = camera;

            return node;
        }

        public static Node WithOrthographicCamera(this Node node, float xmag, float ymag, float znear, float zfar)
        {
            var camera = node.LogicalParent.CreateCamera();
            camera.SetOrthographicMode(xmag, ymag, znear, zfar);

            node.Camera = camera;

            return node;
        }

        #endregion

        #region evaluation

        /// <summary>
        /// Finds a <see cref="Node"/> by name in the current graph.
        /// </summary>
        /// <param name="scene">This <see cref="Scene"/> instance.</param>
        /// <param name="predicate">A function to test each <see cref="Node"/> for a condition.</param>
        /// <returns>A <see cref="Node"/> instance, or Null.</returns>
        public static Node FindNode(this Scene scene, Predicate<Node> predicate)
        {
            Guard.NotNull(scene, nameof(scene));
            Guard.NotNull(predicate, nameof(predicate));

            return scene.VisualChildren.FirstOrDefault(n => predicate(n));
        }

        /// <summary>
        /// Finds a <see cref="Node"/> by name in the current graph.
        /// </summary>
        /// <param name="node">This <see cref="Node"/> instance.</param>
        /// <param name="predicate">A function to test each <see cref="Node"/> for a condition.</param>
        /// <returns>A <see cref="Node"/> instance, or Null.</returns>
        public static Node FindNode(this Node node, Predicate<Node> predicate)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNull(predicate, nameof(predicate));

            if (predicate(node)) return node;

            foreach (var child in node.VisualChildren)
            {
                var r = child.FindNode(predicate);
                if (r != null) return r;
            }

            return null;
        }

        /// <summary>
        /// Yields a collection of triangles representing the geometry in world space.
        /// </summary>
        /// <typeparam name="TvG">The vertex fragment type with Position, Normal and Tangent.</typeparam>
        /// <typeparam name="TvM">The vertex fragment type with Colors and Texture Coordinates.</typeparam>
        /// <param name="scene">A <see cref="Scene"/> instance.</param>
        /// <param name="animation">An <see cref="Animation"/> instance, or null.</param>
        /// <param name="time">The animation time.</param>
        /// <returns>A collection of triangles in world space.</returns>
        public static IEnumerable<(VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, Material)> EvaluateTriangles<TvG, TvM>(this Scene scene, Animation animation = null, float time = 0)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            return Node
                .Flatten(scene)
                .SelectMany(item => item.EvaluateTriangles<TvG, TvM>(animation, time));
        }

        /// <summary>
        /// Yields a collection of triangles representing the geometry in world space.
        /// </summary>
        /// <typeparam name="TvG">The vertex fragment type with Position, Normal and Tangent.</typeparam>
        /// <typeparam name="TvM">The vertex fragment type with Colors and Texture Coordinates.</typeparam>
        /// <param name="node">A <see cref="Node"/> instance.</param>
        /// <param name="animation">An <see cref="Animation"/> instance, or null.</param>
        /// <param name="time">The animation time.</param>
        /// <returns>A collection of triangles in world space.</returns>
        public static IEnumerable<(VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, Material)> EvaluateTriangles<TvG, TvM>(this Node node, Animation animation = null, float time = 0)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            var mesh = node?.Mesh;

            if (node == null || mesh == null) return Enumerable.Empty<(VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, Material)>();

            var xform = node.GetMeshWorldTransform(animation, time);

            return mesh.EvaluateTriangles<TvG, TvM>(xform);
        }

        public static Scenes.SceneBuilder ToSceneBuilder(this Scene srcScene)
        {
            if (srcScene == null) return null;

            var dstNodes = new Dictionary<Node, Scenes.NodeBuilder>();

            foreach (var srcArmature in srcScene.VisualChildren)
            {
                var dstArmature = new Scenes.NodeBuilder();
                srcArmature.CopyToNodeBuilder(dstArmature, dstNodes);
            }

            var srcInstances = Node.Flatten(srcScene).Where(item => item.Mesh != null).ToList();

            // TODO: we must process the armatures of the skin, in case the joints are outside the scene.

            var dstMeshes = srcInstances
                .Select(item => item.Mesh)
                .Distinct()
                .ToDictionary(item => item, item => item.ToMeshBuilder());

            var dstScene = new Scenes.SceneBuilder();

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
                    var joints = new (Scenes.NodeBuilder, Matrix4x4)[srcInstance.Skin.JointsCount];

                    for (int i = 0; i < joints.Length; ++i)
                    {
                        var j = srcInstance.Skin.GetJoint(i);
                        joints[i] = (dstNodes[j.Item1], j.Item2);
                    }

                    dstScene.AddSkinnedMesh(dstMesh, joints);
                }
            }

            return dstScene;
        }

        public static void CopyToNodeBuilder(this Node srcNode, Scenes.NodeBuilder dstNode,  IDictionary<Node, Scenes.NodeBuilder> nodeMapping)
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
                srcChild.CopyToNodeBuilder(dstChild, nodeMapping);
            }
        }

        #endregion
    }
}
