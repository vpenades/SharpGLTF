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

        #endregion

        #region non serializable data

        /// <summary>
        /// Immediately after deserialization, binary buffer is loaded/parsed and stored here
        /// </summary>
        internal Byte[] _Content;

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent.LogicalBuffers.IndexOfReference(this);

        public Byte[] Content => _Content;

        #endregion

        #region binary read

        const string EMBEDDEDOCTETSTREAM = "data:application/octet-stream;base64,";
        const string EMBEDDEDGLTFBUFFER = "data:application/gltf-buffer;base64,";

        internal void _ResolveUri(AssetReader externalReferenceSolver)
        {
            _Content = _LoadBinaryBufferUnchecked(_uri, externalReferenceSolver);

            _uri = null; // When _Data is not empty, clear URI
        }

        private static Byte[] _LoadBinaryBufferUnchecked(string uri, AssetReader externalReferenceSolver)
        {
            return uri._TryParseBase64Unchecked(EMBEDDEDGLTFBUFFER)
                ?? uri._TryParseBase64Unchecked(EMBEDDEDOCTETSTREAM)
                ?? externalReferenceSolver?.Invoke(uri);
        }

        #endregion

        #region binary write

        internal void _WriteToExternal(string uri, AssetWriter writer)
        {
            this._uri = uri;
            this._byteLength = _Content.Length;

            writer(uri, _Content);
        }

        internal void _WriteToInternal()
        {
            this._uri = null;
            this._byteLength = _Content.Length;
        }

        internal void _ClearAfterWrite()
        {
            this._uri = null;
            this._byteLength = 0;
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a buffer with <paramref name="byteCount"/> size.
        /// </summary>
        /// <param name="byteCount">the size of the buffer</param>
        /// <returns>the buffer</returns>
        public Buffer CreateBuffer(int byteCount)
        {
            var buffer = new Buffer();
            buffer._Content = new byte[byteCount];

            _buffers.Add(buffer);

            return buffer;
        }

        /// <summary>
        /// Finds and existing buffer that is already using <paramref name="content"/> , or creates a new one if none is found.
        /// </summary>
        /// <param name="content">the byte array to be wrapped as a buffer</param>
        /// <returns>the buffer</returns>
        public Buffer UseBuffer(Byte[] content)
        {
            Guard.IsFalse(content == null, nameof(content));

            foreach (var b in this.LogicalBuffers)
            {
                if (b._Content == content) return b;
            }

            var buffer = new Buffer();
            buffer._Content = content;

            _buffers.Add(buffer);

            return buffer;
        }

        /// <summary>
        /// Merges all the Buffer objects into a single, big one.
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

            var b = new Buffer
            {
                _Content = sbbuilder.ToArray()
            };

            this._buffers.Add(b);
        }
    }
}
