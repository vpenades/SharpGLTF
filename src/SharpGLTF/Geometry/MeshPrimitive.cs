using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    public abstract class NamedObject
    {
        public NamedObject() { }

        public NamedObject(Schema2.LogicalChildOfRoot source)
        {
            Name = source.Name;
        }

        public string Name { get; set; }
        public Object Extra { get; set; }
    }

    [System.Diagnostics.DebuggerDisplay("{Name}.{_MemoryAccessor.Attribute.Name} {_MemoryAccessor.Attribute.ItemsCount}")]
    public class VertexAccessor : NamedObject
    {
        #region lifecycle

        public VertexAccessor(string attributeName, Schema2.Accessor accessor)
            : base(accessor)
        {
            _MemoryAccessor = accessor._GetMemoryAccessor();
            _MemoryAccessor.SetName(attributeName);
            _Sparse = accessor._GetSparseMemoryAccessor();
        }

        public VertexAccessor(MemoryAccessInfo info)
        {
            _MemoryAccessor = new MemoryAccessor(info);
        }

        public static VertexAccessor[] CreateAccessors(int itemsCount, params string[] attributes)
        {
            var accessors = MemoryAccessInfo
                .Create(attributes)
                .Select(item => new VertexAccessor(item))
                .ToArray();

            int byteOffset = 0;
            var byteStride = accessors.Sum(item => item._MemoryAccessor.Attribute.ByteLength);

            var data = new ArraySegment<Byte>(new Byte[byteStride * itemsCount]);

            for (int i = 0; i < accessors.Length; ++i)
            {
                var a = accessors[i];

                a._MemoryAccessor.SetVertexDataSource(data, byteOffset, itemsCount, byteStride);

                byteOffset += a._MemoryAccessor.Attribute.ByteLength;
            }

            return accessors;
        }

        #endregion

        #region data

        private MemoryAccessor _MemoryAccessor;
        private KeyValuePair<Memory.IntegerArray, MemoryAccessor>? _Sparse;

        #endregion

        #region API

        public void SetValues(int startIndex, params Single[] values)
        {
            Memory.EncodedArrayUtils.FillFrom(AsScalarArray(), startIndex, values);
        }

        public void SetValues(int startIndex, params Vector2[] values)
        {
            Memory.EncodedArrayUtils.FillFrom(AsVector2Array(), startIndex, values);
        }

        public void SetValues(int startIndex, params Vector3[] values)
        {
            Memory.EncodedArrayUtils.FillFrom(AsVector3Array(), startIndex, values);
        }

        public void SetValues(int startIndex, params Vector4[] values)
        {
            Memory.EncodedArrayUtils.FillFrom(AsVector4Array(), startIndex, values);
        }

        public Memory.IEncodedArray<Single> AsScalarArray()
        {
            if (!_Sparse.HasValue) return _MemoryAccessor.AsScalarArray();

            return MemoryAccessor.CreateScalarSparseArray(_MemoryAccessor, _Sparse.Value.Key, _Sparse.Value.Value);
        }

        public Memory.IEncodedArray<Vector2> AsVector2Array()
        {
            if (!_Sparse.HasValue) return _MemoryAccessor.AsVector2Array();

            return MemoryAccessor.CreateVector2SparseArray(_MemoryAccessor, _Sparse.Value.Key, _Sparse.Value.Value);
        }

        public Memory.IEncodedArray<Vector3> AsVector3Array()
        {
            if (!_Sparse.HasValue) return _MemoryAccessor.AsVector3Array();

            return MemoryAccessor.CreateVector3SparseArray(_MemoryAccessor, _Sparse.Value.Key, _Sparse.Value.Value);
        }

        public Memory.IEncodedArray<Vector4> AsVector4Array()
        {
            if (!_Sparse.HasValue) return _MemoryAccessor.AsVector4Array();

            return MemoryAccessor.CreateVector4SparseArray(_MemoryAccessor, _Sparse.Value.Key, _Sparse.Value.Value);
        }

        public void AssignTo(Schema2.MeshPrimitive dstPrim)
        {
            var dstAccessor = dstPrim.LogicalParent.LogicalParent.CreateAccessor(this.Name);
            dstAccessor.SetVertexData(_MemoryAccessor);
            dstPrim.SetVertexAccessor(this._MemoryAccessor.Attribute.Name, dstAccessor);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{Name}.{_MemoryAccessor.Attribute.Name} {_MemoryAccessor.Attribute.ItemsCount}")]
    public class IndicesAccessor : NamedObject
    {
        #region lifecycle

        public IndicesAccessor(Schema2.Accessor accessor)
            : base(accessor)
        {
            _MemoryAccessor = accessor._GetMemoryAccessor();
            _MemoryAccessor.SetName("INDEX");
        }

        public IndicesAccessor(MemoryAccessInfo info)
        {
            _MemoryAccessor = new MemoryAccessor(info);
        }

        public static IndicesAccessor CreateAccessors(int itemsCount)
        {
            var info = MemoryAccessInfo.CreateDefaultElement("INDEX");
            info.ItemsCount = itemsCount;

            var data = new ArraySegment<Byte>(new Byte[info.ByteLength * itemsCount]);

            var accessor =  new IndicesAccessor(info);

            accessor._MemoryAccessor.SetIndexDataSource(data, 0, itemsCount);

            return accessor;
        }

        #endregion

        #region data

        private MemoryAccessor _MemoryAccessor;

        #endregion

        #region API

        public void SetValues(int startIndex, params UInt32[] values)
        {
            Memory.EncodedArrayUtils.FillFrom(AsIntegerArray(), startIndex, values);
        }

        public Memory.IEncodedArray<UInt32> AsIntegerArray()
        {
            return _MemoryAccessor.AsIntegerArray();
        }

        public void AssignToSchema(Schema2.MeshPrimitive dstPrim)
        {
            var dstAccessor = dstPrim.LogicalParent.LogicalParent.CreateAccessor(this.Name);
            dstAccessor.SetIndexData(_MemoryAccessor);
            dstPrim.IndexAccessor = dstAccessor;
        }

        #endregion
    }

    public class MeshPrimitive<TMaterial>
    {
        #region lifecycle

        internal MeshPrimitive(Mesh<TMaterial> owner) { _Owner = owner; }

        internal MeshPrimitive(Mesh<TMaterial> owner, Schema2.MeshPrimitive primitive)
            : this(owner)
        {
            _Vertices = primitive.VertexAccessors
                .Select(kvp => new VertexAccessor(kvp.Key, kvp.Value))
                .ToArray();

            for (int i = 0; i < primitive.MorphTargetsCount; ++i)
            {
                var accessors = primitive.GetMorphTargetAccessors(i)
                    .Select(kvp => new VertexAccessor(kvp.Key, kvp.Value))
                    .ToArray();

                _MorphAccessors.Add(accessors);
            }

            _Indices = primitive.IndexAccessor == null ? null : new IndicesAccessor(primitive.IndexAccessor);
            _PrimitiveDrawType = primitive.DrawPrimitiveType;

            if (primitive.Material != null)
            {
                if (typeof(TMaterial) == typeof(Schema2.Material))
                {
                    _Material = (TMaterial)(Object)primitive.Material;
                }

                if (typeof(TMaterial) == typeof(int?))
                {
                    var materialIndex = primitive.Material?.LogicalIndex;

                    _Material = (TMaterial)(Object)materialIndex;
                }
            }
        }

        #endregion

        #region data

        private readonly Mesh<TMaterial> _Owner;

        private VertexAccessor[] _Vertices;
        private readonly List<VertexAccessor[]> _MorphAccessors = new List<VertexAccessor[]>();

        private IndicesAccessor _Indices;

        private Schema2.PrimitiveType _PrimitiveDrawType;

        private TMaterial _Material;

        #endregion

        #region properties

        public IReadOnlyList<VertexAccessor> Vertices => _Vertices;

        public IndicesAccessor Indices => _Indices;

        public TMaterial Material
        {
            get => _Material;
            set => _Material = value;
        }

        #endregion

        #region API

        public void AllocateVertices(int itemsCount, params string[] attributes)
        {
            _Vertices = VertexAccessor.CreateAccessors(itemsCount, attributes);
        }

        public void AllocateIndices(int itemsCount, Schema2.PrimitiveType primitiveType)
        {
            _PrimitiveDrawType = primitiveType;
            _Indices = IndicesAccessor.CreateAccessors(itemsCount);
        }

        public void AssignToSchema(Schema2.MeshPrimitive dstPrim)
        {
            // TODO: clear primitive

            foreach (var va in this._Vertices)
            {
                va.AssignTo(dstPrim);
            }

            if (this._Indices != null) this._Indices.AssignToSchema(dstPrim);

            dstPrim.DrawPrimitiveType = this._PrimitiveDrawType;

            if (typeof(TMaterial) == typeof(Schema2.Material))
            {
                var material = (Schema2.Material)(Object)_Material;
                dstPrim.Material = material;
            }

            if (typeof(TMaterial) == typeof(int?))
            {
                var materialIndex = (int?)(Object)_Material;
                var material = materialIndex.HasValue ? dstPrim.LogicalParent.LogicalParent.LogicalMaterials[materialIndex.Value] : null;
                dstPrim.Material = material;
            }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Mesh {Name}")]
    public class Mesh<TMaterial> : NamedObject
    {
        #region lifecycle

        public Mesh() { }

        /*
        public Mesh(Schema2.Mesh mesh)
            : base(mesh)
        {
            _Primitives.AddRange(mesh.Primitives, item => new MeshPrimitive(this, item));
            _MorpthWeights.AddRange(mesh.MorphWeights);
        }*/

        #endregion

        #region data

        private readonly List<MeshPrimitive<TMaterial>> _Primitives = new List<MeshPrimitive<TMaterial>>();
        private readonly List<Single> _MorpthWeights = new List<float>();

        #endregion

        #region API

        public MeshPrimitive<TMaterial> CreatePrimitive()
        {
            var p = new MeshPrimitive<TMaterial>(this);
            _Primitives.Add(p);

            return p;
        }

        public void AssignToSchema(Schema2.Mesh mesh)
        {
            mesh.Name = this.Name;

            foreach (var srcp in this._Primitives)
            {
                var dstp = mesh.CreatePrimitive();

                srcp.AssignToSchema(dstp);
            }

            // todo: set morph targets
        }

        #endregion
    }
}
