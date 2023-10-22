using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// Helper used to handle property assignment with bidirectional referencing
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TParent"></typeparam>
    public readonly struct ChildSetter<TParent>        
        where TParent : class
    {
        #region lifecycle

        public ChildSetter(TParent parent)
        {
            Guard.NotNull(parent, nameof(parent));
            _Parent = parent;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly TParent _Parent;

        #endregion

        #region API        

        public void SetProperty<T>(ref T target, T value)
            where T : class, IChildOfList<TParent>
        {
            if (value == target) return;

            // orphan the current child
            target?.SetLogicalParent(null, -1);
            target = null;

            // adopt the new child
            target = value;
            target?.SetLogicalParent(_Parent, 0);
        }

        #endregion
    }
}
