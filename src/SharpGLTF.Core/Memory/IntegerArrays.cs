using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

using BYTES = System.Memory<byte>;

using ENCODING = SharpGLTF.Schema2.IndexEncodingType;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{UInt32}"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Integer[{Count}]")]
    public readonly struct IntegerArray : IList<UInt32>, IReadOnlyList<UInt32>
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerArray"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="encoding">Byte encoding.</param>
        public IntegerArray(BYTES source, ENCODING encoding = ENCODING.UNSIGNED_INT)
            : this(source, 0, int.MaxValue, encoding) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerArray"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteOffset">The zero-based index of the first <see cref="Byte"/> in <paramref name="source"/>.</param>
        /// <param name="itemsCount">The number of <see cref="UInt32"/> items in <paramref name="source"/>.</param>
        /// <param name="encoding">Byte encoding.</param>
        public IntegerArray(BYTES source, int byteOffset, int itemsCount, ENCODING encoding)
        {
            _Data = source.Slice(byteOffset);
            _ByteStride = encoding.ByteLength();
            this._Setter = null;
            this._Getter = null;

            if (itemsCount < this.Count) _Data = _Data.Slice(0, itemsCount * _ByteStride);

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

                default: throw new ArgumentException("Unsupported encoding.", nameof(encoding));
            }
        }

        private UInt32 _GetValueU8(int index) { return _GetValue<Byte>(index); }
        private void _SetValueU8(int index, UInt32 value) { _SetValue<Byte>(index, (Byte)value); }

        private UInt32 _GetValueU16(int index) { return _GetValue<UInt16>(index); }
        private void _SetValueU16(int index, UInt32 value) { _SetValue<UInt16>(index, (UInt16)value); }

        private T _GetValue<T>(int index)
            where T : unmanaged
        {
            return System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, T>(_Data.Span)[index];
        }

        private void _SetValue<T>(int index, T value)
            where T : unmanaged
        {
            System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, T>(_Data.Span)[index] = value;
        }

        #endregion

        #region data

        private delegate UInt32 _GetterCallback(int index);

        private delegate void _SetterCallback(int index, UInt32 value);

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

        /// <summary>
        /// Gets the number of elements in the range delimited by the <see cref="IntegerArray"/>
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Data.Length / _ByteStride;

        bool ICollection<UInt32>.IsReadOnly => false;

        public UInt32 this[int index]
        {
            get => _Getter(index);
            set => _Setter(index, value);
        }

        public IEnumerator<UInt32> GetEnumerator() { return new EncodedArrayEnumerator<UInt32>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<UInt32>(this); }

        public bool Contains(UInt32 item) { return IndexOf(item) >= 0; }

        public int IndexOf(UInt32 item) { return this._FirstIndexOf(item); }

        public void CopyTo(UInt32[] array, int arrayIndex) { Guard.NotNull(array, nameof(array)); this._CopyTo(array, arrayIndex); }

        public void Fill(IEnumerable<Int32> values, int dstStart = 0) { Guard.NotNull(values, nameof(values)); values._CopyTo(this, dstStart); }

        public void Fill(IEnumerable<UInt32> values, int dstStart = 0) { Guard.NotNull(values, nameof(values)); values._CopyTo(this, dstStart); }

        void IList<UInt32>.Insert(int index, UInt32 item) { throw new NotSupportedException(); }

        void IList<UInt32>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<UInt32>.Add(UInt32 item) { throw new NotSupportedException(); }

        void ICollection<UInt32>.Clear() { throw new NotSupportedException(); }

        bool ICollection<UInt32>.Remove(uint item) { throw new NotSupportedException(); }

        #endregion
    }
}
