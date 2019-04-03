using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public static partial class Schema2Toolkit
    {
        #region fluent creation

        public static Node WithLocalTranslation(this Node node, Vector3 translation)
        {
            var xform = node.LocalTransform;
            xform.Translation = translation;
            node.LocalTransform = xform;

            return node;
        }

        public static Node WithLocalRotation(this Node node, Quaternion rotation)
        {
            var xform = node.LocalTransform;
            xform.Rotation = rotation;
            node.LocalTransform = xform;

            return node;
        }

        public static Node WithLocalScale(this Node node, Vector3 scale)
        {
            var xform = node.LocalTransform;
            xform.Scale = scale;
            node.LocalTransform = xform;

            return node;
        }

        public static Node WithMesh(this Node node, Mesh mesh)
        {
            node.Mesh = mesh;
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
        /// Yield a collection of triangles representing the geometry
        /// of the input <see cref="Scene"/> in world space.
        /// </summary>
        /// <typeparam name="TvP">The vertex fragment type with Position, Normal and Tangent.</typeparam>
        /// <typeparam name="TvM">The vertex fragment type with Colors and Texture Coordinates.</typeparam>
        /// <typeparam name="JvS">The vertex fragment type with Skin Joint Weights.</typeparam>
        /// <param name="scene">A <see cref="Scene"/> instance.</param>
        /// <returns>A collection of triangles in world space.</returns>
        public static IEnumerable<((TvP, TvM, JvS), (TvP, TvM, JvS), (TvP, TvM, JvS), Material)> Triangulate<TvP, TvM, JvS>(this Scene scene)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where JvS : struct, Geometry.VertexTypes.IVertexSkinning
        {
            return Node.Flatten(scene).SelectMany(item => item.Triangulate<TvP, TvM, JvS>(true));
        }

        /// <summary>
        /// Yield a collection of triangles representing the geometry
        /// of the input <see cref="Node"/> in local or world space.
        /// </summary>
        /// <typeparam name="TvP">The vertex fragment type with Position, Normal and Tangent.</typeparam>
        /// <typeparam name="TvM">The vertex fragment type with Colors and Texture Coordinates.</typeparam>
        /// <typeparam name="JvS">The vertex fragment type with Skin Joint Weights.</typeparam>
        /// <param name="node">A <see cref="Node"/> instance.</param>
        /// <param name="inWorldSpace">A value indicating whether the returned triangles must be in local (false) or world (true) space.</param>
        /// <returns>A collection of triangles in local or world space.</returns>
        public static IEnumerable<((TvP, TvM, JvS), (TvP, TvM, JvS), (TvP, TvM, JvS), Material)> Triangulate<TvP, TvM, JvS>(this Node node, bool inWorldSpace)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where JvS : struct, Geometry.VertexTypes.IVertexSkinning
        {
            var mesh = node.Mesh;
            if (mesh == null) return Enumerable.Empty<((TvP, TvM, JvS), (TvP, TvM, JvS), (TvP, TvM, JvS), Material)>();

            var xform = inWorldSpace ? node.WorldMatrix : Matrix4x4.Identity;

            return mesh.Triangulate<TvP, TvM, JvS>(xform);
        }

        #endregion
    }
}
