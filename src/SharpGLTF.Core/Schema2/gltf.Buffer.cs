using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Buffer[{LogicalIndex}] {Name} Bytes:{_Content?.Length ?? 0}")]
    public sealed partial class Buffer
    {
        #region lifecycle

        internal Buffer() { }

        internal Buffer(Byte[] content)
        {
            _Content = content;
        }

        #endregion

        #region non serializable data

        /// <summary>
        /// Immediately after deserialization, binary buffer is loaded/parsed and stored here
        /// </summary>
        private Byte[] _Content;

        #endregion

        #region properties

        #pragma warning disable CA1819 // Properties should not return arrays
        public Byte[] Content => _Content;
        #pragma warning restore CA1819 // Properties should not return arrays

        #endregion

        #region binary read

        const string EMBEDDEDOCTETSTREAM = "data:application/octet-stream";
        const string EMBEDDEDGLTFBUFFER = "data:application/gltf-buffer";

        internal void _ResolveUri(ReadContext context)
        {
            _Content = _LoadBinaryBufferUnchecked(_uri, context);

            _uri = null; // When _Data is not empty, clear URI
        }

        private static Byte[] _LoadBinaryBufferUnchecked(string uri, ReadContext context)
        {
            var data = uri.TryParseBase64Unchecked(EMBEDDEDGLTFBUFFER, EMBEDDEDOCTETSTREAM);
            if (data != null) return data;

            var segment = context
                .ReadAllBytesToEnd(uri);

            return segment.TryGetUnderlayingArray(out var array)
                ? array
                : segment.ToArray();
        }

        #endregion

        #region binary write

        /// <summary>
        /// Called internally by the serializer when the buffer content is to be written as an external file
        /// </summary>
        /// <param name="writer">The satellite asset writer</param>
        /// <param name="satelliteUri">A local satellite URI</param>
        internal void _WriteToSatellite(WriteContext writer, string satelliteUri)
        {
            writer.WriteAllBytesToEnd(satelliteUri, new ArraySegment<byte>(_Content.GetPaddedContent()));

            this._uri = satelliteUri._EscapeStringInternal();
            this._byteLength = _Content.Length;
        }

        /// <summary>
        /// Called internally by the serializer when the buffer content is to be written internally.
        /// </summary>
        internal void _WriteToInternal()
        {
            this._uri = null;
            this._byteLength = _Content.Length;
        }

        /// <summary>
        /// Called by the serializer immediatelly after
        /// calling <see cref="_WriteToSatellite(Schema2.WriteContext, string)"/>
        /// or <see cref="_WriteToInternal"/>
        /// </summary>
        internal void _ClearAfterWrite()
        {
            this._uri = null;
            this._byteLength = 0;
        }

        #endregion

        #region API

        internal void _IsolateMemory()
        {
            if (_Content == null) return;

            var content = new Byte[_Content.Length];
            _Content.CopyTo(content, 0);
            _Content = content;
        }

        #endregion

        #region validation

        internal void OnValidateBinaryChunk(Validation.ValidationContext validate, Byte[] binaryChunk)
        {
            validate = validate.GetContext(this);

            if (_uri == null)
            {
                validate
                    .NotNull(nameof(binaryChunk), binaryChunk)
                    .IsLessOrEqual("ByteLength", _byteLength, binaryChunk.Length);
            }
            else
            {
                validate
                    .IsValidURI(nameof(_uri), _uri, EMBEDDEDGLTFBUFFER, EMBEDDEDOCTETSTREAM);
            }
        }

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            validate.IsGreaterOrEqual("ByteLength", _byteLength, _byteLengthMinimum);
            // result.CheckSchemaIsMultipleOf("ByteLength", _byteLength, 4);

            base.OnValidateReferences(validate);
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            validate
                .NotNull("Content", _Content)
                .IsLessOrEqual("ByteLength", _byteLength, _Content.Length);

            base.OnValidateContent(validate);
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Buffer"/> instance
        /// and adds it to <see cref="ModelRoot.LogicalBuffers"/>.
        /// </summary>
        /// <param name="byteCount">the size of the buffer, in bytes.</param>
        /// <returns>A <see cref="Buffer"/> instance.</returns>
        public Buffer CreateBuffer(int byteCount)
        {
            var buffer = new Buffer(new byte[byteCount]);

            _buffers.Add(buffer);

            return buffer;
        }

        /// <summary>
        /// Creates or reuses a <see cref="Buffer"/> instance
        /// at <see cref="ModelRoot.LogicalBuffers"/>.
        /// </summary>
        /// <param name="content">the byte array to be wrapped as a buffer</param>
        /// <returns>A <see cref="Buffer"/> instance.</returns>
        public Buffer UseBuffer(Byte[] content)
        {
            Guard.NotNull(content, nameof(content));

            foreach (var b in this.LogicalBuffers)
            {
                if (b.Content == content) return b;
            }

            var buffer = new Buffer(content);

            _buffers.Add(buffer);

            return buffer;
        }

        /// <summary>
        /// Merges all the <see cref="LogicalBuffers"/> instances into a single big one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When merging the buffers, it also adjusts the BufferView offsets so the data they point to remains the same.
        /// </para>
        /// <para>
        /// If images are required to be included in the binary, call <see cref="MergeImages"/>
        /// before calling <see cref="MergeBuffers()"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// .Net arrays have an upper limit of 2Gb, so this is the biggest a buffer can normally grow,
        /// so attempting to merge buffers that sum more than 2Gb will throw this exception.
        /// </exception>
        public void MergeBuffers()
        {
            // retrieve all buffers and merge them into a single, big buffer

            var views = _bufferViews
                .OrderByDescending(item => item.Content.Count)
                .ToList();

            if (views.Count <= 1) return; // nothing to do.

            // check final size

            long totalLen = views.Sum(item => (long)item.Content.Count.WordPadded());
            if (totalLen >= (long)int.MaxValue) throw new InvalidOperationException("Can't merge a buffer larger than 2Gb");

            // begin merge

            var reservedMemory = (int) (totalLen * 1.01); // increase by 1%
            var sbbuilder = new _StaticBufferBuilder(0, reservedMemory);

            foreach (var bv in views) bv._IsolateBufferMemory(sbbuilder);

            // build final buffer

            this._buffers.Clear();

            var b = new Buffer(sbbuilder.ToArray());

            this._buffers.Add(b);
        }

        /// <summary>
        /// Merges all the <see cref="LogicalBuffers"/> instances into buffers of <paramref name="maxSize"/> size.
        /// </summary>
        /// <param name="maxSize">
        /// The maximum size of each buffer.
        /// Notice that if a single BufferView is larger than <paramref name="maxSize"/>, that buffer will be also larger.
        /// </param>
        public void MergeBuffers(int maxSize)
        {
            // retrieve all buffers and merge them into a single, big buffer

            var views = _bufferViews
                .OrderByDescending(item => item.Content.Count)
                .ToList();

            if (views.Count <= 1) return; // nothing to do.

            // begin merge

            var buffers = new List<_StaticBufferBuilder>();
            buffers.Add(new _StaticBufferBuilder(0));

            foreach (var bv in views)
            {
                var last = buffers.Last();

                var alreadyFull = last.BufferSize >= maxSize;
                var notEmpty = last.BufferSize > 0;
                var bvTooBig = (long)bv.Content.Count + last.BufferSize >= maxSize;

                if (alreadyFull || (notEmpty && bvTooBig))
                {
                    last = new _StaticBufferBuilder(buffers.Count);
                    buffers.Add(last);
                }

                bv._IsolateBufferMemory(last);
            }

            // build final buffers

            this._buffers.Clear();

            foreach (var buffer in buffers)
            {
                var b = new Buffer(buffer.ToArray());
                this._buffers.Add(b);
            }
        }

        /// <summary>
        /// Refreshes all internal memory buffers.
        /// </summary>
        /// <remarks>
        /// <see cref="Buffer"/> instances can be created using external <see cref="Byte"/> arrays, which
        /// can potentially be shared with other instances. Editing these arrays directly can lead to data
        /// corruption.
        /// This method refreshes all internal memory buffers, by copying the data into newly allocated
        /// buffers. This ensures that at this point, all memory buffers are not shared and of exclusive
        /// use of this <see cref="ModelRoot"/> instance.
        /// </remarks>
        public void IsolateMemory()
        {
            foreach (var b in this.LogicalBuffers)
            {
                b._IsolateMemory();
            }
        }
    }
}
