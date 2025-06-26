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
        public static string LocalRepoDirectory => System.IO.Path.Combine(AppContext.BaseDirectory, "glTF");

        #endregion
    }
}
