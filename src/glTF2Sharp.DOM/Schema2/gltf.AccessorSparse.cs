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
            Guard.MustBeGreaterThanOrEqualTo(count, _countMinimum,nameof(count));

            this._count = count;
            this._indices = new AccessorSparseIndices(indices, indicesOffset, indicesEncoding);
            this._values = new AccessorSparseValues(values, valuesOffset);
        }

        public int Count => _count; // what is this!?? TODO: check with specs        

        public Memory.SparseAccessor<Single> GetScalarAccesor(Accessor baseAccessor)
        {
            var bot = baseAccessor.CastToScalarAccessor();
            var top = this._values.CastToScalarAccessor(baseAccessor.LogicalParent, baseAccessor.Encoding, baseAccessor.Normalized);
            var idx = this._indices.CastToIndicesAccessor(baseAccessor.LogicalParent);

            return new Memory.SparseAccessor<Single>(bot, top, idx);
        }

        public Memory.SparseAccessor<Vector2> GetVector2Accesor(Accessor baseAccessor)
        {
            var bot = baseAccessor.CastToVector2Accessor();
            var top = this._values.CastToVector2Accessor(baseAccessor.LogicalParent, baseAccessor.Encoding, baseAccessor.Normalized);
            var idx = this._indices.CastToIndicesAccessor(baseAccessor.LogicalParent);

            return new Memory.SparseAccessor<Vector2>(bot, top, idx);
        }

        public Memory.SparseAccessor<Vector3> GetVector3Accesor(Accessor baseAccessor)
        {
            var bot = baseAccessor.CastToVector3Accessor();
            var top = this._values.CastToVector3Accessor(baseAccessor.LogicalParent, baseAccessor.Encoding, baseAccessor.Normalized);
            var idx = this._indices.CastToIndicesAccessor(baseAccessor.LogicalParent);

            return new Memory.SparseAccessor<Vector3>(bot, top, idx);
        }

        public Memory.SparseAccessor<Vector4> GetVector4Accesor(Accessor baseAccessor)
        {
            var bot = baseAccessor.CastToVector4Accessor();
            var top = this._values.CastToVector4Accessor(baseAccessor.LogicalParent, baseAccessor.Encoding, baseAccessor.Normalized);
            var idx = this._indices.CastToIndicesAccessor(baseAccessor.LogicalParent);

            return new Memory.SparseAccessor<Vector4>(bot, top, idx);
        }
    }

    public partial class AccessorSparseIndices
    {
        internal AccessorSparseIndices() { }

        internal AccessorSparseIndices(BufferView bv, int byteOffset, IndexType encoding)
        {
            Guard.NotNull(bv,nameof(bv));
            Guard.MustBeGreaterThanOrEqualTo(byteOffset, _byteOffsetMinimum, nameof(byteOffset));

            this._bufferView = bv.LogicalIndex;
            this._byteOffset = byteOffset.AsNullable(_byteOffsetDefault);
            this._componentType = encoding;
        }

        public Memory.IntegerAccessor CastToIndicesAccessor(ROOT root)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return srcBuffer.CreateIndicesAccessor(this._byteOffset ?? 0, this._componentType);
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

        public Memory.ScalarAccessor CastToScalarAccessor(ROOT root, ComponentType encoding, Boolean normalized)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return srcBuffer.CreateScalarAccessor(this._byteOffset ?? 0, encoding, normalized);
        }

        public Memory.Vector2Accessor CastToVector2Accessor(ROOT root, ComponentType encoding, Boolean normalized)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return srcBuffer.CreateVector2Accessor(this._byteOffset ?? 0, encoding, normalized);
        }

        public Memory.Vector3Accessor CastToVector3Accessor(ROOT root, ComponentType encoding, Boolean normalized)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return srcBuffer.CreateVector3Accessor(this._byteOffset ?? 0, encoding, normalized);
        }

        public Memory.Vector4Accessor CastToVector4Accessor(ROOT root, ComponentType encoding, Boolean normalized)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];
            return srcBuffer.CreateVector4Accessor(this._byteOffset ?? 0, encoding, normalized);
        }
    }
}
