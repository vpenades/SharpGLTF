using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MeshBuffers
{
    struct ListSegment<T> : IList<T>
    {
        public ListSegment(IList<T> source) : this(source,0,source.Count) { }

        public ListSegment(IList<T> source, int offset, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(offset));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (offset >= source.Count) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset+count-1 >= source.Count) throw new ArgumentOutOfRangeException(nameof(count));

            _Source = source;
            _Offset = offset;
            _Count = count;
        }

        private readonly IList<T> _Source;
        private readonly int _Offset;
        private int _Count;

        public T this[int index]
        {
            get
            {
                if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
                if (index >= _Count) throw new ArgumentOutOfRangeException(nameof(index));
                return _Source[_Offset + index];
            }
            set
            {
                if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
                if (index >= _Count) throw new ArgumentOutOfRangeException(nameof(index));
                _Source[_Offset + index] = value;
            }
        }

        public int Count => _Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            _Source.Insert(_Offset + _Count, item);
            ++_Count;
        }

        public void Clear() { throw new NotImplementedException(); }

        public bool Contains(T item) { throw new NotImplementedException(); }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for(int i=0; i < _Count; ++i)
            {
                array[i + arrayIndex] = _Source[i + _Offset];
            }
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < _Count; ++i)
            {
                if (Object.Equals(item, _Source[i + _Offset])) return i;
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (index >= _Count) throw new ArgumentOutOfRangeException(nameof(index));
            _Source.Insert(index + _Offset, item);
            ++_Count;
        }

        public bool Remove(T item) { throw new NotImplementedException(); }

        public void RemoveAt(int index) { throw new NotImplementedException(); }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _Count; ++i)
            {
                yield return _Source[i + _Offset];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < _Count; ++i)
            {
                yield return _Source[i + _Offset];
            }
        }
    }
}
