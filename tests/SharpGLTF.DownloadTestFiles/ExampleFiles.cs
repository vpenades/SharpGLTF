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

            _KhronosSchemaDir = System.IO.Path.Combine(_WorkingDirectory, "glTF-Schema");
            _KhronosValidatorDir = System.IO.Path.Combine(_WorkingDirectory, "glTF-Validator");
            _KhronosSampleModelsDir = System.IO.Path.Combine(_WorkingDirectory, "glTF-Sample-Models");

            _BabylonJsMeshesDir = System.IO.Path.Combine(_WorkingDirectory, "BabylonJS-MeshesLibrary");
            _BabylonJsPlaygroundDir = System.IO.Path.Combine(_WorkingDirectory, "BabylonJS-PlaygroundScenes");

            _PollyModelsDir = System.IO.Path.Combine(_WorkingDirectory, "glTF-Blender-Exporter");
            _UniVRMModelsDir = System.IO.Path.Combine(_WorkingDirectory, "UniVRM");            
        }

        public void DownloadReferenceModels()
        {
            Console.WriteLine("Downloading reference files... It might take a while, please, wait...");            

            var dstPath = System.IO.Path.Combine(_WorkingDirectory, "GeneratedReferenceModels", "v_0_6_1.zip");
            _GeneratedModelsDir = DownloadUtils.DownloadFile("https://github.com/KhronosGroup/glTF-Asset-Generator/releases/download/v0.6.1/GeneratedAssets-0.6.1.zip", dstPath);

            dstPath = System.IO.Path.Combine(_UniVRMModelsDir, "AliciaSolid_vrm-0.51.vrm");
            DownloadUtils.DownloadFile("https://github.com/vrm-c/UniVRM/raw/master/Tests/Models/Alicia_vrm-0.51/AliciaSolid_vrm-0.51.vrm", dstPath);

            Console.WriteLine("Checking out test files... It might take a while, please, wait...");

            DownloadUtils.SyncronizeGitRepository("https://github.com/KhronosGroup/glTF.git", _KhronosSchemaDir);
            DownloadUtils.SyncronizeGitRepository("https://github.com/KhronosGroup/glTF-Validator.git", _KhronosValidatorDir);
            DownloadUtils.SyncronizeGitRepository("https://github.com/KhronosGroup/glTF-Sample-Models.git", _KhronosSampleModelsDir);
            
            DownloadUtils.SyncronizeGitRepository("https://github.com/BabylonJS/MeshesLibrary.git", _BabylonJsMeshesDir);
            // DownloadUtils.SyncronizeGitRepository("https://github.com/BabylonJS/Babylon.js.git", _BabylonJsPlaygroundDir);        

            DownloadUtils.SyncronizeGitRepository("https://github.com/KhronosGroup/glTF-Blender-Exporter.git", _PollyModelsDir);

            Console.WriteLine("... Download Completed.");
        }

        #endregion

        #region data

        private readonly string _WorkingDirectory;

        private readonly string _KhronosSchemaDir;
        private readonly string _KhronosValidatorDir;
        private readonly string _KhronosSampleModelsDir;
        
        private readonly string _PollyModelsDir;
        private readonly string _UniVRMModelsDir;
        private readonly string _BabylonJsMeshesDir;
        private readonly string _BabylonJsPlaygroundDir;

        private readonly string[] _BabylonJsInvalidFiles = { };

        private string _GeneratedModelsDir;        

        #endregion        
    }
}
