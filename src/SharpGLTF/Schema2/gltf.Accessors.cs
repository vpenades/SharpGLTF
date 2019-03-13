using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    using Memory;

    using EXCEPTION = IO.ModelException;

    using ROOT = ModelRoot;

    // https://github.com/KhronosGroup/glTF/issues/827#issuecomment-277537204

    [System.Diagnostics.DebuggerDisplay("Accessor[{LogicalIndex}] BufferView[{SourceBufferView.LogicalIndex}][{ByteOffset}...] => 0 => {Dimensions}x{Encoding}x{Normalized} => [{Count}]")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._AccessorDebugProxy))]
    public sealed partial class Accessor
    {
        #region debug

        internal string _DebuggerDisplay_TryIdentifyContent()
        {
            return $"{Dimensions}_{Encoding}[{_count}]";
        }

        #endregion

        #region lifecycle

        internal Accessor()
        {
            _min = new List<double>();
            _max = new List<double>();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Accessor"/> at <see cref="ModelRoot.LogicalAccessors"/>
        /// </summary>
        public int LogicalIndex                 => this.LogicalParent.LogicalAccessors.IndexOfReference(this);

        internal int _LogicalBufferViewIndex    => this._bufferView.AsValue(-1);

        public BufferView SourceBufferView      => this._bufferView.HasValue ? this.LogicalParent.LogicalBufferViews[this._bufferView.Value] : null;

        public int Count                        => this._count;

        public int ByteOffset                   => this._byteOffset.AsValue(0);

        public ElementType Dimensions           => this._type;

        public ComponentType Encoding           => this._componentType;

        public Boolean Normalized               => this._normalized.AsValue(false);

        public Boolean IsSparse                 => this._sparse != null;

        public int ItemByteSize                 => Encoding.ByteLength() * Dimensions.DimCount();

        public Transforms.BoundingBox3? LocalBounds3
        {
            get
            {
                if (this._min.Count != 3) return null;
                if (this._max.Count != 3) return null;

                return new Transforms.BoundingBox3(this._min, this._max);
            }
        }

        #endregion

        #region API

        internal Geometry.MemoryAccessor _GetMemoryAccessor()
        {
            var view = SourceBufferView;
            var info = new Geometry.MemoryAccessInfo(null, ByteOffset, Count, view.ByteStride, Dimensions, Encoding, Normalized);
            return new Geometry.MemoryAccessor(info, view.Content);
        }

        internal KeyValuePair<Memory.IntegerArray, Geometry.MemoryAccessor>? _GetSparseMemoryAccessor()
        {
            return this._sparse == null
                ?
                (KeyValuePair<Memory.IntegerArray, Geometry.MemoryAccessor>?)null
                :
                this._sparse._CreateMemoryAccessors(this);
        }

        #endregion

        #region Data Buffer API

        public Accessor WithData(BufferView buffer, int byteOffset, int itemCount, ElementType dimensions, ComponentType encoding, Boolean normalized)
        {
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));

            Guard.MustBeGreaterThanOrEqualTo(byteOffset, _byteOffsetMinimum, nameof(byteOffset));
            Guard.MustBeGreaterThanOrEqualTo(itemCount, _countMinimum, nameof(itemCount));

            this._bufferView = buffer.LogicalIndex;
            this._byteOffset = byteOffset.AsNullable(_byteOffsetDefault, _byteOffsetMinimum, int.MaxValue);
            this._count = itemCount;

            this._type = dimensions;
            this._componentType = encoding;
            this._normalized = normalized.AsNullable(_normalizedDefault);

            UpdateBounds();

            return this;
        }

        public Memory.Matrix4x4Array AsMatrix4x4Array()
        {
            return _GetMemoryAccessor().AsMatrix4x4Array();
        }

        #endregion

        #region Index Buffer API

        public Accessor WithIndexData(Geometry.MemoryAccessor src)
        {
            var bv = this.LogicalParent.UseBufferView(src.Data, src.Attribute.ByteStride, BufferMode.ELEMENT_ARRAY_BUFFER);
            return this.WithIndexData(bv, src.Attribute.ByteOffset, src.Attribute.ItemsCount, src.Attribute.Encoding.ToIndex());
        }

        public Accessor WithIndexData(BufferView buffer, int byteOffset, IReadOnlyList<Int32> items, IndexType encoding = IndexType.UNSIGNED_INT)
        {
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));

            WithIndexData(buffer, byteOffset, items.Count, encoding)
                .AsIndicesArray()
                .FillFrom(0, items);

            this.UpdateBounds();

            return this;
        }

        public Accessor WithIndexData(BufferView buffer, int byteOffset, IReadOnlyList<UInt32> items, IndexType encoding = IndexType.UNSIGNED_INT)
        {
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));

            WithIndexData(buffer, byteOffset, items.Count, encoding)
                .AsIndicesArray()
                .FillFrom(0, items);

            this.UpdateBounds();

            return this;
        }

        public Accessor WithIndexData(BufferView buffer, int byteOffset, int itemCount, IndexType encoding)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));

            if (buffer.DeviceBufferTarget.HasValue) Guard.IsTrue(buffer.DeviceBufferTarget.Value == BufferMode.ELEMENT_ARRAY_BUFFER, nameof(buffer));

            return WithData(buffer, byteOffset, itemCount, ElementType.SCALAR, encoding.ToComponent(), false);
        }

        public Memory.IntegerArray AsIndicesArray()
        {
            Guard.IsFalse(this.IsSparse, nameof(IsSparse));
            Guard.IsTrue(this.Dimensions == ElementType.SCALAR, nameof(Dimensions));

            return new Memory.IntegerArray(SourceBufferView.Content, this.ByteOffset, this._count, this.Encoding.ToIndex());
        }

        #endregion

        #region Vertex Buffer API

        public Accessor WithVertexData(Geometry.MemoryAccessor src)
        {
            var bv = this.LogicalParent.UseBufferView(src.Data, src.Attribute.ByteStride, BufferMode.ARRAY_BUFFER);
            return this.WithVertexData(bv, src.Attribute.ByteOffset, src.Attribute.ItemsCount, src.Attribute.Dimensions, src.Attribute.Encoding, src.Attribute.Normalized);
        }

        public Accessor WithVertexData(BufferView buffer, int bufferByteOffset, IReadOnlyList<Single> items, ComponentType encoding = ComponentType.FLOAT, Boolean normalized = false)
        {
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));
            Guard.MustBePositiveAndMultipleOf(ElementType.SCALAR.DimCount() * encoding.ByteLength(), 4, nameof(encoding));

            WithVertexData(buffer, bufferByteOffset, items.Count, ElementType.SCALAR, encoding, normalized)
                .AsScalarArray()
                .FillFrom(0, items);

            this.UpdateBounds();

            return this;
        }

        public Accessor WithVertexData(BufferView buffer, int bufferByteOffset, IReadOnlyList<Vector2> items, ComponentType encoding = ComponentType.FLOAT, Boolean normalized = false)
        {
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));
            Guard.MustBePositiveAndMultipleOf(ElementType.VEC2.DimCount() * encoding.ByteLength(), 4, nameof(encoding));

            WithVertexData(buffer, bufferByteOffset, items.Count, ElementType.VEC2, encoding, normalized)
                .AsVector2Array()
                .FillFrom(0, items);

            this.UpdateBounds();

            return this;
        }

        public Accessor WithVertexData(BufferView buffer, int bufferByteOffset, IReadOnlyList<Vector3> items, ComponentType encoding = ComponentType.FLOAT, Boolean normalized = false)
        {
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));
            Guard.MustBePositiveAndMultipleOf(ElementType.VEC3.DimCount() * encoding.ByteLength(), 4, nameof(encoding));

            WithVertexData(buffer, bufferByteOffset, items.Count, ElementType.VEC3, encoding, normalized)
                .AsVector3Array()
                .FillFrom(0, items);

            this.UpdateBounds();

            return this;
        }

        public Accessor WithVertexData(BufferView buffer, int bufferByteOffset, IReadOnlyList<Vector4> items, ComponentType encoding = ComponentType.FLOAT, Boolean normalized = false)
        {
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));
            Guard.MustBePositiveAndMultipleOf(ElementType.VEC4.DimCount() * encoding.ByteLength(), 4, nameof(encoding));

            WithVertexData(buffer, bufferByteOffset, items.Count, ElementType.VEC4, encoding, normalized)
                .AsVector4Array()
                .FillFrom(0, items);

            this.UpdateBounds();

            return this;
        }

        public Accessor WithVertexData(BufferView buffer, int bufferByteOffset, IReadOnlyList<Quaternion> items, ComponentType encoding = ComponentType.FLOAT, Boolean normalized = false)
        {
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));
            Guard.MustBePositiveAndMultipleOf(ElementType.VEC4.DimCount() * encoding.ByteLength(), 4, nameof(encoding));

            WithVertexData(buffer, bufferByteOffset, items.Count, ElementType.VEC4, encoding, normalized)
                .AsQuaternionArray()
                .FillFrom(0, items);

            this.UpdateBounds();

            return this;
        }

        public Accessor WithVertexData(BufferView buffer, int bufferByteOffset, int itemCount, ElementType dimensions = ElementType.VEC3, ComponentType encoding = ComponentType.FLOAT, Boolean normalized = false)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));
            Guard.MustBePositiveAndMultipleOf(dimensions.DimCount() * encoding.ByteLength(), 4, nameof(encoding));

            if (buffer.DeviceBufferTarget.HasValue) Guard.IsTrue(buffer.DeviceBufferTarget.Value == BufferMode.ARRAY_BUFFER, nameof(buffer));

            return WithData(buffer, bufferByteOffset, itemCount, dimensions, encoding, normalized);
        }

        public IEncodedArray<float> AsScalarArray()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsScalarArray();

            var sparseKV = this._sparse._CreateMemoryAccessors(this);
            return Geometry.MemoryAccessor.CreateScalarSparseArray(memory, sparseKV.Key, sparseKV.Value);
        }

        public IEncodedArray<Vector2> AsVector2Array()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsVector2Array();

            var sparseKV = this._sparse._CreateMemoryAccessors(this);
            return Geometry.MemoryAccessor.CreateVector2SparseArray(memory, sparseKV.Key, sparseKV.Value);
        }

        public IEncodedArray<Vector3> AsVector3Array()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsVector3Array();

            var sparseKV = this._sparse._CreateMemoryAccessors(this);
            return Geometry.MemoryAccessor.CreateVector3SparseArray(memory, sparseKV.Key, sparseKV.Value);
        }

        public IEncodedArray<Vector4> AsVector4Array()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsVector4Array();

            var sparseKV = this._sparse._CreateMemoryAccessors(this);
            return Geometry.MemoryAccessor.CreateVector4SparseArray(memory, sparseKV.Key, sparseKV.Value);
        }

        public IEncodedArray<Quaternion> AsQuaternionArray()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsQuaternionArray();

            throw new NotImplementedException();
        }

        public ArraySegment<Byte> TryGetVertexBytes(int vertexIdx)
        {
            if (_sparse != null) throw new InvalidOperationException("Can't be used on Acessors with Sparse Data");

            var byteSize = Encoding.ByteLength() * Dimensions.DimCount();
            var byteStride = Math.Max(byteSize, SourceBufferView.ByteStride);
            var byteOffset = vertexIdx * byteStride;

            return SourceBufferView.Content.Slice(this.ByteOffset + (vertexIdx * byteStride), byteSize);
        }

        #endregion

        #region API

        protected override IEnumerable<glTFProperty> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_sparse);
        }

        public void UpdateBounds()
        {
            var dimensions = this._type.DimCount();

            var bounds = new double[dimensions];
            this._min.Clear();
            this._min.AddRange(bounds);
            this._max.Clear();
            this._max.AddRange(bounds);

            this._min.Fill(double.MaxValue);
            this._max.Fill(double.MinValue);

            var array = new MultiArray(this.SourceBufferView.Content, this.ByteOffset, this.Count, this.SourceBufferView.ByteStride, dimensions, this.Encoding, false);

            var current = new float[dimensions];

            for (int i = 0; i < array.Count; ++i)
            {
                array.CopyItemTo(i, current);

                for (int j = 0; j < current.Length; ++j)
                {
                    this._min[j] = Math.Min(this._min[j], current[j]);
                    this._max[j] = Math.Max(this._max[j], current[j]);
                }
            }
        }

        public override IEnumerable<Exception> Validate()
        {
            var exxx = base.Validate().ToList();

            if (!_bufferView.HasValue) { exxx.Add(new EXCEPTION(this, $"BufferView index missing")); return exxx; }
            if (_bufferView < 0 || _bufferView >= LogicalParent.LogicalBufferViews.Count) exxx.Add(new EXCEPTION(this, $"BufferView index out of range"));

            if (_count < 0) exxx.Add(new EXCEPTION(this, $"Count is out of range"));
            if (_byteOffset < 0) exxx.Add(new EXCEPTION(this, $"ByteOffset is out of range"));

            if (SourceBufferView.DeviceBufferTarget == BufferMode.ARRAY_BUFFER)
            {
                var len = Encoding.ByteLength() * Dimensions.DimCount();
                if (len > 0 && (len & 3) != 0) exxx.Add(new EXCEPTION(this, $"Expected length to be multiple of 4, found {len}"));
            }

            if (SourceBufferView.DeviceBufferTarget == BufferMode.ELEMENT_ARRAY_BUFFER)
            {
                var len = Encoding.ByteLength() * Dimensions.DimCount();
                if (len != 1 && len != 2 && len != 4) exxx.Add(new EXCEPTION(this, $"Expected length to be 1, 2 or 4, found {len}"));
            }

            // validate bounds

            if (_min.Count != _max.Count) { exxx.Add(new EXCEPTION(this, "min and max length mismatch")); return exxx; }

            for (int i = 0; i < _min.Count; ++i)
            {
                if (_min[i] > _max[i]) exxx.Add(new EXCEPTION(this, $"min[{i}] is larger than max[{i}]"));
            }

            return exxx;
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Accessor"/> instance
        /// and adds it to <see cref="ModelRoot.LogicalAccessors"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Accessor"/> instance.</returns>
        public Accessor CreateAccessor(string name = null)
        {
            var accessor = new Accessor
            {
                Name = name
            };

            _accessors.Add(accessor);

            return accessor;
        }
    }
}
