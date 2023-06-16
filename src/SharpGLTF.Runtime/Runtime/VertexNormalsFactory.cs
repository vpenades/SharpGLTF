using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Factory used to calculate the normals of a mesh which implements <see cref="IMeshPrimitive"/>
    /// </summary>
    static class VertexNormalsFactory
    {
        /// <summary>
        /// This interface must be implemented by a mesh, or a proxy of a mesh,
        /// in order to calculate its normals
        /// </summary>
        #pragma warning disable CA1034 // Nested types should not be visible
        public interface IMeshPrimitive
        #pragma warning restore CA1034 // Nested types should not be visible
        {
            /// <summary>
            /// gets the number of vertices.
            /// </summary>
            int VertexCount { get; }

            /// <summary>
            /// Gets the position of a given vertex.
            /// </summary>
            /// <param name="idx">The Vertex Index</param>
            /// <returns>The local vertex position.</returns>
            Vector3 GetVertexPosition(int idx);

            /// <summary>
            /// Sets the normal for the given vertex
            /// </summary>
            /// <param name="idx">The vertex index to set</param>
            /// <param name="normal">The normal that will be set to the vertex</param>
            void SetVertexNormal(int idx, Vector3 normal);

            /// <summary>
            /// Gets a sequence of triangles, where each triangle is defined by three vertex indices.
            /// </summary>
            /// <returns>A collection of triangles</returns>
            IEnumerable<(int A, int B, int C)> GetTriangleIndices();
        }

        public static void CalculateSmoothNormals<T>(IEnumerable<T> primitives)
            where T : IMeshPrimitive
        {
            Guard.NotNull(primitives, nameof(primitives));

            var normalMap = new Dictionary<Vector3, Vector3>();

            // calculate

            foreach (var primitive in primitives)
            {
                foreach (var (ta, tb, tc) in primitive.GetTriangleIndices())
                {
                    var p1 = primitive.GetVertexPosition(ta);
                    var p2 = primitive.GetVertexPosition(tb);
                    var p3 = primitive.GetVertexPosition(tc);

                    var d = Vector3.Cross(p2 - p1, p3 - p1);

                    _AddDirection(normalMap, p1, d);
                    _AddDirection(normalMap, p2, d);
                    _AddDirection(normalMap, p3, d);
                }
            }

            // normalize

            foreach (var pos in normalMap.Keys.ToList())
            {
                var nrm = Vector3.Normalize(normalMap[pos]);

                normalMap[pos] = nrm._IsFinite() && nrm.LengthSquared() > 0.5f ? nrm : Vector3.UnitZ;
            }

            // apply

            foreach (var primitive in primitives)
            {
                for (int i = 0; i < primitive.VertexCount; ++i)
                {
                    var pos = primitive.GetVertexPosition(i);

                    if (normalMap.TryGetValue(pos, out Vector3 nrm))
                    {
                        primitive.SetVertexNormal(i, nrm);
                    }
                    else
                    {
                        primitive.SetVertexNormal(i, Vector3.UnitZ);
                    }
                }
            }
        }

        private static void _AddDirection(Dictionary<Vector3, Vector3> dict, Vector3 pos, Vector3 dir)
        {
            if (!dir._IsFinite()) return;
            if (!dict.TryGetValue(pos, out Vector3 n)) n = Vector3.Zero;
            dict[pos] = n + dir;
        }
    }
}
