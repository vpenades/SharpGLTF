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
    static class SchemaProcessing
    {
        #region schema loader

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

        public static void EmitCodeFromSchema(string dstFile, SchemaType.Context ctx)
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
            newEmitter.SetRuntimeName("KHR_materials_specular glTF extension", "MaterialSpecular");
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
