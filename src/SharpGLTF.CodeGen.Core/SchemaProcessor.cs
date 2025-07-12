using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    public abstract class SchemaProcessor
    {
        public virtual string GetTargetProject() { return Constants.TargetProjectDirectory; }

        public abstract IEnumerable<(string TargetFileName, SchemaReflection.SchemaType.Context Schema)> ReadSchema();

        public abstract void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaReflection.SchemaType.Context ctx);
    }
}
