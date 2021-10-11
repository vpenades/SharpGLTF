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
        private static string KhronosSchemaDir => System.IO.Path.Combine(LocalRepoDirectory, "extensions", "2.0", "Khronos");
        private static string VendorSchemaDir => System.IO.Path.Combine(LocalRepoDirectory, "extensions", "2.0", "Vendor");

        internal static string KhronosExtensionPath(string ext, string json)
        {
            return System.IO.Path.Combine(KhronosSchemaDir, ext, "schema", json);
        }        

        internal static string VendorExtensionPath(string ext, string json)
        {
            return System.IO.Path.Combine(VendorSchemaDir, ext, "schema", json);
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
