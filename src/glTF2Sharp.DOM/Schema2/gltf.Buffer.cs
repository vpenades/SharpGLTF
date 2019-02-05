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
            Guard.NotNullOrEmpty(data,nameof(data));

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

        public int LogicalIndex => this.LogicalParent._LogicalBuffers.IndexOfReference(this);

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
}
