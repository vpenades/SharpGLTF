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
        /// Program directory
        /// </summary>
        public static string ProgramDirectory => System.IO.Path.GetDirectoryName(typeof(Constants).Assembly.Location);

        /// <summary>
        /// Directory where the schema is downloaded and used as source
        /// </summary>
        public static string LocalRepoDirectory => System.IO.Path.Combine(ProgramDirectory, "glTF");

        #endregion
    }
}
