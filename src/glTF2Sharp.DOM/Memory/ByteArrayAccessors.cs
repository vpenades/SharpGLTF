using System;
using System.Collections.Generic;
using System.Text;

namespace glTF2Sharp.Memory
{
    using BYTES = ArraySegment<Byte>;

    /// <summary>
    /// Helper structure to access any Byte array as an array of <see cref="Schema2.ComponentType"/>
    /// </summary>
    public struct FloatingAccessor
    {
        #region constructors

        public static FloatingAccessor Create(Byte[] data, Schema2.ComponentType type, Boolean normalized)
        {
            return Create(new BYTES(data), type, normalized);
        }

        public static FloatingAccessor Create(BYTES data, Schema2.ComponentType type, Boolean normalized)
        {
            var accessor = new FloatingAccessor { _Data = data };

            if (type == Schema2.ComponentType.FLOAT)
            {
                accessor._Setter = accessor._SetValue<Single>;
                accessor._Getter = accessor._GetValue<Single>;
                return accessor;
            }

            if (normalized)
            {
                switch(type)
                {
                    case Schema2.ComponentType.BYTE:
                        {
                            accessor._Setter = accessor._SetNormalizedS8;
                            accessor._Getter = accessor._GetNormalizedS8;
                            return accessor;
                        }

                    case Schema2.ComponentType.UNSIGNED_BYTE:
                        {
                            accessor._Setter = accessor._SetNormalizedU8;
                            accessor._Getter = accessor._GetNormalizedU8;
                            return accessor;
                        }

                    case Schema2.ComponentType.SHORT:
                        {
                            accessor._Setter = accessor._SetNormalizedS16;
                            accessor._Getter = accessor._GetNormalizedS16;
                            return accessor;
                        }

                    case Schema2.ComponentType.UNSIGNED_SHORT:
                        {
                            accessor._Setter = accessor._SetNormalizedU16;
                            accessor._Getter = accessor._GetNormalizedU16;
                            return accessor;
                        }                    
                }
            }
            else
            {
                switch(type)
                {
                    case Schema2.ComponentType.BYTE:
                        {
                            accessor._Setter = accessor._SetValueS8;
                            accessor._Getter = accessor._GetValueS8;
                            return accessor;
                        }

                    case Schema2.ComponentType.UNSIGNED_BYTE:
                        {
                            accessor._Setter = accessor._SetValueU8;
                            accessor._Getter = accessor._GetValueU8;
                            return accessor;
                        }

                    case Schema2.ComponentType.SHORT:
                        {
                            accessor._Setter = accessor._SetValueS16;
                            accessor._Getter = accessor._GetValueS16;
                            return accessor;
                        }

                    case Schema2.ComponentType.UNSIGNED_SHORT:
                        {
                            accessor._Setter = accessor._SetValueU16;
                            accessor._Getter = accessor._GetValueU16;
                            return accessor;
                        }

                    case Schema2.ComponentType.UNSIGNED_INT:
                        {
                            accessor._Setter = accessor._SetValueU32;
                            accessor._Getter = accessor._GetValueU32;
                            return accessor;
                        }
                }
            }

            throw new NotSupportedException();
        }

        private Single _GetValueU8(int byteOffset, int index) { return _GetValue<Byte>(byteOffset, index); }
        private void _SetValueU8(int byteOffset, int index, Single value) { _SetValue<Byte>(byteOffset, index, (Byte)value); }

        private Single _GetValueS8(int byteOffset, int index) { return _GetValue<SByte>(byteOffset, index); }
        private void _SetValueS8(int byteOffset, int index, Single value) { _SetValue<SByte>(byteOffset, index, (SByte)value); }

        private Single _GetValueU16(int byteOffset, int index) { return _GetValue<UInt16>(byteOffset, index); }
        private void _SetValueU16(int byteOffset, int index, Single value) { _SetValue<UInt16>(byteOffset, index, (UInt16)value); }

        private Single _GetValueS16(int byteOffset, int index) { return _GetValue<Int16>(byteOffset, index); }
        private void _SetValueS16(int byteOffset, int index, Single value) { _SetValue<Int16>(byteOffset, index, (Int16)value); }

        private Single _GetValueU32(int byteOffset, int index) { return _GetValue<UInt32>(byteOffset, index); }
        private void _SetValueU32(int byteOffset, int index, Single value) { _SetValue<UInt32>(byteOffset, index, (UInt32)value); }

        private Single _GetNormalizedU8(int byteOffset, int index) { return _GetValueU8(byteOffset, index) / 255.0f; }
        private void _SetNormalizedU8(int byteOffset, int index, Single value) { _SetValueU8(byteOffset, index, value * 255.0f); }

