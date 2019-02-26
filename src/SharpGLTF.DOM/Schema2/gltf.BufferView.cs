using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    using BYTES = ArraySegment<Byte>;

    using ENCODING = ComponentType;

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._BufferDebugView))]
    public sealed partial class BufferView
    {
        #region lifecycle

        internal BufferView() { }

        internal BufferView(Buffer buffer, int? byteLength, int? byteOffset, int? byteStride, BufferMode? target)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.NotNull(buffer._Content, nameof(buffer));
            Guard.NotNull(buffer.LogicalParent, nameof(buffer));

            byteLength = byteLength.AsValue(buffer._Content.Length - byteOffset.AsValue(0));

            Guard.MustBeGreaterThanOrEqualTo(byteLength.AsValue(0), _byteLengthMinimum, nameof(byteLength));
            Guard.MustBeGreaterThanOrEqualTo(byteOffset.AsValue(0), _byteOffsetMinimum, nameof(byteOffset));

            if (target == BufferMode.ELEMENT_ARRAY_BUFFER)
            {
                Guard.IsTrue(byteStride.AsValue(0) == 0, nameof(byteStride));
            }
            else if (byteStride.AsValue(0) > 0)
            {
                // TODO: clarify under which conditions bytestride needs to be defined or forbidden.

                Guard.IsTrue(byteStride.AsValue(0).IsMultipleOf(4), nameof(byteStride));
                Guard.MustBeBetweenOrEqualTo(byteStride.AsValue(0), _byteStrideMinimum, _byteStrideMaximum, nameof(byteStride));
            }

            this._buffer = buffer.LogicalIndex;

            this._byteLength = byteLength.AsValue(buffer._Content.Length);

            this._byteOffset = byteOffset.AsValue(0).AsNullable(0);
            this._byteStride = byteStride.AsValue(0).AsNullable(0);

            this._target = target;
        }

        #endregion

        #region properties

        public int LogicalIndex                 => this.LogicalParent.LogicalBufferViews.IndexOfReference(this);

        public BufferMode? DeviceBufferTarget   => this._target;

        public int ByteStride                   => this._byteStride.AsValue(0);

        public BYTES Content
        {
            get
            {
                var buffer = this.LogicalParent.LogicalBuffers[this._buffer];
                return new BYTES(buffer._Content, this._byteOffset ?? 0, this._byteLength);
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
            var srcBuf = this.LogicalParent.LogicalBuffers[this._buffer]._Content;
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
        /// Checks if all the accessors use this buffer in interleaved arrangement
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
        public BufferView CreateBufferView(Buffer buffer, int? byteLength = null, int? byteOffset = null, int? byteStride = null, BufferMode? target = null)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));

            var bv = new BufferView(buffer, byteLength, byteOffset, byteStride, target);

            this._bufferViews.Add(bv);

            return bv;
        }

        public BufferView UseBufferView(ArraySegment<Byte> data, int byteStride = 0, BufferMode? mode = null)
        {
            var buffer = UseBuffer(data.Array);

            foreach (var bv in this.LogicalBufferViews)
            {
                if (bv.Content.Array != data.Array) continue;
                if (bv.Content.Offset != data.Offset) continue;
                if (bv.Content.Count != data.Count) continue;
                if (bv.ByteStride != byteStride) continue;
                if (bv.DeviceBufferTarget != mode) continue;

                return bv;
            }

            return CreateBufferView(buffer, data.Count, data.Offset, byteStride, mode);
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

            // todo: search data on existing data for reusability.

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
