using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Collections;

namespace glTF2Sharp.Memory
{    
    using BYTES = ArraySegment<Byte>;

    using ENCODING = Schema2.ComponentType;

    /// <summary>
    /// Helper structure to access any Byte array as an array of floating Singles/>
    /// </summary>
    struct FloatingAccessor
    {
        #region constructors

        public FloatingAccessor(Byte[] data, ENCODING encoding, Boolean normalized)
            : this(new BYTES(data), encoding, normalized) { }        

        public FloatingAccessor(BYTES data, ENCODING encoding, Boolean normalized)
        {
            this._Data = data;
            this._Getter = null;
            this._Setter = null;

            if (encoding == ENCODING.FLOAT)
            {
                this._Setter = this._SetValue<Single>;
                this._Getter = this._GetValue<Single>;
            }
            else
            {
                if (normalized)
                {
                    switch (encoding)
                    {
                        case ENCODING.BYTE:
                            {
                                this._Setter = this._SetNormalizedS8;
                                this._Getter = this._GetNormalizedS8;
                                break;
                            }

                        case ENCODING.UNSIGNED_BYTE:
                            {
                                this._Setter = this._SetNormalizedU8;
                                this._Getter = this._GetNormalizedU8;
                                break;
                            }

                        case ENCODING.SHORT:
                            {
                                this._Setter = this._SetNormalizedS16;
                                this._Getter = this._GetNormalizedS16;
                                break;
                            }

                        case ENCODING.UNSIGNED_SHORT:
                            {
                                this._Setter = this._SetNormalizedU16;
                                this._Getter = this._GetNormalizedU16;
                                break;
                            }
                        default: throw new ArgumentException(nameof(encoding));
                    }
                }
                else
                {
                    switch (encoding)
                    {
                        case ENCODING.BYTE:
                            {
                                this._Setter = this._SetValueS8;
                                this._Getter = this._GetValueS8;
                                break;
                            }

                        case ENCODING.UNSIGNED_BYTE:
                            {
                                this._Setter = this._SetValueU8;
                                this._Getter = this._GetValueU8;
                                break;
                            }

                        case ENCODING.SHORT:
                            {
                                this._Setter = this._SetValueS16;
                                this._Getter = this._GetValueS16;
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
                                this._Setter = this._SetValueU32;
                                this._Getter = this._GetValueU32;
                                break;
                            }

                        case ENCODING.FLOAT:
                            break;

                        default: throw new ArgumentException(nameof(encoding));
                    }
                }
            }            
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

        private Single _GetNormalizedS8(int byteOffset, int index) { return Math.Max(_GetValueS8(byteOffset, index) / 127.0f, -1); }
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

        private readonly BYTES _Data;
        private readonly _GetterCallback _Getter;
        private readonly _SetterCallback _Setter;

        #endregion

        #region API

        public int ByteLength => _Data.Count;

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

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._MemoryAccessorDebugView<Single>))]
    public struct ScalarAccessor : IAccessor<Single>
    {
        #region constructors

        public ScalarAccessor(Byte[] data, int byteStride, ENCODING encoding, Boolean normalized)
            : this(new BYTES(data), byteStride, encoding, normalized) { }

        public ScalarAccessor(BYTES data, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accesor = new FloatingAccessor(data, encoding, normalized);
            _ByteStride = Math.Max(encoding.ByteLength() * 1, byteStride);
        }

        #endregion

        #region data

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        #endregion

        #region API

        public int Count => _Accesor.ByteLength / _ByteStride;
        
        public Single this[int index]
        {
            get => _Accesor[index * _ByteStride, 0];
            set => _Accesor[index * _ByteStride, 0] = value;
        }                

        public void CopyTo(ArraySegment<Single> dst) { AccessorsUtils.Copy<Single>(this, dst); }        

        public IEnumerator<Single> GetEnumerator() { return new AccessorEnumerator<Single>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<Single>(this); }

        public (Single, Single) GetBounds() { return AccessorsUtils.GetBounds(this); }

        public IAccessor<Vector4> AsVector4() { return new _MapScalarToVector4(this); }        

        #endregion
    }

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._MemoryAccessorDebugView<Vector2>))]
    public struct Vector2Accessor : IAccessor<Vector2>
    {
        #region constructors

        public Vector2Accessor(Byte[] data, int byteStride, ENCODING encoding, Boolean normalized)
            : this(new BYTES(data),byteStride,encoding,normalized) { }
        
        public Vector2Accessor(BYTES data, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accesor = new FloatingAccessor(data, encoding, normalized);
            _ByteStride = Math.Max(encoding.ByteLength() * 2, byteStride);
        }

        #endregion

        #region data

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        #endregion

        #region API

        public int Count => _Accesor.ByteLength / _ByteStride;

        public Vector2 this[int index]
        {
            get
            {
                index *= _ByteStride;
                return new Vector2(_Accesor[index, 0], _Accesor[index, 1]);
            }

            set
            {
                index *= _ByteStride;
                _Accesor[index, 0] = value.X;
                _Accesor[index, 1] = value.Y;
            }
        }        

        public void CopyTo(ArraySegment<Vector2> dst) { AccessorsUtils.Copy<Vector2>(this, dst); }        

        public IEnumerator<Vector2> GetEnumerator() { return new AccessorEnumerator<Vector2>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<Vector2>(this); }

        public (Vector2, Vector2) GetBounds() { return AccessorsUtils.GetBounds(this); }

        public IAccessor<Vector4> AsVector4() { return new _MapVector2ToVector4(this); }        

        #endregion
    }

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._MemoryAccessorDebugView<Vector3>))]
    public struct Vector3Accessor: IAccessor<Vector3>
    {
        #region constructors

        public Vector3Accessor(Byte[] data, int byteStride, ENCODING encoding, Boolean normalized)
            : this(new BYTES(data), byteStride, encoding, normalized) { }

        public Vector3Accessor(BYTES data, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accesor = new FloatingAccessor(data, encoding, normalized);
            _ByteStride = Math.Max(encoding.ByteLength() * 3, byteStride);
        }

        #endregion

        #region data

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        #endregion

        #region API

        public int Count => _Accesor.ByteLength / _ByteStride;

        public Vector3 this[int index]
        {
            get
            {
                index *= _ByteStride;
                return new Vector3(_Accesor[index, 0], _Accesor[index, 1], _Accesor[index, 2]);
            }

            set
            {
                index *= _ByteStride;
                _Accesor[index, 0] = value.X;
                _Accesor[index, 1] = value.Y;
                _Accesor[index, 2] = value.Z;
            }
        }        

        public void CopyTo(ArraySegment<Vector3> dst) { AccessorsUtils.Copy<Vector3>(this, dst); }        

        public IEnumerator<Vector3> GetEnumerator() { return new AccessorEnumerator<Vector3>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<Vector3>(this); }

        public (Vector3, Vector3) GetBounds() { return AccessorsUtils.GetBounds(this); }

        public IAccessor<Vector4> AsVector4() { return new _MapVector3ToVector4(this); }

        #endregion
    }

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._MemoryAccessorDebugView<Vector4>))]
    public struct Vector4Accessor: IAccessor<Vector4>
    {
        #region constructors

        public Vector4Accessor(Byte[] data, int byteStride, ENCODING encoding, Boolean normalized)
            : this(new BYTES(data), byteStride, encoding, normalized) { }

        public Vector4Accessor(BYTES data, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accesor = new FloatingAccessor(data, encoding, normalized);
            _ByteStride = Math.Max(encoding.ByteLength() * 4, byteStride);
        }

        #endregion

        #region data

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        #endregion

        #region API

        public int Count => _Accesor.ByteLength / _ByteStride;

        public Vector4 this[int index]
        {
            get
            {
                index *= _ByteStride;
                return new Vector4(_Accesor[index, 0], _Accesor[index, 1], _Accesor[index, 2], _Accesor[index, 3]);
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

        public void CopyTo(ArraySegment<Vector4> dst) { AccessorsUtils.Copy<Vector4>(this, dst); }

        public IEnumerator<Vector4> GetEnumerator() { return new AccessorEnumerator<Vector4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<Vector4>(this); }

        public (Vector4, Vector4) GetBounds() { return AccessorsUtils.GetBounds(this); }

        public IAccessor<Vector4> AsVector4() { return this; }

        #endregion
    }

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._MemoryAccessorDebugView<Quaternion>))]
    public struct QuaternionAccessor : IAccessor<Quaternion>
    {
        #region constructors

        public QuaternionAccessor(Byte[] data, int byteStride, ENCODING encoding, Boolean normalized)
            : this(new BYTES(data), byteStride, encoding, normalized) { }

        public QuaternionAccessor(BYTES data, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accesor = new FloatingAccessor(data, encoding, normalized);
            _ByteStride = Math.Max(encoding.ByteLength() * 4, byteStride);
        }

        #endregion

        #region data

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        #endregion

        #region API

        public int Count => _Accesor.ByteLength / _ByteStride;

        public Quaternion this[int index]
        {
            get
            {
                index *= _ByteStride;
                return new Quaternion(_Accesor[index, 0], _Accesor[index, 1], _Accesor[index, 2], _Accesor[index, 3]);
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

        public void CopyTo(ArraySegment<Quaternion> dst) { AccessorsUtils.Copy<Quaternion>(this, dst); }        

        public IEnumerator<Quaternion> GetEnumerator() { return new AccessorEnumerator<Quaternion>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<Quaternion>(this); }

        public (Quaternion, Quaternion) GetBounds() { throw new NotImplementedException(); }

        public IAccessor<Vector4> AsVector4() { return new _MapQuaternionToVector4(this); }

        #endregion
    }

    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._MemoryAccessorDebugView<Matrix4x4>))]
    public struct Matrix4x4Accessor : IAccessor<Matrix4x4>
    {
        #region constructors

        public Matrix4x4Accessor(Byte[] data, int byteStride, ENCODING encoding, Boolean normalized)
            : this(new BYTES(data), byteStride, encoding, normalized) { }

        public Matrix4x4Accessor(BYTES data, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accesor = new FloatingAccessor(data, encoding, normalized);
            _ByteStride = Math.Max(encoding.ByteLength() * 16, byteStride);
        }

        #endregion

        #region data

        private FloatingAccessor _Accesor;
        private readonly int _ByteStride;

        #endregion

        #region API

        public int Count => _Accesor.ByteLength / _ByteStride;

        public Matrix4x4 this[int index]
        {
            get
            {
                index *= _ByteStride;
                return new Matrix4x4
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

        public void CopyTo(ArraySegment<Matrix4x4> dst) { AccessorsUtils.Copy<Matrix4x4>(this, dst); }

        public IEnumerator<Matrix4x4> GetEnumerator() { return new AccessorEnumerator<Matrix4x4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new AccessorEnumerator<Matrix4x4>(this); }

        public (Matrix4x4, Matrix4x4) GetBounds() { throw new NotImplementedException(); }

        #endregion
    }
}
