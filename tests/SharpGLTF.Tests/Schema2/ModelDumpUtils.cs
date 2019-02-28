using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Utility class that hierarchicaly traverses a gltf model, evaluates the geometry and yields a collection of worldspace triangles.
    /// </summary>
    static class ModelDumpUtils
    {
        public static WavefrontWriter ToWavefrontWriter(this ModelRoot model)
        {
            var writer = new WavefrontWriter();

            foreach(var triangle in model.Triangulate())
            {
                writer.AddTriangle(triangle.Item1, triangle.Item2, triangle.Item3);
            }

            return writer;
        }

        /// <summary>
        /// Yields all the triangles in the model's default scene, in world space
        /// </summary>
        /// <param name="model"The input model</param>
        /// <returns>A collection of worldspace triangles</returns>
        public static IEnumerable<(Vector3,Vector3,Vector3)> Triangulate(this ModelRoot model)
        {
            return model.DefaultScene.Triangulate();
        }

        /// <summary>
        /// Yields all the triangles in the scene, in world space
        /// </summary>
        /// <param name="scene"The input scene</param>
        /// <returns>A collection of worldspace triangles</returns>
        public static IEnumerable<(Vector3, Vector3, Vector3)> Triangulate(this Scene scene)
        {
            return Node.Flatten(scene).SelectMany(item => item.Triangulate(true));
        }

        /// <summary>
        /// Yields all the triangles in the node
        /// </summary>
        /// <param name="node">The input node</param>
        /// <param name="inWorldSpace">true if we want to transform the local triangles to worldspace</param>
        /// <returns>A collection of triangles in the node's space, or in worldspace</returns>
        public static IEnumerable<(Vector3,Vector3,Vector3)> Triangulate(this Node node, bool inWorldSpace)
        {
            var mesh = node.Mesh;
            if (mesh == null) return Enumerable.Empty<(Vector3, Vector3, Vector3)>();

            var xform = inWorldSpace ? node.WorldMatrix : Matrix4x4.Identity;

            return mesh.Primitives.SelectMany(item => item.Triangulate(xform));
        }

        /// <summary>
        /// Yields all the triangles in the mesh
        /// </summary>
        /// <param name="prim">The input primitive</param>
        /// <param name="xform">The transform matrix</param>
        /// <returns>A collection of triangles transformed by <paramref name="xform"/> </returns>
        public static IEnumerable<(Vector3, Vector3, Vector3)> Triangulate(this MeshPrimitive prim, Matrix4x4 xform)
        {
            var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();

            var indices = prim.IndexAccessor != null
                ?
                prim.IndexAccessor.AsIndicesArray()
                :
                Memory.EncodedArrayUtils.IndicesRange(0,positions.Count);

            if (prim.DrawPrimitiveType != PrimitiveType.TRIANGLES) yield break;

            for(int i=0; i< indices.Count; i+=3)
            {
                var a = (int)indices[i + 0];
                var b = (int)indices[i + 1];
                var c = (int)indices[i + 2];

                var aa = Vector3.Transform(positions[a], xform);
                var bb = Vector3.Transform(positions[b], xform);
                var cc = Vector3.Transform(positions[c], xform);

                yield return (aa, bb, cc);
            }
        }
    }
}
