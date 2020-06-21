using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DIMENSIONS = SharpGLTF.Schema2.DimensionType;
using ENCODING = SharpGLTF.Schema2.EncodingType;

namespace SharpGLTF.Memory
{
    partial class MemoryAccessor
    {
        #region helpers

        public static void SanitizeVertexAttributes(MemoryAccessor[] vertexAccessors)
        {
            System.Diagnostics.Debug.Assert(vertexAccessors.All(item => !string.IsNullOrWhiteSpace(item.Attribute.Name)), nameof(vertexAccessors));

            // https://github.com/KhronosGroup/glTF/pull/1749

            var weights0 = vertexAccessors.FirstOrDefault(item => item.Attribute.Name == "WEIGHTS_0");
            var weights1 = vertexAccessors.FirstOrDefault(item => item.Attribute.Name == "WEIGHTS_1");
            SanitizeWeightsSum(weights0, weights1);
        }

        public static bool HaveOverlappingBuffers(MemoryAccessor a, MemoryAccessor b)
        {
            Guard.NotNull(a, nameof(a));
            Guard.NotNull(b, nameof(b));

            var aa = a._GetBytes();
            var bb = b._GetBytes();

            if (aa.Array != bb.Array) return false;

            if (aa.Offset >= bb.Offset + bb.Count) return false;
            if (bb.Offset >= aa.Offset + aa.Count) return false;

            return true;
        }

        public static bool HaveOverlappingBuffers(IEnumerable<MemoryAccessor> abc)
        {
            var items = abc.ToList();

            for (int i = 0; i < items.Count - 1; ++i)
            {
                for (int j = i + 1; j < items.Count; ++j)
                {
                    if (HaveOverlappingBuffers(items[i], items[j])) return true;
                }
            }

            return false;
        }

        #endregion

        #region sanitize weights sum

        public static void SanitizeWeightsSum(MemoryAccessor weights0, MemoryAccessor weights1)
        {
            if (weights1 == null)
            {
                if (weights0 == null) return;

                foreach (var item in weights0.GetItemsAsRawBytes())
                {
                    _SanitizeWeightSum(item, weights0.Attribute.Encoding);
                }

                return;
            }

            if (weights0 == null) return;

            var len = weights0.Attribute.ItemByteLength;
            Span<Byte> dst = stackalloc byte[len * 2];

            var zip = weights0.GetItemsAsRawBytes().Zip(weights1.GetItemsAsRawBytes(), (a, b) => (a, b));

            foreach (var (a, b) in zip)
            {
                a.AsSpan().CopyTo(dst);
                b.AsSpan().CopyTo(dst.Slice(len));

                if (_SanitizeWeightSum(dst, weights0.Attribute.Encoding))
                {
                    dst.Slice(0, len).CopyTo(a);
                    dst.Slice(len, len).CopyTo(b);
                }
            }
        }

        private static bool _SanitizeWeightSum(Span<byte> dst, ENCODING encoding)
        {
            if (encoding == ENCODING.UNSIGNED_BYTE)
            {
                var weights = dst;

                int r = 0;
                for (int j = 0; j < weights.Length; ++j) r += weights[j];
                if (r == 255) return false;

                weights[0] += (Byte)(255 - r);

                return true;
            }

            if (encoding == ENCODING.UNSIGNED_SHORT)
            {
                var weights = System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, UInt16>(dst);

                int r = 0;
                for (int j = 0; j < weights.Length; ++j) r += weights[j];

                if (r == 65535) return false;

                weights[0] += (Byte)(65535 - r);

                return true;
            }

            if (encoding == ENCODING.FLOAT)
            {
                var weights = System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, Single>(dst);

                float nonZero = 0;
                float sum = 0;

                for (int j = 0; j < weights.Length; ++j)
                {
                    var w = weights[j];

                    if (float.IsNaN(w)) return false;
                    if (w < 0 || w > 1) return false;

                    if (w > 0)
                    {
                        sum += w;
                        nonZero += 1;
                    }
                }

                float err = 2e-7f * nonZero;

                if (Math.Abs(sum - 1) <= err) return false;

                for (int j = 0; j < weights.Length; ++j)
                {
                    weights[j] /= sum;
                }

                return true;
            }

            return false;
        }

        #endregion

        #region validate weights sum

