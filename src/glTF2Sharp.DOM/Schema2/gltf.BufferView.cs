using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Schema2
{
    [System.Diagnostics.DebuggerTypeProxy(typeof(BufferView._DebugView))]
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

        public int LogicalIndex => this.LogicalParent.LogicalBufferViews.IndexOfReference(this);

        public BufferMode? DeviceBufferTarget => this._target;

        public int ByteStride => this._byteStride ?? 0;

        public ArraySegment<Byte> Data
        {
            get
            {
                var buffer = this.LogicalParent._LogicalBuffers[this._buffer];
                return new ArraySegment<byte>(buffer._Data, this._byteOffset ?? 0, this._byteLength);
            }
        }

        public IEnumerable<Accessor> Accessors
        {
            get
            {
                var idx = LogicalIndex;

                return this.LogicalParent
                    ._LogicalAccessors
                    .Where(accessor => accessor._LogicalBufferViewIndex == idx);
            }
        }

        #endregion

        #region API        

        internal void _ConvertToStaticBuffer(_StaticBufferBuilder targetBuffer)
        {
            // retrieve old buffer
            var srcBuf = this.LogicalParent._LogicalBuffers[this._buffer]._Data;
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
        
        public Func<int, int> CreateIndexDecoder(IndexType it, int byteOffset)
        {
            Guard.IsTrue(this.ByteStride == 0,null,"bytestride must be zero");            

            var reader = Memory.IntegerAccessor.Create(this.Data.Slice(byteOffset), it);

            return idx => (int)reader[idx];            
        }
        
        public Func<int, Object> CreateValueDecoder(ElementType et, ComponentType ct, bool normalized, int offset)
        {
            switch (et)
            {
                case ElementType.SCALAR: { var d = CreateScalarDecoder(ct, offset); return idx => d(idx); }
                case ElementType.VEC2: { var d = CreateVector2Decoder(ct, normalized, offset); return idx => d(idx); }
                case ElementType.VEC3: { var d = CreateVector3Decoder(ct, normalized, offset); return idx => d(idx); }
                case ElementType.VEC4: { var d = CreateVector4Decoder(ct, normalized, offset); return idx => d(idx); }
                case ElementType.MAT4: { var d = CreateMatrix4x4Decoder(ct, normalized, offset); return idx => d(idx); }
                default: throw new NotImplementedException();
            }
        }
        
        public Func<int,Single> CreateScalarDecoder(ComponentType ct, int offset)
        {
            // var reader = new ScalarIndexer(this.Data, this.ByteStride, offset, ct.ToDevice(false));

            var reader = new Memory.ScalarAccessor(this.Data.Slice(offset), this.ByteStride, ct, false);

            return idx => reader[idx];          
        }
        
        public Func<int,Vector2> CreateVector2Decoder(ComponentType ct,bool normalized, int offset)
        {
            // var reader = new Vector2Reader(this.Data, this.ByteStride, offset, ct.ToDevice(normalized));

            var reader = new Memory.Vector2Accessor(this.Data.Slice(offset), this.ByteStride, ct, normalized);

            return idx => reader[idx];
        }

        public Func<int, Vector3> CreateVector3Decoder(ComponentType ct, bool normalized, int offset)
        {
            // var reader = new Vector3Indexer(this.Data, this.ByteStride, offset, ct.ToDevice(normalized));

            var reader = new Memory.Vector3Accessor(this.Data.Slice(offset), this.ByteStride, ct, normalized);

            return idx => reader[idx];
        }

        public Func<int, Vector4> CreateVector4Decoder(ComponentType ct, bool normalized, int offset)
        {
            // var reader = new Vector4Indexer(this.Data, this.ByteStride, offset, ct.ToDevice(normalized));

            var reader = new Memory.Vector4Accessor(this.Data.Slice(offset), this.ByteStride, ct, normalized);

            return idx => reader[idx];
        }

        public Func<int, Quaternion> CreateQuaternionDecoder(ComponentType ct, bool normalized, int offset)
        {
            // var reader = new QuaternionIndexer(this.Data, this.ByteStride, offset, ct.ToDevice(normalized));

            var reader = new Memory.QuaternionAccessor(this.Data.Slice(offset), this.ByteStride, ct, normalized);

            return idx => reader[idx];
        }

        public Func<int, Matrix4x4> CreateMatrix4x4Decoder(ComponentType ct, bool normalized, int offset)
        {
            // var reader = new Matrix4x4Indexer(this.Data, this.ByteStride, offset, ct.ToDevice(normalized));

            var reader = new Memory.Matrix4x4Accessor(this.Data.Slice(offset), this.ByteStride, ct, normalized);

            return idx => reader[idx];
        }

        /// <summary>
        /// Checks if all the accessors use this buffer in interleaved arrangement
        /// </summary>
        /// <param name="accessors">A collection of accessors that use this buffer</param>
        /// <returns>true if the buffer is interleaved</returns>
        public bool IsInterleaved(IEnumerable<Accessor> accessors)
        {
            Guard.NotNullOrEmpty(Accessors, nameof(accessors));
            Guard.IsTrue(accessors.All(item => item.Buffer == this), nameof(accessors));

            return accessors
                .Select(item => item.ByteOffset)
                .All(o => o < this.ByteStride);
        }

        #endregion       

        #region debug        

        [System.Diagnostics.DebuggerDisplay("BufferView[{_Value.LogicalIndex}] {_Value.Name} {_Value._target} Bytes:{_Value.Buffer1.Count}")]
        internal sealed class _DebugView
        {
            public _DebugView(BufferView value) { _Value = value; }

            public int LogicalIndex => _Value.LogicalParent.LogicalBufferViews.IndexOfReference(_Value);

            private readonly BufferView _Value;

            public int ByteStride => _Value._byteStride ?? 0;

            public int ByteLength => _Value._byteLength;

            public BufferMode? BufferMode => _Value._target;

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            public Accessor[] Accessors => _Value.Accessors.ToArray();
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

        public BufferView CreateIndexBufferView(ReadOnlySpan<Byte> data)
        {
            var buffer = CreateBuffer(data);

            return CreateBufferView(buffer, data.Length, null, null, BufferMode.ELEMENT_ARRAY_BUFFER);
        }

        public BufferView CreateVertexBufferView(ReadOnlySpan<Byte> data, int byteStride)
        {
            var buffer = CreateBuffer(data);

            return CreateBufferView(buffer, data.Length, null, byteStride, BufferMode.ARRAY_BUFFER);
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
