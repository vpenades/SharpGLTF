using System;
using System.Collections.Generic;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    public interface IPrimitiveReader<TMaterial>
    {
        /// <summary>
        /// Gets a generic type of <see cref="VertexBuilder{TvG, TvM, TvS}"/>.
        /// </summary>
        Type VertexType { get; }

        /// <summary>
        /// Gets the current <typeparamref name="TMaterial"/> instance used by this primitive.
        /// </summary>
        TMaterial Material { get; }

        /// <summary>
        /// Gets the number of vertices used by each primitive shape.
        /// Valid values:
        ///   1- Points.
        ///   2- Lines.
        ///   3- Triangles.
        /// </summary>
        int VerticesPerPrimitive { get; }

        /// <summary>
        /// Gets the list of <see cref="IVertexBuilder"/> vertices.
        /// </summary>
        IReadOnlyList<IVertexBuilder> Vertices { get; }

        /// <summary>
        /// Gets the list of <see cref="IPrimitiveMorphTargetReader"/>.
        /// </summary>
        IReadOnlyList<IPrimitiveMorphTargetReader> MorphTargets { get; }

        /// <summary>
        /// Gets the indices of all points.
        /// </summary>
        /// <exception cref="NotSupportedException">If <see cref="VerticesPerPrimitive"/> is different than 1</exception>
        IReadOnlyList<int> Points { get; }

        /// <summary>
        /// Gets the indices of all lines.
        /// </summary>
        /// <exception cref="NotSupportedException">If <see cref="VerticesPerPrimitive"/> is different than 2</exception>
        IReadOnlyList<(int A, int B)> Lines { get; }

        /// <summary>
        /// Gets the indices of all the surfaces as triangles.
        /// </summary>
        /// <exception cref="NotSupportedException">If <see cref="VerticesPerPrimitive"/> is different than 3</exception>
        IReadOnlyList<(int A, int B, int C)> Triangles { get; }

        /// <summary>
        /// Gets the indices of all the surfaces.
        /// </summary>
        /// <exception cref="NotSupportedException">If <see cref="VerticesPerPrimitive"/> is different than 3</exception>
        IReadOnlyList<(int A, int B, int C, int? D)> Surfaces { get; }

        /// <summary>
        /// Calculates the raw list of indices to use for this primitive.
        /// </summary>
        /// <returns>a list of indices.</returns>
        IReadOnlyList<int> GetIndices();
    }

    public interface IPrimitiveBuilder
    {
        /// <summary>
        /// Gets a generic type of <see cref="VertexBuilder{TvG, TvM, TvS}"/>.
        /// </summary>
        Type VertexType { get; }

        void SetVertexDelta(int morphTargetIndex, int vertexIndex, VertexGeometryDelta geometryDelta, VertexMaterialDelta materialDelta);

        int AddPoint(IVertexBuilder a);

        (int A, int B) AddLine(IVertexBuilder a, IVertexBuilder b);

        (int A, int B, int C) AddTriangle(IVertexBuilder a, IVertexBuilder b, IVertexBuilder c);

        (int A, int B, int C, int D) AddQuadrangle(IVertexBuilder a, IVertexBuilder b, IVertexBuilder c, IVertexBuilder d);
    }
}
