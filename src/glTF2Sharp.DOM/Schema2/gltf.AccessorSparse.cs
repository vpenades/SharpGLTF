using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Schema2
{
    using ROOT = ModelRoot;

    public partial class AccessorSparse
    {
        public void CopyTo(Accessor srcAccessor, Byte[] dstBuffer, int dstStride)
        {
            if (this._count == 0) return;

            var idxDecoder = this._indices.GetDecoder(srcAccessor.LogicalParent);
            var valCopier = this._values.CopyTo(srcAccessor.LogicalParent, srcAccessor.Dimensions, srcAccessor.Encoding);

            for (int i = 0; i < this._count; ++i)
            {
                var key = idxDecoder.Invoke(i);

                valCopier(i, dstBuffer, dstStride, key);
            }
        }

        public void CopyTo(Accessor srcAccessor, Vector4[] dstBuffer)
        {
            if (this._count == 0) return;

            var idxDecoder = this._indices.GetDecoder(srcAccessor.LogicalParent);
            var valDecoder = this._values.GetDecoder(srcAccessor.LogicalParent, srcAccessor.Encoding, srcAccessor.Dimensions, srcAccessor.Normalized);

            for (int i = 0; i < this._count; ++i)
            {
                var key = idxDecoder.Invoke(i);
                var val = valDecoder[i];
                dstBuffer[key] = val;
            }
        }
    }

    public partial class AccessorSparseIndices
    {
        public Func<int, int> GetDecoder(ROOT root)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];

            var accessor = srcBuffer.CreateIndicesAccessor(this._byteOffset ?? 0, this._componentType);

            return index => (int)accessor[index];
        }

        public IReadOnlyList<int> GetIndices(ROOT root, int count)
        {
            var srcDecoder = GetDecoder(root);

            var indices = new int[count];

            for (int i = 0; i < indices.Length; ++i)
            {
                indices[i] = srcDecoder(i);
            }

            return indices;
        }
    }

    public partial class AccessorSparseValues
    {
        public Memory.IAccessor<Vector4> GetDecoder(ROOT root, ComponentType encoding, ElementType dimensions, Boolean normalized)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];

            return srcBuffer.CreateVertexAccessor(this._byteOffset ?? 0, dimensions, encoding, normalized);
        }

        public Action<int, IList<Byte>, int, int> CopyTo(ROOT root, ElementType et, ComponentType ct)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];

            var itemLen = et.DimCount() * ct.ByteLength();
            var srcStride = Math.Max(srcBuffer.ByteStride, itemLen);

            return (srcIdx, dstBuff, dstStride, dstIdx) =>
            {
                srcBuffer.Data.CopyTo(srcStride * srcIdx, dstBuff, dstStride * dstIdx, itemLen);
            };
        }
    }
}
