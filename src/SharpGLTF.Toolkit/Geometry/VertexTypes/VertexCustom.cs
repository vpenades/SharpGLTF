using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    /// <summary>
    /// Represents the interface that must be implemented by a custom vertex fragment.
    /// </summary>
    public interface IVertexCustom : IVertexMaterial
    {
        /// <summary>
        /// Validates the custom attributes of the vertex fragment.<br/>
        /// Called by <see cref="VertexBuilder{TvG, TvM, TvS}.Validate"/>.
        /// </summary>
        void Validate();

        /// <summary>
        /// Gets a collection of the attribute keys defined in this vertex.
        /// </summary>
        /// <example>
        /// <code>
        /// private static readonly string[] _CustomNames = { "CustomFloat" };
        /// public IEnumerable&lt;string&gt; CustomAttributes =&gt; _CustomNames;
        /// </code>
        /// </example>
        IEnumerable<string> CustomAttributes { get; }

        /// <summary>
        /// Tries to get a custom attribute.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <param name="value">the value if found, or null if not found.</param>
        /// <returns>true if the value was found. False otherwise.</returns>
        /// <example>
        /// <code>
        /// public bool TryGetCustomAttribute(string attributeName, out object value)
        /// {
        ///     if (attributeName != "CustomFloat") { value = null; return false; }
        ///     value = this.CustomValue; return true;
        /// }
        /// </code>
        /// </example>
        bool TryGetCustomAttribute(string attributeName, out Object value);

        /// <summary>
        /// Sets a custom attribute only if <paramref name="attributeName"/> is defined in the vertex.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <param name="value">The attribute value.</param>
        /// <example>
        /// <code>
        /// public void SetCustomAttribute(string attributeName, object value)
        /// {
        ///     if (attributeName == "CustomFloat" &amp;&amp; value is float f) this.CustomValue = f;
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// If <paramref name="attributeName"/> is not defined in the custom vertex,<br/>
        /// the method must not do any action.
        /// </remarks>
        void SetCustomAttribute(string attributeName, Object value);
    }
}
