using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Transforms
{
    [System.Diagnostics.DebuggerDisplay("{Index} = {Weight}")]
    readonly struct IndexWeight
    {
        #region constructor

        public IndexWeight((int Index, float Weight) pair)
        {
            Index = pair.Index;
            Weight = pair.Weight;
        }

        public static implicit operator IndexWeight((int Index, float Weight) pair) { return new IndexWeight(pair.Index, pair.Weight); }

        public IndexWeight(int i, float w)
        {
            Index = i;
            Weight = w;
        }

        #endregion

        #region data

        public readonly int Index;
        public readonly float Weight;

        #endregion

        #region API

        public static IndexWeight operator +(IndexWeight a, IndexWeight b)
        {
            System.Diagnostics.Debug.Assert(a.Index == b.Index);
            return new IndexWeight(a.Index, a.Weight + b.Weight);
        }

        public static int IndexOf(Span<IndexWeight> span, int index)
        {
            for (int i = 0; i < span.Length; ++i)
            {
                if (span[i].Index == index) return i;
            }

            return -1;
        }

        private static IndexWeight GetIndexedWeight(in SparseWeight8 src, int offset)
        {
            switch (offset)
            {
                case 0: return new IndexWeight(src.Index0, src.Weight0);
                case 1: return new IndexWeight(src.Index1, src.Weight1);
                case 2: return new IndexWeight(src.Index2, src.Weight2);
                case 3: return new IndexWeight(src.Index3, src.Weight3);
                case 4: return new IndexWeight(src.Index4, src.Weight4);
                case 5: return new IndexWeight(src.Index5, src.Weight5);
                case 6: return new IndexWeight(src.Index6, src.Weight6);
                case 7: return new IndexWeight(src.Index7, src.Weight7);
                default: throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }

        public static int CopyTo(in SparseWeight8 src, Span<IndexWeight> dst)
        {
            System.Diagnostics.Debug.Assert(dst.Length >= 8);

            var offset = 0;

            for (int i = 0; i < 8; ++i)
            {
                var pair = GetIndexedWeight(src, i);
                if (pair.Weight == 0) continue;

                var idx = IndexOf(dst.Slice(0, offset), pair.Index);

                if (idx < 0)
                {
                    // the index doesn't exist, insert it.
                    dst[offset++] = pair;
                }
                else
                {
                    // the index already exists, so we aggregate the weights
                    dst[idx] += pair;
                }
            }

            return offset;
        }

        public static int CopyTo(in SparseWeight8 src, Span<int> dstIndices, Span<float> dstWeights, int dstLength)
        {
            System.Diagnostics.Debug.Assert(dstIndices.Length == dstWeights.Length);
            System.Diagnostics.Debug.Assert(dstIndices.Length >= dstLength, $"{nameof(dstIndices)}.Length must be at least {nameof(dstLength)}");
            System.Diagnostics.Debug.Assert(dstWeights.Length >= dstLength, $"{nameof(dstWeights)}.Length must be at least {nameof(dstLength)}");
            System.Diagnostics.Debug.Assert(dstWeights.Slice(0, dstLength).ToArray().All(item => item == 0), "All weights must be zero");

            for (int i = 0; i < 8; ++i)
            {
                var pair = GetIndexedWeight(src, i);
                if (pair.Weight == 0) continue;

                var idx = dstIndices
                    .Slice(0, dstLength)
                    .IndexOf(pair.Index);

                if (idx < 0)
                {
                    // the index doesn't exist, insert it.
                    dstIndices[dstLength] = pair.Index;
                    dstWeights[dstLength] = pair.Weight;
                    ++dstLength;
                }
                else
                {
                    // the index already exists, so we aggregate the weights
                    dstWeights[idx] += pair.Weight;
                }
            }

            return dstLength;
        }

        public static void BubbleSortByWeight(Span<IndexWeight> pairs)
        {
            for (int i = 0; i < pairs.Length - 1; ++i)
            {
                bool sorted = true;

                for (int j = 1; j < pairs.Length; ++j)
                {
                    var k = j - 1;

                    var kk = pairs[k];
                    var jj = pairs[j];

                    var kw = Math.Abs(kk.Weight);
                    var jw = Math.Abs(jj.Weight);

                    if (kw  > jw) continue;
                    if (kw == jw && kk.Index < jj.Index) continue;

                    pairs[k] = jj;
                    pairs[j] = kk;

                    sorted = false;
                }

                if (sorted) return;
            }
        }

        public static void BubbleSortByIndex(Span<IndexWeight> pairs)
        {
            for (int i = 0; i < pairs.Length - 1; ++i)
            {
                bool sorted = true;

                for (int j = 1; j < pairs.Length; ++j)
                {
                    var k = j - 1;

                    var kk = pairs[k];
                    var jj = pairs[j];

                    if (kk.Index  < jj.Index) continue;
                    if (kk.Index == jj.Index && kk.Weight > jj.Weight) continue;

                    pairs[k] = jj;
                    pairs[j] = kk;

                    sorted = false;
                }

                if (sorted) return;
            }
        }

        #endregion
    }
}