        public static void VerifyWeightsSum(MemoryAccessor weights0, MemoryAccessor weights1)
        {
            int idx = 0;

            if (weights1 == null)
            {
                if (weights0 == null) return;

                foreach (var item in weights0.GetItemsAsRawBytes())
                {
                    if (!_CheckWeightSum(item, weights0.Attribute.Encoding))
                    {
                        throw new ArgumentException($"Weight Sum invalid at Index {idx}", nameof(weights0));
                    }

                    ++idx;
                }

                return;
            }

            if (weights0 == null) throw new ArgumentNullException(nameof(weights0));

            var len = weights0.Attribute.ItemByteLength;
            Span<Byte> dst = stackalloc byte[len * 2];

            var zip = weights0.GetItemsAsRawBytes().Zip(weights1.GetItemsAsRawBytes(), (a, b) => (a, b));

            foreach (var (a, b) in zip)
            {
                a.AsSpan().CopyTo(dst);
                b.AsSpan().CopyTo(dst.Slice(len));

                if (!_CheckWeightSum(dst, weights0.Attribute.Encoding))
                {
                    throw new ArgumentException($"Weight Sum invalid at Index {idx}", nameof(weights1));
                }

                ++idx;
            }
        }

        private static bool _CheckWeightSum(Span<byte> dst, ENCODING encoding)
        {
            if (encoding == ENCODING.UNSIGNED_BYTE)
            {
                var weights = dst;

                int r = 0;
                for (int j = 0; j < weights.Length; ++j) r += weights[j];
                return r == 255;
            }

            if (encoding == ENCODING.UNSIGNED_SHORT)
            {
                var weights = System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, UInt16>(dst);

                int r = 0;
                for (int j = 0; j < weights.Length; ++j) r += weights[j];
                return r == 65535;
            }

            if (encoding == ENCODING.FLOAT)
            {
                var weights = System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, Single>(dst);

                float nonZero = 0;
                float sum = 0;

                for (int j = 0; j < weights.Length; ++j)
                {
                    var w = weights[j];

                    if (float.IsNaN(w)) return false;
                    if (w < 0 || w > 1) return false;

                    if (w > 0)
                    {
                        sum += w;
                        nonZero += 1;
                    }
                }

                float err = 2e-7f * nonZero;

                return Math.Abs(sum - 1) <= err;
            }

            return false;
        }

        #endregion

        #region bounds validation

        public static void VerifyAccessorBounds(MemoryAccessor memory, IReadOnlyList<double> min, IReadOnlyList<double> max)
        {
            Guard.NotNull(memory, nameof(memory));
            Guard.NotNull(min, nameof(min));
            Guard.NotNull(max, nameof(max));

            if (min.Count == 0 && max.Count == 0) return;

            var dimensions = memory.Attribute.Dimensions.DimCount();

            if (min.Count != dimensions) throw new ArgumentException($"min size mismatch; expected {dimensions} but found {min.Count}", nameof(min));
            if (max.Count != dimensions) throw new ArgumentException($"max size mismatch; expected {dimensions} but found {max.Count}", nameof(max));

            for (int i = 0; i < min.Count; ++i)
            {
                // if (_min[i] > _max[i]) result.AddError(this, $"min[{i}] is larger than max[{i}]");
            }

            var minimum = min.Select(item => (float)item).ToArray();
            var maximum = max.Select(item => (float)item).ToArray();

            var xinfo = memory.Attribute;
            xinfo.Dimensions = DIMENSIONS.SCALAR;
            memory = new MemoryAccessor(memory.Data, xinfo);

            var array = new MultiArray(memory.Data, memory.Attribute.ByteOffset, memory.Attribute.ItemsCount, memory.Attribute.ByteStride, dimensions, memory.Attribute.Encoding, memory.Attribute.Normalized);

            var current = new float[dimensions];

            for (int i = 0; i < array.Count; ++i)
            {
                array.CopyItemTo(i, current);

                for (int j = 0; j < current.Length; ++j)
                {
                    var v = current[j];

                    // if (!v._IsFinite()) result.AddError(this, $"Item[{j}][{i}] is not a finite number: {v}");

                    var axisMin = minimum[j];
                    var axisMax = maximum[j];

                    if (v < axisMin || v > axisMax) throw new ArgumentOutOfRangeException(nameof(memory), $"Value[{i}] is out of bounds. {axisMin} <= {v} <= {axisMax}");

                    // if (v < min || v > max) result.AddError(this, $"Item[{j}][{i}] is out of bounds. {min} <= {v} <= {max}");
                }
            }
        }

        #endregion

        #region vertex index validation

        public static void VerifyVertexIndices(MemoryAccessor memory, uint vertexCount)
        {
            Guard.NotNull(memory, nameof(memory));

            uint restart_value = 0xff;
            if (memory.Attribute.Encoding == ENCODING.UNSIGNED_SHORT) restart_value = 0xffff;
            if (memory.Attribute.Encoding == ENCODING.UNSIGNED_INT) restart_value = 0xffffffff;

            memory.AsIntegerArray();

            var indices = memory.AsIntegerArray();

            for (int i = 0; i < indices.Count; ++i)
            {
                var idx = indices[i];

                if (idx >= vertexCount) throw new ArgumentException($"Value[{i}] is out of bounds {vertexCount}.", nameof(memory));
                if (idx == restart_value) throw new ArgumentException($"Value[{i}] is restart value.", nameof(memory));
            }
        }

        #endregion
    }
}
