using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Buffer[{LogicalIndex}] {Name} Bytes:{_Data.Length}")]
    public partial class Buffer
    {
        #region lifecycle

        internal Buffer() { }

        internal Buffer(int byteCount)
        {
            Guard.MustBeGreaterThan(byteCount, 0, nameof(byteCount));

            _Data = new byte[byteCount];
        }

        internal Buffer(IReadOnlyList<Byte> data)
        {
            Guard.NotNullOrEmpty(data, nameof(data));

            _Data = data.ToArray();
        }

        internal Buffer(ReadOnlySpan<Byte> data)
        {
            Guard.IsFalse(data.IsEmpty, nameof(data));

            _Data = data.ToArray();
        }

        #endregion

        #region non serializable data

        /// <summary>
        /// Immediately after deserialization, binary buffer is loaded/parsed and stored here
        /// </summary>
        internal Byte[] _Data;

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent.LogicalBuffers.IndexOfReference(this);

        #endregion

        #region binary read

        const string EMBEDDEDOCTETSTREAM = "data:application/octet-stream;base64,";
        const string EMBEDDEDGLTFBUFFER = "data:application/gltf-buffer;base64,";

        internal void _ResolveUri(AssetReader externalReferenceSolver)
        {
            _Data = _LoadBinaryBufferUnchecked(_uri, externalReferenceSolver);

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
            this._byteLength = _Data.Length;

            writer(uri, _Data);
        }

        internal void _WriteToInternal()
        {
            this._uri = null;
            this._byteLength = _Data.Length;
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
        public Buffer CreateBuffer(int byteCount)
        {
            var buffer = new Buffer(byteCount);
            _buffers.Add(buffer);

            return buffer;
        }

        public Buffer CreateBuffer(ReadOnlySpan<Byte> data)
        {
            Guard.IsFalse(data.IsEmpty, nameof(data));

            var buffer = new Buffer(data);
            _buffers.Add(buffer);

            return buffer;
        }

        public Buffer CreateIndexBuffer(params int[] indices)
        {
            var buffer = CreateBuffer(indices.Length * 4);

            var accessor = Memory.IntegerAccessor.Create(buffer._Data, IndexType.UNSIGNED_INT);

            for (int i = 0; i < indices.Length; ++i)
            {
                accessor[i] = (UInt32)indices[i];
            }

            return buffer;
        }

        public Buffer CreateVector3Buffer(params Vector3[] vectors)
        {
            var buffer = CreateBuffer(vectors.Length * 12);

            var accessor = new Memory.Vector3Accessor(new ArraySegment<byte>(buffer._Data), 0, ComponentType.FLOAT, false);

            for (int i = 0; i < vectors.Length; ++i)
            {
                accessor[i] = vectors[i];
            }

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
                .OrderByDescending(item => item.Data.Count)
                .ToArray();

            if (views.Length <= 1) return; // nothing to do.

            var sbbuilder = new _StaticBufferBuilder(0);

            foreach (var bv in views) bv._ConvertToStaticBuffer(sbbuilder);

            this._buffers.Clear();

            var b = new Buffer
            {
                _Data = sbbuilder.ToArray()
            };

            this._buffers.Add(b);
        }
    }
}