        private Single _GetNormalizedS8(int byteOffset, int index) { return Math.Max(_GetValueS8(byteOffset, index) / 127.0f,-1); }
        private void _SetNormalizedS8(int byteOffset, int index, Single value) { _SetValueS8(byteOffset, index, (Single)Math.Round(value * 127.0f)); }

        private Single _GetNormalizedU16(int byteOffset, int index) { return _GetValueU16(byteOffset, index) / 65535.0f; }
        private void _SetNormalizedU16(int byteOffset, int index, Single value) { _SetValueU16(byteOffset, index, value * 65535.0f); }

        private Single _GetNormalizedS16(int byteOffset, int index) { return Math.Max(_GetValueS16(byteOffset, index) / 32767.0f, -1); }
        private void _SetNormalizedS16(int byteOffset, int index, Single value) { _SetValueS16(byteOffset, index, (Single)Math.Round(value * 32767.0f)); }

        private T _GetValue<T>(int byteOffset, int index) where T : unmanaged
        {
            return System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, T>(_Data.AsSpan(byteOffset))[index];
        }

        private void _SetValue<T>(int byteOffset, int index, T value) where T : unmanaged
        {
            System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, T>(_Data.AsSpan(byteOffset))[index] = value;
        }

        #endregion

        #region data

        delegate Single _GetterCallback(int byteOffset, int index);
        delegate void _SetterCallback(int byteOffset, int index, Single value);

        private BYTES _Data;
        private _GetterCallback _Getter;
        private _SetterCallback _Setter;

        #endregion

        #region API

        public Single this[int index]
        {
            get => _Getter(0, index);
            set => _Setter(0, index, value);
        }

        public Single this[int byteOffset, int index]
        {
            get => _Getter(byteOffset, index);
            set => _Setter(byteOffset, index, value);
        }

        #endregion
    }

    /// <summary>
    /// Helper structure to access any Byte array as an array of <see cref="Schema2.IndexType"/>
    /// </summary>
    public struct IntegerAccessor
    {
        #region constructors

        public static IntegerAccessor Create(Byte[] data, Schema2.IndexType type, Boolean normalized)
        {
            return Create(new BYTES(data), type);
        }        

        public static IntegerAccessor Create(BYTES data, Schema2.IndexType type)
        {
            var accessor = new IntegerAccessor { _Data = data };            

            switch (type)
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
        private _GetterCallback _Getter;
        private _SetterCallback _Setter;

        #endregion

        #region API

        public UInt32 this[int idx]
        {
            get => _Getter(idx);
            set => _Setter(idx, value);
        }

        #endregion
    }
   

    public interface IWordAccessor
    {
        System.Numerics.Vector4 GetWord(int index);

        void SetWord(int index, in System.Numerics.Vector4 word);
    }

    public struct ScalarAccessor : IWordAccessor
    {
        public ScalarAccessor(BYTES data, int byteStride, Schema2.ComponentType type, Boolean normalized)
        {
            _Accesor = FloatingAccessor.Create(data, type, normalized);
            _ByteStride = Math.Max(type.ByteLength() * 1, byteStride);
        }

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        public Single this[int index]
        {
            get => _Accesor[index * _ByteStride, 0];
            set => _Accesor[index * _ByteStride, 0] = value;
        }

        public System.Numerics.Vector4 GetWord(int index) => new System.Numerics.Vector4(this[index],0,0,0);

        public void SetWord(int index, in System.Numerics.Vector4 word) => this[index] = word.X;
    }

    public struct Vector2Accessor : IWordAccessor
    {
        public Vector2Accessor(BYTES data, int byteStride, Schema2.ComponentType type, Boolean normalized)
        {
            _Accesor = FloatingAccessor.Create(data, type, normalized);
            _ByteStride = Math.Max(type.ByteLength() * 2, byteStride);
        }

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        public System.Numerics.Vector2 this[int index]
        {
            get
            {
                index *= _ByteStride;
                return new System.Numerics.Vector2(_Accesor[index, 0], _Accesor[index, 1]);
            }
            set
            {
                index *= _ByteStride;
                _Accesor[index, 0] = value.X;
                _Accesor[index, 1] = value.Y;
            }
        }

        public System.Numerics.Vector4 GetWord(int index) { var v = this[index]; return new System.Numerics.Vector4(v.X, v.Y, 0, 0); }

        public void SetWord(int index, in System.Numerics.Vector4 word) => this[index] = new System.Numerics.Vector2(word.X, word.Y);
    }

