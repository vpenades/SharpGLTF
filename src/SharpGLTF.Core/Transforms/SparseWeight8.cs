using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Transforms
{
    /// <summary>
    /// Represents a sparse collection of non zero weight values, with a maximum of 8 weights.
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
                .Where(item => item.val != 0)
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

        public static bool AreWeightsEqual(in SparseWeight8 x, in SparseWeight8 y)
        {
            Span<int>   indices = stackalloc int[16];
            Span<float> xWeights = stackalloc float[16];
            Span<float> yWeights = stackalloc float[16];

            int c = 0;
            c = CopyTo(x, indices, xWeights, c);
            c = CopyTo(y, indices, yWeights, c);

            xWeights = xWeights.Slice(0, c);
            yWeights = yWeights.Slice(0, c);

            return xWeights.SequenceEqual(yWeights);
        }

        public int GetWeightsHashCode()
        {
            Span<int> indices = stackalloc int[8];
            Span<float> weights = stackalloc float[8];

            var c = CopyTo(this, indices, weights, 0);

            BubbleSortByIndex(indices, weights, c);

            int h = 0;

            for (int i = 0; i < c; ++i)
            {
                h += indices[i].GetHashCode() ^ weights[i].GetHashCode();
                h *= 17;
            }

            return h;
        }

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

        public static SparseWeight8 Add(in SparseWeight8 x, in SparseWeight8 y)
        {
            return _OperateLinear(x, y, (xx, yy) => xx + yy);
        }

        public static SparseWeight8 Subtract(in SparseWeight8 x, in SparseWeight8 y)
        {
            return _OperateLinear(x, y, (xx, yy) => xx - yy);
        }

        public static SparseWeight8 Multiply(in SparseWeight8 x, in SparseWeight8 y)
        {
            return _OperateLinear(x, y, (xx, yy) => xx * yy);
        }

        public static SparseWeight8 Divide(in SparseWeight8 x, in SparseWeight8 y)
        {
            return _OperateLinear(x, y, (xx, yy) => xx / yy);
        }

        public static SparseWeight8 InterpolateLinear(SparseWeight8 x, SparseWeight8 y, float amount)
        {
            var xAmount = 1.0f - amount;
            var yAmount = amount;

            return _OperateLinear(x, y, (xx, yy) => (xx * xAmount) + (yy * yAmount));
        }

        public static SparseWeight8 InterpolateCubic(SparseWeight8 x, SparseWeight8 xt, SparseWeight8 y, SparseWeight8 yt, float amount)
        {
            var basis = Animations.SamplerFactory.CreateHermitePointWeights(amount);

            return _OperateCubic(x, xt, y, yt, (xx, xxt, yy, yyt) => (xx * basis.Item1) + (yy * basis.Item2) + (xxt * basis.Item3) + (yyt * basis.Item4));
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

        public override string ToString()
        {
            var sb = new StringBuilder();

            int c = this.Count;

            for (int i = 0; i < c; ++i)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(this[i]);
            }

            return sb.ToString();
        }

        #endregion

        #region code

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
                    weights[idx] = pair.Item2; // should perform ADD (in case there's more than one element?)
                }
            }

            return offset;
        }

        /// <summary>
        /// Performs <paramref name="operationFunc"/> over all the elements of the operands.
        /// </summary>
        /// <param name="x">The first <see cref="SparseWeight8"/> operand.</param>
        /// <param name="y">The second <see cref="SparseWeight8"/> operand.</param>
        /// <param name="operationFunc">The operator function to apply to every element.</param>
        /// <returns>A new <see cref="SparseWeight8"/>.</returns>
        private static SparseWeight8 _OperateLinear(in SparseWeight8 x, in SparseWeight8 y, Func<float, float, float> operationFunc)
        {
            // prepare buffers in the stack
            Span<int> indices = stackalloc int[16];
            Span<float> xxx = stackalloc float[16];
            Span<float> yyy = stackalloc float[16];

            // fill the buffers so all the elements are aligned by column
            int offset = 0;
            offset = CopyTo(x, indices, xxx, offset);
            offset = CopyTo(y, indices, yyy, offset);

            // perform operation element by element
            for (int i = 0; i < offset; ++i)
            {
                xxx[i] = operationFunc(xxx[i], yyy[i]);
            }
            
            // sort results by relevance, so they can fit
            // in a new structure in case there's more than
            // 8 results

            indices = indices.Slice(0, offset);
            xxx = xxx.Slice(0, offset);

            BubbleSortByWeight(indices, xxx, offset);

            return new SparseWeight8(indices, xxx);
        }

        /// <summary>
        /// Performs <paramref name="operationFunc"/> over all the elements of the operands.
        /// </summary>
        /// <param name="x">The first <see cref="SparseWeight8"/> operand.</param>
        /// <param name="y">The second <see cref="SparseWeight8"/> operand.</param>
        /// <param name="z">The third <see cref="SparseWeight8"/> operand.</param>
        /// <param name="w">The fourth <see cref="SparseWeight8"/> operand.</param>
        /// <param name="operationFunc">The operator function to apply to every element.</param>
        /// <returns>A new <see cref="SparseWeight8"/>.</returns>
        private static SparseWeight8 _OperateCubic
            (
            in SparseWeight8 x,
            in SparseWeight8 y,
            in SparseWeight8 z,
            in SparseWeight8 w,
            Func<float, float, float, float, float> operationFunc
            )
        {
            // prepare buffers in the stack
            Span<int> indices = stackalloc int[16];
            Span<float> xxx = stackalloc float[16];
            Span<float> yyy = stackalloc float[16];
            Span<float> zzz = stackalloc float[16];
            Span<float> www = stackalloc float[16];

            // fill the buffers so all the elements are aligned by column
            int offset = 0;
            offset = CopyTo(x, indices, xxx, offset);
            offset = CopyTo(y, indices, yyy, offset);
            offset = CopyTo(z, indices, zzz, offset);
            offset = CopyTo(w, indices, www, offset);

            // perform operation element by element
            for (int i = 0; i < offset; ++i)
            {
                xxx[i] = operationFunc(xxx[i], yyy[i], zzz[i], www[i]);
            }

            // sort results by relevance, so they can fit
            // in a new structure in case there's more than
            // 8 results

            indices = indices.Slice(0, offset);
            xxx = xxx.Slice(0, offset);

            BubbleSortByWeight(indices, xxx, offset);

            return new SparseWeight8(indices, xxx);
        }

        private static void BubbleSortByWeight(Span<int> indices, Span<float> weights, int count = int.MaxValue)
        {
            System.Diagnostics.Debug.Assert(indices.Length == weights.Length);

            count = Math.Min(indices.Length, count);

            for (int i = 0; i < count - 1; ++i)
            {
                for (int j = i + 1; j < count; ++j)
                {
                    if (weights[j - 1] > weights[j]) continue;

                    if (weights[j - 1] == weights[j] && indices[j - 1] < indices[j]) continue;

                    var index = indices[j - 1];
                    indices[j - 1] = indices[j];
                    indices[j] = index;

                    var weight = weights[j - 1];
                    weights[j - 1] = weights[j];
                    weights[j] = weight;
                }
            }
        }

        private static void BubbleSortByIndex(Span<int> indices, Span<float> weights, int count = int.MaxValue)
        {
            System.Diagnostics.Debug.Assert(indices.Length == weights.Length);

            count = Math.Min(indices.Length, count);

            for (int i = 0; i < count - 1; ++i)
            {
                for (int j = i + 1; j < count; ++j)
                {
                    if (indices[j - 1] < indices[j]) continue;

                    if (indices[j - 1] == indices[j] && weights[j - 1] > weights[j]) continue;

                    var index = indices[j - 1];
                    indices[j - 1] = indices[j];
                    indices[j] = index;

                    var weight = weights[j - 1];
                    weights[j - 1] = weights[j];
                    weights[j] = weight;
                }
            }
        }

        internal IEnumerable<(int, float)> GetPairs()
        {
            if (Weight0 != 0) yield return (Index0, Weight0);
            if (Weight1 != 0) yield return (Index1, Weight1);
            if (Weight2 != 0) yield return (Index2, Weight2);
            if (Weight3 != 0) yield return (Index3, Weight3);
            if (Weight4 != 0) yield return (Index4, Weight4);
            if (Weight5 != 0) yield return (Index5, Weight5);
            if (Weight6 != 0) yield return (Index6, Weight6);
            if (Weight7 != 0) yield return (Index7, Weight7);
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
            if (Weight0 != 0 && c <= Index0) c = Index0 + 1;
            if (Weight1 != 0 && c <= Index1) c = Index1 + 1;
            if (Weight2 != 0 && c <= Index2) c = Index2 + 1;
            if (Weight3 != 0 && c <= Index3) c = Index3 + 1;
            if (Weight4 != 0 && c <= Index4) c = Index4 + 1;
            if (Weight5 != 0 && c <= Index5) c = Index5 + 1;
            if (Weight6 != 0 && c <= Index6) c = Index6 + 1;
            if (Weight7 != 0 && c <= Index7) c = Index7 + 1;

            return c;
        }

        #endregion
    }
}
