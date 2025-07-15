using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using SharpGLTF.Memory;

using VALIDATIONCTX = SharpGLTF.Validation.ValidationContext;

namespace SharpGLTF.Schema2
{
    public sealed partial class Accessor
    {
        #region Data Arrays

        public IAccessorArray<Matrix3x2> AsMatrix2x2Array()
        {
            return _TryGetMemoryAccessor(out var mem)
                ? mem.AsMatrix2x2Array()
                : new ZeroAccessorArray<Matrix3x2>(this._count);
        }

        public IAccessorArray<Matrix4x4> AsMatrix3x3Array()
        {
            return _TryGetMemoryAccessor(out var mem)
                ? mem.AsMatrix3x3Array()
                : new ZeroAccessorArray<Matrix4x4>(this._count);
        }

        public IAccessorArray<Matrix4x4> AsMatrix4x3Array()
        {
            /*
            const int dimsize = 4 * 3;

            var view = SourceBufferView;
            var stride = Math.Max(dimsize * this.Encoding.ByteLength(), view.ByteStride);
            var content = view.Content.Slice(this.ByteOffset, Count * stride);

            return new Matrix4x3Array(content, stride, this.Encoding, this.Normalized);
            */

            return _TryGetMemoryAccessor(out var mem)
                ? mem.AsMatrix4x3Array()
                : new ZeroAccessorArray<Matrix4x4>(this._count);
        }

        public IAccessorArray<Matrix4x4> AsMatrix4x4Array()
        {
            return _TryGetMemoryAccessor(out var mem)
                ? mem.AsArrayOf<Matrix4x4>()
                : new ZeroAccessorArray<Matrix4x4>(this._count);
        }

        [Obsolete("Use AsMatrix4x4Array instead", true)]
        internal IReadOnlyList<Matrix4x4> AsMatrix4x4ReadOnlyList()
        {
            return _TryGetMemoryAccessor(out var mem)
                ? mem.AsArrayOf<Matrix4x4>()
                : new ZeroAccessorArray<Matrix4x4>(this._count);
        }

        public IAccessorArray<Quaternion> AsQuaternionArray()
        {
            if (_TryGetMemoryAccessor(out var memory))
            {
                if (this._sparse == null) return memory.AsQuaternionArray();

                throw new NotImplementedException();
            }
            else
            {
                if (this._sparse == null) return new ZeroAccessorArray<Quaternion>(this._count);

                throw new NotImplementedException();
            }
        }

        public IAccessorArray<Single[]> AsMultiArray(int dimensions)
        {
            if (_TryGetMemoryAccessor(out var memory))
            {
                if (this._sparse == null) return memory.AsMultiArray(dimensions);

                throw new NotImplementedException();
            }
            else
            {
                // if (this._sparse == null) return new ZeroAccessorArray<Single[]>(this._count);

                throw new NotImplementedException();
            }
        }

        #endregion

        #region Index Buffer Arrays
        

        public IAccessorArray<UInt32> AsIndicesArray()
        {
            Guard.IsFalse(this.IsSparse, nameof(IsSparse));
            Guard.IsTrue(this.Dimensions == DimensionType.SCALAR, nameof(Dimensions));

            return _TryGetMemoryAccessor(out var mem)
                ? new IntegerArray(SourceBufferView.Content, this.ByteOffset, this._count, this.Encoding.ToIndex())
                : new ZeroAccessorArray<UInt32>(this._count);
        }

        #endregion

        #region Vertex Buffer Arrays

        public IAccessorArray<Single> AsScalarArray() => AsArrayOf<Single>();

        public IAccessorArray<Vector2> AsVector2Array() => AsArrayOf<Vector2>();

        public IAccessorArray<Vector3> AsVector3Array() => AsArrayOf<Vector3>();

        public IAccessorArray<Vector4> AsVector4Array() => AsArrayOf<Vector4>();

        public IAccessorArray<T> AsArrayOf<T>()
            where T:unmanaged
        {
            if (_TryGetMemoryAccessor(out var memory))
            {
                if (this._sparse == null) return memory.AsArrayOf<T>();

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateSparseArray<T>(memory, sparseKV.Key, sparseKV.Value);
            }
            else
            {
                if (this._sparse == null) return new ZeroAccessorArray<T>(this._count);

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateSparseArray<T>(this._count, sparseKV.Key, sparseKV.Value);
            }
        }        

        public IAccessorArray<Vector4> AsColorArray(Single defaultW = 1)
        {
            if (_TryGetMemoryAccessor(out var memory))
            {

                if (this._sparse == null) return memory.AsColorArray(defaultW);

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateColorSparseArray(memory, sparseKV.Key, sparseKV.Value, defaultW);
            }
            else
            {
                if (this._sparse == null) return new ZeroAccessorArray<Vector4>(this._count);

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateColorSparseArray(this._count, sparseKV.Key, sparseKV.Value);
            }
        }        

        public ArraySegment<Byte> TryGetVertexBytes(int vertexIdx)
        {
            if (_sparse != null) throw new InvalidOperationException("Can't be used on Acessors with Sparse Data");

            if (!this.TryGetBufferView(out var bufferView)) return default;

            var itemByteSz = Encoding.ByteLength() * Dimensions.DimCount();
            var byteStride = Math.Max(itemByteSz, bufferView.ByteStride);
            var byteOffset = vertexIdx * byteStride;

            return bufferView.Content.Slice(this.ByteOffset + (vertexIdx * byteStride), itemByteSz);
        }

        #endregion        
    }
}
