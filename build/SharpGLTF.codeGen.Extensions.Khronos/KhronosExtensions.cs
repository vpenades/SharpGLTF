using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    public static class KhronosExtensions
    {
        #region API

        public static IEnumerable<SchemaProcessor> GetExtensionsProcessors()
        {
            // material extensions       
            yield return new UnlitExtension();
            yield return new IorExtension();
            yield return new SheenExtension();
            yield return new VolumeExtension();
            yield return new SpecularExtension();
            yield return new ClearCoatExtension();
            yield return new DispersionExtension();
            yield return new AnisotropyExtension();
            yield return new IridescenceExtension();
            yield return new TransmissionExtension();
            yield return new DiffuseTransmissionExtension();
            yield return new EmissiveStrengthExtension();
            yield return new SpecularGlossinessExtension();

            // lights
            yield return new LightsPunctualExtension();

            // gpu mesh instancing
            yield return new MeshGpuInstancingExtension();

            // textures
            yield return new TextureTransformExtension();
            yield return new TextureDDSExtension();
            yield return new TextureWebpExtension();
            yield return new TextureKtx2Extension();

            // other
            yield return new XmpJsonLdExtension();

            yield return new AnimationPointerExtension();
        }

        #endregion

        #region root paths        

        /// <summary>
        /// Program directory
        /// </summary>
        public static string ProgramDirectory => System.IO.Path.GetDirectoryName(typeof(KhronosExtensions).Assembly.Location);

        /// <summary>
        /// Directory where the schema is downloaded and used as source
        /// </summary>
        public static string LocalRepoDirectory => System.IO.Path.Combine(ProgramDirectory, "glTF");

        #endregion        

        #region extension paths        
        private static string ArchivedSchemaDir => System.IO.Path.Combine(LocalRepoDirectory, "extensions", "2.0", "Archived");
        private static string KhronosSchemaDir => System.IO.Path.Combine(LocalRepoDirectory, "extensions", "2.0", "Khronos");
        private static string VendorSchemaDir => System.IO.Path.Combine(LocalRepoDirectory, "extensions", "2.0", "Vendor");

        internal static string ArchivedExtensionPath(string ext, string json)
        {
            return System.IO.Path.Combine(ArchivedSchemaDir, ext, "schema", json);
        }

        internal static string KhronosExtensionPath(string ext, string json)
        {
            return System.IO.Path.Combine(KhronosSchemaDir, ext, "schema", json);
        }
        
        internal static string VendorExtensionPath(string ext, string json)
        {
            return System.IO.Path.Combine(VendorSchemaDir, ext, "schema", json);
        }

        #endregion        
    }
}
