using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpGLTF.CodeGen
{
    using SchemaReflection;
    
    partial class CSharpEmitter
    {
        /// <summary>
        /// Represents an field within <see cref="_RuntimeType"/>
        /// </summary>
        [System.Diagnostics.DebuggerDisplay("Enum: {_Name}")]
        class _RuntimeField
        {
            #region lifecycle
            internal _RuntimeField(FieldInfo f) { _PersistentField = f; }
            #endregion

            #region data

            private readonly FieldInfo _PersistentField;

            #endregion

            #region properties

            public string PrivateField { get; set; }
            public string PublicProperty { get; set; }

            public string CollectionContainer { get; set; }

            // MinVal, MaxVal, readonly, static

            // serialization sections
            // deserialization sections
            // validation sections
            // clone sections

            #endregion
        }
    }
}

