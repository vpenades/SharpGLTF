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

            // XMP
            _ProcessKhronosXMPExtension();

            // material extensions            
            _ProcessKhronosUnlitExtension();
            _ProcessKhronosIorExtension();
            _ProcessKhronosSheenExtension();
            _ProcessKhronosClearCoatExtension();
            _ProcessKhronosTransmissionExtension();
            _ProcessKhronosSpecularGlossinessExtension();

            // lights
            _ProcessKhronosLightsPunctualExtension();

            // gpu mesh instancing
            _ProcessMeshGpuInstancingExtension();

            // textures
            _ProcessKhronosTextureTransformExtension();
            _ProcessMicrosoftTextureDDSExtension();
            _ProcessTextureWebpExtension();
            _ProcessTextureKtx2Extension();

            // these extansions are not fully supported and temporarily removed:
            // _ProcessDracoExtension();
            // _ProcessMicrosoftLODExtension();

            _ProcessAgiArticulationsExtension();
            _ProcessAgiStkMetadataExtension();
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

            ctx1.FindEnum("LINEAR-NEAREST")
                .SetValue("DEFAULT",0);

            ctx1.FindEnum("LINEAR-LINEAR_MIPMAP_LINEAR-LINEAR_MIPMAP_NEAREST-NEAREST-NEAREST_MIPMAP_LINEAR-NEAREST_MIPMAP_NEAREST")
                .SetValue("DEFAULT", 0);

            // Accessor.type is declared as AnyOfEnum, but also as a STRING,
            // which can be used by extensions to store non standard values like MAT4x3
            ctx1.FindClass("Accessor")
                .GetField("type").SetDataType(typeof(string), true);

            // Since DimensionType can have additional values other than the ones defined by the schema
            // we need a "special" value to define it
            ctx1.FindEnum("MAT2-MAT3-MAT4-SCALAR-VEC2-VEC3-VEC4")
                .SetValue("CUSTOM", 0);

            ProcessSchema("gltf.g", ctx1);
        }

        #endregion

        #region Extensions code generation

        private static void _ProcessKhronosXMPExtension()
        {
            // Model extension

            var ctx = LoadSchemaContext(Constants.KhronosExtensions.XMP_Model);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            /*
            var jdict = ctx.UseClass("JsonDictionary");
            var jlist = ctx.UseClass("JsonList");

            ctx.FindClass("KHR_xmp glTF extension")
                .GetField("@context")
                .SetDataType(jdict);

            ctx.FindClass("KHR_xmp glTF extension")
                .GetField("packets")
                .SetDataType(jlist);*/

            /*
            ctx.FindClass("KHR_xmp glTF extension")
                .GetField("@context")
                .SetDataType(typeof(Dictionary<string,Object>), true);
            */

            ProcessSchema("ext.XMP.Model.g", ctx);

            // Node extension

            ctx = LoadSchemaContext(Constants.KhronosExtensions.XMP_Node);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            ProcessSchema("ext.XMP.Node.g", ctx);
        }        

        private static void _ProcessKhronosSpecularGlossinessExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosExtensions.PbrSpecularGlossiness);
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
            var ctx = LoadSchemaContext(Constants.KhronosExtensions.Unlit);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            ProcessSchema("ext.Unlit.g", ctx);
        }

        private static void _ProcessKhronosClearCoatExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosExtensions.PbrClearCoat);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            ctx.IgnoredByCodeEmitter("Material Normal Texture Info");            

            ProcessSchema("ext.ClearCoat.g", ctx);
        }

        private static void _ProcessKhronosTransmissionExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosExtensions.PbrTransmission);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            ctx.IgnoredByCodeEmitter("Material Normal Texture Info");            

            ProcessSchema("ext.Transmission.g", ctx);
        }

        private static void _ProcessKhronosSheenExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosExtensions.PbrSheen);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            ctx.IgnoredByCodeEmitter("Material Normal Texture Info");

            
            ctx.FindClass("KHR_materials_sheen glTF extension")
                .GetField("sheenColorFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.Zero")
                .SetItemsRange(0);

            ctx.FindClass("KHR_materials_sheen glTF extension")
                .GetField("sheenRoughnessFactor")
                .SetDataType(typeof(float), true)                
                .SetItemsRange(0);

            ProcessSchema("ext.Sheen.g", ctx);
        }

        private static void _ProcessKhronosIorExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosExtensions.MaterialIor);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            // ctx.IgnoredByCodeEmitter("Texture Info");
            // ctx.IgnoredByCodeEmitter("Material Normal Texture Info");

            ProcessSchema("ext.Ior.g", ctx);
        }

        private static void _ProcessKhronosLightsPunctualExtension()
        {
            // Model

            var ctx = LoadSchemaContext(Constants.KhronosExtensions.LightsPunctual_Model);            
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            ctx.FindClass("light")
                .GetField("color")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
                .SetItemsRange(0);

            ProcessSchema("ext.ModelLightsPunctual.g", ctx);

            // Node

            ctx = LoadSchemaContext(Constants.KhronosExtensions.LightsPunctual_Node);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            ProcessSchema("ext.NodeLightsPunctual.g", ctx);
        }

        private static void _ProcessKhronosTextureTransformExtension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosExtensions.TextureTransform);
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
            var ctx = LoadSchemaContext(Constants.VendorExtensions.TextureDDS);
            ctx.IgnoredByCodeEmitter("glTF Property");

            ProcessSchema("ext.MSFT.TextureDDS.g", ctx);
        }

        private static void _ProcessTextureWebpExtension()
        {
            var ctx = LoadSchemaContext(Constants.VendorExtensions.TextureWebp);
            ctx.IgnoredByCodeEmitter("glTF Property");

            ProcessSchema("ext.TextureWEBP.g", ctx);
        }

        private static void _ProcessTextureKtx2Extension()
        {
            var ctx = LoadSchemaContext(Constants.KhronosExtensions.Ktx2);
            ctx.IgnoredByCodeEmitter("glTF Property");

            ProcessSchema("ext.TextureKTX2.g", ctx);
        }

        private static void _ProcessMeshGpuInstancingExtension()
        {
            var ctx = LoadSchemaContext(Constants.VendorExtensions.MeshGpuInstancing);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            
            ProcessSchema("ext.MeshGpuInstancing.g", ctx);
        }

        private static void _ProcessAgiArticulationsExtension()
        {
            var ctx1 = LoadSchemaContext(Constants.VendorExtensions.AgiRootArticulations);
            ctx1.IgnoredByCodeEmitter("glTF Property");
            ctx1.IgnoredByCodeEmitter("glTF Child of Root Property");

            ctx1.FindClass("Articulation")
                .GetField("pointingVector")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetItemsRange(0);

            ProcessSchema("ext.AgiRootArticulations.g", ctx1);

            var ctx2 = LoadSchemaContext(Constants.VendorExtensions.AgiNodeArticulations);
            ctx2.IgnoredByCodeEmitter("glTF Property");
            ctx2.IgnoredByCodeEmitter("glTF Child of Root Property");

            ProcessSchema("ext.AgiNodeArticulations.g", ctx2);
        }

        private static void _ProcessAgiStkMetadataExtension()
        {
            var ctx1 = LoadSchemaContext(Constants.VendorExtensions.AgiRootStkMetadata);
            ctx1.IgnoredByCodeEmitter("glTF Property");
            ctx1.IgnoredByCodeEmitter("glTF Child of Root Property");

            ProcessSchema("ext.AgiRootStkMetadata.g", ctx1);

            var ctx2 = LoadSchemaContext(Constants.VendorExtensions.AgiNodeStkMetadata);
            ctx2.IgnoredByCodeEmitter("glTF Property");
            ctx2.IgnoredByCodeEmitter("glTF Child of Root Property");

            ProcessSchema("ext.AgiNodeStkMetadata.g", ctx2);
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
            newEmitter.SetRuntimeName("KHR_materials_transmission glTF extension", "MaterialTransmission");
            newEmitter.SetRuntimeName("KHR_materials_sheen glTF extension", "MaterialSheen");
            newEmitter.SetRuntimeName("KHR_materials_ior glTF extension", "MaterialIOR");

            newEmitter.SetRuntimeName("KHR_xmp glTF extension", "XMPPacketsCollection");
            newEmitter.SetRuntimeName("KHR_xmp node extension", "XMPPacketReference");



            newEmitter.SetRuntimeName("light", "PunctualLight");
            newEmitter.SetRuntimeName("light/spot", "PunctualLightSpot");
            newEmitter.SetRuntimeName("KHR_lights_punctual glTF extension", "_ModelPunctualLights");
            newEmitter.SetRuntimeName("KHR_lights_punctual node extension", "_NodePunctualLight");

            newEmitter.SetRuntimeName("KHR_texture_transform textureInfo extension", "TextureTransform");

            newEmitter.SetRuntimeName("MSFT_texture_dds extension", "TextureDDS");
            newEmitter.SetRuntimeName("EXT_texture_webp glTF extension", "TextureWEBP");
            newEmitter.SetRuntimeName("KHR_texture_basisu glTF extension", "TextureKTX2");

            newEmitter.SetRuntimeName("EXT_mesh_gpu_instancing glTF extension", "MeshGpuInstancing");

            newEmitter.SetRuntimeName("AGI_articulations glTF extension", "AgiRootArticulations");
            newEmitter.SetRuntimeName("AGI_articulations glTF Node extension", "AgiNodeArticulations");
            newEmitter.SetRuntimeName("Articulation", "AgiArticulation");
            newEmitter.SetRuntimeName("Articulation Stage", "AgiArticulationStage");
            newEmitter.SetRuntimeName("uniformScale-xRotate-xScale-xTranslate-yRotate-yScale-yTranslate-zRotate-zScale-zTranslate", "AgiArticulationTransformType");
            newEmitter.SetRuntimeName("AGI_stk_metadata glTF extension", "AgiRootStkMetadata");
            newEmitter.SetRuntimeName("AGI_stk_metadata glTF Node extension", "AgiNodeStkMetadata");
            newEmitter.SetRuntimeName("Solar Panel Group", "AgiStkSolarPanelGroup");

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

            var agiArticulationRootClass = ctx.FindClass("AGI_articulations glTF extension");
            if (agiArticulationRootClass != null)
            {
                newEmitter.SetCollectionContainer(agiArticulationRootClass.UseField("articulations"), "ChildrenCollection<TItem,AgiRootArticulations>");
            }

            var agiArticulationClass = ctx.FindClass("Articulation");
            if (agiArticulationClass != null)
            {
                newEmitter.SetCollectionContainer(agiArticulationClass.UseField("stages"), "ChildrenCollection<TItem,AgiArticulation>");
            }

            var agiStkMetadataRootClass = ctx.FindClass("AGI_stk_metadata glTF extension");
            if (agiStkMetadataRootClass != null)
            {
                newEmitter.SetCollectionContainer(agiStkMetadataRootClass.UseField("solarPanelGroups"), "ChildrenCollection<TItem,AgiRootStkMetadata>");
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

            public override Task<IJsonReference> ResolveFileReferenceAsync(string filePath, System.Threading.CancellationToken cancellationToken)
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
