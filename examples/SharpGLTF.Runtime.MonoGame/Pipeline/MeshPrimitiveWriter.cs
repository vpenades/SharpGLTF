using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Runtime.Template;

namespace SharpGLTF.Runtime.Pipeline
{
    /// <summary>
    /// Writes the vertex and index buffer data to MonoGame vertex and index buffers.
    /// </summary>
    sealed class MeshPrimitiveWriter
    {
        #region data

        // shared buffers
        private readonly Dictionary<Type, IPrimitivesBuffers> _Buffers = new Dictionary<Type, IPrimitivesBuffers>();

        // primitives
        private readonly List<_MeshPrimitive> _MeshPrimitives = new List<_MeshPrimitive>();

        #endregion

        #region API

        public void WriteMeshPrimitive<TVertex>(int logicalMeshIndex, Effect effect, MeshPrimitiveReader primitive)
            where TVertex : unmanaged, IVertexType
        {
            if (!_Buffers.TryGetValue(typeof(TVertex), out IPrimitivesBuffers pb))
            {
                _Buffers[typeof(TVertex)] = pb = new _PrimitivesBuffers<TVertex>();
            }

            var part = (pb as _PrimitivesBuffers<TVertex>).Append(logicalMeshIndex, effect, primitive);

            _MeshPrimitives.Add(part);
        }

        internal IReadOnlyDictionary<int, RuntimeModelMesh> GetRuntimeMeshes(GraphicsDevice device, GraphicsResourceTracker disposables)
        {
            // create shared vertex/index buffers

            var vbuffers = _Buffers.Values.ToDictionary(key => key, val => val.CreateVertexBuffer(device));
            var ibuffers = _Buffers.Values.ToDictionary(key => key, val => val.CreateIndexBuffer(device));

            foreach (var vb in vbuffers.Values) disposables.AddDisposable(vb);
            foreach (var ib in ibuffers.Values) disposables.AddDisposable(ib);

            // create RuntimeModelMesh

            RuntimeModelMesh _convert(IEnumerable<_MeshPrimitive> srcParts)
            {
                var dstMesh = new RuntimeModelMesh(device);

                foreach (var srcPart in srcParts)
                {
                    var vb = vbuffers[srcPart.PrimitiveBuffers];
                    var ib = ibuffers[srcPart.PrimitiveBuffers];

                    var dstPart = dstMesh.CreateMeshPart();
                    dstPart.Effect = srcPart.PrimitiveEffect;
                    dstPart.SetVertexBuffer(vb, srcPart.VertexOffset, srcPart.VertexCount);
                    dstPart.SetIndexBuffer(ib, srcPart.TriangleOffset * 3, srcPart.TriangleCount);
                }

                return dstMesh;
            }

            return _MeshPrimitives
                .GroupBy(item => item.LogicalMeshIndex)
                .ToDictionary(k => k.Key, v => _convert(v));
        }

        #endregion

        #region nested types

        interface IPrimitivesBuffers
        {
            VertexBuffer CreateVertexBuffer(GraphicsDevice device);
            IndexBuffer CreateIndexBuffer(GraphicsDevice device);
        }

        /// <summary>
        /// Contains the shared vertex/index buffers of all the mesh primitive that share the same vertex type.
        /// </summary>
        /// <typeparam name="TVertex"></typeparam>
        sealed class _PrimitivesBuffers<TVertex> : IPrimitivesBuffers
            where TVertex : unmanaged, IVertexType
        {
            #region data

            private readonly List<TVertex> _Vertices = new List<TVertex>();
            private readonly List<(int, int, int)> _Triangles = new List<(int, int, int)>();

            #endregion

            #region API

            public _MeshPrimitive Append(int meshKey, Effect effect, MeshPrimitiveReader primitive)
            {
                var partVertices = primitive.ToXnaVertices<TVertex>();
                var partTriangles = primitive.TriangleIndices;

                var part = new _MeshPrimitive
                {
                    LogicalMeshIndex = meshKey,
                    PrimitiveEffect = effect,
                    PrimitiveBuffers = this,
                    VertexOffset = _Vertices.Count,
                    VertexCount = partVertices.Length,
                    TriangleOffset = _Triangles.Count,
                    TriangleCount = partTriangles.Length
                };

                _Vertices.AddRange(partVertices);
                _Triangles.AddRange(partTriangles);

                return part;
            }

            public VertexBuffer CreateVertexBuffer(GraphicsDevice device)
            {
                var data = new VertexBuffer(device, typeof(TVertex), _Vertices.Count, BufferUsage.None);
                data.SetData(_Vertices.ToArray());
                return data;
            }

            public IndexBuffer CreateIndexBuffer(GraphicsDevice device)
            {
                return CreateIndexBuffer(device, _Triangles);
            }

            private static IndexBuffer CreateIndexBuffer(GraphicsDevice device, IEnumerable<(int A, int B, int C)> triangles)
            {
                var sequence32 = triangles
                    .SelectMany(item => new[] { (UInt32)item.C, (UInt32)item.B, (UInt32)item.A })
                    .ToArray();

                var max = sequence32.Max();

                if (max > 65535)
                {
                    var indices = new IndexBuffer(device, typeof(UInt32), sequence32.Length, BufferUsage.None);

                    indices.SetData(sequence32);
                    return indices;
                }
                else
                {
                    var sequence16 = sequence32.Select(item => (UInt16)item).ToArray();

                    var indices = new IndexBuffer(device, typeof(UInt16), sequence16.Length, BufferUsage.None);

                    indices.SetData(sequence16);
                    return indices;
                }
            }

            #endregion
        }

        /// <summary>
        /// Represents a mesh primitive
        /// </summary>
        struct _MeshPrimitive
        {
            public int LogicalMeshIndex;
            public Effect PrimitiveEffect;
            public IPrimitivesBuffers PrimitiveBuffers;
            public int VertexOffset;
            public int VertexCount;
            public int TriangleOffset;
            public int TriangleCount;
        }

        #endregion
    }
}
