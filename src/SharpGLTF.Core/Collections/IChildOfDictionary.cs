using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Collections
{
    /// <summary>
    /// Implemented by children of <see cref="ChildrenDictionary{T, TParent}"/>
    /// </summary>
    /// <typeparam name="TParent">The type of the parent class containing the collection.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "no better option")]
    public interface IChildOfDictionary<TParent>
        where TParent : class
    {
        /// <summary>
        /// Gets the logical parent that owns the collection containing this object.
        /// </summary>
        TParent LogicalParent { get; }

        /// <summary>
        /// Gets the logical key of this item within the parent's collection.
        /// </summary>
        string LogicalKey { get; }

        /// <summary>
        /// Assigns a parent and index to this object.
        /// </summary>
        /// <param name="parent">The new parent, or null</param>
        /// <param name="key">The new key, or null</param>
        /// <remarks>
        /// For internal use of the collection.<br/>
        /// ALWAYS IMPLEMENT EXPLICITLY!
        /// </remarks>
        void SetLogicalParent(TParent parent, string key);
    }
}
