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
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an array of <see cref="Vector4"/> values.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Vector4 Accessor {Count}")]
    public struct ColorArray : IList<Vector4>, IReadOnlyList<Vector4>
    {
        #region constructors

        public ColorArray(Byte[] source, int byteOffset, int itemsCount, int byteStride, int dimensions = 4, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(new BYTES(source), byteOffset, itemsCount, byteStride, dimensions, encoding, normalized) { }

        public ColorArray(BYTES source, int byteStride = 0, int dimensions = 4, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, dimensions, encoding, normalized) { }

        public ColorArray(BYTES source, int byteOffset, int itemsCount, int byteStride, int dimensions = 4, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
        {
            Guard.MustBeBetweenOrEqualTo(dimensions, 3, 4, nameof(dimensions));

            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, dimensions, encoding, normalized);
            _Dimensions = dimensions;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private FloatingAccessor _Accessor;

        private readonly int _Dimensions;

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
                return new Vector4(_Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2], _Dimensions < 4 ? 1 : _Accessor[index, 3]);
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

        public int IndexOf(Vector4 item) { return EncodedArrayUtils.FirstIndexOf(this, item); }

        public void CopyTo(Vector4[] array, int arrayIndex) { EncodedArrayUtils.CopyTo(this, array, arrayIndex); }

        void IList<Vector4>.Insert(int index, Vector4 item) { throw new NotSupportedException(); }

        void IList<Vector4>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Vector4>.Add(Vector4 item) { throw new NotSupportedException(); }

        void ICollection<Vector4>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Vector4>.Remove(Vector4 item) { throw new NotSupportedException(); }

        #endregion
    }
}
