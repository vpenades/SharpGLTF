using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    public static class AgiExtensions
    {
        #region API

        public static IEnumerable<SchemaProcessor> GetExtensionsProcessors()
        {
            yield return new AgiArticulationsExtension();
            yield return new AgiStkMetadataExtension();
        }

        #endregion

        #region root paths        

        /// <summary>
        /// Program directory
        /// </summary>
        public static string ProgramDirectory => System.IO.Path.GetDirectoryName(typeof(AgiExtensions).Assembly.Location);

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

        #endregion

        #region code generation output paths
       
        public static string AgiProjectDirectory => "src\\SharpGLTF.Ext.Agi\\Schema2\\Generated";

        public static string AgiNameSpace => "SharpGLTF.Schema2.AGI";

        #endregion
    }
}
