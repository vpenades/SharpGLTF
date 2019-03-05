using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Buffer[{LogicalIndex}] {Name} Bytes:{_Data.Length}")]
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

        public Byte[] Content => _Content;

        #endregion

        #region binary read

        const string EMBEDDEDOCTETSTREAM = "data:application/octet-stream;base64,";
        const string EMBEDDEDGLTFBUFFER = "data:application/gltf-buffer;base64,";

        internal void _ResolveUri(AssetReader satelliteReferenceSolver)
        {
            _Content = _LoadBinaryBufferUnchecked(_uri, satelliteReferenceSolver);

            _uri = null; // When _Data is not empty, clear URI
        }

        private static Byte[] _LoadBinaryBufferUnchecked(string uri, AssetReader satelliteReferenceSolver)
        {
            return uri._TryParseBase64Unchecked(EMBEDDEDGLTFBUFFER)
                ?? uri._TryParseBase64Unchecked(EMBEDDEDOCTETSTREAM)
                ?? satelliteReferenceSolver?.Invoke(uri);
        }

        #endregion

        #region binary write

        /// <summary>
        /// Called internally by the serializer when the buffer content is to be written as an external file
        /// </summary>
        /// <param name="writer">The satellite asset writer</param>
        /// <param name="satelliteUri">A local satellite URI</param>
        internal void _WriteToSatellite(AssetWriter writer, string satelliteUri)
        {
            this._uri = satelliteUri;
            this._byteLength = _Content.Length;

            writer(satelliteUri, _Content.GetPaddedContent());
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
        /// calling <see cref="_WriteToSatellite(AssetWriter, string)"/>
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
        /// Merges all the <see cref="ModelRoot.LogicalBuffers"/> instances into a single, big one.
        /// </summary>
        /// <remarks>
        /// When merging the buffers, it also adjusts the BufferView offsets so the data they point to remains the same.
        /// </remarks>
        public void MergeBuffers()
        {
            // retrieve all buffers and merge them into a single, big buffer

            var views = _bufferViews
                .OrderByDescending(item => item.Content.Count)
                .ToArray();

            if (views.Length <= 1) return; // nothing to do.

            var sbbuilder = new _StaticBufferBuilder(0);

            foreach (var bv in views) bv._ConvertToStaticBuffer(sbbuilder);

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
