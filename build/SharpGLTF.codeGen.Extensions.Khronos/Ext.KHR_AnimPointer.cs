using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    class AnimationPointerExtension : SchemaProcessor
    {
        private static string SchemaUri => KhronosExtensions.KhronosExtensionPath("KHR_animation_pointer", "animation.channel.target.KHR_animation_pointer.schema.json");

        private const string ExtensionRootClassName = "KHR_animation_pointer glTF Animation Channel Target Extension";

        public override IEnumerable<(string, SchemaType.Context)> ReadSchema()
        {
            var ctx = SchemaProcessing.LoadExtensionSchemaContext(SchemaUri);

            yield return ("ext.AnimPointer.g", ctx);
        }

        public override void PrepareTypes(CodeGen.CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName(ExtensionRootClassName, "AnimationPointer");
        }
    }
}
