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
        private List<string> _ByIndex;

        public IReadOnlyCollection<String> Names => _ByName != null ? _ByName.Keys : (IReadOnlyCollection<String>)Array.Empty<string>();

        public void SetName(int index, string name, T value)
        {
            Guard.MustBeGreaterThanOrEqualTo(index, 0, nameof(index));

            if (!string.IsNullOrEmpty(name))
            {
                if (_ByName == null) _ByName = new Dictionary<string, int>();
                if (_ByIndex == null) _ByIndex = new List<string>();
                if (_ByIndex.Count <= index) _ByIndex.Add(null);

                _ByName[name] = index;
                _ByIndex[index] = name;
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

        public string NameOf(int index)
        {
            if (_ByIndex == null) return null;
            if (index < 0 || index >= _ByIndex.Count) return null;
            return _ByIndex[index];
        }
    }
}
