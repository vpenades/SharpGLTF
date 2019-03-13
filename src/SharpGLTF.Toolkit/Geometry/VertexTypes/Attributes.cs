using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    [AttributeUsageAttribute(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class VertexAttributeAttribute : Attribute
    {
        #region constructors

        public VertexAttributeAttribute(string attributeName)
        {
            this.Name = attributeName;
            this.Encoding = Schema2.EncodingType.FLOAT;
            this.Normalized = false;
        }

        public VertexAttributeAttribute(string attributeName, Schema2.EncodingType encoding, Boolean normalized)
        {
            this.Name = attributeName;
            this.Encoding = encoding;
            this.Normalized = normalized;
        }

        #endregion

        #region data

        public string Name { get; private set; }

        public Schema2.EncodingType Encoding { get; private set; }

        public Boolean Normalized { get; private set; }

        #endregion
    }
}
