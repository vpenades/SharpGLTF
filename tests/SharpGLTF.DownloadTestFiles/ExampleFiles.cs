using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF
{
    /// <summary>
    /// Encapsulates the access to test files.
    /// </summary>
    class ExampleFiles
    {
        #region lifecycle        

        public ExampleFiles(string workingDirectory)
        {
            _WorkingDirectory = workingDirectory;            

            _SchemaDir = System.IO.Path.Combine(_WorkingDirectory, "glTF-Schema");
            _SampleModelsDir = System.IO.Path.Combine(_WorkingDirectory, "glTF-Sample-Models");
            _PollyModelsDir = System.IO.Path.Combine(_WorkingDirectory, "glTF-Blender-Exporter");
            _UniVRMModelsDir = System.IO.Path.Combine(_WorkingDirectory, "UniVRM");
            _BabylonJsMeshesDir = System.IO.Path.Combine(_WorkingDirectory, "BabylonJS-MeshesLibrary");
            _BabylonJsPlaygroundDir = System.IO.Path.Combine(_WorkingDirectory, "BabylonJS-PlaygroundScenes");
        }

        public void DownloadReferenceModels()
        {
            Console.WriteLine("Downloading reference files... It might take a while, please, wait...");            

            var dstPath = System.IO.Path.Combine(_WorkingDirectory, "GeneratedReferenceModels", "v_0_6_1.zip");
            _GeneratedModelsDir = DownloadUtils.DownloadFile("https://github.com/KhronosGroup/glTF-Asset-Generator/releases/download/v0.6.1/GeneratedAssets-0.6.1.zip", dstPath);

            dstPath = System.IO.Path.Combine(_UniVRMModelsDir, "AliciaSolid_vrm-0.40.vrm");
            DownloadUtils.DownloadFile("https://github.com/vrm-c/UniVRMTest/raw/master/Models/Alicia_vrm-0.40/AliciaSolid_vrm-0.40.vrm", dstPath);

            Console.WriteLine("Checking out test files... It might take a while, please, wait...");            

            DownloadUtils.SyncronizeGitRepository("https://github.com/KhronosGroup/glTF-Sample-Models.git", _SampleModelsDir);
            DownloadUtils.SyncronizeGitRepository("https://github.com/KhronosGroup/glTF-Blender-Exporter.git", _PollyModelsDir);            
            DownloadUtils.SyncronizeGitRepository("https://github.com/KhronosGroup/glTF.git", _SchemaDir);
            DownloadUtils.SyncronizeGitRepository("https://github.com/BabylonJS/MeshesLibrary.git", _BabylonJsMeshesDir);
            // DownloadUtils.SyncronizeGitRepository("https://github.com/BabylonJS/Babylon.js.git", _BabylonJsPlaygroundDir);

            Console.WriteLine("... Download Completed.");
        }

        #endregion

        #region data

        private readonly string _WorkingDirectory;

        private readonly string _SchemaDir;
        private readonly string _SampleModelsDir;
        private readonly string _PollyModelsDir;
        private readonly string _UniVRMModelsDir;
        private readonly string _BabylonJsMeshesDir;
        private readonly string _BabylonJsPlaygroundDir;

        private readonly string[] _BabylonJsInvalidFiles = { };

        private string _GeneratedModelsDir;        

        #endregion        
    }
}
