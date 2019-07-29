using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Transforms
{
    /// <summary>
    /// Represents a sparse collection of non zero weight values, with a maximum of 8 weights.
    /// </summary>
    /// <remarks>
    /// <see cref="SparseWeight8"/> is being used in two different contexts:
    /// - As an utility class to define per vertex joint weights in mesh skinning.
    /// - As an animation key in morph targets; a mesh can have many morph targets, but realistically and due to GPU limitations, only up to 8 morph targets can be blended at the same time.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("[{Index0}]={Weight0}  [{Index1}]={Weight1}  [{Index2}]={Weight2}  [{Index3}]={Weight3}  [{Index4}]={Weight4}  [{Index5}]={Weight5}  [{Index6}]={Weight6}  [{Index7}]={Weight7}")]
    public struct SparseWeight8 : IReadOnlyList<float>
    {
        #region lifecycle

        public static SparseWeight8 Create(params float[] weights)
        {
            return Create((IEnumerable<float>)weights);
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

            return Create(indexedWeights);
        }

        public static SparseWeight8 Create(params (int, float)[] pairs)
        {
            if (pairs == null) return default;

            Span<int> indices = stackalloc int[pairs.Length];
            Span<float> weights = stackalloc float[pairs.Length];

            for (int i = 0; i < pairs.Length; ++i)
            {
                indices[i] = pairs[i].Item1;
                weights[i] = pairs[i].Item2;
            }

            if (pairs.Length > 8)
            {
                BubbleSortByWeight(indices, weights, indices.Length);
                indices = indices.Slice(0, 8);
                weights = weights.Slice(0, 8);
            }

            return new SparseWeight8(indices, weights);
        }

        public SparseWeight8(in Vector4 idx0123, in Vector4 wgt0123)
        {
            Index0 = (int)idx0123.X;
            Index1 = (int)idx0123.Y;
            Index2 = (int)idx0123.Z;
            Index3 = (int)idx0123.W;

            Index4 = 0;
            Index5 = 0;
            Index6 = 0;
            Index7 = 0;

            Weight0 = wgt0123.X;
            Weight1 = wgt0123.Y;
            Weight2 = wgt0123.Z;
            Weight3 = wgt0123.W;

            Weight4 = 0;
            Weight5 = 0;
            Weight6 = 0;
            Weight7 = 0;
        }

        public SparseWeight8(in Vector4 idx0123, in Vector4 idx4567, in Vector4 wgt0123, in Vector4 wgt4567)
        {
            Index0 = (int)idx0123.X;
            Index1 = (int)idx0123.Y;
            Index2 = (int)idx0123.Z;
            Index3 = (int)idx0123.W;

            Index4 = (int)idx4567.X;
            Index5 = (int)idx4567.Y;
            Index6 = (int)idx4567.Z;
            Index7 = (int)idx4567.W;

            Weight0 = wgt0123.X;
            Weight1 = wgt0123.Y;
            Weight2 = wgt0123.Z;
            Weight3 = wgt0123.W;

            Weight4 = wgt4567.X;
            Weight5 = wgt4567.Y;
            Weight6 = wgt4567.Z;
            Weight7 = wgt4567.W;
        }

        private SparseWeight8(ReadOnlySpan<int> indices, ReadOnlySpan<float> weights)
        {
            System.Diagnostics.Debug.Assert(indices.Length <= 8, nameof(indices));
            System.Diagnostics.Debug.Assert(indices.Length == weights.Length, nameof(weights));

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

        /// <summary>
        /// Gets a value indicating whether all the weights in this <see cref="SparseWeight8"/> are zero.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public bool IsWeightless => Weight0 == 0 & Weight1 == 0 & Weight2 == 0 & Weight3 == 0 & Weight4 == 0 & Weight5 == 0 & Weight6 == 0 & Weight7 == 0;

        public float WeightSum => Weight0 + Weight1+ Weight2 + Weight3 + Weight4 + Weight5 + Weight6 + Weight7;

        #endregion

        #region API

        public static SparseWeight8 OrderedByWeight(in SparseWeight8 sparse)
        {
            Span<int> indices = stackalloc int[8];
            Span<float> weights = stackalloc float[8];

            var c = CopyTo(sparse, indices, weights, 0);
            BubbleSortByWeight(indices, weights, c);

            indices = indices.Slice(0, c);
            weights = weights.Slice(0, c);

            return new SparseWeight8(indices, weights);
        }

        public static SparseWeight8 OrderedByIndex(in SparseWeight8 sparse)
        {
            Span<int> indices = stackalloc int[8];
            Span<float> weights = stackalloc float[8];

            var c = CopyTo(sparse, indices, weights, 0);
            BubbleSortByIndex(indices, weights, c);

            indices = indices.Slice(0, c);
            weights = weights.Slice(0, c);

            return new SparseWeight8(indices, weights);
        }

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

        public static SparseWeight8 Multiply(in SparseWeight8 x, Single y)
        {
            return new SparseWeight8
            {
                Index0 = x.Index0,
                Index1 = x.Index1,
                Index2 = x.Index2,
                Index3 = x.Index3,
                Index4 = x.Index4,
                Index5 = x.Index5,
                Index6 = x.Index6,
                Index7 = x.Index7,
                Weight0 = x.Weight0 * y,
                Weight1 = x.Weight1 * y,
                Weight2 = x.Weight2 * y,
                Weight3 = x.Weight3 * y,
                Weight4 = x.Weight4 * y,
                Weight5 = x.Weight5 * y,
                Weight6 = x.Weight6 * y,
                Weight7 = x.Weight7 * y
            };
        }

        public static SparseWeight8 Divide(in SparseWeight8 x, in SparseWeight8 y)
        {
            return _OperateLinear(x, y, (xx, yy) => xx / yy);
        }

        public static SparseWeight8 InterpolateLinear(in SparseWeight8 x, in SparseWeight8 y, float amount)
        {
            var xAmount = 1.0f - amount;
            var yAmount = amount;

            return _OperateLinear(x, y, (xx, yy) => (xx * xAmount) + (yy * yAmount));
        }

        public static SparseWeight8 InterpolateCubic(in SparseWeight8 x, in SparseWeight8 xt, in SparseWeight8 y, in SparseWeight8 yt, float amount)
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
            return GetSparseWeights().Select(item => item.Item1).Contains(key);
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

        /// <summary>
        /// Copies the current index-weight pairs to available slots in destination <paramref name="dstIndices"/> and <paramref name="dstWeights"/>
        /// </summary>
        /// <param name="src">The source <see cref="SparseWeight8"/>.</param>
        /// <param name="dstIndices">The destination index array.</param>
        /// <param name="dstWeights">The destination weight array.</param>
        /// <param name="dstLength">The explicit length of both <paramref name="dstIndices"/> and <paramref name="dstWeights"/></param>
        /// <returns>The new length of the destination arrays.</returns>
        private static int CopyTo(in SparseWeight8 src, Span<int> dstIndices, Span<float> dstWeights, int dstLength)
        {
            System.Diagnostics.Debug.Assert(dstIndices.Length == dstWeights.Length);
            System.Diagnostics.Debug.Assert(dstIndices.Length >= dstLength, $"{nameof(dstIndices)}.Length must be at least {nameof(dstLength)}");
            System.Diagnostics.Debug.Assert(dstWeights.Length >= dstLength, $"{nameof(dstWeights)}.Length must be at least {nameof(dstLength)}");
            System.Diagnostics.Debug.Assert(dstWeights.Slice(0, dstLength).ToArray().All(item => item == 0), "All weights must be zero");

            for (int i = 0; i < 8; ++i)
            {
                var pair = src.GetSparseWeight(i);
                if (pair.Item2 == 0) continue;
                var idx = dstIndices
                    .Slice(0, dstLength)
                    .IndexOf(pair.Item1);

                if (idx < 0)
                {
                    // the index doesn't exist, insert it to the list.
                    dstIndices[dstLength] = pair.Item1;
                    dstWeights[dstLength] = pair.Item2;
                    ++dstLength;
                }
                else
                {
                    // the index already exists, so we aggregate the weights
                    dstWeights[idx] += pair.Item2;
                }
            }

            return dstLength;
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
            Span<int> indices = stackalloc int[32];
            Span<float> xxx = stackalloc float[32];
            Span<float> yyy = stackalloc float[32];
            Span<float> zzz = stackalloc float[32];
            Span<float> www = stackalloc float[32];

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
                    if (weights[i] > weights[j]) continue;

                    if (weights[i] == weights[j] && indices[i] < indices[j]) continue;

                    var index = indices[i];
                    indices[i] = indices[j];
                    indices[j] = index;

                    var weight = weights[i];
                    weights[i] = weights[j];
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
                    if (indices[i] < indices[j]) continue;

                    if (indices[i] == indices[j] && weights[i] > weights[j]) continue;

                    var index = indices[i];
                    indices[i] = indices[j];
                    indices[j] = index;

                    var weight = weights[i];
                    weights[i] = weights[j];
                    weights[j] = weight;
                }
            }
        }

        internal IEnumerable<(int, float)> GetSparseWeights()
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

        private (int, float) GetSparseWeight(int sparseIndex)
        {
            switch (sparseIndex)
            {
                case 0: return (Index0, Weight0);
                case 1: return (Index1, Weight1);
                case 2: return (Index2, Weight2);
                case 3: return (Index3, Weight3);
                case 4: return (Index4, Weight4);
                case 5: return (Index5, Weight5);
                case 6: return (Index6, Weight6);
                case 7: return (Index7, Weight7);
                default: throw new ArgumentOutOfRangeException(nameof(sparseIndex));
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
