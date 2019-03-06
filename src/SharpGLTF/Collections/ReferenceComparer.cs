using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// this class is used to compare two T class references,
    /// bypassing any equality operator implemented by the T class.
    /// </summary>
    /// <see cref="https://stackoverflow.com/questions/4901320/is-there-any-kind-of-referencecomparer-in-net"/>
    sealed class ReferenceComparer<T> : IEqualityComparer<T>
        where T : class
    {
        private ReferenceComparer() { }

        public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
