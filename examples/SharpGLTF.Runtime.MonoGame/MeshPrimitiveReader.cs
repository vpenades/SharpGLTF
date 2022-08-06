using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Schema2;

using XY = System.Numerics.Vector2;
using XYZ = System.Numerics.Vector3;
using XYZW = System.Numerics.Vector4;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Reads the content of a glTF <see cref="MeshPrimitive"/> object into a structure that's easier to consume by MonoGame.
    /// </summary>
    public sealed class MeshPrimitiveReader
        : VertexNormalsFactory.IMeshPrimitive
        , VertexTangentsFactory.IMeshPrimitive
    {
        #region lifecycle

        internal MeshPrimitiveReader(MeshPrimitive srcPrim, bool doubleSided)
        {
            _Positions = srcPrim.GetVertexAccessor("POSITION")?.AsVector3Array();
            _Normals = srcPrim.GetVertexAccessor("NORMAL")?.AsVector3Array();
            _Tangents = srcPrim.GetVertexAccessor("TANGENT")?.AsVector4Array();
            
            _Color0 = srcPrim.GetVertexAccessor("COLOR_0")?.AsColorArray();
            _TexCoord0 = srcPrim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();

            _Joints0 = srcPrim.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
            _Joints1 = srcPrim.GetVertexAccessor("JOINTS_1")?.AsVector4Array();
            _Weights0 = srcPrim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();
            _Weights1 = srcPrim.GetVertexAccessor("WEIGHTS_1")?.AsVector4Array();

            if (_Joints0 == null || _Weights0 == null) { _Joints0 = _Joints1 = _Weights0 = _Weights1 = null; }
            if (_Joints1 == null || _Weights1 == null) { _Joints1 = _Weights1 = null; }

            if (_Weights0 != null)
            {
                _Weights0 = _Weights0.ToArray(); // isolate memory to prevent overwriting source glTF.

                for (int i = 0; i < _Weights0.Count; ++i)
                {
                    var r = XYZW.Dot(_Weights0[i], XYZW.One);
                    _Weights0[i] /= r;
                }
            }

            _TrianglesSource = srcPrim.GetTriangleIndices().ToArray();

            if (doubleSided) // Monogame's effect material does not support double sided materials, so we simulate it by adding reverse faces
            {
                var back = _TrianglesSource.Select(item => (item.A, item.C, item.B));
                _Triangles = _TrianglesSource.Concat(back).ToArray();
            }
            else
            {
                _Triangles = _TrianglesSource;
            }            
        }

        #endregion

        #region data

        private readonly (int A, int B, int C)[] _TrianglesSource;

        private readonly (int A, int B, int C)[] _Triangles;

        private readonly IList<XYZ> _Positions;
        private IList<XYZ> _Normals;
        private IList<XYZW> _Tangents;

        private readonly IList<XYZW> _Color0;
        private readonly IList<XY> _TexCoord0;

        private readonly IList<XYZW> _Joints0;
        private readonly IList<XYZW> _Joints1;

        private readonly IList<XYZW> _Weights0;
        private readonly IList<XYZW> _Weights1;

        #endregion

        #region properties        

        public bool IsSkinned => _Joints0 != null;
        public int VertexCount => _Positions?.Count ?? 0;
        public (int A, int B, int C)[] TriangleIndices => _Triangles;        

        #endregion

        #region API

        public XYZ GetPosition(int idx) { return _Positions[idx]; }

        public XYZ GetNormal(int idx) { return _Normals[idx]; }

        public XYZW GetTangent(int idx) { return _Tangents[idx]; }

        public XY GetTextureCoord(int idx, int set)
        {
            if (set == 0 && _TexCoord0 != null) return _TexCoord0[idx];

            return XY.Zero;
        }

        public XYZW GetColor(int idx, int set)
        {
            if (set == 0 && _Color0 != null) return _Color0[idx];

            return XYZW.One;
        }

        public XYZW GetIndices(int idx)
        {
            if (_Joints0 != null) return _Joints0[idx];
            return XYZW.Zero;
        }

        public XYZW GetWeights(int idx)
        {
            if (_Weights0 != null) return _Weights0[idx];
            return XYZW.UnitX;
        }

        /// <summary>
        /// Gets the current Vertex attributes as an array of <see cref="{TVertex}"/> vertices.
        /// </summary>
        /// <typeparam name="TVertex">A Vertex type implementing <see cref="IVertexType"/>.</typeparam>
        /// <returns>A <see cref="{TVertex}"/> array</returns>
        public unsafe TVertex[] ToXnaVertices<TVertex>()
            where TVertex:unmanaged, IVertexType
        {
            var declaration = default(TVertex).VertexDeclaration;

            if (sizeof(TVertex) != declaration.VertexStride) throw new ArgumentException(nameof(TVertex));

            var dst = new TVertex[_Positions.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = _VertexWriter.CreateFromArray(dst, i);                

                foreach(var element in declaration.GetVertexElements())
                {
                    switch(element.VertexElementUsage)
                    {
                        case VertexElementUsage.Position: v.SetValue(element, GetPosition(i)); break;
                        case VertexElementUsage.Normal: v.SetValue(element, GetNormal(i)); break;
                        case VertexElementUsage.Tangent: v.SetValue(element, GetTangent(i), true); break;

                        case VertexElementUsage.TextureCoordinate: v.SetValue(element, GetTextureCoord(i,element.UsageIndex)); break;
                        case VertexElementUsage.Color: v.SetValue(element, GetColor(i, element.UsageIndex) , true); break;

                        case VertexElementUsage.BlendIndices: v.SetValue(element, GetIndices(i), false); break;
                        case VertexElementUsage.BlendWeight: v.SetValue(element, GetWeights(i), true); break;
                    }                            
                }                
            }

            return dst;
        }        

        #endregion

        #region nested types

        readonly ref struct _VertexWriter 
        {
            #region constructor
            public static _VertexWriter CreateFromArray<TVertex>(TVertex[] vvv, int idx)
                where TVertex : unmanaged, IVertexType
            {
                var v = vvv.AsSpan().Slice(idx, 1);

                var d = System.Runtime.InteropServices.MemoryMarshal.Cast<TVertex, Byte>(v);

                return new _VertexWriter(d);
            }

            public _VertexWriter(Span<Byte> vertex)
            {
                _Vertex = vertex;
            }

            #endregion

            #region data

            private readonly Span<Byte> _Vertex;

            #endregion

            #region API            

            public unsafe void SetValue(VertexElement element, XY value)
            {
                if (element.VertexElementFormat == VertexElementFormat.Vector2)
                {
                    var dst = _Vertex.Slice(element.Offset, sizeof(XY));
                    System.Runtime.InteropServices.MemoryMarshal.Write(dst, ref value);
                    return;
                }

                throw new NotImplementedException();
            }

            public unsafe void SetValue(VertexElement element, XYZ value)
            {
                if (element.VertexElementFormat == VertexElementFormat.Vector3)
                {
                    var dst = _Vertex.Slice(element.Offset, sizeof(XYZ));
                    System.Runtime.InteropServices.MemoryMarshal.Write(dst, ref value);
                    return;
                }

                throw new NotImplementedException();
            }

            public unsafe void SetValue(VertexElement element, XYZW value, bool valueIsUnitLength)
            {
                var dst = _Vertex.Slice(element.Offset);

                switch (element.VertexElementFormat)
                {
                    case VertexElementFormat.Vector4:                        
                        System.Runtime.InteropServices.MemoryMarshal.Write(dst, ref value);
                        return;

                    case VertexElementFormat.Byte4:
                        if (valueIsUnitLength)
                        {
                            SetValue(element, new Microsoft.Xna.Framework.Graphics.PackedVector.NormalizedByte4(value.ToXna()));
                        }
                        else
                        {
                            SetValue(element, new Microsoft.Xna.Framework.Graphics.PackedVector.Byte4(value.ToXna()));
                        }
                        
                        return;
                    
                    case VertexElementFormat.Short4:
                        SetValue(element, new Microsoft.Xna.Framework.Graphics.PackedVector.Short4(value.ToXna()));
                        return;

                    case VertexElementFormat.NormalizedShort4:
                        SetValue(element, new Microsoft.Xna.Framework.Graphics.PackedVector.NormalizedShort4(value.ToXna()));
                        return;
                }

                throw new NotImplementedException();
            }

            public unsafe void SetValue(VertexElement element, Microsoft.Xna.Framework.Graphics.PackedVector.Byte4 value)
            {
                if (element.VertexElementFormat != VertexElementFormat.Byte4) throw new ArgumentException(nameof(element));
                
                var dst = _Vertex.Slice(element.Offset, sizeof(Microsoft.Xna.Framework.Graphics.PackedVector.Byte4));
                System.Runtime.InteropServices.MemoryMarshal.Write(dst, ref value);                
            }

            public unsafe void SetValue(VertexElement element, Microsoft.Xna.Framework.Graphics.PackedVector.NormalizedByte4 value)
            {
                if (element.VertexElementFormat != VertexElementFormat.Byte4) throw new ArgumentException(nameof(element));

                var dst = _Vertex.Slice(element.Offset, sizeof(Microsoft.Xna.Framework.Graphics.PackedVector.Byte4));
                System.Runtime.InteropServices.MemoryMarshal.Write(dst, ref value);
            }

            public unsafe void SetValue(VertexElement element, Microsoft.Xna.Framework.Graphics.PackedVector.Short4 value)
            {
                if (element.VertexElementFormat != VertexElementFormat.Short4) throw new ArgumentException(nameof(element));

                var dst = _Vertex.Slice(element.Offset, sizeof(Microsoft.Xna.Framework.Graphics.PackedVector.Short4));
                System.Runtime.InteropServices.MemoryMarshal.Write(dst, ref value);
            }

            public unsafe void SetValue(VertexElement element, Microsoft.Xna.Framework.Graphics.PackedVector.NormalizedShort4 value)
            {
                if (element.VertexElementFormat != VertexElementFormat.NormalizedShort4) throw new ArgumentException(nameof(element));
                
                var dst = _Vertex.Slice(element.Offset, sizeof(Microsoft.Xna.Framework.Graphics.PackedVector.NormalizedShort4));
                System.Runtime.InteropServices.MemoryMarshal.Write(dst, ref value);                
            }

            #endregion
        }

        #endregion

        #region Support methods for VertexNormalsFactory and VertexTangentsFactory

        IEnumerable<(int A, int B, int C)> VertexNormalsFactory.IMeshPrimitive.GetTriangleIndices() { return _TrianglesSource; }

        IEnumerable<(int A, int B, int C)> VertexTangentsFactory.IMeshPrimitive.GetTriangleIndices() { return _TrianglesSource; }

        XYZ VertexNormalsFactory.IMeshPrimitive.GetVertexPosition(int idx) { return GetPosition(idx); }
        XYZ VertexTangentsFactory.IMeshPrimitive.GetVertexPosition(int idx) { return GetPosition(idx); }
        XYZ VertexTangentsFactory.IMeshPrimitive.GetVertexNormal(int idx) { return GetNormal(idx); }
        XY VertexTangentsFactory.IMeshPrimitive.GetVertexTexCoord(int idx) { return GetTextureCoord(idx, 0); }

        void VertexNormalsFactory.IMeshPrimitive.SetVertexNormal(int idx, XYZ normal)
        {
            if (_Normals == null) _Normals = new XYZ[VertexCount];
            if (!(_Normals is XYZ[])) return; // if it's not a plain array, it's a glTF source, so we prevent writing existing normals.            
            _Normals[idx] = normal;
        }

        void VertexTangentsFactory.IMeshPrimitive.SetVertexTangent(int idx, XYZW tangent)
        {
            if (_Tangents == null) _Tangents = new XYZW[VertexCount];
            if (!(_Tangents is XYZW[])) return; // if it's not a plain array, it's a glTF source, so we prevent writing existing tangents.            
            _Tangents[idx] = tangent;
        }        

        #endregion
    }

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

                foreach(var srcPart in srcParts)
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
            private readonly List<(int,int,int)> _Triangles = new List<(int, int, int)>();

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
