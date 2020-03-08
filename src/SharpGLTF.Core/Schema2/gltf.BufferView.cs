using System;
using System.Collections.Generic;
using System.Linq;

using BYTES = System.ArraySegment<byte>;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._BufferViewDebugProxy))]
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public sealed partial class BufferView
    {
        #region debug

        internal string _GetDebuggerDisplay()
        {
            return Debug.DebuggerDisplay.ToReport(this);
        }

        #endregion

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

        /// <summary>
        /// Gets a value indicating whether this <see cref="BufferView"/> defines a GPU Ready Vertex Buffer.
        /// </summary>
        public bool IsVertexBuffer              => this._target == BufferMode.ARRAY_BUFFER;

        /// <summary>
        /// Gets a value indicating whether this <see cref="BufferView"/> defines a GPU Ready Index Buffer.
        /// </summary>
        public bool IsIndexBuffer               => this._target == BufferMode.ELEMENT_ARRAY_BUFFER;

        /// <summary>
        /// Gets a value indicating whether this <see cref="BufferView"/> defines a general purpose data buffer.
        /// </summary>
        public bool IsDataBuffer                => this._target == null;

        /// <summary>
        /// Gets the number of bytes between the beginnings of successive elements, or Zero.
        /// </summary>
        public int ByteStride                   => this._byteStride.AsValue(0);

        /// <summary>
        /// Gets the actual bytes defined by this <see cref="BufferView"/>
        /// </summary>
        public BYTES Content
        {
            get
            {
                var buffer = this.LogicalParent.LogicalBuffers[this._buffer];
                var offset = this._byteOffset.AsValue(_byteOffsetDefault);
                var length = this._byteLength;

                return new BYTES(buffer.Content, offset, length);
            }
        }

        internal int LogicalBufferIndex => this._buffer;

        #endregion

        #region API

        public IEnumerable<Image> FindImages()
        {
            var idx = LogicalIndex;

            return this.LogicalParent
                .LogicalImages
                .Where(image => image._SourceBufferViewIndex == idx);
        }

        /// <summary>
        /// Finds all the accessors using this BufferView
        /// </summary>
        /// <returns>A collection of accessors</returns>
        public IEnumerable<Accessor> FindAccessors()
        {
            var idx = LogicalIndex;

            return this.LogicalParent
                .LogicalAccessors
                .Where(accessor => accessor._SourceBufferViewIndex == idx);
        }

        internal void _IsolateBufferMemory(_StaticBufferBuilder targetBuffer)
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

        internal static bool AreEqual(BufferView bv, BYTES content, int byteStride, BufferMode? target)
        {
            if (bv.Content.Array != content.Array) return false;
            if (bv.Content.Offset != content.Offset) return false;
            if (bv.Content.Count != content.Count) return false;
            if (bv.ByteStride != byteStride) return false;
            if (bv._target != target) return false;
            return true;
        }

        /// <summary>
        /// Calculates the number of bytes to which this accessors reads
        /// taking into account if the source <see cref="BufferView"/> is strided.
        /// </summary>
        /// <returns>The number of bytes to access.</returns>
        internal int GetAccessorByteLength(in Memory.AttributeFormat fmt, int count)
        {
            var elementByteSize = fmt.ByteSize;
            if (this.ByteStride == 0) return elementByteSize * count;
            return (this.ByteStride * (count - 1)) + elementByteSize;
        }

        #endregion

        #region Validation

        internal static void VerifyAccess(Validation.ValidationContext validate, BufferView bv, int accessorByteOffset, Memory.AttributeFormat format, int count)
        {
            if (format.Normalized)
            {
                validate.IsAnyOf("Encoding", format.Encoding, EncodingType.UNSIGNED_BYTE, EncodingType.UNSIGNED_SHORT, EncodingType.BYTE, EncodingType.SHORT);
            }

            if (bv.IsVertexBuffer)
            {
                if (bv.ByteStride == 0) validate.IsMultipleOf("ElementByteSize", format.ByteSize, 4);
            }

            if (bv.IsIndexBuffer)
            {
                validate.GetContext(bv).IsUndefined("ByteStride", bv._byteStride);
                validate.IsAnyOf("Format", format, (DimensionType.SCALAR, EncodingType.UNSIGNED_BYTE), (DimensionType.SCALAR, EncodingType.UNSIGNED_SHORT), (DimensionType.SCALAR, EncodingType.UNSIGNED_INT));
            }

            if (bv.ByteStride > 0) validate.IsGreaterOrEqual("ElementByteSize", bv.ByteStride, format.ByteSize);

            var accessorByteLength = bv.GetAccessorByteLength(format, count);

            // "Accessor(offset: {0}, length: {1}) does not fit referenced bufferView[% 3] length %4.";
            validate.IsNullOrInRange(("BufferView", bv.LogicalIndex), accessorByteOffset, accessorByteLength, bv.Content);
        }

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate
                .IsNullOrIndex(nameof(Buffer), _buffer, this.LogicalParent.LogicalBuffers)
                .NonNegative("ByteOffset", _byteOffset)
                .IsGreaterOrEqual("ByteLength", _byteLength, _byteLengthMinimum);

            // ByteStride must defined only with BufferMode.ARRAY_BUFFER, be multiple of 4, and between 4 and 252
            if (!_byteStride.HasValue) return;

            validate
                .IsAnyOf("Target", _target, null,  BufferMode.ARRAY_BUFFER)
                .IsInRange(nameof(ByteStride), _byteStride.Value, _byteStrideMinimum, _byteStrideMaximum)
                .IsMultipleOf(nameof(ByteStride), _byteStride.Value, 4);
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            base.OnValidateContent(validate);

            var buffer = this.LogicalParent.LogicalBuffers[this._buffer];
            var bcontent = buffer.Content;

            validate
                .IsNullOrInRange("ByteOffset", _byteOffset, _byteLength, buffer.Content)
                .IsLessOrEqual(nameof(ByteStride), ByteStride, _byteLength);
        }

        internal void ValidateBufferUsageGPU(Validation.ValidationContext validate, BufferMode usingMode)
        {
            validate = validate.GetContext(this);

            if (this._target.HasValue) validate.EnumsAreEqual(nameof(_target), _target.Value, usingMode);
        }

        internal void ValidateBufferUsagePlainData(Validation.ValidationContext validate)
        {
            /*
            if (this._byteStride.HasValue)
            {
                if (result.TryFixLinkOrError("BufferView", "Unexpected ByteStride found. Expected null"))
                {
                    this._byteStride = null;
                }
            }*/

            validate = validate.GetContext(this);

            if (!this._target.HasValue) return;

            if (validate.TryFix) this._target = null;

            validate.IsUndefined(nameof(_target), this._target);
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
        public BufferView UseBufferView(BYTES data, int byteStride = 0, BufferMode? target = null)
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

            var content = new BYTES(buffer.Content, byteOffset, byteLength.AsValue(buffer.Content.Length - byteOffset) );

            foreach (var bv in this.LogicalBufferViews)
            {
                if (BufferView.AreEqual(bv, content, byteStride, target)) return bv;
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
