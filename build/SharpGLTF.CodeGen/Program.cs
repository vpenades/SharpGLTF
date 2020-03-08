using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using NJsonSchema.References;

using JSONSCHEMA = NJsonSchema.JsonSchema;

namespace SharpGLTF
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

            _ProcessKhronosSpecularGlossinessExtension();
            _ProcessKhronosUnlitExtension();
            _ProcessKhronosClearCoatExtension();
            _ProcessKhronosModelLightsPunctualExtension();
            _ProcessKhronosNodeLightsPunctualExtension();
            _ProcessKhronosTextureTransformExtension();
            _ProcessMicrosoftTextureDDSExtension();
            _ProcessTextureWebpExtension();

            // these extansions are not fully supported and temporarily removed:
            // _ProcessDracoExtension();
            // _ProcessMicrosoftLODExtension();
        }

        #endregion

        #region Main Schema code generation        

        private static void _ProcessMainSchema()
        {
            // load and process schema
            var ctx1 = LoadSchemaContext(Constants.MainSchemaFile);

            // Ignore "glTF Property" because it is completely hand coded.
            ctx1.IgnoredByCodeEmitter("glTF Property");

            // We will mimeType "anyof" as a plain string.
            ctx1.Remove("image/jpeg-image/png");            

            // replace Image.mimeType type from an Enum to String, so we can serialize it with more formats if required
            ctx1.FindClass("Image")
                .GetField("mimeType")
                .FieldType = ctx1.UseString();

            // replace Node.Matrix, Node.Rotation, Node.Scale and Node.Translation with System.Numerics.Vectors types
            var node = ctx1.FindClass("Node");
            node.GetField("matrix").SetDataType(typeof(System.Numerics.Matrix4x4), true).RemoveDefaultValue().SetItemsRange(0);
            node.GetField("rotation").SetDataType(typeof(System.Numerics.Quaternion), true).RemoveDefaultValue().SetItemsRange(0);
            node.GetField("scale").SetDataType(typeof(System.Numerics.Vector3), true).RemoveDefaultValue().SetItemsRange(0);
            node.GetField("translation").SetDataType(typeof(System.Numerics.Vector3), true).RemoveDefaultValue().SetItemsRange(0);

            // replace Material.emissiveFactor with System.Numerics.Vectors types
            ctx1.FindClass("Material")
                .GetField("emissiveFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.Zero")
                .SetItemsRange(0);

            // replace Material.baseColorFactor with System.Numerics.Vectors types
            ctx1.FindClass("Material PBR Metallic Roughness")
                .GetField("baseColorFactor")
                .SetDataType(typeof(System.Numerics.Vector4), true)
                .SetDefaultValue("Vector4.One")
                .SetItemsRange(0);

            var tex1 = ctx1.FindEnum("LINEAR-NEAREST");
            tex1.SetValue("DEFAULT",0);

            var tex2 = ctx1.FindEnum("LINEAR-LINEAR_MIPMAP_LINEAR-LINEAR_MIPMAP_NEAREST-NEAREST-NEAREST_MIPMAP_LINEAR-NEAREST_MIPMAP_NEAREST");
            tex2.SetValue("DEFAULT", 0);

            ProcessSchema("gltf.g", ctx1);
        }

        #endregion

        #region Extensions code generation

        private static void _ProcessKhronosSpecularGlossinessExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosPbrSpecGlossSchemaFile);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");

            ctx.FindClass("KHR_materials_pbrSpecularGlossiness glTF extension")
                .GetField("diffuseFactor")
                .SetDataType(typeof(System.Numerics.Vector4), true)
                .SetDefaultValue("Vector4.One")
                .SetItemsRange(0);

            ctx.FindClass("KHR_materials_pbrSpecularGlossiness glTF extension")
                .GetField("specularFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
                .SetItemsRange(0);

            ProcessSchema("ext.pbrSpecularGlossiness.g", ctx);
        }

        private static void _ProcessKhronosUnlitExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosUnlitSchemaFile);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            ProcessSchema("ext.Unlit.g", ctx);
        }

        private static void _ProcessKhronosClearCoatExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosPbrClearCoatSchemaFile);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            ctx.IgnoredByCodeEmitter("Material Normal Texture Info");

            /*
            ctx.FindClass("KHR_materials_pbrSpecularGlossiness glTF extension")
                .GetField("diffuseFactor")
                .SetDataType(typeof(System.Numerics.Vector4), true)
                .SetDefaultValue("Vector4.One")
                .SetItemsRange(0);

            ctx.FindClass("KHR_materials_pbrSpecularGlossiness glTF extension")
                .GetField("specularFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
                .SetItemsRange(0);*/

            ProcessSchema("ext.ClearCoat.g", ctx);
        }

        private static void _ProcessKhronosModelLightsPunctualExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosModelLightsPunctualSchemaFile);            
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            ctx.FindClass("light")
                .GetField("color")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
                .SetItemsRange(0);

            ProcessSchema("ext.ModelLightsPunctual.g", ctx);
        }

        private static void _ProcessKhronosNodeLightsPunctualExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosNodeLightsPunctualSchemaFile);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            ProcessSchema("ext.NodeLightsPunctual.g", ctx);
        }

        private static void _ProcessKhronosTextureTransformExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosTextureTransformSchemaFile);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            var tex = ctx.FindClass("KHR_texture_transform textureInfo extension");

            tex.GetField("offset")
                .SetDataType(typeof(System.Numerics.Vector2), true)
                .SetDefaultValue("Vector2.Zero")
                .SetItemsRange(0);

            tex.GetField("scale")
                .SetDataType(typeof(System.Numerics.Vector2), true)
                .SetDefaultValue("Vector2.One")
                .SetItemsRange(0);

            ProcessSchema("ext.TextureTransform.g", ctx);
        }

        private static void _ProcessMicrosoftTextureDDSExtension()
        {
            var ctx = LoadSchemaContext(Constants.MicrosoftTextureDDSSchemaFile);
            ctx.IgnoredByCodeEmitter("glTF Property");

            ProcessSchema("ext.MSFT.textureDDS.g", ctx);
        }

        private static void _ProcessTextureWebpExtension()
        {
            var ctx = LoadSchemaContext(Constants.TextureWebpSchemaFile);
            ctx.IgnoredByCodeEmitter("glTF Property");

            ProcessSchema("ext.textureWEBP.g", ctx);
        }

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
            newEmitter.SetRuntimeName("glTF Property", "ExtraProperties");
            newEmitter.SetRuntimeName("glTF Child of Root Property", "LogicalChildOfRoot");

            newEmitter.SetRuntimeName("Sampler", "TextureSampler");

            newEmitter.SetRuntimeName("UNSIGNED_BYTE-UNSIGNED_INT-UNSIGNED_SHORT", "IndexEncodingType");            
            newEmitter.SetRuntimeName("BYTE-FLOAT-SHORT-UNSIGNED_BYTE-UNSIGNED_INT-UNSIGNED_SHORT", "EncodingType");            
            newEmitter.SetRuntimeName("MAT2-MAT3-MAT4-SCALAR-VEC2-VEC3-VEC4", "DimensionType");
            newEmitter.SetRuntimeName("rotation-scale-translation-weights", "PropertyPath");
            newEmitter.SetRuntimeName("ARRAY_BUFFER-ELEMENT_ARRAY_BUFFER", "BufferMode");
            newEmitter.SetRuntimeName("orthographic-perspective", "CameraType");
            newEmitter.SetRuntimeName("BLEND-MASK-OPAQUE", "AlphaMode");
            newEmitter.SetRuntimeName("LINE_LOOP-LINE_STRIP-LINES-POINTS-TRIANGLE_FAN-TRIANGLE_STRIP-TRIANGLES", "PrimitiveType");
            newEmitter.SetRuntimeName("CUBICSPLINE-LINEAR-STEP", "AnimationInterpolationMode");
            newEmitter.SetRuntimeName("LINEAR-NEAREST", "TextureInterpolationFilter");
            newEmitter.SetRuntimeName("CLAMP_TO_EDGE-MIRRORED_REPEAT-REPEAT", "TextureWrapMode");
            newEmitter.SetRuntimeName("LINEAR-LINEAR_MIPMAP_LINEAR-LINEAR_MIPMAP_NEAREST-NEAREST-NEAREST_MIPMAP_LINEAR-NEAREST_MIPMAP_NEAREST", "TextureMipMapFilter");

            newEmitter.SetRuntimeName("KHR_materials_pbrSpecularGlossiness glTF extension", "MaterialPBRSpecularGlossiness");
            newEmitter.SetRuntimeName("KHR_materials_unlit glTF extension", "MaterialUnlit");
            newEmitter.SetRuntimeName("KHR_materials_clearcoat glTF extension", "MaterialClearCoat");



            newEmitter.SetRuntimeName("light", "PunctualLight");
            newEmitter.SetRuntimeName("light/spot", "PunctualLightSpot");

            newEmitter.SetRuntimeName("KHR_texture_transform textureInfo extension", "TextureTransform");

            newEmitter.SetRuntimeName("MSFT_texture_dds extension", "TextureDDS");
            newEmitter.SetRuntimeName("EXT_texture_webp glTF extension", "TextureWEBP");

            var classes = ctx.Classes.ToArray();
            var fields = classes.SelectMany(item => item.Fields).ToArray();

            var meshClass = ctx.FindClass("Mesh");
            if (meshClass != null)
            {
                newEmitter.SetCollectionContainer(meshClass.UseField("primitives"), "ChildrenCollection<TItem,Mesh>");
            }

            var animationClass = ctx.FindClass("Animation");
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

        static JSONSCHEMA LoadSchema(string filePath)
        {
            // https://blogs.msdn.microsoft.com/benjaminperkins/2017/03/08/how-to-call-an-async-method-from-a-console-app-main-method/

            if (!System.IO.File.Exists(filePath)) throw new System.IO.FileNotFoundException(nameof(filePath), filePath);

            return JSONSCHEMA
                .FromFileAsync(filePath, s => _Resolver(s,filePath) )
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        static NJsonSchema.JsonReferenceResolver _Resolver(JSONSCHEMA schema, string basePath)
        {
            var generator = new NJsonSchema.Generation.JsonSchemaGeneratorSettings();

            var solver = new NJsonSchema.JsonSchemaAppender(schema, generator.TypeNameGenerator);

            return new MyReferenceResolver(solver);            
        }

        class MyReferenceResolver : NJsonSchema.JsonReferenceResolver
        {
            public MyReferenceResolver(NJsonSchema.JsonSchemaAppender resolver) : base(resolver) { }

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
