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
            _ProcessKhronosSpecularExtension();
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
            var ctx1 = SchemaProcessing.LoadSchemaContext(Constants.MainSchemaFile);

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

            SchemaProcessing.EmitCodeFromSchema("gltf.g", ctx1);
        }

        #endregion

        #region Extensions code generation

        private static void _ProcessKhronosXMPExtension()
        {
            // Model extension

            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.XMP_Model);
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

            SchemaProcessing.EmitCodeFromSchema("ext.XMP.Model.g", ctx);

            // Node extension

            ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.XMP_Node);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            SchemaProcessing.EmitCodeFromSchema("ext.XMP.Node.g", ctx);
        }        

        private static void _ProcessKhronosSpecularGlossinessExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.PbrSpecularGlossiness);
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

            SchemaProcessing.EmitCodeFromSchema("ext.pbrSpecularGlossiness.g", ctx);
        }

        private static void _ProcessKhronosUnlitExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.Unlit);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            SchemaProcessing.EmitCodeFromSchema("ext.Unlit.g", ctx);
        }

        private static void _ProcessKhronosClearCoatExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.PbrClearCoat);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            ctx.IgnoredByCodeEmitter("Material Normal Texture Info");

            SchemaProcessing.EmitCodeFromSchema("ext.ClearCoat.g", ctx);
        }

        private static void _ProcessKhronosSpecularExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.PbrSpecular);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            ctx.IgnoredByCodeEmitter("Material Normal Texture Info");

            ctx.FindClass("KHR_materials_specular glTF extension")
                .GetField("specularColorFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
                .SetItemsRange(0);

            SchemaProcessing.EmitCodeFromSchema("ext.pbrSpecular.g", ctx);
        }

        private static void _ProcessKhronosTransmissionExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.PbrTransmission);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            ctx.IgnoredByCodeEmitter("Texture Info");
            ctx.IgnoredByCodeEmitter("Material Normal Texture Info");

            SchemaProcessing.EmitCodeFromSchema("ext.Transmission.g", ctx);
        }

        private static void _ProcessKhronosSheenExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.PbrSheen);
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

            SchemaProcessing.EmitCodeFromSchema("ext.Sheen.g", ctx);
        }

        private static void _ProcessKhronosIorExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.MaterialIor);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");
            // ctx.IgnoredByCodeEmitter("Texture Info");
            // ctx.IgnoredByCodeEmitter("Material Normal Texture Info");

            SchemaProcessing.EmitCodeFromSchema("ext.Ior.g", ctx);
        }

        private static void _ProcessKhronosLightsPunctualExtension()
        {
            // Model

            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.LightsPunctual_Model);            
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            ctx.FindClass("light")
                .GetField("color")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.One")
                .SetItemsRange(0);

            SchemaProcessing.EmitCodeFromSchema("ext.ModelLightsPunctual.g", ctx);

            // Node

            ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.LightsPunctual_Node);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            SchemaProcessing.EmitCodeFromSchema("ext.NodeLightsPunctual.g", ctx);
        }

        private static void _ProcessKhronosTextureTransformExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.TextureTransform);
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

            SchemaProcessing.EmitCodeFromSchema("ext.TextureTransform.g", ctx);
        }

        private static void _ProcessMicrosoftTextureDDSExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.VendorExtensions.TextureDDS);
            ctx.IgnoredByCodeEmitter("glTF Property");

            SchemaProcessing.EmitCodeFromSchema("ext.MSFT.TextureDDS.g", ctx);
        }

        private static void _ProcessTextureWebpExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.VendorExtensions.TextureWebp);
            ctx.IgnoredByCodeEmitter("glTF Property");

            SchemaProcessing.EmitCodeFromSchema("ext.TextureWEBP.g", ctx);
        }

        private static void _ProcessTextureKtx2Extension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.KhronosExtensions.Ktx2);
            ctx.IgnoredByCodeEmitter("glTF Property");

            SchemaProcessing.EmitCodeFromSchema("ext.TextureKTX2.g", ctx);
        }

        private static void _ProcessMeshGpuInstancingExtension()
        {
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.VendorExtensions.MeshGpuInstancing);
            ctx.IgnoredByCodeEmitter("glTF Property");
            ctx.IgnoredByCodeEmitter("glTF Child of Root Property");

            SchemaProcessing.EmitCodeFromSchema("ext.MeshGpuInstancing.g", ctx);
        }

        private static void _ProcessAgiArticulationsExtension()
        {
            var ctx1 = SchemaProcessing.LoadSchemaContext(Constants.VendorExtensions.AgiRootArticulations);
            ctx1.IgnoredByCodeEmitter("glTF Property");
            ctx1.IgnoredByCodeEmitter("glTF Child of Root Property");

            ctx1.FindClass("Articulation")
                .GetField("pointingVector")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetItemsRange(0);

            SchemaProcessing.EmitCodeFromSchema("ext.AgiRootArticulations.g", ctx1);

            var ctx2 = SchemaProcessing.LoadSchemaContext(Constants.VendorExtensions.AgiNodeArticulations);
            ctx2.IgnoredByCodeEmitter("glTF Property");
            ctx2.IgnoredByCodeEmitter("glTF Child of Root Property");

            SchemaProcessing.EmitCodeFromSchema("ext.AgiNodeArticulations.g", ctx2);
        }

        private static void _ProcessAgiStkMetadataExtension()
        {
            var ctx1 = SchemaProcessing.LoadSchemaContext(Constants.VendorExtensions.AgiRootStkMetadata);
            ctx1.IgnoredByCodeEmitter("glTF Property");
            ctx1.IgnoredByCodeEmitter("glTF Child of Root Property");

            SchemaProcessing.EmitCodeFromSchema("ext.AgiRootStkMetadata.g", ctx1);

            var ctx2 = SchemaProcessing.LoadSchemaContext(Constants.VendorExtensions.AgiNodeStkMetadata);
            ctx2.IgnoredByCodeEmitter("glTF Property");
            ctx2.IgnoredByCodeEmitter("glTF Child of Root Property");

            SchemaProcessing.EmitCodeFromSchema("ext.AgiNodeStkMetadata.g", ctx2);
        }

        #endregion                
    }    
}
