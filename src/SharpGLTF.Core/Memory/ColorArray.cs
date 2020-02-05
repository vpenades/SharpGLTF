using System;
using System.Collections.Generic;
using System.Numerics;
using System.Collections;
using System.Linq;

using BYTES = System.Memory<byte>;

using ENCODING = SharpGLTF.Schema2.EncodingType;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an array of <see cref="Vector4"/> values.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Color4[{Count}]")]
    public readonly struct ColorArray : IList<Vector4>, IReadOnlyList<Vector4>
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorArray"/> struct.
        /// </summary>
        /// <param name="source">The array to wrap.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="dimensions">The number of elements per item. Currently only values 3 and 4 are supported.</param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        /// <param name="defaultW">If <paramref name="dimensions"/> is 3, the W values are filled with this value</param>
        public ColorArray(BYTES source, int byteStride = 0, int dimensions = 4, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false, Single defaultW = 1)
            : this(source, 0, int.MaxValue, byteStride, dimensions, encoding, normalized, defaultW)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorArray"/> struct.
        /// </summary>
        /// <param name="source">The array to wrap.</param>
        /// <param name="byteOffset">The zero-based index of the first <see cref="Byte"/> in <paramref name="source"/>.</param>
        /// <param name="itemsCount">The number of <see cref="Vector4"/> items in <paramref name="source"/>.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="dimensions">The number of elements per item. Currently only values 3 and 4 are supported.</param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        /// <param name="defaultW">If <paramref name="dimensions"/> is 3, the W values are filled with this value</param>
        public ColorArray(BYTES source, int byteOffset, int itemsCount, int byteStride, int dimensions = 4, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false, Single defaultW = 1)
        {
            Guard.MustBeBetweenOrEqualTo(dimensions, 3, 4, nameof(dimensions));

            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, dimensions, encoding, normalized);
            _Dimensions = dimensions;

            _DefaultW = defaultW;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        private readonly int _Dimensions;

        private readonly float _DefaultW;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Vector4[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Vector4>.IsReadOnly => false;

        public Vector4 this[int index]
        {
            get
            {
                return new Vector4(_Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2], _Dimensions < 4 ? _DefaultW : _Accessor[index, 3]);
            }

            set
            {
                _Accessor[index, 0] = value.X;
                _Accessor[index, 1] = value.Y;
                _Accessor[index, 2] = value.Z;
                if (_Dimensions >= 4 ) _Accessor[index, 3] = value.W;
            }
        }

        public IEnumerator<Vector4> GetEnumerator() { return new EncodedArrayEnumerator<Vector4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Vector4>(this); }

        public bool Contains(Vector4 item) { return IndexOf(item) >= 0; }

        public int IndexOf(Vector4 item) { return this._FirstIndexOf(item); }

        public void CopyTo(Vector4[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            this._CopyTo(array, arrayIndex);
        }

        public void Fill(IEnumerable<Vector4> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        void IList<Vector4>.Insert(int index, Vector4 item) { throw new NotSupportedException(); }

        void IList<Vector4>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Vector4>.Add(Vector4 item) { throw new NotSupportedException(); }

        void ICollection<Vector4>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Vector4>.Remove(Vector4 item) { throw new NotSupportedException(); }

        #endregion
    }
}
