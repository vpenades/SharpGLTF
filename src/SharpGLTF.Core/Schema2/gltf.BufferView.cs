using System;
using System.Collections.Generic;
using System.Linq;

using BYTES = System.ArraySegment<byte>;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._BufferDebugProxy))]
    public sealed partial class BufferView
    {
        #region lifecycle

        internal BufferView() { }

        internal BufferView(Buffer buffer, int byteOffset, int? byteLength, int byteStride, BufferMode? target)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.NotNull(buffer.Content, nameof(buffer));
            Guard.NotNull(buffer.LogicalParent, nameof(buffer));

            byteLength = byteLength.AsValue(buffer.Content.Length - byteOffset);

            Guard.MustBeGreaterThanOrEqualTo(byteLength.AsValue(0), _byteLengthMinimum, nameof(byteLength));
            Guard.MustBeGreaterThanOrEqualTo(byteOffset, _byteOffsetMinimum, nameof(byteOffset));

            if (target == BufferMode.ELEMENT_ARRAY_BUFFER || byteStride == 0)
            {
                Guard.IsTrue(byteStride == 0, nameof(byteStride));
                this._byteStride = null;
            }
            else if (byteStride > 0)
            {
                // TODO: clarify under which conditions bytestride needs to be defined or forbidden.

                Guard.IsTrue(byteStride.IsMultipleOf(4), nameof(byteStride));
                Guard.MustBeBetweenOrEqualTo(byteStride, _byteStrideMinimum, _byteStrideMaximum, nameof(byteStride));
                this._byteStride = byteStride.AsNullable(0, _byteStrideMinimum, _byteStrideMaximum);
            }

            this._buffer = buffer.LogicalIndex;

            this._byteLength = byteLength.AsValue(buffer.Content.Length);

            this._byteOffset = byteOffset.AsNullable(_byteOffsetDefault, _byteOffsetMinimum, int.MaxValue);

            this._target = target;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="BufferView"/> at <see cref="ModelRoot.LogicalBufferViews"/>
        /// </summary>
        public int LogicalIndex                 => this.LogicalParent.LogicalBufferViews.IndexOfReference(this);

        public BufferMode? DeviceBufferTarget   => this._target;

        public int ByteStride                   => this._byteStride.AsValue(0);

        public BYTES Content
        {
            get
            {
                var buffer = this.LogicalParent.LogicalBuffers[this._buffer];
                return new BYTES(buffer.Content, this._byteOffset.AsValue(0), this._byteLength);
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Finds all the accessors using this BufferView
        /// </summary>
        /// <returns>A collection of accessors</returns>
        public IEnumerable<Accessor> FindAccessors()
        {
            var idx = LogicalIndex;

            return this.LogicalParent
                .LogicalAccessors
                .Where(accessor => accessor._LogicalBufferViewIndex == idx);
        }

        internal void _ConvertToStaticBuffer(_StaticBufferBuilder targetBuffer)
        {
            // retrieve old buffer
            var srcBuf = this.LogicalParent.LogicalBuffers[this._buffer].Content;
            var data = new Byte[this._byteLength];
            Array.Copy(srcBuf, this._byteOffset ?? 0, data, 0, this._byteLength);

            // append data to new buffer
            this._buffer = targetBuffer.BufferIndex;
            this._byteLength = data.Length;
            this._byteOffset = targetBuffer.Append(data);
        }

        private string _DebuggerDisplay_TryIdentifyContent()
        {
            var accessors = this
                .FindAccessors()
                .OrderBy(item => item.ByteOffset)
                .ToList();

            return String.Join(" ", accessors.Select(item => item._DebuggerDisplay_TryIdentifyContent()));
        }

        /// <summary>
        /// Checks if <paramref name="accessors"/> use this buffer in interleaved arrangement
        /// </summary>
        /// <param name="accessors">A collection of accessors that use this buffer</param>
        /// <returns>true if the buffer is interleaved</returns>
        public bool IsInterleaved(IEnumerable<Accessor> accessors)
        {
            Guard.NotNullOrEmpty(accessors, nameof(accessors));
            Guard.IsTrue(accessors.All(item => item.SourceBufferView == this), nameof(accessors));

            return accessors
                .Select(item => item.ByteOffset)
                .All(o => o < this.ByteStride);
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates or reuses a <see cref="BufferView"/> instance
        /// at <see cref="ModelRoot.LogicalBufferViews"/>.
        /// </summary>
        /// <param name="data">The array range to wrap.</param>
        /// <param name="byteStride">For strided vertex buffers, it must be a value multiple of 4, 0 otherwise</param>
        /// <param name="target">The type hardware device buffer, or null</param>
        /// <returns>A <see cref="BufferView"/> instance.</returns>
        public BufferView UseBufferView(ArraySegment<Byte> data, int byteStride = 0, BufferMode? target = null)
        {
            Guard.NotNull(data.Array, nameof(data));
            return UseBufferView(data.Array, data.Offset, data.Count, byteStride, target);
        }

        /// <summary>
        /// Creates or reuses a <see cref="BufferView"/> instance
        /// at <see cref="ModelRoot.LogicalBufferViews"/>.
        /// </summary>
        /// <param name="buffer">The array to wrap.</param>
        /// <param name="byteOffset">The zero-based index of the first Byte in <paramref name="buffer"/></param>
        /// <param name="byteLength">The number of elements in <paramref name="buffer"/></param>
        /// <param name="byteStride">For strided vertex buffers, it must be a value multiple of 4, 0 otherwise</param>
        /// <param name="target">The type hardware device buffer, or null</param>
        /// <returns>A <see cref="BufferView"/> instance.</returns>
        public BufferView UseBufferView(Byte[] buffer, int byteOffset = 0, int? byteLength = null, int byteStride = 0, BufferMode? target = null)
        {
            Guard.NotNull(buffer, nameof(buffer));
            return UseBufferView(UseBuffer(buffer), byteOffset, byteLength, byteStride, target);
        }

        /// <summary>
        /// Creates or reuses a <see cref="BufferView"/> instance
        /// at <see cref="ModelRoot.LogicalBufferViews"/>.
        /// </summary>
        /// <param name="buffer">The buffer to wrap.</param>
        /// <param name="byteOffset">The zero-based index of the first Byte in <paramref name="buffer"/></param>
        /// <param name="byteLength">The number of elements in <paramref name="buffer"/></param>
        /// <param name="byteStride">For strided vertex buffers, it must be a value multiple of 4, 0 otherwise</param>
        /// <param name="target">The type hardware device buffer, or null</param>
        /// <returns>A <see cref="BufferView"/> instance.</returns>
        public BufferView UseBufferView(Buffer buffer, int byteOffset = 0, int? byteLength = null, int byteStride = 0, BufferMode? target = null)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.MustShareLogicalParent(this, "this", buffer, nameof(buffer));

            byteLength = byteLength.AsValue(buffer.Content.Length - byteOffset);

            foreach (var bv in this.LogicalBufferViews)
            {
                if (bv.Content.Array != buffer.Content) continue;
                if (bv.Content.Offset != byteOffset) continue;
                if (bv.Content.Count != byteLength.Value) continue;
                if (bv.ByteStride != byteStride) continue;
                if (bv.DeviceBufferTarget != target) continue;

                return bv;
            }

            var newbv = new BufferView(buffer, byteOffset, byteLength, byteStride, target);

            this._bufferViews.Add(newbv);

            return newbv;
        }
    }

    /// <summary>
    /// Utility class to merge BufferViews into a single big buffer
    /// </summary>
    sealed class _StaticBufferBuilder
    {
        #region lifecycle

        public _StaticBufferBuilder(int bufferIndex)
        {
            _BufferIndex = bufferIndex;
        }

        #endregion

        #region data

        // target buffer LogicalIndex
        private readonly int _BufferIndex;

        // accumulated data
        private readonly List<Byte> _Data = new List<byte>();

        #endregion

        #region properties

        public int BufferIndex => _BufferIndex;

        #endregion

        #region API

        public int Append(Byte[] data)
        {
            Guard.NotNullOrEmpty(data, nameof(data));

            // todo: search data on existing buffers for reusability and compression.

            // padding
            while ((_Data.Count & 3) != 0) _Data.Add(0);

            var offset = _Data.Count;

            _Data.AddRange(data);

            return offset;
        }

        public Byte[] ToArray()
        {
            var len = _Data.Count;
            while ((len & 3) != 0) ++len;

            var data = new Byte[len];

            _Data.CopyTo(data);

            return data;
        }

        #endregion
    }
}
