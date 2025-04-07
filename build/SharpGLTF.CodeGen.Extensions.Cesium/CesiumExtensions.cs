using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    public static class CesiumExtensions
    {
        #region API

        public static IEnumerable<SchemaProcessor> GetExtensionsProcessors()
        {
            yield return new CesiumPrimitiveOutlineExtension();

            yield return new ExtMeshFeaturesExtension();
            yield return new ExtInstanceFeaturesExtension();
            yield return new ExtStructuralMetadataExtension();
        }

        #endregion

        #region root paths        

        /// <summary>
        /// Program directory
        /// </summary>
        public static string ProgramDirectory => System.IO.Path.GetDirectoryName(typeof(CesiumExtensions).Assembly.Location);

        /// <summary>
        /// Directory where the schema is downloaded and used as source
        /// </summary>
        public static string LocalRepoDirectory => System.IO.Path.Combine(ProgramDirectory, "glTF");

        #endregion        

        #region extension paths        
        
        private static string VendorSchemaDir => System.IO.Path.Combine(LocalRepoDirectory, "extensions", "2.0", "Vendor");        
        
        internal static string VendorExtensionPath(string ext, string json)
        {
            return System.IO.Path.Combine(VendorSchemaDir, ext, "schema", json);
        }

        internal static string CustomExtensionsPath(string ext, string json)
        {
            return System.IO.Path.Combine(ProgramDirectory, "Schemas", ext, "schema", json);
        }


        #endregion

        #region code generation output paths

        /// <summary>
        /// directory within the solution where the generated code is emitted
        /// </summary>
        
        public static string CesiumProjectDirectory => "src\\SharpGLTF.Ext.3DTiles\\Schema2\\Generated";            

        public static string CesiumNameSpace => "SharpGLTF.Schema2.Tiles3D";        

        #endregion
    }
}
