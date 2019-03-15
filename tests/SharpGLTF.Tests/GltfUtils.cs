using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{    
    using Schema2;

    static class GltfUtils
    {
        #region model loading

        public static ModelRoot LoadModel(string filePath)
        {
            try
            {
                return ModelRoot.Load(filePath);
            }
            catch (IO.UnsupportedExtensionException eex)
            {
                TestContext.WriteLine($"{filePath} ERROR: {eex.Message}");

                return null;
            }
        }

        #endregion
    }
}
