using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp
{    
    using Schema2;

    static class GltfUtils
    {
        #region model loading

        public static ModelRoot LoadModel(string filePath)
        {
            try
            {
                TestContext.Progress.WriteLine($"Loading {filePath.ToShortDisplayPath()}");

                return ModelRoot.Load(filePath);
            }
            catch (UnsupportedExtensionException eex)
            {
                TestContext.WriteLine($"{filePath} ERROR: {eex.Message}");

                return null;
            }
        }

        #endregion
    }
}
