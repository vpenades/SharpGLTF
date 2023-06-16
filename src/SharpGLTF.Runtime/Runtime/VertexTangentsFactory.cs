using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Runtime
{
    using VERTEXKEY = System.ValueTuple<Vector3, Vector3, Vector2>;

    /// <summary>
    /// Factory used to calculate the tangents of a mesh which implements <see cref="IMeshPrimitive"/>
    /// </summary>
    static class VertexTangentsFactory
    {
        // https://gamedev.stackexchange.com/questions/128023/how-does-mikktspace-work-for-calculating-the-tangent-space-during-normal-mapping
        // https://stackoverflow.com/questions/25349350/calculating-per-vertex-tangents-for-glsl
        // https://github.com/buildaworldnet/IrrlichtBAW/wiki/How-to-Normal-Detail-Bump-Derivative-Map,-why-Mikkelsen-is-slightly-wrong-and-why-you-should-give-up-on-calculating-per-vertex-tangents
        // https://gamedev.stackexchange.com/questions/68612/how-to-compute-tangent-and-bitangent-vectors
        // https://www.marti.works/calculating-tangents-for-your-mesh/
        // https://www.html5gamedevs.com/topic/34364-gltf-support-and-mikkt-space/

        /// <summary>
        /// this interface must be defined by the input primitive to which we want to add tangents
        /// </summary>
        public interface IMeshPrimitive
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
            /// Gets the normal of a given vertex.
            /// </summary>
            /// <param name="idx">The Vertex Index</param>
            /// <returns>The local vertex normal.</returns>
            Vector3 GetVertexNormal(int idx);

            /// <summary>
            /// Gets the texture coordinate of a given vertex.
            /// </summary>
            /// <param name="idx">The Vertex Index</param>
            /// <returns>The local texture coordinate.</returns>
            Vector2 GetVertexTexCoord(int idx);

            /// <summary>
            /// Sets the tangent for the given vertex
            /// </summary>
            /// <param name="idx">The vertex index to set</param>
            /// <param name="tangent">The tangent that will be set to the vertex</param>
            void SetVertexTangent(int idx, Vector4 tangent);

            /// <summary>
            /// Gets a sequence of triangles, where each triangle is defined by three vertex indices.
            /// </summary>
            /// <returns>A collection of triangles</returns>
            IEnumerable<(int A, int B, int C)> GetTriangleIndices();
        }

        public static void CalculateTangents<T>(IEnumerable<T> primitives)
            where T : IMeshPrimitive
        {
            Guard.NotNull(primitives, nameof(primitives));

            var tangentsMap = new Dictionary<VERTEXKEY, (Vector3 u, Vector3 v)>();

            // calculate

            foreach (var primitive in primitives)
            {
                foreach (var (i1, i2, i3) in primitive.GetTriangleIndices())
                {
                    var p1 = primitive.GetVertexPosition(i1);
                    var p2 = primitive.GetVertexPosition(i2);
                    var p3 = primitive.GetVertexPosition(i3);

                    // check for degenerated triangle
                    if (p1 == p2 || p1 == p3 || p2 == p3) continue;

                    var uv1 = primitive.GetVertexTexCoord(i1);
                    var uv2 = primitive.GetVertexTexCoord(i2);
                    var uv3 = primitive.GetVertexTexCoord(i3);

                    // check for degenerated triangle
                    if (uv1 == uv2 || uv1 == uv3 || uv2 == uv3) continue;

                    var n1 = primitive.GetVertexNormal(i1);
                    var n2 = primitive.GetVertexNormal(i2);
                    var n3 = primitive.GetVertexNormal(i3);

                    // calculate tangents

                    var svec = p2 - p1;
                    var tvec = p3 - p1;

                    var stex = uv2 - uv1;
                    var ttex = uv3 - uv1;

                    float sx = stex.X;
                    float tx = ttex.X;
                    float sy = stex.Y;
                    float ty = ttex.Y;

                    var r = 1.0F / ((sx * ty) - (tx * sy));

                    if (!r._IsFinite()) continue;

                    var sdir = new Vector3((ty * svec.X) - (sy * tvec.X), (ty * svec.Y) - (sy * tvec.Y), (ty * svec.Z) - (sy * tvec.Z)) * r;
                    var tdir = new Vector3((sx * tvec.X) - (tx * svec.X), (sx * tvec.Y) - (tx * svec.Y), (sx * tvec.Z) - (tx * svec.Z)) * r;

                    if (!sdir._IsFinite()) continue;
                    if (!tdir._IsFinite()) continue;

                    // accumulate tangents

                    _AddTangent(tangentsMap, (p1, n1, uv1), (sdir, tdir));
                    _AddTangent(tangentsMap, (p2, n2, uv2), (sdir, tdir));
                    _AddTangent(tangentsMap, (p3, n3, uv3), (sdir, tdir));
                }
            }

            // normalize

            foreach (var key in tangentsMap.Keys.ToList())
            {
                var val = tangentsMap[key];

                // Gram-Schmidt orthogonalize
                val.u = Vector3.Normalize(val.u - (key.Item2 * Vector3.Dot(key.Item2, val.u)));
                val.v = Vector3.Normalize(val.v - (key.Item2 * Vector3.Dot(key.Item2, val.v)));

                tangentsMap[key] = val;
            }

            // apply

            foreach (var primitive in primitives)
            {
                for (int i = 0; i < primitive.VertexCount; ++i)
                {
                    var p = primitive.GetVertexPosition(i);
                    var n = primitive.GetVertexNormal(i);
                    var t = primitive.GetVertexTexCoord(i);

                    if (tangentsMap.TryGetValue((p, n, t), out (Vector3 u, Vector3 v) tangents))
                    {
                        var handedness = Vector3.Dot(Vector3.Cross(tangents.u, n), tangents.v) < 0 ? -1.0f : 1.0f;

                        primitive.SetVertexTangent(i, new Vector4(tangents.u, handedness));
                    }
                    else
                    {
                        primitive.SetVertexTangent(i, new Vector4(1, 0, 0, 1));
                    }
                }
            }
        }

        private static void _AddTangent(Dictionary<VERTEXKEY, (Vector3, Vector3)> dict, VERTEXKEY key, (Vector3 tu, Vector3 tv) alpha)
        {
            dict.TryGetValue(key, out (Vector3 tu, Vector3 tv) beta);

            dict[key] = (alpha.tu + beta.tu, alpha.tv + beta.tv);
        }
    }
}
