using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;

using System.Threading.Tasks;

using NJsonSchema.References;

using JSONSCHEMA = NJsonSchema.JsonSchema;

namespace SharpGLTF
{
    public static class SchemaProcessing
    {
        #region schema loader

        public static SchemaType.Context LoadExtensionSchemaContext(string srcSchema)
        {
            var context = LoadSchemaContext(srcSchema);
            context.IgnoredByCodeEmittierMainSchema();
            return context;
        }

        public static SchemaType.Context LoadSchemaContext(string srcSchema)
        {
            var schema = LoadSchema(srcSchema);

            var settings = new NJsonSchema.CodeGeneration.CSharp.CSharpGeneratorSettings
            {

                Namespace = "glTf.POCO",
                ClassStyle = NJsonSchema.CodeGeneration.CSharp.CSharpClassStyle.Poco
            };

            var ctypes = new NJsonSchema.CodeGeneration.CSharp.CSharpTypeResolver(settings);
            ctypes.Resolve(schema, false, null);

            return SchemaTypesReader.Generate(ctypes);
        }

        private static JSONSCHEMA LoadSchema(string filePath)
        {
            // https://blogs.msdn.microsoft.com/benjaminperkins/2017/03/08/how-to-call-an-async-method-from-a-console-app-main-method/

            if (!System.IO.File.Exists(filePath)) throw new System.IO.FileNotFoundException(nameof(filePath), filePath);

            return JSONSCHEMA
                .FromFileAsync(filePath, s => _Resolver(s, filePath))
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        static NJsonSchema.JsonReferenceResolver _Resolver(JSONSCHEMA schema, string basePath)
        {
            var generator = new NJsonSchema.NewtonsoftJson.Generation.NewtonsoftJsonSchemaGeneratorSettings();

            var solver = new NJsonSchema.JsonSchemaAppender(schema, generator.TypeNameGenerator);

            return new MyReferenceResolver(solver);
        }

        class MyReferenceResolver : NJsonSchema.JsonReferenceResolver
        {
            public MyReferenceResolver(NJsonSchema.JsonSchemaAppender resolver) : base(resolver) { }

            public override Task<IJsonReference> ResolveFileReferenceAsync(string filePath, System.Threading.CancellationToken cancellationToken)
            {
                if (System.IO.File.Exists(filePath)) return base.ResolveFileReferenceAsync(filePath, cancellationToken);

                filePath = System.IO.Path.GetFileName(filePath);
                filePath = System.IO.Path.Combine(Constants.MainSchemaDir, filePath);

                if (System.IO.File.Exists(filePath)) return base.ResolveFileReferenceAsync(filePath, cancellationToken);

                throw new System.IO.FileNotFoundException(filePath);
            }
        }

        #endregion

        public static void EmitCodeFromSchema(string projectPath, string dstFile, SchemaType.Context ctx, IReadOnlyList<SchemaProcessor> extensions)
        {
            var newEmitter = new CSharpEmitter();
            newEmitter.DeclareContext(ctx);           

            foreach(var ext in extensions)
            {
                ext.PrepareTypes(newEmitter, ctx);
            }

            var textOut = newEmitter.EmitContext(ctx);

            var dstDir = _FindTargetDirectory(projectPath);
            var dstPath = System.IO.Path.Combine(dstDir, $"{dstFile}.cs");

            System.IO.File.WriteAllText(dstPath, textOut);
        }

        private static string _FindTargetDirectory(string dstDir)
        {
            var dir = Constants.LocalRepoDirectory;

            while (dir.Length > 3)
            {
                var xdir = System.IO.Path.Combine(dir, dstDir);
                if (System.IO.Directory.Exists(xdir)) return xdir;

                dir = System.IO.Path.GetDirectoryName(dir); // move up
            }

            return null;
        }
    }
}
