using System;
using System.Collections.Generic;
using System.Text;

namespace glTF2Sharp.Memory
{
    using System.Collections;
    using BYTES = ArraySegment<Byte>;

    /// <summary>
    /// Helper structure to access any Byte array as an array of <see cref="Schema2.IndexType"/>
    /// </summary>
    public struct IntegerAccessor : IAccessor<UInt32>, IAccessor<Int32>, IReadOnlyCollection<UInt32>
    {
        #region constructors

        public static IntegerAccessor Create(Byte[] data, Schema2.IndexType encoding)
        {
            return Create(new BYTES(data), encoding);
        }

        public static IntegerAccessor Create(BYTES data, Schema2.IndexType encoding)
        {
            var accessor = new IntegerAccessor
            {
                _Data = data,
                _ByteStride = encoding.ByteLength()
            };

            switch (encoding)
            {
                case Schema2.IndexType.UNSIGNED_BYTE:
                    {
                        accessor._Setter = accessor._SetValueU8;
                        accessor._Getter = accessor._GetValueU8;
                        return accessor;
                    }

                case Schema2.IndexType.UNSIGNED_SHORT:
                    {
                        accessor._Setter = accessor._SetValueU16;
                        accessor._Getter = accessor._GetValueU16;
                        return accessor;
                    }

                case Schema2.IndexType.UNSIGNED_INT:
                    {
                        accessor._Setter = accessor._SetValue<UInt32>;
                        accessor._Getter = accessor._GetValue<UInt32>;
                        return accessor;
                    }
            }

            throw new NotSupportedException();
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

        private BYTES _Data;
        private int _ByteStride;
        private _GetterCallback _Getter;
        private _SetterCallback _Setter;

        #endregion

        #region API

        public int Count => _Data.Count / _ByteStride;

        public UInt32 this[int index]
        {
            get => _Getter(index);
            set => _Setter(index, value);
        }

        int IAccessor<int>.this[int index]
        {
            get => (Int32)_Getter(index);
            set => _Setter(index, (UInt32)value);
        }

        public void CopyTo(ArraySegment<UInt32> dst) { AccessorsUtils.Copy<UInt32>(this, dst); }

        public void CopyTo(ArraySegment<Int32> dst) { AccessorsUtils.Copy<Int32>(this, dst); }

        public IEnumerator<UInt32> GetEnumerator() { return new AccessorEnumerator<UInt32>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<UInt32>(this); }

        #endregion
    }
}
