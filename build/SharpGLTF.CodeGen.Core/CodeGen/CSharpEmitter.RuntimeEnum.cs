using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpGLTF.CodeGen
{    
    partial class CSharpEmitter
    {
        /// <summary>
        /// Represents an enum within <see cref="_RuntimeType"/>
        /// </summary>
        [System.Diagnostics.DebuggerDisplay("Enum: {_Name}")]
        class _RuntimeEnum
        {
            internal _RuntimeEnum(string name) { _Name = name; }

            private readonly string _Name;
        }
    }
}

