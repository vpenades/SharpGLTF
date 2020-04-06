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

            foreach (var j in joints) Guard.MustShareLogicalParent(node, j, nameof(joints));

            var skin = node.LogicalParent.CreateSkin();
            skin.BindJoints(meshPoseTransform, joints);

            node.Skin = skin;
            return node;
        }

        public static Node WithSkinBinding(this Node node, params (Node Joint, Matrix4x4 InverseBindMatrix)[] joints)
        {
            Guard.NotNull(node, nameof(node));

            foreach (var j in joints)
            {
                Guard.MustShareLogicalParent(node, j.Joint, nameof(joints));
                Guard.IsTrue(Matrix4x4.Invert(j.InverseBindMatrix, out Matrix4x4 r), nameof(joints), "Invalid Matrix");
            }

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

        public static Node WithSkinnedMesh(this Node node, Mesh mesh, params (Node Joint, Matrix4x4 InverseBindMatrix)[] joints)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNull(mesh, nameof(mesh));
            Guard.NotNull(joints, nameof(joints));
            Guard.MustShareLogicalParent(node, mesh, nameof(mesh));

            foreach (var j in joints)
            {
                Guard.MustShareLogicalParent(node, j.Joint, nameof(joints));
                Guard.IsTrue(Matrix4x4.Invert(j.InverseBindMatrix, out Matrix4x4 r), nameof(joints), "Invalid Matrix");
            }

            // TODO: the joints must be visible in the visual tree that contains node.

            return node
                .WithMesh(mesh)
                .WithSkinBinding(joints);
        }

        public static Node WithPerspectiveCamera(this Node node, float? aspectRatio, float fovy, float znear, float zfar = float.PositiveInfinity)
        {
            Guard.NotNull(node, nameof(node));
            CameraPerspective.VerifyParameters(aspectRatio, fovy, znear, zfar);

            var camera = node.LogicalParent.CreateCamera();
            camera.SetPerspectiveMode(aspectRatio, fovy, znear, zfar);

            node.Camera = camera;

            return node;
        }

        public static Node WithOrthographicCamera(this Node node, float xmag, float ymag, float znear, float zfar)
        {
            Guard.NotNull(node, nameof(node));
            CameraOrthographic.VerifyParameters(xmag, ymag, znear, zfar);

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
        /// /// <param name="scene">A <see cref="Scene"/> instance.</param>
        /// <param name="animation">An <see cref="Animation"/> instance, or null.</param>
        /// <param name="time">The animation time.</param>
        /// <returns>A collection of triangles in world space.</returns>
        public static IEnumerable<(IVertexBuilder A, IVertexBuilder B, IVertexBuilder C, Material Material)> EvaluateTriangles(this Scene scene, Animation animation = null, float time = 0)
        {
            if (scene == null) return Enumerable.Empty<(IVertexBuilder, IVertexBuilder, IVertexBuilder, Material)>();

            var instance = Runtime.SceneTemplate
                .Create(scene, false)
                .CreateInstance();

            if (animation == null)
            {
                instance.SetPoseTransforms();
            }
            else
            {
                instance.SetAnimationFrame(animation.Name, time);
            }

            var meshes = scene.LogicalParent.LogicalMeshes;

            return instance
                .DrawableInstances
                .Where(item => item.Transform.Visible)
                .SelectMany(item => meshes[item.Template.LogicalMeshIndex].EvaluateTriangles(item.Transform));
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
        public static IEnumerable<(VertexBuilder<TvG, TvM, VertexEmpty> A, VertexBuilder<TvG, TvM, VertexEmpty> B, VertexBuilder<TvG, TvM, VertexEmpty> C, Material Material)> EvaluateTriangles<TvG, TvM>(this Scene scene, Animation animation = null, float time = 0)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            if (scene == null) return Enumerable.Empty<(VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, Material)>();

            var instance = Runtime.SceneTemplate
                .Create(scene, false)
                .CreateInstance();

            if (animation == null)
            {
                instance.SetPoseTransforms();
            }
            else
            {
                instance.SetAnimationFrame(animation.Name, time);
            }

            var meshes = scene.LogicalParent.LogicalMeshes;

            return instance
                .DrawableInstances
                .Where(item => item.Transform.Visible)
                .SelectMany(item => meshes[item.Template.LogicalMeshIndex].EvaluateTriangles<TvG, TvM, VertexEmpty>(item.Transform));
        }

        public static Scenes.SceneBuilder ToSceneBuilder(this Scene srcScene)
        {
            return Scenes.SceneBuilder.CreateFrom(srcScene);
        }

        #endregion
    }
}
