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
        public static class KhronosExtensions
        {
            private static string KhronosSchemaDir => System.IO.Path.Combine(Constants.LocalRepoDirectory, "extensions", "2.0", "Khronos");

            private static string _Path(string ext, string json)
            {
                return System.IO.Path.Combine(KhronosSchemaDir, ext, "schema", json);
            }

            // public static string Draco => _Path("KHR_draco_mesh_compression", "schema");

            public static string XMP_Model => _Path("KHR_xmp", "glTF.KHR_xmp.schema.json");
            public static string XMP_Node => _Path("KHR_xmp", "node.KHR_xmp.schema.json");
            public static string PbrSpecularGlossiness => _Path("KHR_materials_pbrSpecularGlossiness", "glTF.KHR_materials_pbrSpecularGlossiness.schema.json");
            public static string PbrClearCoat => _Path("KHR_materials_clearcoat", "glTF.KHR_materials_clearcoat.schema.json");
            public static string PbrTransmission => _Path("KHR_materials_transmission", "glTF.KHR_materials_transmission.schema.json");
            public static string PbrSheen => _Path("KHR_materials_sheen", "glTF.KHR_materials_sheen.schema.json");
            public static string MaterialIor => _Path("KHR_materials_ior", "glTF.KHR_materials_ior.schema.json");
            public static string Unlit => _Path("KHR_materials_unlit", "glTF.KHR_materials_unlit.schema.json");
            public static string LightsPunctual_Model => _Path("KHR_lights_punctual", "glTF.KHR_lights_punctual.schema.json");
            public static string LightsPunctual_Node => _Path("KHR_lights_punctual", "node.KHR_lights_punctual.schema.json");
            public static string TextureTransform => _Path("KHR_texture_transform", "KHR_texture_transform.textureInfo.schema.json");
            public static string Ktx2 => _Path("KHR_texture_basisu", "texture.KHR_texture_basisu.schema.json");
        }

        public static class VendorExtensions
        {
            private static string VendorSchemaDir => System.IO.Path.Combine(Constants.LocalRepoDirectory, "extensions", "2.0", "Vendor");

            private static string _Path(string ext, string json)
            {
                return System.IO.Path.Combine(VendorSchemaDir, ext, "schema", json);
            }

            public static string TextureDDS => _Path("MSFT_texture_dds", "glTF.MSFT_texture_dds.schema.json");
            public static string TextureWebp => _Path("EXT_texture_webp", "glTF.EXT_texture_webp.schema.json");
            public static string MeshGpuInstancing => _Path("EXT_mesh_gpu_instancing", "glTF.EXT_mesh_gpu_instancing.schema.json");

            public static string AgiRootArticulations => _Path("AGI_articulations", "glTF.AGI_articulations.schema.json");
            public static string AgiNodeArticulations => _Path("AGI_articulations", "node.AGI_articulations.schema.json");
            public static string AgiRootStkMetadata => _Path("AGI_stk_metadata", "glTF.AGI_stk_metadata.schema.json");
            public static string AgiNodeStkMetadata => _Path("AGI_stk_metadata", "node.AGI_stk_metadata.schema.json");
        }

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
