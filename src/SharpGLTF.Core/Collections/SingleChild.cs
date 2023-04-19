using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Collections
{
    sealed class SingleChild<T, TParent>
        where T : class, IChildOfList<TParent>
        where TParent : class
    {
        #region lifecycle

        public SingleChild(TParent parent)
        {
            Guard.NotNull(parent, nameof(parent));
            _Parent = parent;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly TParent _Parent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private T _Child;

        #endregion

        #region properties

        public T Value
        {
            get => this._Child;
            set
            {
                if (this.Value == this._Child)
                {
                    return;
                }

                // orphan the current child
                if (this._Child != null) { this._Child.SetLogicalParent(null, -1); }
                this._Child = null;

                // adopt the new child
                this._Child = value;
                if (this._Child != null) { this._Child.SetLogicalParent(_Parent, 0); }
            }
        }

        #endregion
    }
}
