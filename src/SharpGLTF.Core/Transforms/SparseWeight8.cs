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
    public readonly struct SparseWeight8 : IReadOnlyList<float>
    {
        #region constructors

        /// <summary>
        /// Creates a new <see cref="SparseWeight8"/> from a weights collection.
        /// If there's more than 8 non zero values, the 8 most representative values are taken.
        /// </summary>
        /// <param name="weights">A sequence of weight values.</param>
        /// <returns>A <see cref="SparseWeight8"/> instance.</returns>
        public static SparseWeight8 Create(params float[] weights)
        {
            return Create((IEnumerable<float>)weights);
        }

        /// <summary>
        /// Creates a new <see cref="SparseWeight8"/> from a weights collection.
        /// If there's more than 8 non zero values, the 8 most representative values are taken
        /// </summary>
        /// <param name="weights">A sequence of weight values.</param>
        /// <returns>A <see cref="SparseWeight8"/> instance.</returns>
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

        /// <summary>
        /// Creates a new <see cref="SparseWeight8"/> from an indexed weight collection.
        /// If there's more than 8 non zero values, the 8 most representative values are taken
        /// </summary>
        /// <param name="pairs">A sequence of indexed weight values.</param>
        /// <returns>A <see cref="SparseWeight8"/> instance.</returns>
        public static SparseWeight8 Create(params (int, float)[] pairs)
        {
            if (pairs == null) return default;

            Span<IndexWeight> sparse = stackalloc IndexWeight[pairs.Length];

            for (int i = 0; i < pairs.Length; ++i)
            {
                sparse[i] = pairs[i];
            }

            if (pairs.Length > 8)
            {
                IndexWeight.BubbleSortByWeight(sparse);
                sparse = sparse.Slice(0, 8);
            }

            return new SparseWeight8(sparse);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SparseWeight8"/> struct.
        /// </summary>
        /// <param name="idx0123">The indices of weights 0 to 3.</param>
        /// <param name="wgt0123">The weights of indices 0 to 3.</param>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SparseWeight8"/> struct.
        /// </summary>
        /// <param name="idx0123">The indices of weights 0 to 3.</param>
        /// <param name="idx4567">The indices of weights 4 to 7.</param>
        /// <param name="wgt0123">The weights of indices 0 to 3.</param>
        /// <param name="wgt4567">The weights of indices 4 to 7.</param>
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

        private SparseWeight8(ReadOnlySpan<IndexWeight> pairs)
        {
            System.Diagnostics.Debug.Assert(pairs.Length <= 8, nameof(pairs));

            this = default;

            if (pairs.Length < 1) return;
            this.Index0 = pairs[0].Index;
            this.Weight0 = pairs[0].Weight;

            if (pairs.Length < 2) return;
            this.Index1 = pairs[1].Index;
            this.Weight1 = pairs[1].Weight;

            if (pairs.Length < 3) return;
            this.Index2 = pairs[2].Index;
            this.Weight2 = pairs[2].Weight;

            if (pairs.Length < 4) return;
            this.Index3 = pairs[3].Index;
            this.Weight3 = pairs[3].Weight;

            if (pairs.Length < 5) return;
            this.Index4 = pairs[4].Index;
            this.Weight4 = pairs[4].Weight;

            if (pairs.Length < 6) return;
            this.Index5 = pairs[5].Index;
            this.Weight5 = pairs[5].Weight;

            if (pairs.Length < 7) return;
            this.Index6 = pairs[6].Index;
            this.Weight6 = pairs[6].Weight;

            if (pairs.Length < 8) return;
            this.Index7 = pairs[7].Index;
            this.Weight7 = pairs[7].Weight;
        }

        private SparseWeight8(in SparseWeight8 sparse, float scale)
        {
            Index0 = sparse.Index0;
            Index1 = sparse.Index1;
            Index2 = sparse.Index2;
            Index3 = sparse.Index3;
            Index4 = sparse.Index4;
            Index5 = sparse.Index5;
            Index6 = sparse.Index6;
            Index7 = sparse.Index7;
            Weight0 = sparse.Weight0 * scale;
            Weight1 = sparse.Weight1 * scale;
            Weight2 = sparse.Weight2 * scale;
            Weight3 = sparse.Weight3 * scale;
            Weight4 = sparse.Weight4 * scale;
            Weight5 = sparse.Weight5 * scale;
            Weight6 = sparse.Weight6 * scale;
            Weight7 = sparse.Weight7 * scale;
        }

        #endregion

        #region data

        public readonly int Index0;
        public readonly int Index1;
        public readonly int Index2;
        public readonly int Index3;
        public readonly int Index4;
        public readonly int Index5;
        public readonly int Index6;
        public readonly int Index7;

        public readonly float Weight0;
        public readonly float Weight1;
        public readonly float Weight2;
        public readonly float Weight3;
        public readonly float Weight4;
        public readonly float Weight5;
        public readonly float Weight6;
        public readonly float Weight7;

        public static bool AreWeightsEqual(in SparseWeight8 x, in SparseWeight8 y)
        {
            const int STACKSIZE = 8 * 2;

            Span<int>   indices = stackalloc int[STACKSIZE];
            Span<float> xWeights = stackalloc float[STACKSIZE];
            Span<float> yWeights = stackalloc float[STACKSIZE];

            int offset = 0;
            offset = IndexWeight.CopyTo(x, indices, xWeights, offset);
            offset = IndexWeight.CopyTo(y, indices, yWeights, offset);

            xWeights = xWeights.Slice(0, offset);
            yWeights = yWeights.Slice(0, offset);

            return xWeights.SequenceEqual(yWeights);
        }

        public int GetWeightsHashCode()
        {
            Span<IndexWeight> iw = stackalloc IndexWeight[8];

            var c = IndexWeight.CopyTo(this, iw);

            iw = iw.Slice(0, c);

            IndexWeight.BubbleSortByIndex(iw);

            int h = 0;

            for (int i = 0; i < iw.Length; ++i)
            {
                h += iw[i].GetHashCode();
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

        /// <summary>
        /// Returns a copy of this <see cref="SparseWeight8"/> where all the
        /// indices have been reordered by weight in descending order.
        /// </summary>
        /// <param name="sparse">The <see cref="SparseWeight8"/> to get ordered.</param>
        /// <returns>A weight ordered <see cref="SparseWeight8"/>.</returns>
        public static SparseWeight8 OrderedByWeight(in SparseWeight8 sparse)
        {
            Span<IndexWeight> iw = stackalloc IndexWeight[8];

            var c = IndexWeight.CopyTo(sparse, iw);

            iw = iw.Slice(0, c);

            IndexWeight.BubbleSortByWeight(iw);

            return new SparseWeight8(iw);
        }

        /// <summary>
        /// Returns a copy of this <see cref="SparseWeight8"/> where all the
        /// indices have been reordered by index in ascending order.
        /// </summary>
        /// <param name="sparse">The <see cref="SparseWeight8"/> to get ordered.</param>
        /// <returns>An index ordered <see cref="SparseWeight8"/>.</returns>
        public static SparseWeight8 OrderedByIndex(in SparseWeight8 sparse)
        {
            Span<IndexWeight> iw = stackalloc IndexWeight[8];

            var c = IndexWeight.CopyTo(sparse, iw);

            iw = iw.Slice(0, c);

            IndexWeight.BubbleSortByIndex(iw);

            return new SparseWeight8(iw);
        }

        /// <summary>
        /// Adds <paramref name="x"/> with <paramref name="y"/> element wise.
        /// If there's more than 8 non zero result values, the 8 most representative values are taken.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>A new <see cref="SparseWeight8"/></returns>
        public static SparseWeight8 Add(in SparseWeight8 x, in SparseWeight8 y)
        {
            return _OperateLinear(x, y, (xx, yy) => xx + yy);
        }

        /// <summary>
        /// Subtracts <paramref name="y"/> from <paramref name="x"/> element wise.
        /// If there's more than 8 non zero result values, the 8 most representative values are taken.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>A new <see cref="SparseWeight8"/></returns>
        public static SparseWeight8 Subtract(in SparseWeight8 x, in SparseWeight8 y)
        {
            return _OperateLinear(x, y, (xx, yy) => xx - yy);
        }

        /// <summary>
        /// Multiplies <paramref name="x"/> with <paramref name="y"/> element wise.
        /// If there's more than 8 non zero result values, the 8 most representative values are taken.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>A new <see cref="SparseWeight8"/></returns>
        public static SparseWeight8 Multiply(in SparseWeight8 x, in SparseWeight8 y)
        {
            return _OperateLinear(x, y, (xx, yy) => xx * yy);
        }

        /// <summary>
        /// Multiplies <paramref name="x"/> with <paramref name="y"/> element wise.
        /// If there's more than 8 non zero result values, the 8 most representative values are taken.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>A new <see cref="SparseWeight8"/></returns>
        public static SparseWeight8 Multiply(in SparseWeight8 x, Single y)
        {
            return new SparseWeight8(x, y);
        }

        /// <summary>
        /// Interpolates Linearly <paramref name="x"/> with <paramref name="y"/> an <paramref name="amount"/>.
        /// If there's more than 8 non zero result values, the 8 most representative values are taken.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <param name="amount">The amount of <paramref name="y"/></param>
        /// <returns>A new <see cref="SparseWeight8"/></returns>
        public static SparseWeight8 InterpolateLinear(in SparseWeight8 x, in SparseWeight8 y, float amount)
        {
            var xAmount = 1.0f - amount;
            var yAmount = amount;

            return _OperateLinear(x, y, (xx, yy) => (xx * xAmount) + (yy * yAmount));
        }

        /// <summary>
        /// Interpolates (<paramref name="x"/> , <paramref name="xt"/>) with (<paramref name="y"/> , <paramref name="yt"/>) an <paramref name="amount"/>.
        /// If there's more than 8 non zero result values, the 8 most representative values are taken.
        /// </summary>
        /// <param name="x">The first value operand.</param>
        /// <param name="xt">The first tangent operand.</param>
        /// <param name="y">The second value operand.</param>
        /// <param name="yt">The second tangent operand.</param>
        /// <param name="amount">The amount of <paramref name="y"/></param>
        /// <returns>A new <see cref="SparseWeight8"/></returns>
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
        /// Performs <paramref name="operationFunc"/> over all the elements of the operands.
        /// </summary>
        /// <param name="x">The first <see cref="SparseWeight8"/> operand.</param>
        /// <param name="y">The second <see cref="SparseWeight8"/> operand.</param>
        /// <param name="operationFunc">The operator function to apply to every element.</param>
        /// <returns>A new <see cref="SparseWeight8"/>.</returns>
        private static SparseWeight8 _OperateLinear(in SparseWeight8 x, in SparseWeight8 y, Func<float, float, float> operationFunc)
        {
            const int STACKSIZE = 8 * 2;

            // prepare buffers in the stack
            Span<int> indices = stackalloc int[STACKSIZE];
            Span<float> xxx = stackalloc float[STACKSIZE];
            Span<float> yyy = stackalloc float[STACKSIZE];

            // fill the buffers so all the elements are aligned by column
            int offset = 0;
            offset = IndexWeight.CopyTo(x, indices, xxx, offset);
            offset = IndexWeight.CopyTo(y, indices, yyy, offset);

            // perform operation element by element

            int r = 0;
            Span<IndexWeight> rrr = stackalloc IndexWeight[STACKSIZE];

            for (int i = 0; i < offset; ++i)
            {
                var ww = operationFunc(xxx[i], yyy[i]);

                if (ww == 0) continue;

                rrr[r++] = new IndexWeight(indices[i], ww);
            }

            rrr = rrr.Slice(0, r);

            if (rrr.Length > 8)
            {
                IndexWeight.BubbleSortByWeight(rrr);
                rrr = rrr.Slice(0, 8);
            }

            return new SparseWeight8(rrr);
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
            const int STACKSIZE = 8 * 4;

            // prepare buffers in the stack
            Span<int> indices = stackalloc int[STACKSIZE];
            Span<float> xxx = stackalloc float[STACKSIZE];
            Span<float> yyy = stackalloc float[STACKSIZE];
            Span<float> zzz = stackalloc float[STACKSIZE];
            Span<float> www = stackalloc float[STACKSIZE];

            // fill the buffers so all the elements are aligned by column
            int offset = 0;
            offset = IndexWeight.CopyTo(x, indices, xxx, offset);
            offset = IndexWeight.CopyTo(y, indices, yyy, offset);
            offset = IndexWeight.CopyTo(z, indices, zzz, offset);
            offset = IndexWeight.CopyTo(w, indices, www, offset);

            // perform operation element by element

            int r = 0;
            Span<IndexWeight> rrr = stackalloc IndexWeight[STACKSIZE];

            for (int i = 0; i < offset; ++i)
            {
                var ww = operationFunc(xxx[i], yyy[i], zzz[i], www[i]);

                if (ww == 0) continue;

                rrr[r++] = new IndexWeight(indices[i], ww);
            }

            rrr = rrr.Slice(0, r);

            if (rrr.Length > 8)
            {
                IndexWeight.BubbleSortByWeight(rrr);
                rrr = rrr.Slice(0, 8);
            }

            return new SparseWeight8(rrr);
        }

        internal IEnumerable<(int, float)> GetNonZeroWeights()
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

        /// <summary>
        /// Normalizes the current <see cref="SparseWeight8"/> by adding a complement weight
        /// at index 0 that resolves <see cref="WeightSum"/> to 1.
        /// </summary>
        /// <returns>A new <see cref="SparseWeight8"/> with an extra weight.</returns>
        internal SparseWeight8 GetNormalizedWithComplement()
        {
            Span<IndexWeight> weights = stackalloc IndexWeight[8 + 1];

            var offset = IndexWeight.CopyTo(this, weights);

            float ww = 0;

            for (int i = 0; i < offset; ++i)
            {
                var w = weights[i].Weight;

                ww += w;

                weights[i] = new IndexWeight(weights[i].Index + 1, w);
            }

            weights[offset++] = new IndexWeight(0, 1 - ww);

            weights = weights.Slice(0, offset);

            if (offset > 8)
            {
                IndexWeight.BubbleSortByWeight(weights);
                weights = weights.Slice(0, 8);
            }

            return new SparseWeight8(weights);
        }

        #endregion
    }
}
