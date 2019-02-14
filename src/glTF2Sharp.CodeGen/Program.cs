using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NJsonSchema.References;

namespace glTF2Sharp
{
    using CodeGen;
    using SchemaReflection;    

    class Program
    {
        #region MAIN

        static void Main(string[] args)
        {
            SchemaDownload.Syncronize(Constants.RemoteSchemaRepo, Constants.LocalRepoDirectory);

            _ProcessMainSchema();

            _ProcessKhronosPBRExtension();
            _ProcessKhronosUnlitExtension();

            // these extansions are not fully supported and temporarily removed:
            // _ProcessDracoExtension();
            // _ProcessMicrosoftLODExtension();
        }

        #endregion

        #region Main Schema code generation        

        private static void _ProcessMainSchema()
        {
            var ctx1 = LoadSchemaContext(Constants.MainSchemaFile);

            ctx1.Remove(ctx1.Classes.FirstOrDefault(item => item.PersistentName == "glTF Property"));

            // mimeType "anyof" is basically the string to use.
            ctx1.Remove(ctx1.Enumerations.FirstOrDefault(item => item.PersistentName == "image/jpeg-image/png"));
            ctx1.Classes
                .ToArray()
                .FirstOrDefault(item => item.PersistentName == "Image")
                .UseField("mimeType")
                .FieldType = ctx1.UseString();

            var node = ctx1.Classes
                .ToArray()
                .FirstOrDefault(item => item.PersistentName == "Node");

            node.UseField("matrix").SetDataType(typeof(System.Numerics.Matrix4x4), true).RemoveDefaultValue().SetItemsRange(0);
            node.UseField("rotation").SetDataType(typeof(System.Numerics.Quaternion), true).RemoveDefaultValue().SetItemsRange(0);
            node.UseField("scale").SetDataType(typeof(System.Numerics.Vector3), true).RemoveDefaultValue().SetItemsRange(0);
            node.UseField("translation").SetDataType(typeof(System.Numerics.Vector3), true).RemoveDefaultValue().SetItemsRange(0);

            ctx1.Classes
                .ToArray()
                .FirstOrDefault(item => item.PersistentName == "Material")
                .UseField("emissiveFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.Zero")
                .SetItemsRange(0);

            ctx1.Classes
                .ToArray()
                .FirstOrDefault(item => item.PersistentName == "Material PBR Metallic Roughness")
                .UseField("baseColorFactor")
                .SetDataType(typeof(System.Numerics.Vector4), true)
                .SetDefaultValue("Vector4.One")
                .SetItemsRange(0);

            ProcessSchema("gltf.g", ctx1);
        }

        #endregion

        #region Extensions code generation        

        private static void _ProcessDracoExtension()
        {
            var ctx2 = LoadSchemaContext(Constants.KhronosDracoSchemaFile);

            ProcessSchema("ext.draco.g",ctx2);
        }

