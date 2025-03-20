using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;

using MESHXFORM = SharpGLTF.Transforms.IGeometryTransform;


namespace SharpGLTF.Schema2
{
    public readonly struct EvaluatedTriangle<TvG, TvM, TvS>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region factory

        public static IEnumerable<EvaluatedTriangle<TvG, TvM, TvS>> GetTrianglesFromMesh(Mesh mesh, MESHXFORM xform = null)
        {
            if (xform != null && !xform.Visible) mesh = null;
            if (mesh == null) return Enumerable.Empty<EvaluatedTriangle<TvG, TvM, TvS>>();

            var primitives = _GatherMeshGeometry(mesh);

            return Transforms.InstancingTransform
                .Evaluate(xform)
                .SelectMany
                (
                    xinst => primitives.SelectMany
                    (
                        prim =>
                        {
                            var xvertices = xinst != null
                                ? prim.Vertices.WithTransform(xinst)
                                : prim.Vertices;

                            return _EvaluateTriangles(prim.Material, xvertices, prim.Triangles);
                        }

                    )
                );
        }

        private static List<(Material Material, VertexBufferColumns Vertices, IEnumerable<(int, int, int)> Triangles)> _GatherMeshGeometry(Mesh mesh)
        {
            var primitives = mesh.Primitives
                            .Where(prim => prim.GetTriangleIndices().Any())
                            .Select(prim => (prim.Material, prim.GetVertexColumns(), (IEnumerable<(int, int, int)>)prim.GetTriangleIndices().ToList()))
                            .ToList();

            bool needsNormals = default(TvG).TryGetNormal(out Vector3 nrm);
            bool needsTangents = default(TvG).TryGetTangent(out Vector4 tgt);

            if (needsNormals)
            {
                var prims = primitives
                    .Where(p => p.Item2.Normals == null)
                    .Select(p => (p.Item2, p.Item3))
                    .ToList();

                if (prims.Count > 0) VertexBufferColumns.CalculateSmoothNormals(prims);
            }

            if (needsTangents)
            {
                var prims = primitives
                    .Where(p => p.Item2.Tangents == null && p.Item2.TexCoords0 != null)
                    .Select(p => (p.Item2, p.Item3))
                    .ToList();

                if (prims.Count > 0) VertexBufferColumns.CalculateTangents(prims);
            }

            return primitives;
        }

        private static IEnumerable<EvaluatedTriangle<TvG, TvM, TvS>> _EvaluateTriangles(Material material, VertexBufferColumns vertices, IEnumerable<(int A, int B, int C)> indices)            
        {
            foreach (var (ta, tb, tc) in indices)
            {
                var va = vertices.GetVertex<TvG, TvM, TvS>(ta);
                var vb = vertices.GetVertex<TvG, TvM, TvS>(tb);
                var vc = vertices.GetVertex<TvG, TvM, TvS>(tc);

                yield return (va, vb, vc, material);
            }
        }

        #endregion

        #region constructor

        public static implicit operator EvaluatedTriangle<TvG, TvM, TvS>((VertexBuilder<TvG, TvM, TvS> A, VertexBuilder<TvG, TvM, TvS> B, VertexBuilder<TvG, TvM, TvS> C, Material Material) tri)
        {
            return new Schema2.EvaluatedTriangle<TvG, TvM, TvS>(tri.A, tri.B, tri.C, tri.Material);
        }

        public EvaluatedTriangle(VertexBuilder<TvG, TvM, TvS> a, VertexBuilder<TvG, TvM, TvS> b, VertexBuilder<TvG, TvM, TvS> c, Material m)
        {
            A = a;
            B = b;
            C = c;
            Material = m;
        }

        #endregion

        #region data

        public readonly VertexBuilder<TvG, TvM, TvS> A;
        public readonly VertexBuilder<TvG, TvM, TvS> B;
        public readonly VertexBuilder<TvG, TvM, TvS> C;

        public readonly Material Material;

        #endregion

        #region API

        /// <summary>
        /// Materials may include a <see cref="TextureTransform"/> which affects the texture coordinates.
        /// </summary>
        /// <param name="triangles">the triangles to be updated.</param>
        /// <returns>A new collection fo triangles with transformed uv coordinates (if required)</returns>
        public static IEnumerable<EvaluatedTriangle<TvG, TvM, TvS>> TransformTextureCoordsByMaterial(IEnumerable<EvaluatedTriangle<TvG, TvM, TvS>> triangles, Animation track = null, float time = -1)
        {
            // cache transform for speed up
            var diffuseTextureXformDict = new Dictionary<Material, Matrix3x2>();

            EvaluatedTriangle<TvG, TvM, TvS> _getTransformedTriangle(EvaluatedTriangle<TvG, TvM, TvS> triangle)
            {
                if (triangle.Material == null) return triangle;

                if (!diffuseTextureXformDict.TryGetValue(triangle.Material, out var textureXform))
                {
                    textureXform = triangle.Material.GetDiffuseTextureMatrix(track, time) ?? Matrix3x2.Identity;

                    diffuseTextureXformDict[triangle.Material] = textureXform;
                }

                if (textureXform.IsIdentity) return triangle;

                return triangle._TransformTextureBy(textureXform);
            }

            return triangles.Select(tri => _getTransformedTriangle(tri));
        }

        private EvaluatedTriangle<TvG, TvM, TvS> _TransformTextureBy(in Matrix3x2 xform)
        {
            var a = this.A;
            var b = this.B;
            var c = this.C;            

            a.Material.SetTexCoord(0, Vector2.Transform(a.Material.GetTexCoord(0), xform));
            b.Material.SetTexCoord(0, Vector2.Transform(b.Material.GetTexCoord(0), xform));
            c.Material.SetTexCoord(0, Vector2.Transform(c.Material.GetTexCoord(0), xform));

            return (a, b, c, Material);
        }

        #endregion
    }
}
