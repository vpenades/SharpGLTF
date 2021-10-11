using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    abstract class SchemaProcessor
    {
        public abstract IEnumerable<(string TargetFileName, SchemaReflection.SchemaType.Context Schema)> Process();

        public abstract void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaReflection.SchemaType.Context ctx);
    }
}
