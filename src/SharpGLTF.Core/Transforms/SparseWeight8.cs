using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Transforms
{
    /// <summary>
    /// Represents a sparse collection of weight values, with a maximum of 8 weights.
    /// </summary>
    public struct SparseWeight8
    {
        #region lifecycle

        internal SparseWeight8(ReadOnlySpan<int> indices, ReadOnlySpan<float> weights)
        {
            this.Index0 = indices.Length > 0 ? indices[0] : 0;
            this.Index1 = indices.Length > 1 ? indices[1] : 0;
            this.Index2 = indices.Length > 2 ? indices[2] : 0;
            this.Index3 = indices.Length > 3 ? indices[3] : 0;
            this.Index4 = indices.Length > 4 ? indices[4] : 0;
            this.Index5 = indices.Length > 5 ? indices[5] : 0;
            this.Index6 = indices.Length > 6 ? indices[6] : 0;
            this.Index7 = indices.Length > 7 ? indices[7] : 0;

            this.Weight0 = weights.Length > 0 ? weights[0] : 0;
            this.Weight1 = weights.Length > 1 ? weights[1] : 0;
            this.Weight2 = weights.Length > 2 ? weights[2] : 0;
            this.Weight3 = weights.Length > 3 ? weights[3] : 0;
            this.Weight4 = weights.Length > 4 ? weights[4] : 0;
            this.Weight5 = weights.Length > 5 ? weights[5] : 0;
            this.Weight6 = weights.Length > 6 ? weights[6] : 0;
            this.Weight7 = weights.Length > 7 ? weights[7] : 0;
        }

        #endregion

        #region data

        public int Index0;
        public int Index1;
        public int Index2;
        public int Index3;
        public int Index4;
        public int Index5;
        public int Index6;
        public int Index7;

        public float Weight0;
        public float Weight1;
        public float Weight2;
        public float Weight3;
        public float Weight4;
        public float Weight5;
        public float Weight6;
        public float Weight7;

        #endregion

        #region properties

        public (int, int) IndexRange
        {
            get
            {
                int minRange = int.MaxValue;
                int maxRange = int.MinValue;

                foreach (var p in GetPairs())
                {
                    if (minRange > p.Item1) minRange = p.Item1;
                    if (maxRange < p.Item1) maxRange = p.Item1;
                }

                return (minRange, maxRange);
            }
        }

        #endregion

        #region API

        public static SparseWeight8 Lerp(SparseWeight8 a, SparseWeight8 b, float amount)
        {
            var invAmount = 1.0f - amount;

            Span<int> ai = stackalloc int[8];
            Span<float> aw = stackalloc float[8];
            Span<int> bi = stackalloc int[8];
            Span<float> bw = stackalloc float[8];

            ai = a.CopyTo(ai, aw);
            bi = a.CopyTo(bi, bw);

            Span<int> indices = stackalloc int[ai.Length + bi.Length];
            Span<float> weights = stackalloc float[ai.Length + bi.Length];

            // copy first batch
            var c = ai.Length;
            ai.CopyTo(indices);
            aw.Slice(0, c).CopyTo(weights);

            for (int i = 0; i < bi.Length; ++i)
            {
                var bidx = bi[i];
                var bwht = bw[i];

                var idx = indices
                    .Slice(0, c)
                    .IndexOf(bidx);

                if (idx < 0)
                {
                    indices[c] = bidx;
                    weights[c] = bwht;
                    ++c;
                }
                else
                {
                    weights[idx] = (weights[idx] * invAmount) + (bwht * amount);
                }
            }

            return new SparseWeight8(indices, weights);
        }

        private Span<int> CopyTo(Span<int> indices, Span<float> weights)
        {
            int count = 0;

            foreach (var pair in GetPairs())
            {
                var idx = indices
                    .Slice(0, count)
                    .IndexOf(pair.Item1);

                if (idx < 0)
                {
                    indices[count] = pair.Item1;
                    weights[count] = pair.Item2;
                    ++count;
                }
                else
                {
                    weights[idx] += pair.Item2;
                }
            }

            return indices.Slice(0, count);

        }

        private IEnumerable<(int, float)> GetPairs()
        {
            if (Weight0 > 0) yield return (Index0, Weight0);
            if (Weight1 > 0) yield return (Index1, Weight1);
            if (Weight2 > 0) yield return (Index2, Weight2);
            if (Weight3 > 0) yield return (Index3, Weight3);

            if (Weight4 > 0) yield return (Index4, Weight4);
            if (Weight5 > 0) yield return (Index5, Weight5);
            if (Weight6 > 0) yield return (Index6, Weight6);
            if (Weight7 > 0) yield return (Index7, Weight7);
        }

        private (int, float) GetPair(int idx)
        {
            switch (idx)
            {
                case 0: return (Index0, Weight0);
                case 1: return (Index1, Weight1);
                case 2: return (Index2, Weight2);
                case 3: return (Index3, Weight3);
                case 4: return (Index4, Weight4);
                case 5: return (Index5, Weight5);
                case 6: return (Index6, Weight6);
                case 7: return (Index7, Weight7);
                default: throw new ArgumentOutOfRangeException(nameof(idx));
            }
        }

        #endregion

    }
}
