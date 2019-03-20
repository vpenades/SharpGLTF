using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGLTF.Geometry
{
    using Collections;

    public class SkinnedPrimitiveBuilder<TMaterial, TVertex, TSkin>
        where TVertex : struct, VertexTypes.IVertex
        where TSkin : struct, VertexTypes.IVertexJoints
    {
        #region lifecycle

        internal SkinnedPrimitiveBuilder(SkinnedMeshBuilder<TMaterial, TVertex, TSkin> mesh, TMaterial material)
        {
            this._Mesh = mesh;
            this._Material = material;
        }

        #endregion

        #region data

        private readonly SkinnedMeshBuilder<TMaterial, TVertex, TSkin> _Mesh;

        private readonly TMaterial _Material;

        private readonly VertexColumn<(TVertex, TSkin)> _Vertices = new VertexColumn<(TVertex, TSkin)>();
        private readonly List<int> _Indices = new List<int>();

        #endregion

        #region properties

        public SkinnedMeshBuilder<TMaterial, TVertex, TSkin> Mesh => _Mesh;

        public TMaterial Material => _Material;

        public IReadOnlyList<(TVertex, TSkin)> Vertices => _Vertices;

        public IReadOnlyList<int> Indices => _Indices;

        public IEnumerable<(int, int, int)> Triangles
        {
            get
            {
                for (int i = 2; i < _Indices.Count; i += 3)
                {
                    yield return (_Indices[i - 2], _Indices[i - 1], _Indices[i]);
                }
            }
        }

        #endregion

        #region API

        public void AddTriangle((TVertex, TSkin) a, (TVertex, TSkin) b, (TVertex, TSkin) c)
        {
            var aa = _Vertices.Use(a);
            var bb = _Vertices.Use(b);
            var cc = _Vertices.Use(c);

            // check for degenerated triangles:
            if (aa == bb) return;
            if (aa == cc) return;
            if (bb == cc) return;

            _Indices.Add(aa);
            _Indices.Add(bb);
            _Indices.Add(cc);
        }

        #endregion
    }

    public class SkinnedMeshBuilder<TMaterial, TVertex, TSkin>
        where TVertex : struct, VertexTypes.IVertex
        where TSkin : struct, VertexTypes.IVertexJoints
    {
        #region lifecycle

        public SkinnedMeshBuilder(string name = null)
        {
            this.Name = name;
        }

        #endregion

        #region data

        private readonly Dictionary<TMaterial, SkinnedPrimitiveBuilder<TMaterial, TVertex, TSkin>> _Primitives = new Dictionary<TMaterial, SkinnedPrimitiveBuilder<TMaterial, TVertex, TSkin>>();

        #endregion

        #region properties

        public string Name { get; set; }

        public IReadOnlyCollection<SkinnedPrimitiveBuilder<TMaterial, TVertex, TSkin>> Primitives => _Primitives.Values;

        #endregion

        #region API

        public void AddPolygon(TMaterial material, params (TVertex, TSkin)[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public void AddTriangle(TMaterial material, (TVertex, TSkin) a, (TVertex, TSkin) b, (TVertex, TSkin) c)
        {
            if (!_Primitives.TryGetValue(material, out SkinnedPrimitiveBuilder<TMaterial, TVertex, TSkin> primitive))
            {
                primitive = new SkinnedPrimitiveBuilder<TMaterial, TVertex, TSkin>(this, material);
                _Primitives[material] = primitive;
            }

            primitive.AddTriangle(a, b, c);
        }

        public IEnumerable<(int, int, int)> GetTriangles(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out SkinnedPrimitiveBuilder<TMaterial, TVertex, TSkin> primitive)) return primitive.Triangles;

            return Enumerable.Empty<(int, int, int)>();
        }

        public IReadOnlyList<int> GetIndices(TMaterial material)
        {
            if (_Primitives.TryGetValue(material, out SkinnedPrimitiveBuilder<TMaterial, TVertex, TSkin> primitive)) return primitive.Indices;

            return new int[0];
        }

        internal static IEnumerable<(TMaterial, MemoryAccessor[], MemoryAccessor)[]> MergeBuffers(IEnumerable<SkinnedMeshBuilder<TMaterial, TVertex, TSkin>> meshBuilders)
        {
            var vertexBlocks = VertexTypes.VertexUtils.CreateVertexMemoryAccessors
                (
                meshBuilders
                .SelectMany(item => item.Primitives)
                .Select(item => item.Vertices)
                ).ToList();

            var indexBlocks = VertexTypes.VertexUtils.CreateIndexMemoryAccessors
                (
                meshBuilders
                .SelectMany(item => item.Primitives)
                .Select(item => item.Indices)
                ).ToList();

            int bidx = 0;

            foreach (var meshBuilder in meshBuilders)
            {
                var dstMesh = new (TMaterial, MemoryAccessor[], MemoryAccessor)[meshBuilder.Primitives.Count];

                int pidx = 0;

                foreach (var primitiveBuilder in meshBuilder.Primitives)
                {
                    dstMesh[pidx] = (primitiveBuilder.Material, vertexBlocks[bidx], indexBlocks[bidx]);

                    ++pidx;
                    ++bidx;
                }

                yield return dstMesh;
            }
        }

        #endregion
    }
}
