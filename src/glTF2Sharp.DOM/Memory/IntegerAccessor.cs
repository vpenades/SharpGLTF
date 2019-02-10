using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Linq;

namespace glTF2Sharp.Memory
{    
    using BYTES = ArraySegment<Byte>;

    using ENCODING = Schema2.IndexType;

    /// <summary>
    /// Wraps an encoded byte array and exposes it as a collection of UInt32 indices
    /// </summary
    [System.Diagnostics.DebuggerDisplay("Integer Accessor {Count}")]
    public struct IntegerAccessor : IAccessor<UInt32>
    {
        #region constructors

        public IntegerAccessor(Byte[] data, ENCODING encoding)
            : this(new BYTES(data), encoding) { }

        public IntegerAccessor(BYTES data, ENCODING encoding)
        {
            _Data = data;
            _ByteStride = encoding.ByteLength();
            this._Setter = null;
            this._Getter = null;

            switch (encoding)
            {
                case ENCODING.UNSIGNED_BYTE:
                    {
                        this._Setter = this._SetValueU8;
                        this._Getter = this._GetValueU8;
                        break;
                    }

                case ENCODING.UNSIGNED_SHORT:
                    {
                        this._Setter = this._SetValueU16;
                        this._Getter = this._GetValueU16;
                        break;
                    }

                case ENCODING.UNSIGNED_INT:
                    {
                        this._Setter = this._SetValue<UInt32>;
                        this._Getter = this._GetValue<UInt32>;
                        break;
                    }
                default: throw new ArgumentException(nameof(encoding));
            }            
        }

        private UInt32 _GetValueU8(int index) { return _GetValue<Byte>(index); }
        private void _SetValueU8(int index, UInt32 value) { _SetValue<Byte>(index, (Byte)value); }

        private UInt32 _GetValueU16(int index) { return _GetValue<UInt16>(index); }
        private void _SetValueU16(int index, UInt32 value) { _SetValue<UInt16>(index, (UInt16)value); }

        private T _GetValue<T>(int index) where T : unmanaged
        {
            return System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, T>(_Data)[index];
        }

        private void _SetValue<T>(int index, T value) where T : unmanaged
        {
            System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, T>(_Data)[index] = value;
        }

        #endregion

        #region data

        delegate UInt32 _GetterCallback(int index);

        delegate void _SetterCallback(int index, UInt32 value);

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly BYTES _Data;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly int _ByteStride;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly _GetterCallback _Getter;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly _SetterCallback _Setter;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private UInt32[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Data.Count / _ByteStride;

        public UInt32 this[int index]
        {
            get => _Getter(index);
            set => _Setter(index, value);
        }        

        public void CopyTo(ArraySegment<UInt32> dst) { AccessorsUtils.Copy<UInt32>(this, dst); }        

        public IEnumerator<UInt32> GetEnumerator() { return new AccessorEnumerator<UInt32>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<UInt32>(this); }

        public (UInt32, UInt32) GetBounds() { throw new NotImplementedException(); }

        #endregion
    }
}
