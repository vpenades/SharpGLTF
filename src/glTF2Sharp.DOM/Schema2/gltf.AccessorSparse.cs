using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Schema2
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

    public partial class AccessorSparse
    {
        internal AccessorSparse() { }

        internal AccessorSparse(BufferView indices, int indicesOffset, IndexType indicesEncoding, BufferView values, int valuesOffset, int count)
        {
            Guard.NotNull(indices, nameof(indices));
            Guard.NotNull(values, nameof(values));
            Guard.MustBeGreaterThanOrEqualTo(count, _countMinimum, nameof(count));

            this._count = count;
            this._indices = new AccessorSparseIndices(indices, indicesOffset, indicesEncoding);
            this._values = new AccessorSparseValues(values, valuesOffset);
        }

        public int Count => _count; // what is this!?? TODO: check with specs

        public Memory.SparseArray<Single> GetScalarArray(Accessor baseAccessor)
        {
            var bot = baseAccessor.AsScalarArray(false);
            var top = this._values.GetScalarArray(baseAccessor.LogicalParent, _count, baseAccessor.Encoding, baseAccessor.Normalized);
            var idx = this._indices.GetIndicesArray(baseAccessor.LogicalParent, _count);

            return new Memory.SparseArray<Single>(bot, top, idx);
        }

        public Memory.SparseArray<Vector2> GetVector2Array(Accessor baseAccessor)
        {
            var bot = baseAccessor.AsVector2Array(false);
            var top = this._values.GetVector2Array(baseAccessor.LogicalParent, _count, baseAccessor.Encoding, baseAccessor.Normalized);
            var idx = this._indices.GetIndicesArray(baseAccessor.LogicalParent, _count);

            return new Memory.SparseArray<Vector2>(bot, top, idx);
        }

        public Memory.SparseArray<Vector3> GetVector3Array(Accessor baseAccessor)
        {
            var bot = baseAccessor.AsVector3Array(false);
            var top = this._values.GetVector3Array(baseAccessor.LogicalParent, _count, baseAccessor.Encoding, baseAccessor.Normalized);
            var idx = this._indices.GetIndicesArray(baseAccessor.LogicalParent, _count);

            return new Memory.SparseArray<Vector3>(bot, top, idx);
        }

        public Memory.SparseArray<Vector4> GetVector4Array(Accessor baseAccessor)
        {
            var bot = baseAccessor.AsVector4Array(false);
            var top = this._values.GetVector4Array(baseAccessor.LogicalParent, _count, baseAccessor.Encoding, baseAccessor.Normalized);
            var idx = this._indices.GetIndicesArray(baseAccessor.LogicalParent, _count);

            return new Memory.SparseArray<Vector4>(bot, top, idx);
        }
    }

    public partial class AccessorSparseIndices
    {
        internal AccessorSparseIndices() { }

        internal AccessorSparseIndices(BufferView bv, int byteOffset, IndexType encoding)
        {
            Guard.NotNull(bv, nameof(bv));
            Guard.MustBeGreaterThanOrEqualTo(byteOffset, _byteOffsetMinimum, nameof(byteOffset));

            this._bufferView = bv.LogicalIndex;
            this._byteOffset = byteOffset.AsNullable(_byteOffsetDefault);
            this._componentType = encoding;
        }

        public Memory.IntegerArray GetIndicesArray(ROOT root, int count)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return srcBuffer.CreateIndicesArray(this._byteOffset ?? 0, count, this._componentType);
        }
    }

    public partial class AccessorSparseValues
    {
        internal AccessorSparseValues() { }

        internal AccessorSparseValues(BufferView bv, int byteOffset)
        {
            Guard.NotNull(bv, nameof(bv));
            Guard.MustBeGreaterThanOrEqualTo(byteOffset, _byteOffsetMinimum, nameof(byteOffset));

            this._bufferView = bv.LogicalIndex;
            this._byteOffset = byteOffset.AsNullable(_byteOffsetDefault);
        }

        public Memory.ScalarArray GetScalarArray(ROOT root, int count, ComponentType encoding, Boolean normalized)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return srcBuffer.CreateScalarArray(this._byteOffset ?? 0, count, encoding, normalized);
        }

        public Memory.Vector2Array GetVector2Array(ROOT root, int count, ComponentType encoding, Boolean normalized)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return srcBuffer.CreateVector2Array(this._byteOffset ?? 0, count, encoding, normalized);
        }

        public Memory.Vector3Array GetVector3Array(ROOT root, int count, ComponentType encoding, Boolean normalized)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return srcBuffer.CreateVector3Array(this._byteOffset ?? 0, count, encoding, normalized);
        }

        public Memory.Vector4Array GetVector4Array(ROOT root, int count, ComponentType encoding, Boolean normalized)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return srcBuffer.CreateVector4Array(this._byteOffset ?? 0, count, encoding, normalized);
        }
    }
}
