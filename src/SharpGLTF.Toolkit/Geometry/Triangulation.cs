using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    public interface IPolygonTriangulator
    {
        void Triangulate(Span<int> outIndices, ReadOnlySpan<Vector3> inVertices);
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

        public void Triangulate(Span<int> outIndices, ReadOnlySpan<Vector3> inVertices)
        {
            int idx = 0;

            for (int i = 2; i < inVertices.Length; ++i)
            {
                outIndices[idx++] = 0;
                outIndices[idx++] = i - 1;
                outIndices[idx++] = i;
            }
        }
    }
}
