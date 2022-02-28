using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Collections
{
    public readonly struct Triple<T> : IReadOnlyList<T>, IEquatable<Triple<T>>
    {
        public static implicit operator Triple<T>(in (T A, T B, T C) triple)
        {
            return new Triple<T>(triple.A, triple.B, triple.C);
        }

        public Triple(T @a, T @b, T @c)
        {
            A = @a;
            B = @b;
            C = @c;
        }

        public readonly T A;
        public readonly T B;
        public readonly T C;

        public override int GetHashCode()
        {
            int h = 0;
            h ^= A?.GetHashCode() ?? 0;
            h ^= B?.GetHashCode() ?? 0;
            h ^= C?.GetHashCode() ?? 0;
            return h;
        }

        public override bool Equals(object obj)
        {
            return obj is Triple<T> && Equals((Triple<T>)obj);
        }

        public bool Equals(Triple<T> other)
        {
            if (!this.A.Equals(other.A)) return false;
            if (!this.B.Equals(other.B)) return false;
            if (!this.C.Equals(other.C)) return false;
            return true;
        }

        public int Count => 3;

        public T this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return A;
                    case 1: return B;
                    case 2: return C;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return A;
            yield return B;
            yield return C;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return A;
            yield return B;
            yield return C;
        }
    }
}
