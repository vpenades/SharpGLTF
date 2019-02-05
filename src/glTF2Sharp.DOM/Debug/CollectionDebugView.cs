using System;
using System.Collections.Generic;
using System.Text;

namespace glTF2Sharp.Debug
{
    /// <summary>
    /// class to visualize collection items during debug
    /// </summary>        
    internal sealed class _CollectionDebugView<T>
    {
        // https://referencesource.microsoft.com/#mscorlib/system/collections/generic/debugview.cs,29

        public _CollectionDebugView(ICollection<T> collection)
        {
            _Collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        private readonly ICollection<T> _Collection;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] items = new T[_Collection.Count];
                _Collection.CopyTo(items, 0);
                return items;
            }
        }
    }
}
