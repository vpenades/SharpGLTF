using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{

    static class PolygonToolkit
    {
        /// <summary>
        /// Checks if all the indices are valid
        /// </summary>
        /// <param name="inVertices">A list of <see cref="Vector3"/> position points.</param>
        /// <param name="indices">A list of vertex indices pointing to <paramref name="inVertices"/>.</param>
        public static void CheckIndices(this ReadOnlySpan<Vector3> inVertices, ReadOnlySpan<int> indices)
        {
            Guard.IsFalse(inVertices.IsEmpty, nameof(inVertices));
            Guard.IsFalse(indices.IsEmpty, nameof(indices));

            int a = 0;
            while (indices.Length > 1 && a < indices.Length)
            {
                // first vertex
                var aa = inVertices[indices[a]];

                // check invalid values
                Guard.IsTrue(aa._IsReal(), nameof(inVertices));

                // second vertex
                var b = a + 1; if (b >= indices.Length) b -= inVertices.Length;
                var bb = inVertices[indices[b]];

                // check collapsed points (would produce degenerated triangles)
                Guard.IsFalse(aa == bb, nameof(indices));

                // third vertex
                var c = b + 1; if (c >= indices.Length) c -= inVertices.Length;
                var cc = inVertices[indices[c]];

                // check collapsed segments (would produce degenerated triangles)
                Guard.IsFalse(aa == cc, nameof(indices));

                a++;
            }
        }

        /// <summary>
        /// Removes the invalid points of a polygon.
        /// </summary>
        /// <param name="inVertices">A list of <see cref="Vector3"/> position points.</param>
        /// <param name="indices">A list of vertex indices pointing to <paramref name="inVertices"/>.</param>
        public static void SanitizeIndices(this ReadOnlySpan<Vector3> inVertices, ref Span<int> indices)
        {
            Guard.IsFalse(inVertices.IsEmpty, nameof(inVertices));
            Guard.IsFalse(indices.IsEmpty, nameof(indices));

            int a = 0;
            while (indices.Length > 1 && a < indices.Length)
            {
                // first vertex
                var aa = inVertices[indices[a]];

                // remove invalid values
                if (!aa._IsReal())
                {
                    RemoveElementAt(ref indices, a);
                    continue;
                }

                // second vertex
                var b = a + 1; if (b >= indices.Length) b -= indices.Length;
                var bb = inVertices[indices[b]];

                // remove collapsed points (would produce degenerated triangles)
                if (aa == bb)
                {
                    RemoveElementAt(ref indices, a);
                    continue;
                }

                // third vertex
                var c = b + 1; if (c >= indices.Length) c -= indices.Length;
                var cc = inVertices[indices[c]];

                // remove collapsed segments (would produce degenerated triangles)
                if (aa == cc)
                {
                    RemoveElementAt(ref indices, b);
                    continue;
                }

                a++;
            }
        }

        internal static void RemoveElementAt<T>(ref Span<T> collection, int index)
        {
            // tri.Item2 is the index of the ear's triangle.
            // remove the ear from the array:
            for (int i = index + 1; i < collection.Length; ++i)
            {
                collection[i - 1] = collection[i];
            }

            collection = collection.Slice(0, collection.Length - 1);
        }
    }

    public interface IPolygonTriangulator
    {
        void Triangulate(Span<int> outIndices, ReadOnlySpan<Vector3> inVertices, ReadOnlySpan<int> inIndices);
    }

    /// <summary>
    /// Naive triangulator that assumes the polygon to be convex
    /// </summary>
    class NaivePolygonTriangulation : IPolygonTriangulator
    {
        static NaivePolygonTriangulation() { }

        private NaivePolygonTriangulation() { }

        private static readonly NaivePolygonTriangulation _Instance = new NaivePolygonTriangulation();

        public static IPolygonTriangulator Default => _Instance;

        public void Triangulate(Span<int> outIndices, ReadOnlySpan<Vector3> inVertices, ReadOnlySpan<int> inIndices)
        {
            int idx = 0;

            for (int i = 2; i < inIndices.Length; ++i)
            {
                var a = inIndices[idx++];
                var b = inIndices[idx++];
                var c = inIndices[idx++];

                outIndices[a] = 0;
                outIndices[b] = i - 1;
                outIndices[c] = i;
            }
        }
    }

    class BasicEarClippingTriangulation : IPolygonTriangulator
    {
        static BasicEarClippingTriangulation() { }

        private BasicEarClippingTriangulation() { }

        private static readonly BasicEarClippingTriangulation _Instance = new BasicEarClippingTriangulation();

        public static IPolygonTriangulator Default => _Instance;

        public void Triangulate(Span<int> outIndices, ReadOnlySpan<Vector3> inVertices, ReadOnlySpan<int> inIndices)
        {
            System.Diagnostics.Debug.Assert(outIndices.Length == (inVertices.Length - 2) * 3, $"{nameof(outIndices)} has invalid length");

            // define master direction
            var masterDirection = Vector3.Zero;
            for (int i = 2; i < inVertices.Length; ++i)
            {
                var ab = inVertices[i - 1] - inVertices[0];
                var ac = inVertices[i - 0] - inVertices[0];
                masterDirection += Vector3.Cross(ab, ac);
            }

            // setup source Indices
            Span<int> tmpIndices = stackalloc int[inIndices.Length];
            inIndices.CopyTo(tmpIndices);

            int triIdx = 0;

            // begin clipping ears
            while (true)
            {
                if (tmpIndices.Length < 3) break;

                PolygonToolkit.SanitizeIndices(inVertices, ref tmpIndices);

                if (tmpIndices.Length < 3) break;

                var tri = _FindEarTriangle(inVertices, tmpIndices, masterDirection);
                if (tri.Item1 < 0) throw new ArgumentException("failed to triangulate", nameof(inVertices));

                outIndices[triIdx++] = tmpIndices[tri.Item1];
                outIndices[triIdx++] = tmpIndices[tri.Item2];
                outIndices[triIdx++] = tmpIndices[tri.Item3];

                // tri.Item2 is the index of the ear's triangle.
                // remove the ear from the array:

                PolygonToolkit.RemoveElementAt(ref tmpIndices, tri.Item2);
            }

            if (tmpIndices.Length >= 3)
            {
                // add last triangle.
                outIndices[triIdx++] = tmpIndices[0];
                outIndices[triIdx++] = tmpIndices[1];
                outIndices[triIdx++] = tmpIndices[2];
            }

            System.Diagnostics.Debug.Assert(outIndices.Length == triIdx, $"{nameof(outIndices)} has invalid length");
        }

        private static (int, int, int) _FindEarTriangle(ReadOnlySpan<Vector3> inVertices, ReadOnlySpan<int> inIndices, Vector3 masterDirection)
        {
            for (int i = 0; i < inIndices.Length; ++i)
            {
                // define indices of the ear.
                var a = i + 0;
                var b = i + 1; if (b >= inIndices.Length) b -= inIndices.Length;
                var c = i + 2; if (c >= inIndices.Length) c -= inIndices.Length;

                // map to vertex indices
                var aa = inIndices[a];
                var bb = inIndices[b];
                var cc = inIndices[c];

                var aaa = inVertices[aa];
                var bbb = inVertices[bb];
                var ccc = inVertices[cc];

                var dir = Vector3.Cross(bbb - aaa, ccc - aaa);

                // determine the winding of the ear, and skip it if it's reversed.
                if (Vector3.Dot(masterDirection, dir) <= 0) continue;

                return (a, b, c);
            }

            return (-1, -1, -1);
        }
    }
}
