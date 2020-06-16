using System;
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

        /// <summary>
        /// Gets the zero-based index of this <see cref="Buffer"/> at <see cref="ModelRoot.LogicalBuffers"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalBuffers.IndexOfReference(this);

        #pragma warning disable CA1819 // Properties should not return arrays
        public Byte[] Content => _Content;
        #pragma warning restore CA1819 // Properties should not return arrays

        #endregion

        #region binary read

        const string EMBEDDEDOCTETSTREAM = "data:application/octet-stream";
        const string EMBEDDEDGLTFBUFFER = "data:application/gltf-buffer";

        internal void _ResolveUri(IO.ReadContext context)
        {
            _Content = _LoadBinaryBufferUnchecked(_uri, context);

            _uri = null; // When _Data is not empty, clear URI
        }

        private static Byte[] _LoadBinaryBufferUnchecked(string uri, IO.ReadContext context)
        {
            var data = uri.TryParseBase64Unchecked(EMBEDDEDGLTFBUFFER, EMBEDDEDOCTETSTREAM);
            if (data != null) return data;

            return context
                .ReadAllBytesToEnd(uri)
                .ToUnderlayingArray();
        }

        #endregion

        #region binary write

        /// <summary>
        /// Called internally by the serializer when the buffer content is to be written as an external file
        /// </summary>
        /// <param name="writer">The satellite asset writer</param>
        /// <param name="satelliteUri">A local satellite URI</param>
        internal void _WriteToSatellite(IO.WriteContext writer, string satelliteUri)
        {
            writer.WriteAllBytesToEnd(satelliteUri, new ArraySegment<byte>(_Content.GetPaddedContent()));

            this._uri = Uri.EscapeUriString(satelliteUri);
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
        /// calling <see cref="_WriteToSatellite(IO.WriteContext, string)"/>
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
            Guard.IsFalse(content == null, nameof(content));

            foreach (var b in this.LogicalBuffers)
            {
                if (b.Content == content) return b;
            }

            var buffer = new Buffer(content);

            _buffers.Add(buffer);

            return buffer;
        }

        /// <summary>
        /// Merges all the <see cref="ModelRoot.LogicalBuffers"/> instances into a single big one.
        /// </summary>
        /// <remarks>
        /// When merging the buffers, it also adjusts the BufferView offsets so the data they point to remains the same.
        /// If images are required to be included in the binary, call <see cref="ModelRoot.MergeImages"/> before calling <see cref="MergeBuffers"/>
        /// This action cannot be reversed.
        /// </remarks>
        public void MergeBuffers()
        {
            // retrieve all buffers and merge them into a single, big buffer

            var views = _bufferViews
                .OrderByDescending(item => item.Content.Count)
                .ToArray();

            if (views.Length <= 1) return; // nothing to do.

            var sbbuilder = new _StaticBufferBuilder(0);

            foreach (var bv in views) bv._IsolateBufferMemory(sbbuilder);

            this._buffers.Clear();

            var b = new Buffer(sbbuilder.ToArray());

            this._buffers.Add(b);
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