        private static void _ProcessKhronosPBRExtension()
        {
            var ctx4 = LoadSchemaContext(Constants.KhronosPbrSpecGlossSchemaFile);

            // remove already defined classes
            ctx4.Remove("glTF Property");
            ctx4.Remove("Texture Info");

            ctx4.Classes
                .ToArray()
                .FirstOrDefault(item => item.PersistentName == "KHR_materials_pbrSpecularGlossiness glTF extension")
                .UseField("diffuseFactor")
                .SetDataType(typeof(System.Numerics.Vector4), true)
                .SetDefaultValue("Vector4.One")
                .SetItemsRange(0);

            ctx4.Classes
                .ToArray()
                .FirstOrDefault(item => item.PersistentName == "KHR_materials_pbrSpecularGlossiness glTF extension")
                .UseField("specularFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
                .SetItemsRange(0);

            ProcessSchema("ext.pbrSpecularGlossiness.g", ctx4);
        }

        private static void _ProcessKhronosUnlitExtension()
        {
            var ctx4 = LoadSchemaContext(Constants.KhronesUnlitSchemaFile);
            ctx4.Remove("glTF Property");            

            ProcessSchema("ext.Unlit.g", ctx4);
        }

        /*
        private static void _ProcessMicrosoftLODExtension()
        {
            var ctx3 = LoadSchemaContext("glTF\\extensions\\2.0\\Vendor\\MSFT_lod\\schema\\glTF.MSFT_lod.schema.json");
            ctx3.Remove("glTF Property");
            ProcessSchema("ext.msft_lod.g", ctx3);
        }*/

        #endregion

        #region code generation

        private static string _FindTargetDirectory(string dstDir)
        {
            var dir = Constants.LocalRepoDirectory;

            while(dir.Length > 3)
            {
                var xdir = System.IO.Path.Combine(dir, dstDir);
                if (System.IO.Directory.Exists(xdir)) return xdir;

                dir = System.IO.Path.GetDirectoryName(dir); // move up
            }

            return null;
        }

        private static void ProcessSchema(string dstFile, SchemaType.Context ctx)
        {
            var newEmitter = new CSharpEmitter();
            newEmitter.DeclareContext(ctx);
            newEmitter.SetCollectionContainer("List<TItem>");

            const string rootName = "ModelRoot";            

            newEmitter.SetRuntimeName("glTF", rootName);
            newEmitter.SetRuntimeName("glTF Property", "ExtensionsProperty");
            newEmitter.SetRuntimeName("glTF Child of Root Property", "LogicalChildOfRoot");

            // newEmitter.SetRuntimeName("Sampler", "TextureSampler");

            newEmitter.SetRuntimeName("UNSIGNED_BYTE-UNSIGNED_INT-UNSIGNED_SHORT", "IndexType");            
            newEmitter.SetRuntimeName("BYTE-FLOAT-SHORT-UNSIGNED_BYTE-UNSIGNED_INT-UNSIGNED_SHORT", "ComponentType");            
            newEmitter.SetRuntimeName("MAT2-MAT3-MAT4-SCALAR-VEC2-VEC3-VEC4", "ElementType");
            newEmitter.SetRuntimeName("rotation-scale-translation-weights", "PathType");
            newEmitter.SetRuntimeName("ARRAY_BUFFER-ELEMENT_ARRAY_BUFFER", "BufferMode");
            newEmitter.SetRuntimeName("orthographic-perspective", "CameraType");
            newEmitter.SetRuntimeName("BLEND-MASK-OPAQUE", "AlphaMode");
            newEmitter.SetRuntimeName("LINE_LOOP-LINE_STRIP-LINES-POINTS-TRIANGLE_FAN-TRIANGLE_STRIP-TRIANGLES", "PrimitiveType");
            newEmitter.SetRuntimeName("CUBICSPLINE-LINEAR-STEP", "AnimationInterpolationMode");
            newEmitter.SetRuntimeName("LINEAR-NEAREST", "TextureInterpolationMode");
            newEmitter.SetRuntimeName("CLAMP_TO_EDGE-MIRRORED_REPEAT-REPEAT", "TextureWrapMode");
            newEmitter.SetRuntimeName("LINEAR-LINEAR_MIPMAP_LINEAR-LINEAR_MIPMAP_NEAREST-NEAREST-NEAREST_MIPMAP_LINEAR-NEAREST_MIPMAP_NEAREST", "TextureMipMapMode");

            newEmitter.SetRuntimeName("KHR_materials_pbrSpecularGlossiness glTF extension", "MaterialPBRSpecularGlossiness_KHR");
            newEmitter.SetRuntimeName("KHR_materials_unlit glTF extension", "MaterialUnlit_KHR");

            

            var classes = ctx.Classes.ToArray();
            var fields = classes.SelectMany(item => item.Fields).ToArray();

            var meshClass = classes.FirstOrDefault(c => c.PersistentName == "Mesh");
            if (meshClass != null)
            {
                newEmitter.SetCollectionContainer(meshClass.UseField("primitives"), "ChildrenCollection<TItem,Mesh>");
            }

            var animationClass = classes.FirstOrDefault(c => c.PersistentName == "Animation");

            if (animationClass != null)
            {
                newEmitter.SetCollectionContainer(animationClass.UseField("channels"), "ChildrenCollection<TItem,Animation>");
                newEmitter.SetCollectionContainer(animationClass.UseField("samplers"), "ChildrenCollection<TItem,Animation>");
            }
            
            foreach (var f in fields)
            {
                if (f.FieldType is ArrayType atype)
                {
                    if (atype.ItemType is ClassType ctype)
                    {
                        if (ctype.BaseClass != null && ctype.BaseClass.PersistentName == "glTF Child of Root Property")
                        {
                            newEmitter.SetCollectionContainer(f, $"ChildrenCollection<TItem,{rootName}>");
                        }
                    }
                }
            }

            var textOut = newEmitter.EmitContext(ctx);

            var dstDir = _FindTargetDirectory(Constants.TargetProjectDirectory);
            var dstPath = System.IO.Path.Combine(dstDir, $"{dstFile}.cs");

            System.IO.File.WriteAllText(dstPath, textOut);            
        }

        #endregion

        #region schema loader

        private static SchemaType.Context LoadSchemaContext(string srcSchema)
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

        static NJsonSchema.JsonSchema4 LoadSchema(string filePath)
        {
            // https://blogs.msdn.microsoft.com/benjaminperkins/2017/03/08/how-to-call-an-async-method-from-a-console-app-main-method/

            if (!System.IO.File.Exists(filePath)) throw new System.IO.FileNotFoundException(nameof(filePath), filePath);

            return NJsonSchema.JsonSchema4
                .FromFileAsync(filePath, s => _Resolver(s,filePath) )
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        static NJsonSchema.JsonReferenceResolver _Resolver(NJsonSchema.JsonSchema4 schema, string basePath)
        {
            var solver = new NJsonSchema.JsonSchemaResolver(schema, new NJsonSchema.Generation.JsonSchemaGeneratorSettings());

            return new MyReferenceResolver(solver);            
        }

        class MyReferenceResolver : NJsonSchema.JsonReferenceResolver
        {
            public MyReferenceResolver(NJsonSchema.JsonSchemaResolver resolver) : base(resolver) { }

            public override Task<IJsonReference> ResolveFileReferenceAsync(string filePath)
            {
                if (System.IO.File.Exists(filePath)) return base.ResolveFileReferenceAsync(filePath);

                filePath = System.IO.Path.GetFileName(filePath);
                filePath = System.IO.Path.Combine(Constants.MainSchemaDir, filePath);

                if (System.IO.File.Exists(filePath)) return base.ResolveFileReferenceAsync(filePath);

                throw new System.IO.FileNotFoundException(filePath);
            }
        }

        #endregion
    }    
}
