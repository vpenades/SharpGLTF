using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Represents a readable and sometimes writeable(*) array of elements
    /// </summary>
    /// <typeparam name="T">
    /// For Index Buffers: <see cref="UInt32"/><br/>
    /// For Vertex Buffers: <see cref="System.Numerics.Vector2"/>, <see cref="System.Numerics.Vector3"/>, <see cref="System.Numerics.Vector4"/> and so on.<br/>
    /// For animations and other plain data: <see cref="float"/>, <see cref="System.Numerics.Quaternion"/>, <see cref="System.Numerics.Quaternion"/> and so on.<br/>
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// This interface is exposed by <see cref="Schema2.Accessor"/> to easily encode/decode packed and strided elements
    /// </para>
    /// <para>
    /// Implemented by:<br/>
    /// <see cref="SparseArray{T}"/> (Read Only)<br/>
    /// <see cref="IntegerArray"/><br/>
    /// <see cref="ScalarArray"/><br/>
    /// <see cref="Vector2Array"/><br/>
    /// <see cref="Vector3Array"/><br/>
    /// <see cref="Vector4Array"/><br/>
    /// <see cref="QuaternionArray"/><br/>
    /// <see cref="Matrix2x2Array"/><br/>
    /// <see cref="Matrix3x2Array"/><br/>
    /// <see cref="Matrix3x3Array"/><br/>
    /// <see cref="Matrix4x3Array"/><br/>
    /// <see cref="Matrix4x3Array"/><br/>
    /// </para>
    /// </remarks>
    public interface IAccessorArray<T> : IReadOnlyList<T>  , IList<T>
    {
        // Because this.Count and this[index] exist at both at IList<T> and IReadOnlyList<T>,
        // attempting to use them may cause an ambiguity error. This will be fixed in Net10.        
        // see:
        // https://github.com/dotnet/runtime/issues/31001#issuecomment-2942678308
        // https://github.com/dotnet/runtime/pull/115802

        new T this[int index] { get; set; }
        new int Count { get; }                
    }

    public readonly struct ZeroAccessorArray<T> : IAccessorArray<T>
    {
        static ZeroAccessorArray()
        {
            _Default = default(T);
            if (typeof(T) == typeof(float[])) _Default = (T)(Object)Array.Empty<T>();
        }

        public ZeroAccessorArray(int count)
        {
            Count = count;            
        }

        private static readonly T _Default;

        public bool IsReadOnly => true;

        public T this[int index] { get => _Default; set => throw new NotSupportedException(); }

        public int Count { get; }

        public int IndexOf(T item)
        {
            return Count > 0 && item.Equals(_Default) ? 0 : -1;
        }

        public bool Contains(T item)
        {
            return Count > 0 && item.Equals(_Default);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for(int i=0; i < Count; ++i)
            {
                array[i + arrayIndex] = default;
            }
        }        

        public IEnumerator<T> GetEnumerator()
        {
            return Enumerable.Repeat(_Default, Count).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Enumerable.Repeat(_Default, Count).GetEnumerator();
        }

        void IList<T>.Insert(int index, T item) { throw new NotSupportedException(); }

        void IList<T>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<T>.Add(T item) { throw new NotSupportedException(); }

        void ICollection<T>.Clear() { throw new NotSupportedException(); }

        bool ICollection<T>.Remove(T item) { throw new NotSupportedException(); }
    }
}
