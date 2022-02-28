using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Transforms
{
    [System.Diagnostics.DebuggerDisplay("{Index} = {Weight}")]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    readonly struct IndexWeight : IEquatable<IndexWeight>
    {
        #region implicit

        public static implicit operator IndexWeight((int Index, float Weight) pair) { return new IndexWeight(pair.Index, pair.Weight); }

        public static implicit operator IndexWeight(KeyValuePair<int, float> pair) { return new IndexWeight(pair.Key, pair.Value); }

        #endregion

        #region constructor

        public IndexWeight((int Index, float Weight) pair)
        {
            Index = pair.Index;
            Weight = pair.Weight;
        }

        public IndexWeight(KeyValuePair<int, float> pair)
        {
            Index = pair.Key;
            Weight = pair.Value;
        }

        public IndexWeight(int i, float w)
        {
            Index = i;
            Weight = w;
        }

        #endregion

        #region data

        public readonly int Index;
        public readonly float Weight;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Index.GetHashCode() ^ Weight.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is IndexWeight other && this.Equals(other);
        }

        /// <inheritdoc/>
        public bool Equals(IndexWeight other)
        {
            return this.Index == other.Index && this.Weight == other.Weight;
        }

        public bool IsGreaterThan(in IndexWeight other)
        {
            var tw = Math.Abs(this.Weight);
            var ow = Math.Abs(other.Weight);

            if (tw > ow) return true;
            if (tw == ow && this.Index < other.Index) return true;

            return false;
        }

        #endregion

        #region operators

        public static IndexWeight operator +(IndexWeight a, IndexWeight b)
        {
            if (a.Index != b.Index) throw new InvalidOperationException(nameof(b));
            return new IndexWeight(a.Index, a.Weight + b.Weight);
        }

        public static IndexWeight operator +(IndexWeight a, float w)
        {
            return new IndexWeight(a.Index, a.Weight + w);
        }

        #endregion

        #region API

        /// <summary>
        /// Checks if the collection of <see cref="IndexWeight"/> pairs is well formed.
        /// </summary>
        /// <remarks>
        /// A collection is considered malformed when:<br/>
        /// <list type="bullet">
        /// <item>Weightless Items have indices different than zero.</item>
        /// <item>The same index appears more than once.</item>
        /// </list>
        /// Indices are not required to be sorted in any order.
        /// </remarks>
        /// <param name="iw">The collection of pairs.</param>
        /// <param name="err">the error message when is malformed.</param>
        /// <returns>True if collection is wellformed. False otherwise.</returns>
        public static bool IsWellFormed(ReadOnlySpan<IndexWeight> iw, out string err)
        {
            for (int i = 0; i < iw.Length; ++i)
            {
                var item = iw[i];
                if (item.Weight == 0)
                {
                    if (item.Index != 0) { err = "weightless items must have index 0."; return false; }
                    continue;
                }

                for (int j = 0; j < i; ++j)
                {
                    var prev = iw[j];
                    if (prev.Weight == 0) continue;
                    if (item.Index == prev.Index) { err = "indices must be unique."; return false; }
                }
            }

            err = null;
            return true;
        }

        /// <summary>
        /// Adds the given <see cref="IndexWeight"/> pair to the given collection,<br/>
        /// trying to keep the collection sorted.
        /// </summary>
        /// <param name="buffer">The destination buffer, which might be larger than the collection.</param>
        /// <param name="length">The current collecion length.</param>
        /// <param name="item">The <see cref="IndexWeight"/> pair to add.</param>
        /// <returns>The new collection length.</returns>
        public static int InsertSorted(Span<IndexWeight> buffer, int length, IndexWeight item)
        {
            System.Diagnostics.Debug.Assert(buffer.Length >= length);
            System.Diagnostics.Debug.Assert(item.Weight._IsFinite());

            if (item.Weight == 0) return length;

            // check if the index already exist

            for (int i = 0; i < length; ++i)
            {
                if (buffer[i].Index == item.Index)
                {
                    // add weight to existing item
                    buffer[i] += item;

                    // since we've altered the weight, we might
                    // need to move this value up to keep the
                    // collection sorted by weight.

                    while (i > 1)
                    {
                        var r = Math.Abs(buffer[i - 1].Weight).CompareTo(buffer[i].Weight);
                        if (r == 1) break;
                        if (r == 0 && buffer[i - 1].Index < item.Index) break;

                        // swap values

                        var tmp = buffer[i - 1];
                        buffer[i - 1] = buffer[i];
                        buffer[i] = tmp;

                        --i;
                    }

                    return length;
                }
            }

            // find insertion index

            var idx = length;
            var wgt = Math.Abs(item.Weight);

            for (int i = 0; i < length; ++i)
            {
                // first we compare by weights;
                // if weights are equal, we compare by index,
                // so larger weights and smaller indices take precendence.

                var r = Math.Abs(buffer[i].Weight).CompareTo(wgt);
                if (r == 1) continue;
                if (r == 0 && buffer[i].Index < item.Index) continue;
                idx = i;
                break;
            }

            if (idx >= buffer.Length) return buffer.Length; // can't insert; already full;

            length = Math.Min(length + 1, buffer.Length);

            // shift tail of collection
            for (int i = length - 1; i > idx; --i)
            {
                buffer[i] = buffer[i - 1];
            }

            buffer[idx] = item;

            return length;
        }

        public static int InsertUnsorted(Span<IndexWeight> sparse, in System.Numerics.Vector4 idx0123, in System.Numerics.Vector4 wgt0123)
        {
            int idx = 0;

            if (wgt0123.X != 0)
            {
                sparse[0] = ((int)idx0123.X, wgt0123.X);
                ++idx;
            }

            if (wgt0123.Y != 0)
            {
                var y = (int)idx0123.Y;
                if (idx == 1 && sparse[0].Index == y) { sparse[0] += (y, wgt0123.Y); }
                else { sparse[idx++] = (y, wgt0123.Y); }
            }

            if (wgt0123.Z != 0)
            {
                var z = (int)idx0123.Z;
                if (idx > 0 && sparse[0].Index == z) { sparse[0] += (z, wgt0123.Z); }
                else if (idx > 1 && sparse[1].Index == z) { sparse[1] += (z, wgt0123.Z); }
                else { sparse[idx++] = (z, wgt0123.Z); }
            }

            if (wgt0123.W != 0)
            {
                var w = (int)idx0123.W;
                if (idx > 0 && sparse[0].Index == w) { sparse[0] += (w, wgt0123.W); }
                else if (idx > 1 && sparse[1].Index == w) { sparse[1] += (w, wgt0123.W); }
                else if (idx > 2 && sparse[2].Index == w) { sparse[2] += (w, wgt0123.W); }
                else { sparse[idx++] = (w, wgt0123.W); }
            }

            return idx;
        }

        /// <summary>
        /// Adds the given <see cref="IndexWeight"/> pair to the given collection.
        /// </summary>
        /// <param name="buffer">The destination buffer, which might be larger than the collection.</param>
        /// <param name="length">The current collecion length.</param>
        /// <param name="item">The <see cref="IndexWeight"/> pair to add.</param>
        /// <returns>The new collection length.</returns>
        public static int InsertUnsorted(Span<IndexWeight> buffer, int length, IndexWeight item)
        {
            System.Diagnostics.Debug.Assert(buffer.Length >= length);
            System.Diagnostics.Debug.Assert(item.Weight._IsFinite());

            if (item.Weight == 0) return length;

            // check if the index already exist

            for (int i = 0; i < length; ++i)
            {
                if (buffer[i].Index == item.Index)
                {
                    // add weight to existing item and exit

                    // TODO: adding a positive and a negative weight can lead to a weightless item;
                    // in which case it should be removed from the collection.

                    var w = buffer[i].Weight + item.Weight;

                    buffer[i] = w == 0 ? default : new IndexWeight(item.Index, w);

                    return length;
                }
            }

            // try to append at the end

            if (length < buffer.Length)
            {
                buffer[length] = item;
                return length + 1;
            }

            // collection is already full, try find insertion index
            // by looking for the "smallest" item in the current
            // collection.

            var idx = -1;
            var curr = item;

            for (int i = 0; i < buffer.Length; ++i)
            {
                if (buffer[i].IsGreaterThan(curr)) continue;
                idx = i;
                curr = buffer[i];
            }

            if (idx >= 0) buffer[idx] = item;

            return length;
        }

        public static int CopyTo(in SparseWeight8 src, Span<int> dstIndices, Span<float> dstWeights, int dstLength)
        {
            System.Diagnostics.Debug.Assert(dstIndices.Length == dstWeights.Length);
            System.Diagnostics.Debug.Assert(dstIndices.Length >= dstLength, $"{nameof(dstIndices)}.Length must be at least {nameof(dstLength)}");
            System.Diagnostics.Debug.Assert(dstWeights.Length >= dstLength, $"{nameof(dstWeights)}.Length must be at least {nameof(dstLength)}");
            System.Diagnostics.Debug.Assert(dstWeights.Slice(0, dstLength).ToArray().All(item => item == 0), "All weights must be zero");

            foreach (var pair in src._GetPairs())
            {
                System.Diagnostics.Debug.Assert(pair.Weight != 0);

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
                // repeat len times until the collection is sorted.

                bool sorted = true;

                for (int j = 1; j < pairs.Length; ++j)
                {
                    var k = j - 1;

                    var kk = pairs[k];
                    var jj = pairs[j];

                    if (kk.IsGreaterThan(jj)) continue;

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
