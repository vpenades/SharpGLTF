using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    static class Constants
    {
        #region root paths

        public static string RemoteSchemaRepo = "https://github.com/KhronosGroup/glTF.git";

        /// <summary>
        /// Directory where the schema is downloaded and used as source
        /// </summary>
        public static string LocalRepoDirectory => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location), "glTF");

        #endregion

        #region main schema paths

        /// <summary>
        /// Directory of the main schema within the download repo directory
        /// </summary>
        public static string MainSchemaDir => System.IO.Path.Combine(LocalRepoDirectory, "specification", "2.0", "schema");

        /// <summary>
        /// schema source code file path
        /// </summary>
        public static string MainSchemaFile => System.IO.Path.Combine(MainSchemaDir, "glTF.schema.json");

        #endregion

        #region extension paths


        public static string KhronosSchemaDir => System.IO.Path.Combine(Constants.LocalRepoDirectory, "extensions", "2.0", "Khronos");
        public static string VendorSchemaDir => System.IO.Path.Combine(Constants.LocalRepoDirectory, "extensions", "2.0", "Vendor");

        public static string KhronosModelXMPSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_xmp", "schema", "glTF.KHR_xmp.schema.json");
        public static string KhronosNodeXMPSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_xmp", "schema", "node.KHR_xmp.schema.json");

        public static string KhronosDracoSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_draco_mesh_compression", "schema");
        public static string KhronosPbrSpecGlossSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_materials_pbrSpecularGlossiness", "schema", "glTF.KHR_materials_pbrSpecularGlossiness.schema.json");

        public static string KhronosPbrClearCoatSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_materials_clearcoat", "schema", "glTF.KHR_materials_clearcoat.schema.json");
        public static string KhronosPbrTransmissionSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_materials_transmission", "schema", "glTF.KHR_materials_transmission.schema.json");
        public static string KhronosPbrSheenSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_materials_sheen", "schema", "glTF.KHR_materials_sheen.schema.json");
        public static string KhronosUnlitSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_materials_unlit", "schema", "glTF.KHR_materials_unlit.schema.json");
        public static string KhronosModelLightsPunctualSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_lights_punctual", "schema", "glTF.KHR_lights_punctual.schema.json");
        public static string KhronosNodeLightsPunctualSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_lights_punctual", "schema", "node.KHR_lights_punctual.schema.json");
        public static string KhronosTextureTransformSchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_texture_transform", "schema", "KHR_texture_transform.textureInfo.schema.json");
        
        public static string MicrosoftTextureDDSSchemaFile => System.IO.Path.Combine(VendorSchemaDir, "MSFT_texture_dds", "schema", "glTF.MSFT_texture_dds.schema.json");

        public static string TextureWebpSchemaFile => System.IO.Path.Combine(VendorSchemaDir, "EXT_texture_webp", "schema", "glTF.EXT_texture_webp.schema.json");

        public static string TextureKtx2SchemaFile => System.IO.Path.Combine(KhronosSchemaDir, "KHR_texture_basisu", "schema", "texture.KHR_texture_basisu.schema.json");

        #endregion

        #region code generation output paths

        /// <summary>
        /// directory within the solution where the generated code is emitted
        /// </summary>
        public static string TargetProjectDirectory => "src\\SharpGLTF.Core\\Schema2\\Generated";

        /// <summary>
        /// namespace of the emitted generated code
        /// </summary>
        public static string OutputNamespace => "SharpGLTF.Schema2";

        #endregion
    }
}
