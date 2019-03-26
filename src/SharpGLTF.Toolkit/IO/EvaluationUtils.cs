using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.IO
{
    using Schema2;

    using VERTEX = ValueTuple<Geometry.VertexTypes.VertexPositionNormal, Geometry.VertexTypes.VertexTexture1>;

    /// <summary>
    /// Utility class that hierarchicaly traverses a gltf model, evaluates the geometry and yields a collection of worldspace triangles.
    /// </summary>
    static class EvaluationUtils
    {
        /// <summary>
        /// Yields all the triangles in the model's default scene, in world space
        /// </summary>
        /// <param name="model">The input model</param>
        /// <returns>A collection of worldspace triangles</returns>
        public static IEnumerable<(VERTEX, VERTEX, VERTEX, Material)> Triangulate(this ModelRoot model)
        {
            return model.DefaultScene.Triangulate();
        }

        /// <summary>
        /// Yields all the triangles in the scene, in world space
        /// </summary>
        /// <param name="scene">The input scene</param>
        /// <returns>A collection of worldspace triangles</returns>
        public static IEnumerable<(VERTEX, VERTEX, VERTEX, Material)> Triangulate(this Scene scene)
        {
            return Node.Flatten(scene).SelectMany(item => item.Triangulate(true));
        }

        /// <summary>
        /// Yields all the triangles in the node
        /// </summary>
        /// <param name="node">The input node</param>
        /// <param name="inWorldSpace">true if we want to transform the local triangles to worldspace</param>
        /// <returns>A collection of triangles in the node's space, or in worldspace</returns>
        public static IEnumerable<(VERTEX, VERTEX, VERTEX, Material)> Triangulate(this Node node, bool inWorldSpace)
        {
            var mesh = node.Mesh;
            if (mesh == null) return Enumerable.Empty<(VERTEX, VERTEX, VERTEX, Material)>();

            var xform = inWorldSpace ? node.WorldMatrix : Matrix4x4.Identity;

            var normals = mesh.ComputeNormals();

            return mesh.Primitives.SelectMany(item => item.Triangulate(normals, xform));
        }

        /// <summary>
        /// Yields all the triangles in the mesh
        /// </summary>
        /// <param name="prim">The input primitive</param>
        /// <param name="normals">A dictionary mapping positions to normals</param>
        /// <param name="xform">The transform matrix</param>
        /// <returns>A collection of triangles transformed by <paramref name="xform"/> </returns>
        public static IEnumerable<(VERTEX, VERTEX, VERTEX, Material)> Triangulate(this MeshPrimitive prim, IReadOnlyDictionary<Vector3, Vector3> normals, Matrix4x4 xform)
        {
            var vertices = prim.GetVertexColumns();
            if (vertices.Normals == null) vertices.SetNormals(normals);

            var triangles = prim.GetTriangleIndices();

            foreach (var t in triangles)
            {
                var ap = vertices.GetPositionFragment<Geometry.VertexTypes.VertexPositionNormal>(t.Item1);
                var bp = vertices.GetPositionFragment<Geometry.VertexTypes.VertexPositionNormal>(t.Item2);
                var cp = vertices.GetPositionFragment<Geometry.VertexTypes.VertexPositionNormal>(t.Item3);

                ap.Transform(xform);
                bp.Transform(xform);
                cp.Transform(xform);

                var at = vertices.GetMaterialFragment<Geometry.VertexTypes.VertexTexture1>(t.Item1);
                var bt = vertices.GetMaterialFragment<Geometry.VertexTypes.VertexTexture1>(t.Item2);
                var ct = vertices.GetMaterialFragment<Geometry.VertexTypes.VertexTexture1>(t.Item3);

                yield return ((ap, at), (bp, bt), (cp, ct), prim.Material);
            }
        }
    }
}
