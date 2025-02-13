using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// Implemented by children of <see cref="ChildSetter{TParent}"/>
    /// </summary>
    /// <typeparam name="TParent">The type of the parent class containing the collection.</typeparam>
    public interface IChildOf<TParent>
        where TParent : class
    {
        /// <summary>
        /// Gets the logical parent that owns this object.
        /// </summary>
        TParent LogicalParent { get; }        

        /// <summary>
        /// Assigns a parent and index to this object.
        /// </summary>
        /// <param name="parent">The new parent, or null</param>        
        /// <remarks>
        /// For internal use of the collection.<br/>
        /// ALWAYS IMPLEMENT EXPLICITLY!
        /// </remarks>
        void SetLogicalParent(TParent parent);
    }
}
