using System.Collections.Generic;

using SharpGLTF.Validation;

using ROOT = SharpGLTF.Schema2.ModelRoot;

namespace SharpGLTF.Schema2
{
    public sealed partial class AccessorSparse
    {
        #region lifecycle

        internal AccessorSparse() { }

        internal AccessorSparse(int sparseCount, BufferView indices, int indicesOffset, IndexEncodingType indicesEncoding, BufferView values, int valuesOffset)
        {
            Guard.NotNull(indices, nameof(indices));
            Guard.NotNull(values, nameof(values));
            Guard.MustBeGreaterThanOrEqualTo(sparseCount, _countMinimum, nameof(sparseCount));

            this._count = sparseCount;
            this._indices = new AccessorSparseIndices(indices, indicesOffset, indicesEncoding);
            this._values = new AccessorSparseValues(values, valuesOffset);            
        }

        #endregion

        #region properties

        public int Count => _count;

        #endregion

        #region API        

        internal KeyValuePair<Memory.IntegerArray, Memory.MemoryAccessor> _CreateMemoryAccessors(Accessor baseAccessor)
        {
            var key = this._indices._GetIndicesArray(baseAccessor.LogicalParent, _count);
            var val = this._values._GetMemoryAccessor(baseAccessor.LogicalParent, _count, baseAccessor);

            return new KeyValuePair<Memory.IntegerArray, Memory.MemoryAccessor>(key, val);
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate
                .IsInRange(nameof(Count), _count, _countMinimum, int.MaxValue)
                .IsDefined("Indices", _indices)
                .IsDefined("Values", _values);

            _indices?.ValidateReferences(validate);
            _values?.ValidateReferences(validate);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            _indices.ValidateIndices(validate, _count);
            _values.ValidateValues(validate, _count);
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

        internal Memory.IntegerArray _GetIndicesArray(ROOT root, int sparseCount)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return new Memory.IntegerArray(srcBuffer.Content, this._byteOffset ?? 0, sparseCount, this._componentType);
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate
                .NonNegative("ByteOffset", _byteOffset)
                .IsNullOrIndex("BufferView", _bufferView, validate.Root.LogicalBufferViews);
        }

        internal void ValidateIndices(ValidationContext validate, int count)
        {
            validate = validate.GetContext(this);

            var bv = validate.Root.LogicalBufferViews[_bufferView];

            BufferView.VerifyAccess(validate, bv, _byteOffset ?? _byteOffsetDefault, (DimensionType.SCALAR, _componentType.ToComponent()), count);
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

        internal Memory.MemoryAccessor _GetMemoryAccessor(ROOT root, int sparseCount, Accessor baseAccessor)
        {
            var view = root.LogicalBufferViews[this._bufferView];
            var info = new Memory.MemoryAccessInfo(null, this._byteOffset ?? 0, sparseCount, view.ByteStride, baseAccessor.Format);
            return new Memory.MemoryAccessor(view.Content, info);
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate
                .NonNegative("ByteOffset", _byteOffset)
                .IsNullOrIndex("BufferView", _bufferView, validate.Root.LogicalBufferViews);
        }

        internal void ValidateValues(ValidationContext validate, int count)
        {
            var bv = validate.Root.LogicalBufferViews[_bufferView];

            // we need the accessor's settings to properly check this.
            // result.CheckAccess(bv, _byteOffset, DimensionType.SCALAR, _componentType, count);
        }

        #endregion
    }
}
