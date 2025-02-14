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

        public void SetProperty<TProperty, TValue>(ref TProperty target, TValue value)
            where TProperty : class
            where TValue: TProperty
        {
            if (Object.ReferenceEquals(value , target)) return;

            // orphan the current child
            if (target is IChildOf<TParent> oldChild) oldChild.SetLogicalParent(null);            

            // adopt the new child
            target = value;
            if (target is IChildOf<TParent> newChild) newChild.SetLogicalParent(_Parent);
        }

        #endregion
    }
}
