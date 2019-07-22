using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Transforms
{
    /// <summary>
    /// Represents a sparse collection of weight values, with a maximum of 8 weights.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("[{Index0}]={Weight0}  [{Index1}]={Weight1}  [{Index2}]={Weight2}  [{Index3}]={Weight3}  [{Index4}]={Weight4}  [{Index5}]={Weight5}  [{Index6}]={Weight6}  [{Index7}]={Weight7}")]
    public struct SparseWeight8 : IReadOnlyList<float>, IReadOnlyDictionary<int, float>
    {
        #region lifecycle

        public static SparseWeight8 Create(params float[] weights)
        {
            return Create(weights as IEnumerable<float>);
        }

        public static SparseWeight8 Create(IEnumerable<float> weights)
        {
            if (weights == null) return default;

            var indexedWeights = weights
                .Select((val, idx) => (idx, val))
                .Where(item => item.val > 0)
                .OrderByDescending(item => item.val)
                .Take(8)
                .ToArray();

            return new SparseWeight8(indexedWeights);
        }

        public SparseWeight8(params (int, float)[] items)
        {
            Guard.NotNull(items, nameof(items));

            this.Index0 = items.Length > 0 ? items[0].Item1 : 0;
            this.Index1 = items.Length > 1 ? items[1].Item1 : 0;
            this.Index2 = items.Length > 2 ? items[2].Item1 : 0;
            this.Index3 = items.Length > 3 ? items[3].Item1 : 0;
            this.Index4 = items.Length > 4 ? items[4].Item1 : 0;
            this.Index5 = items.Length > 5 ? items[5].Item1 : 0;
            this.Index6 = items.Length > 6 ? items[6].Item1 : 0;
            this.Index7 = items.Length > 7 ? items[7].Item1 : 0;

            this.Weight0 = items.Length > 0 ? items[0].Item2 : 0;
            this.Weight1 = items.Length > 1 ? items[1].Item2 : 0;
            this.Weight2 = items.Length > 2 ? items[2].Item2 : 0;
            this.Weight3 = items.Length > 3 ? items[3].Item2 : 0;
            this.Weight4 = items.Length > 4 ? items[4].Item2 : 0;
            this.Weight5 = items.Length > 5 ? items[5].Item2 : 0;
            this.Weight6 = items.Length > 6 ? items[6].Item2 : 0;
            this.Weight7 = items.Length > 7 ? items[7].Item2 : 0;
        }

        private SparseWeight8(ReadOnlySpan<int> indices, ReadOnlySpan<float> weights)
        {
            System.Diagnostics.Debug.Assert(indices.Length == weights.Length);

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

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => GetExpandedCount();

        public float this[int index] => GetExpandedAt(index);

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool IsZero => Weight0 == 0 & Weight1 == 0 & Weight2 == 0 & Weight3 == 0 & Weight4 == 0 & Weight5 == 0 & Weight6 == 0 & Weight7 == 0;

        public IEnumerable<int> Keys => GetPairs().Select(item => item.Item1);

        public IEnumerable<float> Values => GetPairs().Select(item => item.Item2);

        #endregion

        #region API

        public static SparseWeight8 InterpolateLinear(SparseWeight8 x, SparseWeight8 y, float amount)
        {
            Span<int> indices = stackalloc int[16];
            Span<float> xWeights = stackalloc float[16];
            Span<float> yWeights = stackalloc float[16];

            int offset = 0;
            offset = CopyTo(x, indices, xWeights, offset);
            offset = CopyTo(y, indices, yWeights, offset);

            indices = indices.Slice(0, offset);
            xWeights = xWeights.Slice(0, offset);
            yWeights = yWeights.Slice(0, offset);

            var invAmount = 1.0f - amount;

            for (int i = 0; i < indices.Length; ++i)
            {
                xWeights[i] = (xWeights[i] * invAmount) + (yWeights[i] * amount);
            }

            BubbleSort(indices, xWeights);

            return new SparseWeight8(indices, xWeights);
        }

        public static SparseWeight8 InterpolateCubic(SparseWeight8 x, SparseWeight8 xt, SparseWeight8 y, SparseWeight8 yt, float amount)
        {
            Span<int> indices = stackalloc int[16];
            Span<float> xWeights = stackalloc float[16];
            Span<float> xTangent = stackalloc float[16];
            Span<float> yWeights = stackalloc float[16];
            Span<float> yTangent = stackalloc float[16];

            int offset = 0;
            offset = CopyTo(x, xt, indices, xWeights, xTangent, offset);
            offset = CopyTo(y, yt, indices, yWeights, yTangent, offset);

            indices = indices.Slice(0, offset);
            xWeights = xWeights.Slice(0, offset);
            xTangent = xWeights.Slice(0, offset);
            yWeights = yWeights.Slice(0, offset);
            yTangent = yTangent.Slice(0, offset);

            var basis = Animations.SamplerFactory.CreateHermitePointWeights(amount);

            for (int i = 0; i < indices.Length; ++i)
            {
                xWeights[i]
                    = (xWeights[i] * basis.Item1)
                    + (yWeights[i] * basis.Item2)
                    + (xTangent[i] * basis.Item3)
                    + (yTangent[i] * basis.Item4);
            }

            BubbleSort(indices, xWeights);

            return new SparseWeight8(indices, xWeights);
        }

        private static int CopyTo(SparseWeight8 p, SparseWeight8 t, Span<int> indices, Span<float> weights, Span<float> tangent, int offset)
        {
            System.Diagnostics.Debug.Assert(indices.Length == weights.Length);
            System.Diagnostics.Debug.Assert(indices.Length == tangent.Length);

            for (int i = 0; i < 8; ++i)
            {
                var pair = p.GetPair(i);
                if (pair.Item2 == 0) continue;
                var idx = indices.Slice(0, offset).IndexOf(pair.Item1);

                if (idx < 0)
                {
                    indices[offset] = pair.Item1;
                    weights[offset] = pair.Item2;
                    tangent[offset] = t[pair.Item1];
                    ++offset;
                }
            }

            return offset;
        }

        private static int CopyTo(SparseWeight8 p, Span<int> indices, Span<float> weights, int offset)
        {
            System.Diagnostics.Debug.Assert(indices.Length == weights.Length);

            for (int i = 0; i < 8; ++i)
            {
                var pair = p.GetPair(i);
                if (pair.Item2 == 0) continue;
                var idx = indices
                    .Slice(0, offset)
                    .IndexOf(pair.Item1);

                if (idx < 0)
                {
                    indices[offset] = pair.Item1;
                    weights[offset] = pair.Item2;
                    ++offset;
                }
                else
                {
                    weights[idx] = pair.Item2;
                }
            }

            return offset;
        }

        private static void BubbleSort(Span<int> indices, Span<float> weights)
        {
            System.Diagnostics.Debug.Assert(indices.Length == weights.Length);

            for (int i = 0; i < weights.Length - 1; ++i)
            {
                for (int j = i + 1; j < weights.Length; ++j)
                {
                    if (weights[j - 1] < weights[j])
                    {
                        var index = indices[j - 1];
                        indices[j - 1] = indices[j];
                        indices[j] = index;

                        var weight = weights[j - 1];
                        weights[j - 1] = weights[j];
                        weights[j] = weight;
                    }
                }
            }
        }

        internal IEnumerable<(int, float)> GetPairs()
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

        private float GetExpandedAt(int idx)
        {
            if (idx == Index0) return Weight0;
            if (idx == Index1) return Weight1;
            if (idx == Index2) return Weight2;
            if (idx == Index3) return Weight3;
            if (idx == Index4) return Weight4;
            if (idx == Index5) return Weight5;
            if (idx == Index6) return Weight6;
            if (idx == Index7) return Weight7;
            return 0;
        }

        private int GetExpandedCount()
        {
            var c = 0;
            if (Weight0 > 0 && c <= Index0) c = Index0 + 1;
            if (Weight1 > 0 && c <= Index1) c = Index1 + 1;
            if (Weight2 > 0 && c <= Index2) c = Index2 + 1;
            if (Weight3 > 0 && c <= Index3) c = Index3 + 1;
            if (Weight4 > 0 && c <= Index4) c = Index4 + 1;
            if (Weight5 > 0 && c <= Index5) c = Index5 + 1;
            if (Weight6 > 0 && c <= Index6) c = Index6 + 1;
            if (Weight7 > 0 && c <= Index7) c = Index7 + 1;

            return c;
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

        public IEnumerable<float> Expand(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                yield return GetExpandedAt(i);
            }
        }

        public IEnumerator<float> GetEnumerator() { return Expand(GetExpandedCount()).GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return Expand(GetExpandedCount()).GetEnumerator(); }

        public bool ContainsKey(int key)
        {
            return GetPairs().Select(item => item.Item1).Contains(key);
        }

        public bool TryGetValue(int key, out float value)
        {
            if (key == Index0) { value = Weight0; return true; }
            if (key == Index1) { value = Weight1; return true; }
            if (key == Index2) { value = Weight2; return true; }
            if (key == Index3) { value = Weight3; return true; }
            if (key == Index4) { value = Weight4; return true; }
            if (key == Index5) { value = Weight5; return true; }
            if (key == Index6) { value = Weight6; return true; }
            if (key == Index7) { value = Weight7; return true; }
            value = 0;
            return false;
        }

        IEnumerator<KeyValuePair<int, float>> IEnumerable<KeyValuePair<int, float>>.GetEnumerator()
        {
            return GetPairs().Select(item => new KeyValuePair<int, float>(item.Item1, item.Item2)).GetEnumerator();
        }

        #endregion

    }
}
