using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    /// <summary>
    /// Exposes "reflection like" functionality of vertex types
    /// </summary>
    /// <remarks>
    /// Implemented by:<br/>
    /// - <see cref="IVertexGeometry"/><br/>
    /// - <see cref="IVertexMaterial"/><br/>
    /// - <see cref="IVertexSkinning"/><br/>    
    /// </remarks>
    public interface IVertexReflection
    {
        /// <summary>
        /// Gets the information used to know how to encode the attributes stored in this vertex fragment.
        /// </summary>
        /// <returns>A list of attribute-encoding pairs</returns>
        public IEnumerable<KeyValuePair<string, Memory.AttributeFormat>> GetEncodingAttributes();
    }
}
