using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
    public readonly partial struct MaterialValue : IEquatable<MaterialValue>
    {
        #region constructors

        public static implicit operator MaterialValue(Single value) { return new MaterialValue(value); }

        public static implicit operator MaterialValue(Vector2 value) { return new MaterialValue(value.X, value.Y); }

        public static implicit operator MaterialValue(Vector3 value) { return new MaterialValue(value.X, value.Y, value.Z); }

        public static implicit operator MaterialValue(Vector4 value) { return new MaterialValue(value.X, value.Y, value.Z, value.W); }

        public static MaterialValue CreateFrom(Object value)
        {
            if (value is Single v1) return v1;
            if (value is Vector2 v2) return v2;
            if (value is Vector3 v3) return v3;
            if (value is Vector4 v4) return v4;
            throw new ArgumentException("Value type not supported.", nameof(value));
        }

        private MaterialValue(float x) { _Length = 1; _X = x; _Y = 0; _Z = 0; _W = 0; }
        private MaterialValue(float x, float y) { _Length = 2; _X = x; _Y = y; _Z = 0; _W = 0; }
        private MaterialValue(float x, float y, float z) { _Length = 3; _X = x; _Y = y; _Z = z; _W = 0; }
        private MaterialValue(float x, float y, float z, float w) { _Length = 4; _X = x; _Y = y; _Z = z; _W = w; }

        #endregion

        #region data

        private readonly int _Length;
        private readonly float _X;
        private readonly float _Y;
        private readonly float _Z;
        private readonly float _W;

        public override int GetHashCode()
        {
            if (_Length == 0) return 0;
            var h = _X.GetHashCode();
            if (_Length == 1) return h;
            h ^= _Y.GetHashCode();
            if (_Length == 2) return h;
            h ^= _Z.GetHashCode();
            if (_Length == 3) return h;
            h ^= _W.GetHashCode();
            return h;
        }

        public override bool Equals(object obj) { return obj is MaterialValue other && Equals(other); }

        public bool Equals(MaterialValue other) { return AreEqual(this, other); }

        public static bool operator ==(in MaterialValue a, in MaterialValue b) => AreEqual(a, b);

        public static bool operator !=(in MaterialValue a, in MaterialValue b) => !AreEqual(a, b);

        public static bool AreEqual(in MaterialValue a, in MaterialValue b)
        {
            if (a._Length != b._Length) return false;

            if (a._Length == 0) return true;
            if (a._X != b._X) return false;
            if (a._Length == 1) return true;
            if (a._Y != b._Y) return false;
            if (a._Length == 2) return true;
            if (a._Z != b._Z) return false;
            if (a._Length == 3) return true;
            if (a._W != b._W) return false;
            return true;
        }

        #endregion

        #region properties

        public Type ValueType
        {
            get
            {
                switch (_Length)
                {
                    case 1: return typeof(float);
                    case 2: return typeof(Vector2);
                    case 3: return typeof(Vector3);
                    case 4: return typeof(Vector4);
                    default: throw new InvalidOperationException($"{_Length} not supported.");
                }
            }
        }

        #endregion

        #region API

        public static explicit operator Single(MaterialValue value)
        {
            if (value._Length != 1) throw new InvalidOperationException();
            return value._X;
        }

        public static explicit operator Vector2(MaterialValue value)
        {
            if (value._Length != 2) throw new InvalidOperationException();
            return new Vector2(value._X, value._Y);
        }

        public static explicit operator Vector3(MaterialValue value)
        {
            if (value._Length != 3) throw new InvalidOperationException();
            return new Vector3(value._X, value._Y, value._Z);
        }

        public static explicit operator Vector4(MaterialValue value)
        {
            if (value._Length != 4) throw new InvalidOperationException();
            return new Vector4(value._X, value._Y, value._Z, value._W);
        }

        public object ToTypeless()
        {
            switch (_Length)
            {
                case 1: return (Single)this;
                case 2: return (Vector2)this;
                case 3: return (Vector3)this;
                case 4: return (Vector4)this;
                default: throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            return ToTypeless().ToString();
        }

        #endregion

        #region nested types

        [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
        internal sealed class _Property : IEquatable<_Property>
        {
            #region lifecycle
            internal _Property(KnownProperty key, float value)
            {
                this.Key = key;
                this._Default = value;
                this.Value = _Default;
            }

            internal _Property(KnownProperty key, Vector2 value)
            {
                this.Key = key;
                this._Default = value;
                this.Value = _Default;
            }

            internal _Property(KnownProperty key, Vector3 value)
            {
                this.Key = key;
                this._Default = value;
                this.Value = _Default;
            }

            internal _Property(KnownProperty key, Vector4 value)
            {
                this.Key = key;
                this._Default = value;
                this.Value = _Default;
            }

            #endregion

            #region data

            public KnownProperty Key { get; }

            private readonly MaterialValue _Default;
            private MaterialValue _Value;

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return Key.GetHashCode() ^ _Value.GetHashCode();
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                return obj is _Property other && Equals(other);
            }

            /// <inheritdoc/>
            public bool Equals(_Property other)
            {
                return AreEqual(this, other);
            }

            public static bool operator ==(_Property a, _Property b) { return a.Equals(b); }

            public static bool operator !=(_Property a, _Property b) { return !a.Equals(b); }

            public static bool AreEqual(_Property a, _Property b)
            {
                if (a.Key != b.Key) return false;
                if (!a._Default.Equals(b._Default)) return false;
                if (!a._Value.Equals(b._Value)) return false;
                return true;
            }

            #endregion

            #region API
            public string Name => Key.ToString();

            public MaterialValue Value
            {
                get => _Value;
                set
                {
                    if (value._Length != this._Default._Length) throw new ArgumentOutOfRangeException(nameof(value));
                    _Value = value;
                }
            }

            public void SetDefault() { _Value = _Default; }

            public override string ToString()
            {
                return new KeyValuePair<string, MaterialValue>(Name, Value).ToString();
            }

            #endregion
        }

        [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
        public sealed class Collection : IReadOnlyDictionary<KnownProperty, MaterialValue>
        {
            #region debug

            private string _GetDebuggerDisplay()
            {
                return string.Join(", ", _Properties.Select(item => item.ToString()));
            }

            #endregion

            #region lifecycle

            internal Collection(_Property[] properties)
            {
                _Properties = properties;
            }

            #endregion

            #region data

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
            private readonly _Property[] _Properties;

            public override int GetHashCode()
            {
                int h = 0;

                foreach (var p in _Properties)
                {
                    h ^= p.GetHashCode();
                }

                return h;
            }

            public static bool AreEqual(Collection x, Collection y)
            {
                Guard.NotNull(x, nameof(x));
                Guard.NotNull(y, nameof(y));

                if (x._Properties.Length != y._Properties.Length) return false;

                for (int i = 0; i < x._Properties.Length; ++i)
                {
                    var xp = x._Properties[i];
                    var yp = y._Properties[i];

                    if (xp.Name != yp.Name) return false;
                    if (xp.Value != yp.Value) return false;
                }

                return true;
            }

            #endregion

            #region properties

            public MaterialValue this[KnownProperty key]
            {
                get => _Properties.First(item => item.Key == key).Value;
                set
                {
                    var idx = Array.FindIndex(_Properties, item => item.Key == key);
                    if (idx < 0) throw new KeyNotFoundException(key.ToString());
                    _Properties[idx].Value = value;
                }
            }

            public MaterialValue this[string keyName]
            {
                get
                {
                    return Enum.TryParse<KnownProperty>(keyName, out var key)
                        ? this[key]
                        : throw new KeyNotFoundException(keyName);
                }

                set
                {
                    if (!Enum.TryParse<KnownProperty>(keyName, out var key)) throw new KeyNotFoundException(keyName);
                    this[key] = value;
                }
            }

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public IEnumerable<KnownProperty> Keys => _Properties.Select(item => item.Key);

            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public IEnumerable<MaterialValue> Values => _Properties.Select(item => item.Value);

            public int Count => _Properties.Length;

            /// <summary>
            /// Combines multiple properties into a single Vector4
            /// (as long as the combined number of floats is 4 or less)
            /// </summary>
            [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
            public Vector4 CombinedVector
            {
                get
                {
                    Span<float> tmp = stackalloc float[4];
                    int idx = 0;

                    foreach (var p in _Properties)
                    {
                        idx += p.Value._CopyTo(tmp.Slice(idx));
                    }

                    return new Vector4(tmp[0], tmp[1], tmp[2], tmp[3]);
                }
                set
                {
                    Span<float> tmp = stackalloc float[4];
                    tmp[0] = value.X;
                    tmp[1] = value.Y;
                    tmp[2] = value.Z;
                    tmp[3] = value.W;

                    int idx = 0;

                    foreach (var p in _Properties)
                    {
                        var t = p.Value.ValueType;

                        if (t == typeof(Single)) { p.Value = tmp[idx++]; }
                        if (t == typeof(Vector2)) { p.Value = new Vector2(tmp[idx + 0], tmp[idx + 1]); idx += 2; }
                        if (t == typeof(Vector3)) { p.Value = new Vector3(tmp[idx + 0], tmp[idx + 1], tmp[idx + 2]); idx += 3; }
                        if (t == typeof(Vector4)) { p.Value = new Vector4(tmp[idx + 0], tmp[idx + 1], tmp[idx + 2], tmp[idx + 3]); idx += 4; }
                    }
                }
            }

            #endregion

            #region API

            public bool ContainsKey(KnownProperty key)
            {
                return _Properties.Any(item => item.Key == key);
            }

            public bool TryGetValue(KnownProperty key, out MaterialValue value)
            {
                var idx = Array.FindIndex(_Properties, item => item.Key == key);

                if (idx < 0) { value = default; return false; }

                value = _Properties[idx].Value;
                return true;
            }

            public IEnumerator<KeyValuePair<KnownProperty, MaterialValue>> GetEnumerator()
            {
                return _Properties
                    .Select(item => new KeyValuePair<KnownProperty, MaterialValue>(item.Key, item.Value))
                    .GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _Properties
                    .Select(item => new KeyValuePair<KnownProperty, MaterialValue>(item.Key, item.Value))
                    .GetEnumerator();
            }

            public void Reset()
            {
                foreach (var p in _Properties) p.SetDefault();
            }

            public void CopyTo(Collection other)
            {
                Guard.NotNull(other, nameof(other));

                for (int i = 0; i < this._Properties.Length; ++i)
                {
                    var src = this._Properties[i];
                    var dst = other._Properties[i];

                    if (src.Name != dst.Name) throw new ArgumentException("Naming mismatch.", nameof(other));

                    dst.Value = src.Value;
                }
            }

            #endregion
        }

        #endregion

        #region helpers

        internal int _CopyTo(Span<float> dst)
        {
            if (_Length > 0) dst[0] = _X;
            if (_Length > 1) dst[1] = _Y;
            if (_Length > 2) dst[2] = _Z;
            if (_Length > 3) dst[3] = _W;
            return _Length;
        }

        #endregion
    }
}
