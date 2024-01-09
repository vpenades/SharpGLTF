using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertexReflection
    {
        /// <summary>
        /// Gets the information used used to know how to encode the attributes stored in this vertex fragment.
        /// </summary>
        /// <returns>A list of attribute-encoding pairs</returns>
        public IEnumerable<KeyValuePair<string, Memory.AttributeFormat>> GetEncodingAttributes();
    }
}
