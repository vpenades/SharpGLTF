using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace glTF2Sharp
{
    /// <summary>
    /// Encapsulates the access to test files.
    /// </summary>
    static class TestFiles
    {
        #region lifecycle

        static TestFiles()
        {
            var workingDir = System.IO.Path.GetDirectoryName(typeof(TestFiles).Assembly.Location);

            _SampleModelsDir = System.IO.Path.Combine(workingDir, "glTF-Sample-Models");
            _PollyModelsDir = System.IO.Path.Combine(workingDir, "glTF-Blender-Exporter");
            _GeneratedAssetsDir = System.IO.Path.Combine(workingDir, "glTF-Asset-Generator");

            _CustomAssetsDir = System.IO.Path.Combine(workingDir, "CustomAssets");
        }

        public static void CheckoutDataDirectories()
        {
            if (_RemotesChecked) return;
            _RemotesChecked = true;

            NUnit.Framework.TestContext.Progress.WriteLine("...");
            NUnit.Framework.TestContext.Progress.WriteLine("Checking out test files... It might take a while, please, wait.");
            NUnit.Framework.TestContext.Progress.WriteLine("...");

            GitUtils.Syncronize("https://github.com/KhronosGroup/glTF-Sample-Models.git", _SampleModelsDir);
            GitUtils.Syncronize("https://github.com/KhronosGroup/glTF-Blender-Exporter.git", _PollyModelsDir);
            GitUtils.Syncronize("https://github.com/bghgary/glTF-Asset-Generator.git", _GeneratedAssetsDir);            
        }

        #endregion

        #region data

        private static Boolean _RemotesChecked = false;
        private static readonly string _SampleModelsDir;
        private static readonly string _PollyModelsDir;
        private static readonly string _GeneratedAssetsDir;

        private static readonly string _CustomAssetsDir;

        #endregion

        #region API

        public static string[] GetSampleFilePaths()
        {
            var dir = System.IO.Path.Combine(_SampleModelsDir, "2.0");

            var gltf = System.IO.Directory.GetFiles(dir, "*.gltf", System.IO.SearchOption.AllDirectories);
            var glbb = System.IO.Directory.GetFiles(dir, "*.glb" , System.IO.SearchOption.AllDirectories);

            var files = gltf.Concat(glbb);

            if (System.IO.Directory.Exists(_CustomAssetsDir))
            {
                var custom = System.IO.Directory.GetFiles(_CustomAssetsDir, "*.glb", System.IO.SearchOption.AllDirectories);
                files = files.Concat(custom);

                custom = System.IO.Directory.GetFiles(_CustomAssetsDir, "*.gltf", System.IO.SearchOption.AllDirectories);
                files = files.Concat(custom);
            }            

            return files
                .OrderBy(item => item)
                .Where(item => !item.Contains("\\glTF-Draco\\"))
                .ToArray();
        }

        public static string[] GetGeneratedFilePaths()
        {
            var dir = System.IO.Path.Combine(_GeneratedAssetsDir, "Output");

            var gltf = System.IO.Directory
                .GetFiles(dir, "*.gltf", System.IO.SearchOption.AllDirectories)
                .Where(item => !item.Contains("Output\\Compatibility\\Compatibility"));

            var glbb = System.IO.Directory
                .GetFiles(dir, "*.glb", System.IO.SearchOption.AllDirectories)
                .Where(item => !item.Contains("Output\\Compatibility\\Compatibility"));

            return gltf.Concat(glbb).OrderBy(item => item).ToArray();
        }

        public static string GetCompatibilityFilePath(int idx)
        {
            return System.IO.Path.Combine(_GeneratedAssetsDir, $"Output\\Compatibility\\Compatibility_0{idx}.gltf");
        }

        public static IEnumerable<string> GetFilePathsWithSpecularGlossinessPBR()
        {
            return GetSampleFilePaths().Where(p => p.Contains("\\glTF-pbrSpecularGlossiness\\"));            
        }

        public static string GetPollyFilePath()
        {
            return System.IO.Path.Combine(_PollyModelsDir, "polly", "project_polly.glb");
        }

        #endregion
    }
}
