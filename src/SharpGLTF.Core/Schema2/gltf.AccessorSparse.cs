using System.Collections.Generic;

namespace SharpGLTF.Schema2
{
    // TODO:
    // AccessorSparse is a single child of an Accessor
    // when an Accessor defines an AccessorSparse, it becomes a "two layers" collection.
    // the layer at the bottom is the data of the base accessor
    // the layer on top, replaces just a few elements in the base accessor.
    // Unlike many other objects in the API, AccessorSparse does not implement IChildOf<Accessor>
    // which would allow AccessorSparse to access the interal data of its parent.
    // So we have two choices here:
    // 1- Implement IChildOf<Accessor> and finish the SingleChild<T> collection object
    // 2- Make these classes non public, and expose them with a public view object.
    // 3- Make the whole Accessor+AccessorSparse block non public, and expose them with a public view.

    using ROOT = ModelRoot;

    public sealed partial class AccessorSparse
    {
        internal AccessorSparse() { }

        /// <inheritdoc />
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

        public int Count => _count;

        internal KeyValuePair<Memory.IntegerArray, Memory.MemoryAccessor> _CreateMemoryAccessors(Accessor baseAccessor)
        {
            var key = this._indices._GetIndicesArray(baseAccessor.LogicalParent, _count);
            var val = this._values._GetMemoryAccessor(baseAccessor.LogicalParent, _count, baseAccessor);

            return new KeyValuePair<Memory.IntegerArray, Memory.MemoryAccessor>(key, val);
        }
    }

    public sealed partial class AccessorSparseIndices
    {
        internal AccessorSparseIndices() { }

        internal AccessorSparseIndices(BufferView bv, int byteOffset, IndexEncodingType encoding)
        {
            Guard.NotNull(bv, nameof(bv));
            Guard.MustBeGreaterThanOrEqualTo(byteOffset, _byteOffsetMinimum, nameof(byteOffset));

            this._bufferView = bv.LogicalIndex;
            this._byteOffset = byteOffset.AsNullable(_byteOffsetDefault);
            this._componentType = encoding;
        }

        internal Memory.IntegerArray _GetIndicesArray(ROOT root, int count)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return new Memory.IntegerArray(srcBuffer.Content, this._byteOffset ?? 0, count, this._componentType);
        }
    }

    public sealed partial class AccessorSparseValues
    {
        internal AccessorSparseValues() { }

        internal AccessorSparseValues(BufferView bv, int byteOffset)
        {
            Guard.NotNull(bv, nameof(bv));
            Guard.MustBeGreaterThanOrEqualTo(byteOffset, _byteOffsetMinimum, nameof(byteOffset));

            this._bufferView = bv.LogicalIndex;
            this._byteOffset = byteOffset.AsNullable(_byteOffsetDefault);
        }

        internal Memory.MemoryAccessor _GetMemoryAccessor(ROOT root, int count, Accessor baseAccessor)
        {
            var view = root.LogicalBufferViews[this._bufferView];
            var info = new Memory.MemoryAccessInfo(null, this._byteOffset ?? 0, count, view.ByteStride, baseAccessor.Dimensions, baseAccessor.Encoding, baseAccessor.Normalized);
            return new Memory.MemoryAccessor(view.Content, info);
        }
    }
}
