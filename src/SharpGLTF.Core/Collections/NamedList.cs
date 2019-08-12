using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// Stores values by index, and optionally, by name.
    /// </summary>
    /// <typeparam name="T">Any type.</typeparam>
    class NamedList<T> : List<T>
    {
        private Dictionary<string, int> _ByName;

        public IReadOnlyCollection<String> Names => _ByName != null ? _ByName.Keys : (IReadOnlyCollection<String>)Array.Empty<string>();

        public void SetValue(int index, string name, T value)
        {
            Guard.MustBeGreaterThanOrEqualTo(index, 0, nameof(index));

            if (!string.IsNullOrEmpty(name))
            {
                if (_ByName == null) _ByName = new Dictionary<string, int>();
                _ByName[name] = index;
            }

            while (this.Count <= index) this.Add(default);

            this[index] = value;
        }

        public int IndexOf(string name)
        {
            if (_ByName == null) return default;

            if (string.IsNullOrEmpty(name)) return default;

            return _ByName.TryGetValue(name, out int index) ? index : -1;
        }
    }
}
