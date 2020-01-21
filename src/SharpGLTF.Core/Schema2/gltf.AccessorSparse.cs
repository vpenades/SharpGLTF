using System.Collections.Generic;

namespace SharpGLTF.Schema2
{
    using SharpGLTF.Validation;
    using ROOT = ModelRoot;

    public sealed partial class AccessorSparse
    {
        #region lifecycle

        internal AccessorSparse() { }

        #endregion

        #region properties

        public int Count => _count;

        #endregion

        #region API

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatItems(_indices, _values);
        }

        internal AccessorSparse(BufferView indices, int indicesOffset, IndexEncodingType indicesEncoding, BufferView values, int valuesOffset, int count)
        {
            Guard.NotNull(indices, nameof(indices));
            Guard.NotNull(values, nameof(values));
            Guard.MustBeGreaterThanOrEqualTo(count, _countMinimum, nameof(count));

            this._count = count;
            this._indices = new AccessorSparseIndices(indices, indicesOffset, indicesEncoding);
            this._values = new AccessorSparseValues(values, valuesOffset);
        }

        internal KeyValuePair<Memory.IntegerArray, Memory.MemoryAccessor> _CreateMemoryAccessors(Accessor baseAccessor)
        {
            var key = this._indices._GetIndicesArray(baseAccessor.LogicalParent, _count);
            var val = this._values._GetMemoryAccessor(baseAccessor.LogicalParent, _count, baseAccessor);

            return new KeyValuePair<Memory.IntegerArray, Memory.MemoryAccessor>(key, val);
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(ValidationContext result)
        {
            result = result.GetContext(this);

            base.OnValidateReferences(result);

            result.CheckIsInRange(nameof(Count), _count, _countMinimum, int.MaxValue);
            result.CheckSchemaIsDefined("Indices", _indices);
            result.CheckSchemaIsDefined("Values", _values);

            _indices?.ValidateReferences(result);
            _values?.ValidateReferences(result);
        }

        protected override void OnValidate(ValidationContext result)
        {
            result = result.GetContext(this);

            base.OnValidate(result);

            _indices.Validate(result, _count);
            _values.Validate(result, _count);
        }

        #endregion
    }

    public sealed partial class AccessorSparseIndices
    {
        #region lifecycle

        internal AccessorSparseIndices() { }

        internal AccessorSparseIndices(BufferView bv, int byteOffset, IndexEncodingType encoding)
        {
            Guard.NotNull(bv, nameof(bv));
            Guard.MustBeGreaterThanOrEqualTo(byteOffset, _byteOffsetMinimum, nameof(byteOffset));

            this._bufferView = bv.LogicalIndex;
            this._byteOffset = byteOffset.AsNullable(_byteOffsetDefault);
            this._componentType = encoding;
        }

        #endregion

        #region API

        internal Memory.IntegerArray _GetIndicesArray(ROOT root, int count)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return new Memory.IntegerArray(srcBuffer.Content, this._byteOffset ?? 0, count, this._componentType);
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(ValidationContext result)
        {
            result = result.GetContext(this);

            base.OnValidateReferences(result);

            result.CheckSchemaNonNegative("ByteOffset", _byteOffset);
            result.CheckArrayIndexAccess("BufferView", _bufferView, result.Root.LogicalBufferViews);
        }

        internal void Validate(ValidationContext result, int count)
        {
            result = result.GetContext(this);

            var bv = result.Root.LogicalBufferViews[_bufferView];

            BufferView.CheckAccess(result, bv, _byteOffset ?? _byteOffsetDefault, DimensionType.SCALAR, _componentType.ToComponent(), false, count);
        }

        #endregion
    }

    public sealed partial class AccessorSparseValues
    {
        #region lifecycle

        internal AccessorSparseValues() { }

        internal AccessorSparseValues(BufferView bv, int byteOffset)
        {
            Guard.NotNull(bv, nameof(bv));
            Guard.MustBeGreaterThanOrEqualTo(byteOffset, _byteOffsetMinimum, nameof(byteOffset));

            this._bufferView = bv.LogicalIndex;
            this._byteOffset = byteOffset.AsNullable(_byteOffsetDefault);
        }

        #endregion

        #region API

        internal Memory.MemoryAccessor _GetMemoryAccessor(ROOT root, int count, Accessor baseAccessor)
        {
            var view = root.LogicalBufferViews[this._bufferView];
            var info = new Memory.MemoryEncoding(null, this._byteOffset ?? 0, count, view.ByteStride, baseAccessor.Dimensions, baseAccessor.Encoding, baseAccessor.Normalized);
            return new Memory.MemoryAccessor(view.Content, info);
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(ValidationContext result)
        {
            result = result.GetContext(this);

            base.OnValidateReferences(result);

            result.CheckSchemaNonNegative("ByteOffset", _byteOffset);
            result.CheckArrayIndexAccess("BufferView", _bufferView, result.Root.LogicalBufferViews);
        }

        internal void Validate(ValidationContext result, int count)
        {
            var bv = result.Root.LogicalBufferViews[_bufferView];

            // we need the accessor's settings to properly check this.
            // result.CheckAccess(bv, _byteOffset, DimensionType.SCALAR, _componentType, count);
        }

        #endregion
    }
}
