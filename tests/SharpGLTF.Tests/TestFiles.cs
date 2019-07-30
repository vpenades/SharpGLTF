using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    /// <summary>
    /// Encapsulates the access to test files.
    /// </summary>
    static class TestFiles
    {
        #region lifecycle

        static TestFiles()
        {
            var workingDir = TestContext.CurrentContext.WorkDirectory;

            _SchemaDir = System.IO.Path.Combine(workingDir, "glTF-Schema");
            _SampleModelsDir = System.IO.Path.Combine(workingDir, "glTF-Sample-Models");
            _PollyModelsDir = System.IO.Path.Combine(workingDir, "glTF-Blender-Exporter");
            _BabylonJsMeshesDir = System.IO.Path.Combine(workingDir, "BabylonJS-MeshesLibrary");
            _BabylonJsPlaygroundDir = System.IO.Path.Combine(workingDir, "BabylonJS-PlaygroundScenes");
        }

        public static void DownloadReferenceModels()
        {
            if (_DonwloadCompleted) return;
            _DonwloadCompleted = true;
            
            TestContext.Progress.WriteLine("Downloading reference files... It might take a while, please, wait...");            

            var dstPath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "GeneratedReferenceModels", "v_0_6_1.zip");
            _GeneratedModelsDir = DownloadUtils.DownloadFile("https://github.com/KhronosGroup/glTF-Asset-Generator/releases/download/v0.6.1/GeneratedAssets-0.6.1.zip", dstPath);
            
            TestContext.Progress.WriteLine("Checking out test files... It might take a while, please, wait...");            

            DownloadUtils.SyncronizeGitRepository("https://github.com/KhronosGroup/glTF-Sample-Models.git", _SampleModelsDir);
            DownloadUtils.SyncronizeGitRepository("https://github.com/KhronosGroup/glTF-Blender-Exporter.git", _PollyModelsDir);            
            DownloadUtils.SyncronizeGitRepository("https://github.com/KhronosGroup/glTF.git", _SchemaDir);
            DownloadUtils.SyncronizeGitRepository("https://github.com/BabylonJS/MeshesLibrary.git", _BabylonJsMeshesDir);
            // DownloadUtils.SyncronizeGitRepository("https://github.com/BabylonJS/Babylon.js.git", _BabylonJsPlaygroundDir);

            TestContext.Progress.WriteLine("... Download Completed.");
        }

        #endregion

        #region data

        private static Boolean _DonwloadCompleted = false;

        private static readonly string _SchemaDir;
        private static readonly string _SampleModelsDir;
        private static readonly string _PollyModelsDir;
        private static readonly string _BabylonJsMeshesDir;
        private static readonly string _BabylonJsPlaygroundDir;

        private static string _GeneratedModelsDir;        

        #endregion

        #region API

        public static IReadOnlyList<string> GetSchemaExtensionsModelsPaths()
        {
            return GetModelPathsInDirectory(_SchemaDir, "extensions", "2.0");         
        }

        public static IEnumerable<(string, bool)> GetReferenceModelPaths(bool useNegative = false)
        {
            var dirPath = _GeneratedModelsDir;
            if (dirPath.EndsWith(".zip")) dirPath = dirPath.Substring(0, dirPath.Length - 4);

            var manifestsPath = System.IO.Path.Combine(dirPath, useNegative? "Negative" : "Positive");

            var manifests = System.IO.Directory.GetFiles(manifestsPath, "Manifest.json", System.IO.SearchOption.AllDirectories)
                .Skip(1)
                .ToArray();

            foreach (var m in manifests)
            {
                var d = System.IO.Path.GetDirectoryName(m);

                var content = System.IO.File.ReadAllText(m);
                var doc = Newtonsoft.Json.Linq.JObject.Parse(content);

                var models = doc.SelectToken("models");
                
                foreach(var model in models)
                {
                    var mdlPath = (String)model.SelectToken("fileName");

                    var loadable = !useNegative;

                    if (loadable) loadable = (Boolean)model.SelectToken("loadable");

                    mdlPath = System.IO.Path.Combine(d, mdlPath);

                    yield return (mdlPath, loadable);
                }
            }

            yield break;
        }

        public static IReadOnlyList<string> GetSampleModelsPaths()
        {
            var files = GetModelPathsInDirectory(_SampleModelsDir, "2.0");

            return files
                .OrderBy(item => item)
                .Where(item => !item.Contains("\\glTF-Draco\\"))
                .ToList();
        }

        public static IReadOnlyList<string> GetBabylonJSModelsPaths()
        {
            var files = GetModelPathsInDirectory(_BabylonJsMeshesDir);

            return files
                .OrderBy(item => item)
                .Where(item => !item.Contains("\\AssetGenerator\\0.6\\"))
                .Where(item => !item.EndsWith("shaderBall.glb")) // invalid
                .ToList();
        }

        public static string GetPollyFileModelPath()
        {
            return System.IO.Path.Combine(_PollyModelsDir, "polly", "project_polly.glb");
        }

        private static IReadOnlyList<string> GetModelPathsInDirectory(params string[] paths)
        {
            var dirPath = System.IO.Path.Combine(paths);

            if (dirPath.EndsWith(".zip")) dirPath = dirPath.Substring(0, dirPath.Length-4);

            if (!System.IO.Path.IsPathFullyQualified(dirPath)) throw new ArgumentException(nameof(dirPath));

            var gltf = System.IO.Directory.GetFiles(dirPath, "*.gltf", System.IO.SearchOption.AllDirectories);
            var glbb = System.IO.Directory.GetFiles(dirPath, "*.glb", System.IO.SearchOption.AllDirectories);

            return gltf.Concat(glbb).ToList();
        }        

        #endregion
    }
}
