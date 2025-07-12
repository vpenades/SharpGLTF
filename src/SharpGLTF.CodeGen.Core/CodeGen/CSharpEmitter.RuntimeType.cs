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
        /// Represents the runtime information associated to a given <see cref="SchemaType"/>
        /// </summary>
        [System.Diagnostics.DebuggerDisplay("{RuntimeNamespace}.{RuntimeName}")]
        class _RuntimeType
        {
            #region lifecycle
            internal _RuntimeType(SchemaType t) { _PersistentType = t; }

            #endregion

            #region data

            /// <summary>
            /// Schema type used for serialization
            /// </summary>
            private readonly SchemaType _PersistentType;

            /// <summary>
            /// Namespace in which the source code will be generated.
            /// </summary>
            public string RuntimeNamespace { get; set; }

            /// <summary>
            /// The name of the type used to generate the source code.
            /// </summary>
            public string RuntimeName { get; set; }

            /// <summary>
            /// Additional comments added to the generated source code.
            /// </summary>
            public List<string> RuntimeComments { get; } = new List<string>();

            /// <summary>
            /// Fields of this type.
            /// </summary>
            private readonly Dictionary<string, _RuntimeField> _Fields = new Dictionary<string, _RuntimeField>();

            /// <summary>
            /// Enums of this type.
            /// </summary>
            private readonly Dictionary<string, _RuntimeEnum> _Enums = new Dictionary<string, _RuntimeEnum>();

            #endregion

            #region API

            public _RuntimeField UseField(FieldInfo finfo)
            {
                var key = $"{finfo.PersistentName}";

                if (_Fields.TryGetValue(key, out _RuntimeField rfield)) return rfield;

                rfield = new _RuntimeField(finfo);

                _Fields[key] = rfield;

                return rfield;
            }

            public _RuntimeEnum UseEnum(string name)
            {
                var key = name;

                if (_Enums.TryGetValue(key, out _RuntimeEnum renum)) return renum;

                renum = new _RuntimeEnum(name);

                _Enums[key] = renum;

                return renum;
            }

            #endregion
        }
    }
}