    public struct Vector3Accessor: IWordAccessor
    {
        public Vector3Accessor(BYTES data, int byteStride, Schema2.ComponentType type, Boolean normalized)
        {
            _Accesor = FloatingAccessor.Create(data, type, normalized);
            _ByteStride = Math.Max(type.ByteLength() * 3, byteStride);
        }

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        public System.Numerics.Vector3 this[int index]
        {
            get
            {
                index *= _ByteStride;
                return new System.Numerics.Vector3(_Accesor[index, 0], _Accesor[index, 1], _Accesor[index, 2]);
            }
            set
            {
                index *= _ByteStride;
                _Accesor[index, 0] = value.X;
                _Accesor[index, 1] = value.Y;
                _Accesor[index, 2] = value.Z;
            }
        }

        public System.Numerics.Vector4 GetWord(int index) { var v = this[index]; return new System.Numerics.Vector4(v.X, v.Y, v.Z, 0); }

        public void SetWord(int index, in System.Numerics.Vector4 word) => this[index] = new System.Numerics.Vector3(word.X, word.Y,word.Z);
    }

    public struct Vector4Accessor: IWordAccessor
    {
        public Vector4Accessor(BYTES data, int byteStride, Schema2.ComponentType type, Boolean normalized)
        {
            _Accesor = FloatingAccessor.Create(data, type, normalized);
            _ByteStride = Math.Max(type.ByteLength() * 4, byteStride);
        }

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        public System.Numerics.Vector4 this[int index]
        {
            get
            {
                index *= _ByteStride;
                return new System.Numerics.Vector4(_Accesor[index, 0], _Accesor[index, 1], _Accesor[index, 2], _Accesor[index, 3]);
            }
            set
            {
                index *= _ByteStride;
                _Accesor[index, 0] = value.X;
                _Accesor[index, 1] = value.Y;
                _Accesor[index, 2] = value.Z;
                _Accesor[index, 3] = value.W;
            }
        }

        public System.Numerics.Vector4 GetWord(int index) => this[index];

        public void SetWord(int index, in System.Numerics.Vector4 word) => this[index] = word;
    }

    public struct QuaternionAccessor : IWordAccessor
    {
        public QuaternionAccessor(BYTES data, int byteStride, Schema2.ComponentType type, Boolean normalized)
        {
            _Accesor = FloatingAccessor.Create(data, type, normalized);
            _ByteStride = Math.Max(type.ByteLength() * 4, byteStride);
        }

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        public System.Numerics.Quaternion this[int index]
        {
            get
            {
                index *= _ByteStride;
                return new System.Numerics.Quaternion(_Accesor[index, 0], _Accesor[index, 1], _Accesor[index, 2], _Accesor[index, 3]);
            }
            set
            {
                index *= _ByteStride;
                _Accesor[index, 0] = value.X;
                _Accesor[index, 1] = value.Y;
                _Accesor[index, 2] = value.Z;
                _Accesor[index, 3] = value.W;
            }
        }

        public System.Numerics.Vector4 GetWord(int index) { var v = this[index]; return new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W); }

        public void SetWord(int index, in System.Numerics.Vector4 word) => this[index] = new System.Numerics.Quaternion(word.X, word.Y, word.Z, word.W);
    }

    public struct Matrix4x4Accessor
    {
        public Matrix4x4Accessor(BYTES data, int byteStride, Schema2.ComponentType type, Boolean normalized)
        {
            _Accesor = FloatingAccessor.Create(data, type, normalized);
            _ByteStride = Math.Max(type.ByteLength() * 16, byteStride);
        }

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        public System.Numerics.Matrix4x4 this[int index]
        {
            get
            {
                index *= _ByteStride;
                return new System.Numerics.Matrix4x4
                    (
                    _Accesor[index, 0], _Accesor[index, 1], _Accesor[index, 2], _Accesor[index, 3],
                    _Accesor[index, 4], _Accesor[index, 5], _Accesor[index, 6], _Accesor[index, 7],
                    _Accesor[index, 8], _Accesor[index, 9], _Accesor[index, 10], _Accesor[index, 11],
                    _Accesor[index, 12], _Accesor[index, 13], _Accesor[index, 14], _Accesor[index, 15]
                    );
            }
            set
            {
                index *= _ByteStride;
                _Accesor[index, 0] = value.M11;
                _Accesor[index, 1] = value.M12;
                _Accesor[index, 2] = value.M13;
                _Accesor[index, 3] = value.M14;
                _Accesor[index, 4] = value.M21;
                _Accesor[index, 5] = value.M22;
                _Accesor[index, 6] = value.M23;
                _Accesor[index, 7] = value.M24;
                _Accesor[index, 8] = value.M31;
                _Accesor[index, 9] = value.M32;
                _Accesor[index, 10] = value.M33;
                _Accesor[index, 11] = value.M34;
                _Accesor[index, 12] = value.M41;
                _Accesor[index, 13] = value.M42;
                _Accesor[index, 14] = value.M43;
                _Accesor[index, 15] = value.M44;
            }
        }
    }


}
