using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Globalization;

using SCHEMA2ACCESSOR = SharpGLTF.Schema2.Accessor;
using SCHEMA2PRIMITIVE = SharpGLTF.Schema2.MeshPrimitive;

using XY = System.Numerics.Vector2;
using XYZ = System.Numerics.Vector3;
using XYZW = System.Numerics.Vector4;

namespace SharpGLTF.Runtime
{
    sealed class _MeshDecoder<TMaterial> : IMeshDecoder<TMaterial>
        where TMaterial : class
    {
        #region lifecycle

        public _MeshDecoder(Schema2.Mesh srcMesh)
        {
            Guard.NotNull(srcMesh, nameof(srcMesh));

            _Name = srcMesh.Name;

            _LogicalIndex = srcMesh.LogicalIndex;

            _Primitives = srcMesh
                .Primitives
                .Select(item => new _MeshPrimitiveDecoder<TMaterial>(item))
                .ToArray();

            _Extras = srcMesh.Extras;
        }

        #endregion

        #region data

        private readonly string _Name;
        private readonly int _LogicalIndex;
        private readonly _MeshPrimitiveDecoder<TMaterial>[] _Primitives;

        private readonly Object _Extras;

        #endregion

        #region properties

        public string Name => _Name;
        public int LogicalIndex => _LogicalIndex;
        public IReadOnlyList<IMeshPrimitiveDecoder<TMaterial>> Primitives => _Primitives;

        #endregion

        #region API

        public void GenerateNormalsAndTangents()
        {
            if (_Primitives.Length == 0) return;

            var geometries = _Primitives.Select(item => item._Geometry);
            VertexNormalsFactory.CalculateSmoothNormals(geometries);
            VertexTangentsFactory.CalculateTangents(geometries);

            var morphTargetsCount = _Primitives.Min(item => item.MorphTargetsCount);

            for (int i = 0; i < morphTargetsCount; ++i)
            {
                var targets = _Primitives.Select(item => item._MorphTargets[i]);
                VertexNormalsFactory.CalculateSmoothNormals(targets);
                VertexTangentsFactory.CalculateTangents(targets);
            }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{_GetDebugString(),nq}")]
    sealed class _MeshPrimitiveDecoder<TMaterial>
        : _MeshPrimitiveDecoder
        , IMeshPrimitiveDecoder<TMaterial>
        where TMaterial : class
    {
        #region lifecycle

        internal _MeshPrimitiveDecoder(SCHEMA2PRIMITIVE srcPrim)
            : base(srcPrim)
        {
            _Material = srcPrim.Material as TMaterial;
        }

        #endregion

        #region data

        private readonly TMaterial _Material;

        #endregion

        #region properties

        public TMaterial Material => _Material;

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{_GetDebugString(),nq}")]
    class _MeshPrimitiveDecoder : IMeshPrimitiveDecoder
    {
        #region debug

        protected virtual string _GetDebugString()
        {
            var vCount = _Geometry.VertexCount;
            var tCount = TriangleIndices.Count();

            return $"Primitive Vertices:{vCount} Triangles:{tCount}";
        }

        #endregion

        #region lifecycle

        internal _MeshPrimitiveDecoder(SCHEMA2PRIMITIVE srcPrim)
        {
            _Extras = srcPrim.Extras;

            // indices (points, lines or triangles)

            _PrimitiveType = srcPrim.DrawPrimitiveType;
            _PrimitiveIndices = srcPrim.GetIndices() as IReadOnlyList<uint>;

            // base geometry (position + normal + tangent).
            _Geometry = new _MeshGeometryDecoder(this, srcPrim);

            // morph targets (positionDelta + normalDelta + tangentDelta)
            for (int i = 0; i < srcPrim.MorphTargetsCount; ++i)
            {
                var morphTarget = new _MorphTargetDecoder(_Geometry, srcPrim, i);
                _MorphTargets.Add(morphTarget);
            }

            // additional vertex attributes (color + UVs + Skinning)

            _Color0 = srcPrim.GetVertexAccessor("COLOR_0")?.AsColorArray() as IReadOnlyList<XYZW>;
            _TexCoord0 = srcPrim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array() as IReadOnlyList<XY>;
            _TexCoord1 = srcPrim.GetVertexAccessor("TEXCOORD_1")?.AsVector2Array() as IReadOnlyList<XY>;

            _Joints0 = srcPrim.GetVertexAccessor("JOINTS_0")?.AsVector4Array() as IReadOnlyList<XYZW>;
            _Joints1 = srcPrim.GetVertexAccessor("JOINTS_1")?.AsVector4Array() as IReadOnlyList<XYZW>;
            _Weights0 = srcPrim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array() as IReadOnlyList<XYZW>;
            _Weights1 = srcPrim.GetVertexAccessor("WEIGHTS_1")?.AsVector4Array() as IReadOnlyList<XYZW>;

            if (_Joints0 == null || _Weights0 == null) { _Joints0 = _Joints1 = _Weights0 = _Weights1 = null; }
            if (_Joints1 == null || _Weights1 == null) { _Joints1 = _Weights1 = null; }

            if (_Weights0 != null)
            {
                var wwww = _Weights0.ToArray(); // isolate memory to prevent overwriting source glTF.

                for (int i = 0; i < _Weights0.Count; ++i)
                {
                    var r = XYZW.Dot(_Weights0[i], XYZW.One);
                    wwww[i] /= r;
                }

                _Weights0 = wwww;
            }
        }

        #endregion

        #region data

        private readonly Schema2.PrimitiveType _PrimitiveType;
        private readonly IReadOnlyList<uint> _PrimitiveIndices;

        internal readonly _MeshGeometryDecoder _Geometry;

        internal readonly List<_MorphTargetDecoder> _MorphTargets = new List<_MorphTargetDecoder>();

        private readonly IReadOnlyList<XYZW> _Color0;

        private readonly IReadOnlyList<XY> _TexCoord0;
        private readonly IReadOnlyList<XY> _TexCoord1;

        private readonly IReadOnlyList<XYZW> _Joints0;
        private readonly IReadOnlyList<XYZW> _Joints1;

        private readonly IReadOnlyList<XYZW> _Weights0;
        private readonly IReadOnlyList<XYZW> _Weights1;

        private readonly Object _Extras;

        #endregion

        #region properties

        public int VertexCount => _Geometry.VertexCount;

        public int ColorsCount => _Color0 != null ? 1 : 0;

        public int TexCoordsCount => (_TexCoord0 != null ? 1 : 0) + (_TexCoord1 != null ? 1 : 0);

        public int JointsWeightsCount => (_Joints0 != null ? 4 : 0) + (_Joints1 != null ? 4 : 0);

        public int MorphTargetsCount => _MorphTargets.Count;

        public IEnumerable<(int A, int B, int C)> TriangleIndices
        {
            get
            {
                if (this._PrimitiveType.GetPrimitiveVertexSize() != 3) return Enumerable.Empty<(int, int, int)>();

                if (this._PrimitiveIndices == null) return this._PrimitiveType.GetTrianglesIndices(VertexCount);

                return this._PrimitiveType.GetTrianglesIndices(this._PrimitiveIndices);
            }
        }

        #endregion

        #region API

        public XYZ GetPosition(int vertexIndex) { return _Geometry.GetPosition(vertexIndex); }

        public IReadOnlyList<XYZ> GetPositionDeltas(int vertexIndex)
        {
            return MorphTargetsCount > 0
                ? (IReadOnlyList<XYZ>)new _MorphTargetPositionSlice(_MorphTargets, vertexIndex)
                : Array.Empty<XYZ>();
        }

        public XYZ GetNormal(int vertexIndex) { return _Geometry.GetNormal(vertexIndex); }

        public IReadOnlyList<XYZ> GetNormalDeltas(int vertexIndex)
        {
            return MorphTargetsCount > 0
                ? (IReadOnlyList<XYZ>)new _MorphTargetNormalSlice(_MorphTargets, vertexIndex)
                : Array.Empty<XYZ>();
        }

        public XYZW GetTangent(int vertexIndex) { return _Geometry.GetTangent(vertexIndex); }

        public IReadOnlyList<XYZ> GetTangentDeltas(int vertexIndex)
        {
            return MorphTargetsCount > 0
                ? (IReadOnlyList<XYZ>)new _MorphTargetTangentSlice(_MorphTargets, vertexIndex)
                : Array.Empty<XYZ>();
        }

        public XY GetTextureCoord(int vertexIndex, int set)
        {
            if (set == 0 && _TexCoord0 != null) return _TexCoord0[vertexIndex];
            if (set == 1 && _TexCoord1 != null) return _TexCoord1[vertexIndex];

            return XY.Zero;
        }

        public XYZW GetColor(int vertexIndex, int set)
        {
            if (set == 0 && _Color0 != null) return _Color0[vertexIndex];

            return XYZW.One;
        }

        public XYZW GetJoints(int vertexIndex)
        {
            if (_Joints0 != null) return _Joints0[vertexIndex];
            return XYZW.Zero;
        }

        public XYZW GetWeights(int vertexIndex)
        {
            if (_Weights0 != null) return _Weights0[vertexIndex];
            return XYZW.UnitX;
        }

        public Transforms.SparseWeight8 GetSkinWeights(int vertexIndex)
        {
            if (_Weights0 == null) return default;
            if (_Weights1 == null) return new Transforms.SparseWeight8(_Joints0[vertexIndex], _Weights0[vertexIndex]);
            return new Transforms.SparseWeight8(_Joints0[vertexIndex], _Joints1[vertexIndex], _Weights0[vertexIndex], _Weights1[vertexIndex]);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Vertices: {VertexCount}")]
    sealed class _MeshGeometryDecoder
        : VertexNormalsFactory.IMeshPrimitive
        , VertexTangentsFactory.IMeshPrimitive
    {
        #region  lifecycle

        public _MeshGeometryDecoder(_MeshPrimitiveDecoder owner, SCHEMA2PRIMITIVE srcPrim)
        {
            _Owner = owner;

            _Positions = srcPrim.GetVertexAccessor("POSITION")?.AsVector3Array() as IReadOnlyList<XYZ>;
            _Normals = srcPrim.GetVertexAccessor("NORMAL")?.AsVector3Array() as IReadOnlyList<XYZ>;
            _Tangents = srcPrim.GetVertexAccessor("TANGENT")?.AsVector4Array() as IReadOnlyList<XYZW>;
        }

        #endregion

        #region data

        private readonly _MeshPrimitiveDecoder _Owner;

        internal readonly IReadOnlyList<XYZ> _Positions;
        private IReadOnlyList<XYZ> _Normals;
        private IReadOnlyList<XYZW> _Tangents;

        private XYZ[] _GeneratedNormals;
        private XYZW[] _GeneratedTangents;

        #endregion

        #region properties

        public int VertexCount => _Positions?.Count ?? 0;

        #endregion

        #region API

        public XYZ GetPosition(int vertexIndex) { return _Positions[vertexIndex]; }

        public XYZ GetNormal(int vertexIndex) { return _Normals[vertexIndex]; }

        public XYZW GetTangent(int vertexIndex) { return _Tangents[vertexIndex]; }

        public XY GetTextureCoord(int vertexIndex, int set) { return _Owner.GetTextureCoord(vertexIndex, set); }

        #endregion

        #region Support methods for VertexNormalsFactory and VertexTangentsFactory

        IEnumerable<(int A, int B, int C)> VertexNormalsFactory.IMeshPrimitive.GetTriangleIndices() { return _Owner.TriangleIndices; }

        IEnumerable<(int A, int B, int C)> VertexTangentsFactory.IMeshPrimitive.GetTriangleIndices() { return _Owner.TriangleIndices; }

        XYZ VertexNormalsFactory.IMeshPrimitive.GetVertexPosition(int idx) { return GetPosition(idx); }
        XYZ VertexTangentsFactory.IMeshPrimitive.GetVertexPosition(int idx) { return GetPosition(idx); }
        XYZ VertexTangentsFactory.IMeshPrimitive.GetVertexNormal(int idx) { return GetNormal(idx); }
        XY VertexTangentsFactory.IMeshPrimitive.GetVertexTexCoord(int idx) { return GetTextureCoord(idx, 0); }

        void VertexNormalsFactory.IMeshPrimitive.SetVertexNormal(int idx, XYZ normal)
        {
            if (_Normals == null) _Normals = _GeneratedNormals = new XYZ[VertexCount];
            if (_GeneratedNormals != null) _GeneratedNormals[idx] = normal;
        }

        void VertexTangentsFactory.IMeshPrimitive.SetVertexTangent(int idx, XYZW tangent)
        {
            if (_Tangents == null) _Tangents = _GeneratedTangents = new XYZW[VertexCount];
            if (_GeneratedTangents != null) _GeneratedTangents[idx] = tangent;
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Vertices: {VertexCount}")]
    sealed class _MorphTargetDecoder
        : VertexNormalsFactory.IMeshPrimitive
        , VertexTangentsFactory.IMeshPrimitive
    {
        #region  lifecycle

        public _MorphTargetDecoder(_MeshGeometryDecoder geometry, SCHEMA2PRIMITIVE srcPrim, int morphTargetIndex)
        {
            _Geometry = geometry;

            // get morph deltas and apply them to our base geometry copy.

            var morphs = srcPrim.GetMorphTargetAccessors(morphTargetIndex);

            if (morphs.TryGetValue("POSITION", out SCHEMA2ACCESSOR pAccessor))
            {
                _PositionsDeltas = pAccessor.AsVector3Array() as IReadOnlyList<Vector3>;
            }

            if (morphs.TryGetValue("NORMAL", out SCHEMA2ACCESSOR nAccessor))
            {
                _NormalsDeltas = nAccessor.AsVector3Array() as IReadOnlyList<Vector3>;
            }

            if (morphs.TryGetValue("TANGENT", out SCHEMA2ACCESSOR tAccessor))
            {
                _TangentsDeltas = tAccessor.AsVector3Array() as IReadOnlyList<Vector3>;
            }
        }

        #endregion

        #region data

        private readonly _MeshGeometryDecoder _Geometry;

        internal readonly IReadOnlyList<XYZ> _PositionsDeltas;
        private IReadOnlyList<XYZ> _NormalsDeltas;
        private IReadOnlyList<XYZ> _TangentsDeltas;

        private XYZ[] _GeneratedNormals;
        private XYZ[] _GeneratedTangents;

        #endregion

        #region properties

        public int VertexCount => _PositionsDeltas?.Count ?? 0;

        #endregion

        #region API

        public XYZ GetPositionBase(int vertexIndex) { return _Geometry.GetPosition(vertexIndex); }

        public XYZ GetPositionDelta(int vertexIndex) { return _PositionsDeltas[vertexIndex]; }

        public XYZ GetNormalBase(int vertexIndex) { return _Geometry.GetNormal(vertexIndex); }

        public XYZ GetNormalDelta(int vertexIndex) { return _NormalsDeltas[vertexIndex]; }

        public XYZW GetTangentBase(int vertexIndex) { return _Geometry.GetTangent(vertexIndex); }

        public XYZ GetTangentDelta(int vertexIndex) { return _TangentsDeltas[vertexIndex]; }

        public XY GetTextureCoord(int vertexIndex, int set) { return _Geometry.GetTextureCoord(vertexIndex, set); }

        #endregion

        #region Support methods for VertexNormalsFactory and VertexTangentsFactory

        IEnumerable<(int A, int B, int C)> VertexNormalsFactory.IMeshPrimitive.GetTriangleIndices()
        {
            return ((VertexNormalsFactory.IMeshPrimitive)_Geometry).GetTriangleIndices();
        }

        IEnumerable<(int A, int B, int C)> VertexTangentsFactory.IMeshPrimitive.GetTriangleIndices()
        {
            return ((VertexTangentsFactory.IMeshPrimitive)_Geometry).GetTriangleIndices();
        }

        XYZ VertexNormalsFactory.IMeshPrimitive.GetVertexPosition(int idx) { return GetPositionBase(idx) + GetPositionDelta(idx); }
        XYZ VertexTangentsFactory.IMeshPrimitive.GetVertexPosition(int idx) { return GetPositionBase(idx) + GetPositionDelta(idx); }
        XYZ VertexTangentsFactory.IMeshPrimitive.GetVertexNormal(int idx) { return GetNormalBase(idx) + GetNormalDelta(idx); }
        XY VertexTangentsFactory.IMeshPrimitive.GetVertexTexCoord(int idx) { return GetTextureCoord(idx, 0); }

        void VertexNormalsFactory.IMeshPrimitive.SetVertexNormal(int idx, XYZ normal)
        {
            if (_NormalsDeltas == null) _NormalsDeltas = _GeneratedNormals = new XYZ[VertexCount];
            if (_GeneratedNormals == null) return;
            _GeneratedNormals[idx] = normal - GetNormalBase(idx);
        }

        void VertexTangentsFactory.IMeshPrimitive.SetVertexTangent(int idx, XYZW tangent)
        {
            if (_TangentsDeltas == null) _TangentsDeltas = _GeneratedTangents = new XYZ[VertexCount];
            if (_GeneratedTangents != null) return;
            var t = tangent - GetTangentBase(idx);
            _GeneratedTangents[idx] = new XYZ(t.X, t.Y, t.Z);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Vertex {_VertexIndex} Positions deltas")]
    readonly struct _MorphTargetPositionSlice : IReadOnlyList<XYZ>
    {
        public _MorphTargetPositionSlice(IReadOnlyList<_MorphTargetDecoder> ggg, int idx)
        {
            _Geometries = ggg;
            _VertexIndex = idx;
        }

        private readonly IReadOnlyList<_MorphTargetDecoder> _Geometries;
        private readonly int _VertexIndex;

        public XYZ this[int index] => _Geometries[index].GetPositionDelta(_VertexIndex);
        public int Count => _Geometries.Count;
        public IEnumerator<XYZ> GetEnumerator() { throw new NotImplementedException(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
    }

    [System.Diagnostics.DebuggerDisplay("Vertex {_VertexIndex} Normals deltas")]
    readonly struct _MorphTargetNormalSlice : IReadOnlyList<XYZ>
    {
        public _MorphTargetNormalSlice(IReadOnlyList<_MorphTargetDecoder> ggg, int idx)
        {
            _Geometries = ggg;
            _VertexIndex = idx;
        }

        private readonly IReadOnlyList<_MorphTargetDecoder> _Geometries;
        private readonly int _VertexIndex;

        public XYZ this[int index] => _Geometries[index].GetNormalDelta(_VertexIndex);
        public int Count => _Geometries.Count;
        public IEnumerator<XYZ> GetEnumerator() { throw new NotImplementedException(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
    }

    [System.Diagnostics.DebuggerDisplay("Vertex {_VertexIndex} Tangents deltas")]
    readonly struct _MorphTargetTangentSlice : IReadOnlyList<XYZ>
    {
        public _MorphTargetTangentSlice(IReadOnlyList<_MorphTargetDecoder> ggg, int idx)
        {
            _Geometries = ggg;
            _VertexIndex = idx;
        }

        private readonly IReadOnlyList<_MorphTargetDecoder> _Geometries;
        private readonly int _VertexIndex;

        public XYZ this[int index] => _Geometries[index].GetTangentDelta(_VertexIndex);
        public int Count => _Geometries.Count;
        public IEnumerator<XYZ> GetEnumerator() { throw new NotImplementedException(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
    }
}
