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

        public _MeshDecoder(Schema2.Mesh srcMesh, RuntimeOptions options)
        {
            Guard.NotNull(srcMesh, nameof(srcMesh));

            _Name = srcMesh.Name;
            _Extras = RuntimeOptions.ConvertExtras(srcMesh, options);

            _LogicalIndex = srcMesh.LogicalIndex;

            _Primitives = srcMesh
                .Primitives
                .Select(item => new _MeshPrimitiveDecoder<TMaterial>(item))
                .ToArray();
        }

        #endregion

        #region data

        private readonly string _Name;
        private readonly Object _Extras;

        private readonly int _LogicalIndex;
        private readonly _MeshPrimitiveDecoder<TMaterial>[] _Primitives;

        #endregion

        #region properties

        public string Name => _Name;
        public Object Extras => _Extras;
        public int LogicalIndex => _LogicalIndex;
        public IReadOnlyList<IMeshPrimitiveDecoder<TMaterial>> Primitives => _Primitives;

        #endregion

        #region API

        public void GenerateNormalsAndTangents()
        {
            if (_Primitives.Length == 0) return;

            var geometries = _Primitives.Select(item => item._Geometry);

            // we can only skip normals and tangents calculation if all the
            // primitives have them. If we get a case in wich some primitives
            // have normals and other not, we need to calculate the normals
            // for all of them because there's vertex adyacencies shared
            // between primitives.

            var hasNormals = geometries.All(item => item.HasNormals);
            var hasTangents = geometries.All(item => item.HasTangents);

            if (!hasNormals) VertexNormalsFactory.CalculateSmoothNormals(geometries);
            if (!hasTangents) VertexTangentsFactory.CalculateTangents(geometries);

            var morphTargetsCount = _Primitives.Min(item => item.MorphTargetsCount);

            for (int i = 0; i < morphTargetsCount; ++i)
            {
                var targets = _Primitives.Select(item => item._MorphTargets[i]);

                hasNormals = targets.All(item => item.HasNormals);
                hasTangents = targets.All(item => item.HasTangents);

                if (!hasNormals) VertexNormalsFactory.CalculateSmoothNormals(targets);
                if (!hasTangents) VertexTangentsFactory.CalculateTangents(targets);
            }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{_GetDebugString(),nq}")]
    sealed class _MeshPrimitiveDecoder<TMaterial> :
        _MeshPrimitiveDecoder,
        IMeshPrimitiveDecoder<TMaterial>
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
            _Color1 = srcPrim.GetVertexAccessor("COLOR_1")?.AsColorArray() as IReadOnlyList<XYZW>;
            _ColorsCount = (_Color0 != null ? 1 : 0) + (_Color1 != null ? 1 : 0);

            _TexCoord0 = srcPrim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array() as IReadOnlyList<XY>;
            _TexCoord1 = srcPrim.GetVertexAccessor("TEXCOORD_1")?.AsVector2Array() as IReadOnlyList<XY>;
            _TexCoord2 = srcPrim.GetVertexAccessor("TEXCOORD_2")?.AsVector2Array() as IReadOnlyList<XY>;
            _TexCoord3 = srcPrim.GetVertexAccessor("TEXCOORD_3")?.AsVector2Array() as IReadOnlyList<XY>;
            _TexCoordCount  = (_TexCoord0 != null ? 1 : 0)
                            + (_TexCoord1 != null ? 1 : 0)
                            + (_TexCoord2 != null ? 1 : 0)
                            + (_TexCoord3 != null ? 1 : 0);

            _Joints0 = srcPrim.GetVertexAccessor("JOINTS_0")?.AsVector4Array() as IReadOnlyList<XYZW>;
            _Joints1 = srcPrim.GetVertexAccessor("JOINTS_1")?.AsVector4Array() as IReadOnlyList<XYZW>;
            _Weights0 = srcPrim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array() as IReadOnlyList<XYZW>;
            _Weights1 = srcPrim.GetVertexAccessor("WEIGHTS_1")?.AsVector4Array() as IReadOnlyList<XYZW>;

            if (_Joints0 == null || _Weights0 == null) { _Joints0 = _Joints1 = _Weights0 = _Weights1 = null; }
            if (_Joints1 == null || _Weights1 == null) { _Joints1 = _Weights1 = null; }
            _JointsWeightsCount = (_Joints0 != null ? 4 : 0) + (_Joints1 != null ? 4 : 0);

            // renormalize weights (As per latest glTF spec should not be needed, but...)

            if (_Weights0 != null && _Weights1 == null)
            {
                var wwww = _Weights0.ToArray(); // isolate memory to prevent overwriting source glTF.

                for (int i = 0; i < wwww.Length; ++i)
                {
                    var r = XYZW.Dot(wwww[i], XYZW.One);
                    wwww[i] /= r;
                }

                _Weights0 = wwww;
            }

            if (_Weights0 != null && _Weights1 != null)
            {
                var wwww0 = _Weights0.ToArray(); // isolate memory to prevent overwriting source glTF.
                var wwww1 = _Weights1.ToArray(); // isolate memory to prevent overwriting source glTF.

                for (int i = 0; i < wwww0.Length; ++i)
                {
                    var r = XYZW.Dot(wwww0[i], XYZW.One) + XYZW.Dot(wwww1[i], XYZW.One);
                    wwww0[i] /= r;
                    wwww1[i] /= r;
                }

                _Weights0 = wwww0;
                _Weights1 = wwww1;
            }
        }

        #endregion

        #region data

        private readonly Schema2.PrimitiveType _PrimitiveType;
        private readonly IReadOnlyList<uint> _PrimitiveIndices;

        internal readonly _MeshGeometryDecoder _Geometry;

        internal readonly List<_MorphTargetDecoder> _MorphTargets = new List<_MorphTargetDecoder>();

        private readonly IReadOnlyList<XYZW> _Color0;
        private readonly IReadOnlyList<XYZW> _Color1;
        private readonly int _ColorsCount;

        private readonly IReadOnlyList<XY> _TexCoord0;
        private readonly IReadOnlyList<XY> _TexCoord1;
        private readonly IReadOnlyList<XY> _TexCoord2;
        private readonly IReadOnlyList<XY> _TexCoord3;
        private readonly int _TexCoordCount;

        private readonly IReadOnlyList<XYZW> _Joints0;
        private readonly IReadOnlyList<XYZW> _Joints1;

        private readonly IReadOnlyList<XYZW> _Weights0;
        private readonly IReadOnlyList<XYZW> _Weights1;

        private readonly int _JointsWeightsCount;

        private readonly Object _Extras;

        #endregion

        #region properties

        public int VertexCount => _Geometry.VertexCount;

        public int ColorsCount => _ColorsCount;

        public int TexCoordsCount => _TexCoordCount;

        public int JointsWeightsCount => _JointsWeightsCount;

        public int MorphTargetsCount => _MorphTargets.Count;

        public bool IsPointIndices => this._PrimitiveType.GetPrimitiveVertexSize() == 1;

        public IEnumerable<(int A, int B)> LineIndices
        {
            get
            {
                if (this._PrimitiveType.GetPrimitiveVertexSize() != 2) return Enumerable.Empty<(int, int)>();
                if (this._PrimitiveIndices == null) return this._PrimitiveType.GetLinesIndices(VertexCount);
                return this._PrimitiveType.GetLinesIndices(this._PrimitiveIndices);
            }
        }

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
            if (set == 2 && _TexCoord2 != null) return _TexCoord2[vertexIndex];
            if (set == 3 && _TexCoord3 != null) return _TexCoord3[vertexIndex];

            return XY.Zero;
        }

        public IReadOnlyList<XY> GetTextureCoordDeltas(int vertexIndex, int texCoordSet)
        {
            return MorphTargetsCount > 0
                ? (IReadOnlyList<XY>)new _MorphTargetTexCoordSlice(_MorphTargets, vertexIndex, texCoordSet)
                : Array.Empty<XY>();
        }

        public XYZW GetColor(int vertexIndex, int set)
        {
            if (set == 0 && _Color0 != null) return _Color0[vertexIndex];
            if (set == 1 && _Color1 != null) return _Color1[vertexIndex];

            return XYZW.One;
        }

        public IReadOnlyList<XYZW> GetColorDeltas(int vertexIndex, int colorSet)
        {
            return MorphTargetsCount > 0
                ? (IReadOnlyList<XYZW>)new _MorphTargetColorSlice(_MorphTargets, vertexIndex, colorSet)
                : Array.Empty<XYZW>();
        }

        public Transforms.SparseWeight8 GetSkinWeights(int vertexIndex)
        {
            if (_Weights0 == null) return default;

            var idx0123 = _Joints0[vertexIndex];
            var idx4567 = _Joints1 == null ? XYZW.Zero : _Joints1[vertexIndex];
            var wgt0123 = _Weights0[vertexIndex];
            var wgt4567 = _Weights1 == null ? XYZW.Zero : _Weights1[vertexIndex];

            return Transforms.SparseWeight8.CreateUnchecked(idx0123, idx4567, wgt0123, wgt4567);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Vertices: {VertexCount}")]
    sealed class _MeshGeometryDecoder :
        VertexNormalsFactory.IMeshPrimitive,
        VertexTangentsFactory.IMeshPrimitive
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
        public bool HasNormals => _Normals != null;
        public bool HasTangents => _Tangents != null;

        #endregion

        #region API

        public XYZ GetPosition(int vertexIndex) { return _Positions[vertexIndex]; }

        public XYZ GetNormal(int vertexIndex) { return _Normals[vertexIndex]; }

        public XYZW GetTangent(int vertexIndex) { return _Tangents[vertexIndex]; }

        public XY GetTextureCoord(int vertexIndex, int set) { return _Owner.GetTextureCoord(vertexIndex, set); }

        public XYZW GetColor(int vertexIndex, int set) { return _Owner.GetColor(vertexIndex, set); }

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
    sealed class _MorphTargetDecoder :
        VertexNormalsFactory.IMeshPrimitive,
        VertexTangentsFactory.IMeshPrimitive
    {
        #region  lifecycle

        public _MorphTargetDecoder(_MeshGeometryDecoder geometry, SCHEMA2PRIMITIVE srcPrim, int morphTargetIndex)
        {
            _Geometry = geometry;

            // get morph deltas and apply them to our base geometry copy.

            var morphs = srcPrim.GetMorphTargetAccessors(morphTargetIndex);

            if (morphs.TryGetValue("POSITION", out SCHEMA2ACCESSOR pAccessor))
            {
                _PositionsDeltas = pAccessor.AsVector3Array() as IReadOnlyList<XYZ>;
            }

            if (morphs.TryGetValue("NORMAL", out SCHEMA2ACCESSOR nAccessor))
            {
                _NormalsDeltas = nAccessor.AsVector3Array() as IReadOnlyList<XYZ>;
            }

            if (morphs.TryGetValue("TANGENT", out SCHEMA2ACCESSOR tAccessor))
            {
                _TangentsDeltas = tAccessor.AsVector3Array() as IReadOnlyList<XYZ>;
            }

            if (morphs.TryGetValue("TEXCOORD_0", out SCHEMA2ACCESSOR uv0Accessor))
            {
                _TexCoordDeltas_0 = uv0Accessor.AsVector2Array() as IReadOnlyList<XY>;
            }

            if (morphs.TryGetValue("TEXCOORD_1", out SCHEMA2ACCESSOR uv1Accessor))
            {
                _TexCoordDeltas_1 = uv1Accessor.AsVector2Array() as IReadOnlyList<XY>;
            }

            if (morphs.TryGetValue("COLOR_0", out SCHEMA2ACCESSOR c0Accessor))
            {
                _ColorDeltas_0 = c0Accessor.AsVector4Array() as IReadOnlyList<XYZW>;
            }

            if (morphs.TryGetValue("COLOR_1", out SCHEMA2ACCESSOR c1Accessor))
            {
                _ColorDeltas_1 = c1Accessor.AsVector4Array() as IReadOnlyList<XYZW>;
            }
        }

        #endregion

        #region data

        private readonly _MeshGeometryDecoder _Geometry;

        internal readonly IReadOnlyList<XYZ> _PositionsDeltas;
        private IReadOnlyList<XYZ> _NormalsDeltas;
        private IReadOnlyList<XYZ> _TangentsDeltas;

        // morph target deltas for uv sets
        private IReadOnlyList<XY> _TexCoordDeltas_0;
        private IReadOnlyList<XY> _TexCoordDeltas_1;

        // morph target deltas for color sets
        private IReadOnlyList<XYZW> _ColorDeltas_0;
        private IReadOnlyList<XYZW> _ColorDeltas_1;

        private XYZ[] _GeneratedNormals;
        private XYZ[] _GeneratedTangents;

        #endregion

        #region properties

        public int VertexCount => _PositionsDeltas?.Count ?? 0;
        public bool HasNormals => _NormalsDeltas != null;
        public bool HasTangents => _TangentsDeltas != null;
        public bool HasTexCoord(int set)
        {
            switch(set)
            {
                case 0: return _TexCoordDeltas_0 != null;
                case 1: return _TexCoordDeltas_1 != null;
            }

            return false;
        }

        public bool HasColor(int set)
        {
            switch (set)
            {
                case 0: return _ColorDeltas_0 != null;
                case 1: return _ColorDeltas_1 != null;
            }

            return false;
        }

        #endregion

        #region API

        public XYZ GetPositionBase(int vertexIndex) { return _Geometry.GetPosition(vertexIndex); }

        public XYZ GetPositionDelta(int vertexIndex) { return _PositionsDeltas == null ? XYZ.Zero : _PositionsDeltas[vertexIndex]; }

        public XYZ GetNormalBase(int vertexIndex) { return _Geometry.GetNormal(vertexIndex); }

        public XYZ GetNormalDelta(int vertexIndex) { return _NormalsDeltas == null ? XYZ.Zero : _NormalsDeltas[vertexIndex]; }

        public XYZW GetTangentBase(int vertexIndex) { return _Geometry.GetTangent(vertexIndex); }

        public XYZ GetTangentDelta(int vertexIndex) { return _TangentsDeltas == null ? XYZ.Zero : _TangentsDeltas[vertexIndex]; }

        public XY GetTextureCoord(int vertexIndex, int set) { return _Geometry.GetTextureCoord(vertexIndex, set); }

        public XY GetTextureCoordDelta(int vertexIndex, int set)
        {
            switch(set)
            {
                case 0: return _TexCoordDeltas_0 == null ? XY.Zero : _TexCoordDeltas_0[vertexIndex];
                case 1: return _TexCoordDeltas_1 == null ? XY.Zero : _TexCoordDeltas_1[vertexIndex];
            }            

            return XY.Zero;
        }

        public XYZW GetColor(int vertexIndex, int set) { return _Geometry.GetColor(vertexIndex, set); }

        public XYZW GetColorDelta(int vertexIndex, int set)
        {
            switch(set)
            {
                case 0: return _ColorDeltas_0 == null ? XYZW.Zero : _ColorDeltas_0[vertexIndex];
                case 1: return _ColorDeltas_1 == null ? XYZW.Zero : _ColorDeltas_1[vertexIndex];
            }

            return XYZW.Zero;
        }

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

    [System.Diagnostics.DebuggerDisplay("Vertex {_VertexIndex} Tangents deltas")]
    readonly struct _MorphTargetTexCoordSlice : IReadOnlyList<XY>
    {
        public _MorphTargetTexCoordSlice(IReadOnlyList<_MorphTargetDecoder> ggg, int idx, int set)
        {
            _Geometries = ggg;
            _VertexIndex = idx;
            _TexCoordSet = set;
        }

        private readonly IReadOnlyList<_MorphTargetDecoder> _Geometries;
        private readonly int _VertexIndex;
        private readonly int _TexCoordSet;

        public XY this[int index] => _Geometries[index].GetTextureCoordDelta(_VertexIndex, _TexCoordSet);
        public int Count => _Geometries.Count;
        public IEnumerator<XY> GetEnumerator() { throw new NotImplementedException(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
    }

    [System.Diagnostics.DebuggerDisplay("Vertex {_VertexIndex} Tangents deltas")]
    readonly struct _MorphTargetColorSlice : IReadOnlyList<XYZW>
    {
        public _MorphTargetColorSlice(IReadOnlyList<_MorphTargetDecoder> ggg, int idx, int set)
        {
            _Geometries = ggg;
            _VertexIndex = idx;
            _ColorSet = set;
        }

        private readonly IReadOnlyList<_MorphTargetDecoder> _Geometries;
        private readonly int _VertexIndex;
        private readonly int _ColorSet;

        public XYZW this[int index] => _Geometries[index].GetColorDelta(_VertexIndex, _ColorSet);
        public int Count => _Geometries.Count;
        public IEnumerator<XYZW> GetEnumerator() { throw new NotImplementedException(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
    }
}
