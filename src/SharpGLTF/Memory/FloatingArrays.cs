using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Collections;
using System.Linq;

namespace SharpGLTF.Memory
{
    using BYTES = ArraySegment<Byte>;

    using ENCODING = Schema2.EncodingType;

    /// <summary>
    /// Wraps a <see cref="ArraySegment{Byte}"/> containing encoded <see cref="Single"/> values
    /// </summary>
    struct FloatingAccessor
    {
        #region constructors

        public FloatingAccessor(BYTES source, int byteOffset, int itemsCount, int byteStride, int dimensions, ENCODING encoding, Boolean normalized)
        {
            var enclen = encoding.ByteLength();

            this._Data = source.Slice(byteOffset);
            this._Getter = null;
            this._Setter = null;
            this._ByteStride = Math.Max(byteStride, enclen * dimensions);
            this._EncodedLen = enclen;
            this._ItemCount = this._Data.Count / this._ByteStride;

            // strided buffers usually have room for an extra item
            if ((_Data.Count % _ByteStride) >= enclen * dimensions) ++_ItemCount;

            _ItemCount = Math.Min(itemsCount, _ItemCount);

            if (encoding == ENCODING.FLOAT)
            {
                this._Setter = this._SetValue<Single>;
                this._Getter = this._GetValue<Single>;
                return;
            }

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

        #endregion

        #region encoding / decoding

        private Single _GetValueU8(int byteOffset) { return _GetValue<Byte>(byteOffset); }
        private void _SetValueU8(int byteOffset, Single value) { _SetValue<Byte>(byteOffset, (Byte)value); }

        private Single _GetValueS8(int byteOffset) { return _GetValue<SByte>(byteOffset); }
        private void _SetValueS8(int byteOffset, Single value) { _SetValue<SByte>(byteOffset, (SByte)value); }

        private Single _GetValueU16(int byteOffset) { return _GetValue<UInt16>(byteOffset); }
        private void _SetValueU16(int byteOffset, Single value) { _SetValue<UInt16>(byteOffset, (UInt16)value); }

        private Single _GetValueS16(int byteOffset) { return _GetValue<Int16>(byteOffset); }
        private void _SetValueS16(int byteOffset, Single value) { _SetValue<Int16>(byteOffset, (Int16)value); }

        private Single _GetValueU32(int byteOffset) { return _GetValue<UInt32>(byteOffset); }
        private void _SetValueU32(int byteOffset, Single value) { _SetValue<UInt32>(byteOffset, (UInt32)value); }

        private Single _GetNormalizedU8(int byteOffset) { return _GetValueU8(byteOffset) / 255.0f; }
        private void _SetNormalizedU8(int byteOffset, Single value) { _SetValueU8(byteOffset, value * 255.0f); }

        private Single _GetNormalizedS8(int byteOffset) { return Math.Max(_GetValueS8(byteOffset) / 127.0f, -1); }
        private void _SetNormalizedS8(int byteOffset, Single value) { _SetValueS8(byteOffset, (Single)Math.Round(value * 127.0f)); }

        private Single _GetNormalizedU16(int byteOffset) { return _GetValueU16(byteOffset) / 65535.0f; }
        private void _SetNormalizedU16(int byteOffset, Single value) { _SetValueU16(byteOffset, value * 65535.0f); }

        private Single _GetNormalizedS16(int byteOffset) { return Math.Max(_GetValueS16(byteOffset) / 32767.0f, -1); }
        private void _SetNormalizedS16(int byteOffset, Single value) { _SetValueS16(byteOffset, (Single)Math.Round(value * 32767.0f)); }

        private T _GetValue<T>(int byteOffset)
            where T : unmanaged
        {
            return System.Runtime.InteropServices.MemoryMarshal.Read<T>(_Data.AsSpan(byteOffset));
        }

        private void _SetValue<T>(int byteOffset, T value)
            where T : unmanaged
        {
            System.Runtime.InteropServices.MemoryMarshal.Write<T>(_Data.AsSpan(byteOffset), ref value);
        }

        #endregion

        #region data

        delegate Single _GetterCallback(int byteOffset);

        delegate void _SetterCallback(int byteOffset, Single value);

        private readonly BYTES _Data;

        private readonly int _ByteStride;
        private readonly int _EncodedLen;

        private readonly int _ItemCount;

        private readonly _GetterCallback _Getter;
        private readonly _SetterCallback _Setter;

        #endregion

        #region API

        public int ByteLength => _Data.Count;

        public int Count => _ItemCount;

        public Single this[int index]
        {
            get => _Getter(index * _ByteStride);
            set => _Setter(index * _ByteStride, value);
        }

        public Single this[int rowIndex, int subIndex]
        {
            get => _Getter((rowIndex * _ByteStride) + (subIndex * _EncodedLen));
            set => _Setter((rowIndex * _ByteStride) + (subIndex * _EncodedLen), value);
        }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an array of <see cref="Single"/> values
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Scalar Accessor {Count}")]
    public struct ScalarArray : IEncodedArray<Single>
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarArray"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public ScalarArray(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarArray"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteOffset">The zero-based index of the first <see cref="Byte"/> in <paramref name="source"/>.</param>
        /// <param name="itemsCount">The number of <see cref="Single"/> items in <paramref name="source"/>.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public ScalarArray(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 1, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Single[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        public Single this[int index]
        {
            get => _Accessor[index, 0];
            set => _Accessor[index, 0] = value;
        }

        public void CopyTo(ArraySegment<Single> dst) { EncodedArrayUtils.Copy<Single>(this, dst); }

        public IEnumerator<Single> GetEnumerator() { return new EncodedArrayEnumerator<Single>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Single>(this); }

        public (Single, Single) GetBounds() { return EncodedArrayUtils.GetBounds(this); }

        public IEncodedArray<Vector4> AsVector4() { return new _MapScalarToVector4(this); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an array of <see cref="Vector2"/> values
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Vector2 Accessor {Count}")]
    public struct Vector2Array : IEncodedArray<Vector2>
    {
        #region constructors

        public Vector2Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        public Vector2Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 2, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Vector2[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        public Vector2 this[int index]
        {
            get
            {
                return new Vector2(_Accessor[index, 0], _Accessor[index, 1]);
            }

            set
            {
                _Accessor[index, 0] = value.X;
                _Accessor[index, 1] = value.Y;
            }
        }

        public void CopyTo(ArraySegment<Vector2> dst) { EncodedArrayUtils.Copy<Vector2>(this, dst); }

        public IEnumerator<Vector2> GetEnumerator() { return new EncodedArrayEnumerator<Vector2>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Vector2>(this); }

        public (Vector2, Vector2) GetBounds() { return EncodedArrayUtils.GetBounds(this); }

        public IEncodedArray<Vector4> AsVector4() { return new _MapVector2ToVector4(this); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an array of <see cref="Vector3"/> values
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Vector3 Accessor {Count}")]
    public struct Vector3Array : IEncodedArray<Vector3>
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3Array"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public Vector3Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3Array"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteOffset">The zero-based index of the first <see cref="Byte"/> in <paramref name="source"/>.</param>
        /// <param name="itemsCount">The number of <see cref="Vector3"/> items in <paramref name="source"/>.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public Vector3Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 3, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Vector3[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        public Vector3 this[int index]
        {
            get
            {
                return new Vector3(_Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2]);
            }

            set
            {
                _Accessor[index, 0] = value.X;
                _Accessor[index, 1] = value.Y;
                _Accessor[index, 2] = value.Z;
            }
        }

        public void CopyTo(ArraySegment<Vector3> dst) { EncodedArrayUtils.Copy<Vector3>(this, dst); }

        public IEnumerator<Vector3> GetEnumerator() { return new EncodedArrayEnumerator<Vector3>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Vector3>(this); }

        public (Vector3, Vector3) GetBounds() { return EncodedArrayUtils.GetBounds(this); }

        public IEncodedArray<Vector4> AsVector4() { return new _MapVector3ToVector4(this); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an array of <see cref="Vector4"/> values
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Vector4 Accessor {Count}")]
    public struct Vector4Array : IEncodedArray<Vector4>
    {
        #region constructors

        public Vector4Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        public Vector4Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 4, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Vector4[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        public Vector4 this[int index]
        {
            get
            {
                return new Vector4(_Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2], _Accessor[index, 3]);
            }

            set
            {
                _Accessor[index, 0] = value.X;
                _Accessor[index, 1] = value.Y;
                _Accessor[index, 2] = value.Z;
                _Accessor[index, 3] = value.W;
            }
        }

        public void CopyTo(ArraySegment<Vector4> dst) { EncodedArrayUtils.Copy<Vector4>(this, dst); }

        public IEnumerator<Vector4> GetEnumerator() { return new EncodedArrayEnumerator<Vector4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Vector4>(this); }

        public (Vector4, Vector4) GetBounds() { return EncodedArrayUtils.GetBounds(this); }

        public IEncodedArray<Vector4> AsVector4() { return this; }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an array of <see cref="Quaternion"/> values
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Quaternion Accessor {Count}")]
    public struct QuaternionArray : IEncodedArray<Quaternion>
    {
        #region constructors

        public QuaternionArray(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        public QuaternionArray(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 4, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Quaternion[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        public Quaternion this[int index]
        {
            get
            {
                return new Quaternion(_Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2], _Accessor[index, 3]);
            }

            set
            {
                _Accessor[index, 0] = value.X;
                _Accessor[index, 1] = value.Y;
                _Accessor[index, 2] = value.Z;
                _Accessor[index, 3] = value.W;
            }
        }

        public void CopyTo(ArraySegment<Quaternion> dst) { EncodedArrayUtils.Copy<Quaternion>(this, dst); }

        public IEnumerator<Quaternion> GetEnumerator() { return new EncodedArrayEnumerator<Quaternion>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Quaternion>(this); }

        public (Quaternion, Quaternion) GetBounds() { throw new NotImplementedException(); }

        public IEncodedArray<Vector4> AsVector4() { return new _MapQuaternionToVector4(this); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an array of <see cref="Matrix4x4"/> values
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("MAtrix4x4 Accessor {Count}")]
    public struct Matrix4x4Array : IEncodedArray<Matrix4x4>
    {
        #region constructors

        public Matrix4x4Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        public Matrix4x4Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 16, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Matrix4x4[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        public Matrix4x4 this[int index]
        {
            get
            {
                return new Matrix4x4
                    (
                    _Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2], _Accessor[index, 3],
                    _Accessor[index, 4], _Accessor[index, 5], _Accessor[index, 6], _Accessor[index, 7],
                    _Accessor[index, 8], _Accessor[index, 9], _Accessor[index, 10], _Accessor[index, 11],
                    _Accessor[index, 12], _Accessor[index, 13], _Accessor[index, 14], _Accessor[index, 15]
                    );
            }

            set
            {
                _Accessor[index, 0] = value.M11;
                _Accessor[index, 1] = value.M12;
                _Accessor[index, 2] = value.M13;
                _Accessor[index, 3] = value.M14;
                _Accessor[index, 4] = value.M21;
                _Accessor[index, 5] = value.M22;
                _Accessor[index, 6] = value.M23;
                _Accessor[index, 7] = value.M24;
                _Accessor[index, 8] = value.M31;
                _Accessor[index, 9] = value.M32;
                _Accessor[index, 10] = value.M33;
                _Accessor[index, 11] = value.M34;
                _Accessor[index, 12] = value.M41;
                _Accessor[index, 13] = value.M42;
                _Accessor[index, 14] = value.M43;
                _Accessor[index, 15] = value.M44;
            }
        }

        public void CopyTo(ArraySegment<Matrix4x4> dst) { EncodedArrayUtils.Copy<Matrix4x4>(this, dst); }

        public IEnumerator<Matrix4x4> GetEnumerator() { return new EncodedArrayEnumerator<Matrix4x4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Matrix4x4>(this); }

        public (Matrix4x4, Matrix4x4) GetBounds() { throw new NotImplementedException(); }

        #endregion
    }

    public struct MultiArray : IEncodedArray<Single[]>
    {
        #region constructors

        public MultiArray(BYTES source, int byteOffset, int itemsCount, int byteStride, int dimensions, ENCODING encoding, Boolean normalized)
        {
            _Dimensions = dimensions;
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, dimensions, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly int _Dimensions;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Single[][] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Dimensions => _Dimensions;

        public Single[] this[int index]
        {
            get
            {
                var val = new Single[_Dimensions];
                CopyItemTo(index, val);
                return val;
            }

            set
            {
                Guard.NotNull(value, nameof(value));
                Guard.IsTrue(value.Length == _Dimensions, nameof(value));

                for (int i = 0; i < _Dimensions; ++i)
                {
                    _Accessor[index, i] = value[i];
                }
            }
        }

        public void CopyItemTo(int index, Single[] dstItem)
        {
            var count = _Dimensions;

            for (int i = 0; i < count; ++i) dstItem[i] = _Accessor[index, i];
        }

        public void CopyTo(ArraySegment<Single[]> dst) { EncodedArrayUtils.Copy<Single[]>(this, dst); }

        public IEnumerator<Single[]> GetEnumerator() { return new EncodedArrayEnumerator<Single[]>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Single[]>(this); }

        public (Single[], Single[]) GetBounds() { throw new NotImplementedException(); }

        #endregion
    }
}
