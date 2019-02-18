using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Schema2
{
    using BYTES = ArraySegment<Byte>;

    using ENCODING = ComponentType;

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._BufferDebugView))]
    public partial class BufferView
    {
        #region lifecycle

        internal BufferView() { }

        internal BufferView(Buffer buffer, int? byteLength, int? byteOffset, int? byteStride, BufferMode? target)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.NotNull(buffer.LogicalParent, nameof(buffer));
            if (byteLength.HasValue) Guard.MustBeGreaterThan(byteLength.Value, 0, nameof(byteLength));

            if (byteOffset.HasValue) Guard.MustBeGreaterThan(byteOffset.Value, 0, nameof(byteOffset));

            if (byteStride.HasValue && target.HasValue)
            {
                if (target.Value == BufferMode.ELEMENT_ARRAY_BUFFER)
                {
                    Guard.IsTrue(byteStride.Value == 2 || byteStride.Value == 4, nameof(byteStride));
                }
                else if (target.Value == BufferMode.ELEMENT_ARRAY_BUFFER)
                {
                    Guard.IsTrue((byteStride.Value % 4) == 0, nameof(byteStride));
                }
            }

            this._buffer = buffer.LogicalIndex;

            this._byteLength = byteLength.AsValue(buffer._Data.Length);

            this._byteOffset = byteOffset;
            this._byteStride = byteStride;

            this._target = target;
        }

        #endregion

        #region properties

        public int LogicalIndex                 => this.LogicalParent.LogicalBufferViews.IndexOfReference(this);

        public BufferMode? DeviceBufferTarget   => this._target;

        public int ByteStride                   => this._byteStride.AsValue(0);

        public BYTES Data
        {
            get
            {
                var buffer = this.LogicalParent.LogicalBuffers[this._buffer];
                return new BYTES(buffer._Data, this._byteOffset ?? 0, this._byteLength);
            }
        }

        public IEnumerable<Accessor> Accessors
        {
            get
            {
                var idx = LogicalIndex;

                return this.LogicalParent
                    .LogicalAccessors
                    .Where(accessor => accessor._LogicalBufferViewIndex == idx);
            }
        }

        #endregion

        #region API

        internal void _ConvertToStaticBuffer(_StaticBufferBuilder targetBuffer)
        {
            // retrieve old buffer
            var srcBuf = this.LogicalParent.LogicalBuffers[this._buffer]._Data;
            var data = new Byte[this._byteLength];
            Array.Copy(srcBuf, this._byteOffset ?? 0, data, 0, this._byteLength);

            // append data to new buffer
            this._buffer = targetBuffer.BufferIndex;
            this._byteLength = data.Length;
            this._byteOffset = targetBuffer.Append(data);
        }

        private string _DebuggerDisplay_TryIdentifyContent()
        {
            var accessors = this.Accessors.OrderBy(item => item.ByteOffset).ToArray();

            return String.Join(" ", accessors.Select(item => item._DebuggerDisplay_TryIdentifyContent()));
        }

        public Memory.IntegerArray CreateIndicesArray(int byteOffset, int count, IndexType encoding)
        {
            Guard.IsTrue(this.ByteStride == 0, null, "bytestride must be zero");

            return new Memory.IntegerArray(this.Data, byteOffset, count, encoding);
        }

        /// <summary>
        /// Checks if all the accessors use this buffer in interleaved arrangement
        /// </summary>
        /// <param name="accessors">A collection of accessors that use this buffer</param>
        /// <returns>true if the buffer is interleaved</returns>
        public bool IsInterleaved(IEnumerable<Accessor> accessors)
        {
            Guard.NotNullOrEmpty(Accessors, nameof(accessors));
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
                if (bv.Data.Array != data.Array) continue;
                if (bv.Data.Offset != data.Offset) continue;
                if (bv.Data.Count != data.Count) continue;
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
