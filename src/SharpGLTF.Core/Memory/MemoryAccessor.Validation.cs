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

        public static void ValidateWeightsSum(Validation.ValidationContext result, MemoryAccessor weights0, MemoryAccessor weights1)
        {
            int idx = 0;

            if (weights1 == null)
            {
                if (weights0 == null) return;

                foreach (var item in weights0.GetItemsAsRawBytes())
                {
                    if (!_CheckWeightSum(item, weights0.Attribute.Encoding))
                    {
                        result.AddDataError($"Weight Sum invalid at Index {idx}");
                    }

                    ++idx;
                }

                return;
            }

            if (weights0 == null)
            {
                result.AddLinkError("");
                return;
            }

            var len = weights0.Attribute.ItemByteLength;
            Span<Byte> dst = stackalloc byte[len * 2];

            var zip = weights0.GetItemsAsRawBytes().Zip(weights1.GetItemsAsRawBytes(), (a, b) => (a, b));

            foreach (var (a, b) in zip)
            {
                a.AsSpan().CopyTo(dst);
                b.AsSpan().CopyTo(dst.Slice(len));

                if (!_CheckWeightSum(dst, weights0.Attribute.Encoding))
                {
                    result.AddDataError($"Weight Sum invalid at Index {idx}");
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
    }
}
