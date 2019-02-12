using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MeshBuffers
{    
    using ROW = ListSegment<Single>;    

    public class VertexBuffer : IList<Vertex>
    {
        #region lifecycle

        public VertexBuffer(VertexDeclaration declaration)
        {
            _Declaration = declaration;
            _Stride = _Declaration.Stride;
        }

        #endregion

        #region data

        private readonly VertexDeclaration _Declaration;

        private readonly int _Stride;

        private readonly List<Single> _Data = new List<float>();

        #endregion

        #region API

        public VertexDeclaration Declaration => _Declaration;        

        public int Count => _Data.Count / _Stride;

        public bool IsReadOnly => throw new NotImplementedException();
        
        public Vertex this[int index]
        {
            get => new Vertex(_Declaration, new ROW(_Data, index * _Stride, _Stride));
            set => throw new NotImplementedException();
        }

        public void Add(Vertex vertex)
        {
            if (!VertexDeclaration.AreEqual(vertex._Declaration, this._Declaration)) throw new ArgumentException(nameof(vertex));
            if (vertex._Data.Count == _Stride) throw new ArgumentException(nameof(vertex));
            _Data.AddRange(vertex._Data);
        }

        public int IndexOf(Vertex item) { throw new NotImplementedException(); }

        public void Insert(int index, Vertex item) { throw new NotImplementedException(); }

        public void RemoveAt(int index) { throw new NotImplementedException(); }

        public void Clear() { _Data.Clear(); }

        public bool Contains(Vertex item) { throw new NotImplementedException(); }

        public void CopyTo(Vertex[] array, int arrayIndex)
        {
            for(int i=0; i < Count; ++i)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public bool Remove(Vertex item) { throw new NotImplementedException(); }

        public IEnumerator<Vertex> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i) yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < Count; ++i) yield return this[i];
        }

        #endregion
    }
}
