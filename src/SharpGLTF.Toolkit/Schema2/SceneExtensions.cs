using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public static partial class Toolkit
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

        public static IEnumerable<((TvP, TvM, TvJ), (TvP, TvM, TvJ), (TvP, TvM, TvJ), Material)> Triangulate<TvP, TvM, TvJ>(this Scene scene)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvJ : struct, Geometry.VertexTypes.IVertexJoints
        {
            return Node.Flatten(scene).SelectMany(item => item.Triangulate<TvP, TvM, TvJ>(true));
        }

        public static IEnumerable<((TvP, TvM, TvJ), (TvP, TvM, TvJ), (TvP, TvM, TvJ), Material)> Triangulate<TvP, TvM, TvJ>(this Node node, bool inWorldSpace)
            where TvP : struct, Geometry.VertexTypes.IVertexPosition
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvJ : struct, Geometry.VertexTypes.IVertexJoints
        {
            var mesh = node.Mesh;
            if (mesh == null) return Enumerable.Empty<((TvP, TvM, TvJ), (TvP, TvM, TvJ), (TvP, TvM, TvJ), Material)>();

            var xform = inWorldSpace ? node.WorldMatrix : Matrix4x4.Identity;

            return mesh.Triangulate<TvP, TvM, TvJ>(xform);
        }

        #endregion
    }
}
