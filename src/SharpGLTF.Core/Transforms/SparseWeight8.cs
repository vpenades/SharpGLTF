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
    /// <see cref="SparseWeight8"/> is being used in two different contexts:<br/>
    /// <list type="bullet">
    /// <item>As an utility structure to define per vertex joint weights in mesh skinning.</item>
    /// <item>As an animation key in morph targets; a mesh can have many morph targets,
    /// but realistically and due to GPU limitations, only up to 8 morph targets can
    /// be blended at the same time.
    /// </item>
    /// </list>
    /// Constructors are designed so weightless values are not taken into account,<br/>
    /// and duplicated indices are merged, so indices are expected to be unique.
    /// <para>
    /// Use static Create* methods to construct instances of <see cref="SparseWeight8"/>.
    /// </para>
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly struct SparseWeight8 : IEquatable<SparseWeight8>
    {
        #region debug

        private string _GetDebuggerDisplay()
        {
            var iw = this.GetIndexedWeights()
                .Where(item => item.Weight != 0)
                .Select(item => $"[{item.Index}]={item.Weight}");

            var txt = string.Join(" ", iw);

            return string.IsNullOrWhiteSpace(txt) ? "Empty" : txt;
        }

        #endregion

        #region factory

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
        /// If there's more than 8 weighted values, the 8 heaviest values are taken.
        /// </summary>
        /// <param name="weights">A sequence of weight values.</param>
        /// <returns>A <see cref="SparseWeight8"/> instance.</returns>
        public static SparseWeight8 Create(IEnumerable<float> weights)
        {
            if (weights == null) return default;

            Span<IndexWeight> sparse = stackalloc IndexWeight[8];

            int index = 0;
            int count = 0;

            foreach (var w in weights)
            {
                if (w != 0) count = IndexWeight.InsertUnsorted(sparse, count, (index, w));
                index++;
            }

            return new SparseWeight8(sparse);
        }

        /// <summary>
        /// Creates a new <see cref="SparseWeight8"/> from an indexed weight collection.
        /// If there's more than 8 weighted values, the 8 heaviest values are taken.
        /// </summary>
        /// <param name="indexedWeights">A sequence of indexed weight pairs.</param>
        /// <returns>A <see cref="SparseWeight8"/> instance.</returns>
        public static SparseWeight8 Create(params (int Index, float Weight)[] indexedWeights)
        {
            return Create((IEnumerable<(int Index, float Weight)>)indexedWeights);
        }

        /// <summary>
        /// Creates a new <see cref="SparseWeight8"/> from an indexed weight collection.
        /// If there's more than 8 weighted values, the 8 heaviest values are taken.
        /// </summary>
        /// <param name="indexedWeights">A sequence of indexed weight pairs.</param>
        /// <returns>A <see cref="SparseWeight8"/> instance.</returns>
        public static SparseWeight8 Create(IEnumerable<(int Index, float Weight)> indexedWeights)
        {
            if (indexedWeights == null) return default;

            Span<IndexWeight> sparse = stackalloc IndexWeight[8];

            int count = 0;

            foreach (var iw in indexedWeights)
            {
                if (iw.Weight == 0) continue;
                count = IndexWeight.InsertUnsorted(sparse, count, iw);
            }

            return new SparseWeight8(sparse);
        }

        /// <summary>
        /// Creates a new <see cref="SparseWeight8"/> struct.
        /// </summary>
        /// <remarks>
        /// Repeating indices will have their weights merged.
        /// </remarks>
        /// <param name="idx0123">The indices of weights 0 to 3.</param>
        /// <param name="wgt0123">The weights of indices 0 to 3.</param>
        /// <returns>A <see cref="SparseWeight8"/> instance.</returns>
        public static SparseWeight8 Create(in Vector4 idx0123, in Vector4 wgt0123)
        {
            Span<IndexWeight> sparse = stackalloc IndexWeight[8];

            IndexWeight.InsertUnsorted(sparse, idx0123, wgt0123);

            return new SparseWeight8(sparse);
        }

        /// <summary>
        /// Creates a new <see cref="SparseWeight8"/> struct.
        /// </summary>
        /// <remarks>
        /// Repeating indices will have their weights merged.
        /// </remarks>
        /// <param name="idx0123">The first 4 indices.</param>
        /// <param name="idx4567">The next 4 indices.</param>
        /// <param name="wgt0123">The first 4 weights.</param>
        /// <param name="wgt4567">The next 4 weights.</param>
        /// <returns>A <see cref="SparseWeight8"/> instance.</returns>
        public static SparseWeight8 Create(in Vector4 idx0123, in Vector4 idx4567, in Vector4 wgt0123, in Vector4 wgt4567)
        {
            Span<IndexWeight> sparse = stackalloc IndexWeight[8];
            int count = IndexWeight.InsertUnsorted(sparse, idx0123, wgt0123);

            count = IndexWeight.InsertUnsorted(sparse, count, ((int)idx4567.X, wgt4567.X));
            count = IndexWeight.InsertUnsorted(sparse, count, ((int)idx4567.Y, wgt4567.Y));
            count = IndexWeight.InsertUnsorted(sparse, count, ((int)idx4567.Z, wgt4567.Z));
            count = IndexWeight.InsertUnsorted(sparse, count, ((int)idx4567.W, wgt4567.W));

            return new SparseWeight8(sparse);
        }

        /// <summary>
        /// Creates a new <see cref="SparseWeight8"/> struct.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Create(in Vector4, in Vector4, in Vector4, in Vector4)"/>, this method<br/>
        /// is a direct call to the constructor, so it's very fast. But it doesn't validate the input<br/>
        /// values, so it's intended to be used in limited scenarios, where performance is paramount.
        /// </remarks>
        /// <param name="idx0123">The first 4 indices.</param>
        /// <param name="idx4567">The next 4 indices.</param>
        /// <param name="wgt0123">The first 4 weights.</param>
        /// <param name="wgt4567">The next 4 weights.</param>
        /// <returns>A <see cref="SparseWeight8"/> instance.</returns>
        public static SparseWeight8 CreateUnchecked(in Vector4 idx0123, in Vector4 idx4567, in Vector4 wgt0123, in Vector4 wgt4567)
        {
            return new SparseWeight8(idx0123, idx4567, wgt0123, wgt4567);
        }

        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SparseWeight8"/> struct.
        /// </summary>
        /// <param name="idx0123">The first 4 indices.</param>
        /// <param name="idx4567">The next 4 indices.</param>
        /// <param name="wgt0123">The first 4 weights.</param>
        /// <param name="wgt4567">The next 4 weights.</param>
        private SparseWeight8(in Vector4 idx0123, in Vector4 idx4567, in Vector4 wgt0123, in Vector4 wgt4567)
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SparseWeight8"/> struct.
        /// </summary>
        /// <param name="iw">A collection of 8 <see cref="IndexWeight"/> pairs.</param>
        private SparseWeight8(ReadOnlySpan<IndexWeight> iw)
        {
            #if DEBUG
            if (iw.Length != 8) throw new ArgumentException("Span must contain 8 elements.", nameof(iw));
            if (!IndexWeight.IsWellFormed(iw, out var err)) throw new ArgumentException(err, nameof(iw));
            #endif

            this.Index0 = iw[0].Index;
            this.Weight0 = iw[0].Weight;

            this.Index1 = iw[1].Index;
            this.Weight1 = iw[1].Weight;

            this.Index2 = iw[2].Index;
            this.Weight2 = iw[2].Weight;

            this.Index3 = iw[3].Index;
            this.Weight3 = iw[3].Weight;

            this.Index4 = iw[4].Index;
            this.Weight4 = iw[4].Weight;

            this.Index5 = iw[5].Index;
            this.Weight5 = iw[5].Weight;

            this.Index6 = iw[6].Index;
            this.Weight6 = iw[6].Weight;

            this.Index7 = iw[7].Index;
            this.Weight7 = iw[7].Weight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SparseWeight8"/> struct<br/>
        /// from another instance, and multiplying the weights by a scale.
        /// </summary>
        /// <param name="sparse">The source <see cref="SparseWeight8"/>.</param>
        /// <param name="scale">The scale.</param>
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
        public readonly float Weight0;

        public readonly int Index1;
        public readonly float Weight1;

        public readonly int Index2;
        public readonly float Weight2;

        public readonly int Index3;
        public readonly float Weight3;

        public readonly int Index4;
        public readonly float Weight4;

        public readonly int Index5;
        public readonly float Weight5;

        public readonly int Index6;
        public readonly float Weight6;

        public readonly int Index7;
        public readonly float Weight7;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // we calculate the hash form the highest weight.

            float h = 0, w;

            w = Math.Abs(this.Weight0); if (w > h) { h = w; }
            w = Math.Abs(this.Weight1); if (w > h) { h = w; }
            w = Math.Abs(this.Weight2); if (w > h) { h = w; }
            w = Math.Abs(this.Weight3); if (w > h) { h = w; }
            w = Math.Abs(this.Weight4); if (w > h) { h = w; }
            w = Math.Abs(this.Weight5); if (w > h) { h = w; }
            w = Math.Abs(this.Weight6); if (w > h) { h = w; }
            w = Math.Abs(this.Weight7); if (w > h) { h = w; }

            return h.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is SparseWeight8 other && AreEqual(this, other);
        }

        /// <inheritdoc/>
        public bool Equals(SparseWeight8 other) { return AreEqual(this, other); }

        public static bool operator ==(SparseWeight8 left, SparseWeight8 right) { return left.Equals(right); }

        public static bool operator !=(SparseWeight8 left, SparseWeight8 right) { return !left.Equals(right); }

        internal static bool AreEqual(in SparseWeight8 x, in SparseWeight8 y)
        {
            const int STACKSIZE = 8;

            Span<IndexWeight> xWeights = stackalloc IndexWeight[STACKSIZE];
            Span<IndexWeight> yWeights = stackalloc IndexWeight[STACKSIZE];

            x.CopyTo(xWeights);
            y.CopyTo(yWeights);

            #if DEBUG
            if (!IndexWeight.IsWellFormed(xWeights, out var errx)) throw new ArgumentException(errx, nameof(x));
            if (!IndexWeight.IsWellFormed(yWeights, out var erry)) throw new ArgumentException(erry, nameof(y));
            #endif

            for (int i = 0; i < STACKSIZE; ++i)
            {
                var xItem = xWeights[i];
                if (xItem.Weight == 0) continue;

                bool match = false;

                for (int j = 0; j < STACKSIZE; ++j)
                {
                    var yItem = yWeights[j];
                    if (yItem.Weight == 0) continue;
                    if (xItem.Index == yItem.Index)
                    {
                        if (xItem.Weight != yItem.Weight) return false;
                        yWeights[j] = default;
                        match = true;
                        break;
                    }
                }

                if (!match) return false;
            }

            for (int i = 0; i < STACKSIZE; ++i)
            {
                if (yWeights[i].Weight != 0) return false;
            }

            return true;
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

        public float WeightSum => Weight0 + Weight1 + Weight2 + Weight3 + Weight4 + Weight5 + Weight6 + Weight7;

        public int MaxIndex => _GetMaxIndex();

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

            iw[0] = (sparse.Index0, sparse.Weight0);
            iw[1] = (sparse.Index1, sparse.Weight1);
            iw[2] = (sparse.Index2, sparse.Weight2);
            iw[3] = (sparse.Index3, sparse.Weight3);
            iw[4] = (sparse.Index4, sparse.Weight4);
            iw[5] = (sparse.Index5, sparse.Weight5);
            iw[6] = (sparse.Index6, sparse.Weight6);
            iw[7] = (sparse.Index7, sparse.Weight7);

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

            var c = sparse.InsertTo(iw);

            IndexWeight.BubbleSortByIndex(iw.Slice(0, c));

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
            var basis = Animations.CurveSampler.CreateHermitePointWeights(amount);

            return _OperateCubic(x, xt, y, yt, (xx, xxt, yy, yyt) => (xx * basis.StartPosition) + (yy * basis.EndPosition) + (xxt * basis.StartTangent) + (yyt * basis.EndTangent));
        }

        public IEnumerable<float> Expand(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                yield return GetExpandedAt(i);
            }
        }

        public IEnumerable<(int Index, float Weight)> GetIndexedWeights()
        {
            yield return (Index0, Weight0);
            yield return (Index1, Weight1);
            yield return (Index2, Weight2);
            yield return (Index3, Weight3);
            yield return (Index4, Weight4);
            yield return (Index5, Weight5);
            yield return (Index6, Weight6);
            yield return (Index7, Weight7);
        }

        public static SparseWeight8 Blend(ReadOnlySpan<SparseWeight8> sparses, ReadOnlySpan<float> weight)
        {
            var r = default(SparseWeight8);

            for (int i = 0; i < sparses.Length; ++i)
            {
                if (sparses[i].IsWeightless) continue;

                r = Add(r, Multiply(sparses[i], weight[i]));
            }

            return r;
        }

        public SparseWeight8 GetTrimmed(int maxWeights)
        {
            Span<IndexWeight> entries = stackalloc IndexWeight[8];

            this.InsertTo(entries.Slice(0, maxWeights));

            return new SparseWeight8(entries);
        }

        public SparseWeight8 GetNormalized()
        {
            return Multiply(this, 1f / this.WeightSum);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            int c = this.Count;

            for (int i = 0; i < c; ++i)
            {
                if (sb.Length > 0) sb.Append(' ');
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

            int len = 0;
            Span<IndexWeight> rrr = stackalloc IndexWeight[8];

            for (int i = 0; i < offset; ++i)
            {
                var ww = operationFunc(xxx[i], yyy[i]);
                if (ww == 0) continue;

                len = IndexWeight.InsertUnsorted(rrr, len, (indices[i], ww));
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

            int len = 0;
            Span<IndexWeight> rrr = stackalloc IndexWeight[8];

            for (int i = 0; i < offset; ++i)
            {
                var ww = operationFunc(xxx[i], yyy[i], zzz[i], www[i]);
                if (ww == 0) continue;

                len = IndexWeight.InsertUnsorted(rrr, len, (indices[i], ww));
            }

            return new SparseWeight8(rrr);
        }

        internal IEnumerable<(int Index, float Weight)> GetNonZeroWeights()
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
        /// Normalizes the current <see cref="SparseWeight8"/> by adding a complementary weight
        /// at index <paramref name="complementIndex"/> that resolves <see cref="WeightSum"/> to 1.
        /// </summary>
        /// <returns>A new <see cref="SparseWeight8"/> with a complementary weight.</returns>
        internal SparseWeight8 GetNormalizedWithComplement(int complementIndex)
        {
            var sum = this.WeightSum;
            if (sum >= 1) return this;

            Span<IndexWeight> weights = stackalloc IndexWeight[8];

            var offset = this.InsertTo(weights);

            // accumulate complement
            offset = IndexWeight.InsertUnsorted(weights, offset, new IndexWeight(complementIndex, 1 - sum));

            return new SparseWeight8(weights);
        }

        internal int _GetMaxIndex()
        {
            int idx = 0;

            if (Weight0 != 0) idx = Math.Max(idx, Index0);
            if (Weight1 != 0) idx = Math.Max(idx, Index1);
            if (Weight2 != 0) idx = Math.Max(idx, Index2);
            if (Weight3 != 0) idx = Math.Max(idx, Index3);
            if (Weight4 != 0) idx = Math.Max(idx, Index4);
            if (Weight5 != 0) idx = Math.Max(idx, Index5);
            if (Weight6 != 0) idx = Math.Max(idx, Index6);
            if (Weight7 != 0) idx = Math.Max(idx, Index7);

            return idx;
        }

        internal IEnumerable<IndexWeight> _GetPairs()
        {
            if (Weight0 != 0) yield return new IndexWeight(Index0, Weight0);
            if (Weight1 != 0) yield return new IndexWeight(Index1, Weight1);
            if (Weight2 != 0) yield return new IndexWeight(Index2, Weight2);
            if (Weight3 != 0) yield return new IndexWeight(Index3, Weight3);
            if (Weight4 != 0) yield return new IndexWeight(Index4, Weight4);
            if (Weight5 != 0) yield return new IndexWeight(Index5, Weight5);
            if (Weight6 != 0) yield return new IndexWeight(Index6, Weight6);
            if (Weight7 != 0) yield return new IndexWeight(Index7, Weight7);
        }

        internal int InsertTo(Span<IndexWeight> dst)
        {
            var offset = 0;

            if (Weight0 != 0) offset = IndexWeight.InsertUnsorted(dst, offset, (Index0, Weight0));
            if (Weight1 != 0) offset = IndexWeight.InsertUnsorted(dst, offset, (Index1, Weight1));
            if (Weight2 != 0) offset = IndexWeight.InsertUnsorted(dst, offset, (Index2, Weight2));
            if (Weight3 != 0) offset = IndexWeight.InsertUnsorted(dst, offset, (Index3, Weight3));
            if (Weight4 != 0) offset = IndexWeight.InsertUnsorted(dst, offset, (Index4, Weight4));
            if (Weight5 != 0) offset = IndexWeight.InsertUnsorted(dst, offset, (Index5, Weight5));
            if (Weight6 != 0) offset = IndexWeight.InsertUnsorted(dst, offset, (Index6, Weight6));
            if (Weight7 != 0) offset = IndexWeight.InsertUnsorted(dst, offset, (Index7, Weight7));

            return offset;
        }

        internal void CopyTo(Span<IndexWeight> dst)
        {
            dst[0] = (Index0, Weight0);
            dst[1] = (Index1, Weight1);
            dst[2] = (Index2, Weight2);
            dst[3] = (Index3, Weight3);
            dst[4] = (Index4, Weight4);
            dst[5] = (Index5, Weight5);
            dst[6] = (Index6, Weight6);
            dst[7] = (Index7, Weight7);
        }

        internal static (SparseWeight8 TangentIn, SparseWeight8 Value, SparseWeight8 TangentOut) AsTuple(float[] tangentIn, float[] value, float[] tangentOut)
        {
            return (Create(tangentIn), Create(value), Create(tangentOut));
        }

        #endregion
    }
}
