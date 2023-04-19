using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// Implemented by children of <see cref="ChildrenList{T, TParent}"/>
    /// </summary>
    /// <typeparam name="TParent">The type of the parent class containing the collection.</typeparam>
    interface IChildOfList<TParent>
        where TParent : class
    {
        /// <summary>
        /// Gets the logical parent that owns the collection containing this object.
        /// </summary>
        TParent LogicalParent { get; }

        /// <summary>
        /// Gets the logical index of this item within the parent's collection.
        /// </summary>
        int LogicalIndex { get; }        

        /// <summary>
        /// Assigns a parent and index to this object.
        /// </summary>
        /// <param name="parent">The new parent, or null</param>
        /// <param name="index">The new index, or -1</param>
        /// <remarks>
        /// For internal use of the collection.<br/>
        /// ALWAYS IMPLEMENT EXPLICITLY!
        /// </remarks>
        void SetLogicalParent(TParent parent, int index);
    }
}
