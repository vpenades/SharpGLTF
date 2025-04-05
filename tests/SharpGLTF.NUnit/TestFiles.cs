using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    /// <summary>
    /// Centralices the access to test files.
    /// </summary>
    public static class TestFiles
    {
        #region lifecycle

        private static System.IO.DirectoryInfo _DefDir(this System.IO.DirectoryInfo defDir, params string[] path)
        {
            var finalPath = System.IO.Path.Combine(defDir.FullName, System.IO.Path.Combine(path));
            return new System.IO.DirectoryInfo(finalPath);
        }

        private static System.IO.FileInfo _DefFile(this System.IO.DirectoryInfo defDir, params string[] path)
        {
            var finalPath = System.IO.Path.Combine(defDir.FullName, System.IO.Path.Combine(path));
            return new System.IO.FileInfo(finalPath);
        }

        private static System.IO.DirectoryInfo _FindRepositoryRootDirectory()
        {
            var examplesFound = false;

            var wdir = new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            while (wdir.FullName.Length > 3)
            {
                if (wdir._DefDir(".git").Exists) return wdir;

                wdir = wdir.Parent;
            }

            Assert.That(examplesFound, "repository root directory not found.");

            return wdir;
        }

        private static System.IO.DirectoryInfo _UsingExternalFiles(params string[] subPath)
        {
            var r = _TestFilesDir1._DefDir(subPath);
            if (r.Exists) return r;
            r = _TestFilesDir2._DefDir(subPath);
            if (r.Exists) return r;

            throw new System.IO.DirectoryNotFoundException(string.Join("/", subPath));
        }

        private static System.IO.DirectoryInfo _UsingInternalFiles(params string[] subPath)
        {
            return _AssetFilesDir._DefDir(subPath);
        }

        #endregion

        #region data

        private static Lazy<System.IO.DirectoryInfo> _RepoRootDir = new Lazy<System.IO.DirectoryInfo>(_FindRepositoryRootDirectory);

        /// <summary>
        /// Path to Tests/Assets/
        /// </summary>
        private static System.IO.DirectoryInfo _AssetFilesDir => _RepoRootDir.Value._DefDir("tests", "Assets");

        /// <summary>
        /// Path to Tests/TestFiles/
        /// </summary>
        private static System.IO.DirectoryInfo _TestFilesDir1 => _RepoRootDir.Value._DefDir("tests", "TestFiles");

        /// <summary>
        /// Path to ..\..\SharpGLTF-TestFiles/
        /// </summary>
        private static System.IO.DirectoryInfo _TestFilesDir2 => _RepoRootDir.Value._DefDir("..", "SharpGLTF-TestFiles");


        private static readonly System.IO.DirectoryInfo _SchemaDir = _UsingExternalFiles("glTF-Schema");
        private static readonly System.IO.DirectoryInfo _ValidationDir = _UsingExternalFiles("glTF-Validator");

        internal static readonly System.IO.DirectoryInfo _KhronosSampleAssetsDir = _UsingExternalFiles("glTF-Sample-Assets");
        
        private static readonly System.IO.DirectoryInfo _BabylonJsMeshesDir = _UsingExternalFiles("BabylonJS-Assets");
        private static readonly System.IO.DirectoryInfo _GeneratedModelsDir = _UsingExternalFiles("GeneratedReferenceModels", "v_0_6_1");

        private static KhronosSampleModel[] _KhronosSampleModels;

        #endregion

        #region properties        

        internal static System.IO.DirectoryInfo KhronosSampleModelsDirectory => _KhronosSampleAssetsDir;

        #endregion

        #region API

        public static string GetKhronosAssetDir(params string[] path)
        {
            return _KhronosSampleAssetsDir._DefDir("Models")._DefDir(path).FullName;
        }

        public static IReadOnlyList<string> GetSchemaExtensionsModelsPaths()
        {
            return _FindModelInDirectory(_SchemaDir._DefDir("extensions", "2.0"));         
        }

        public static IEnumerable<string> GetReferenceModelPaths(bool useNegative = false)
        {
            var dirPath = _GeneratedModelsDir;
            if (dirPath.FullName.EndsWith(".zip"))
            {
                var p= dirPath.FullName.Substring(0, dirPath.FullName.Length - 4);
                dirPath = new System.IO.DirectoryInfo(p);
            }

            var manifestsPath = dirPath._DefDir(useNegative? "Negative" : "Positive");

            var manifests = manifestsPath.GetFiles("Manifest.json", System.IO.SearchOption.AllDirectories)
                .Skip(1)
                .ToArray();

            foreach (var m in manifests)
            {
                var content = System.IO.File.ReadAllText(m.FullName);
                var doc = Newtonsoft.Json.Linq.JObject.Parse(content);

                var models = doc.SelectToken("models");
                
                foreach(var model in models)
                {
                    var mdlPath = (String)model.SelectToken("fileName");

                    var loadable = !useNegative;

                    if (loadable) loadable = (Boolean)model.SelectToken("loadable");                    

                    yield return m.Directory._DefFile(mdlPath).FullName;
                }
            }

            yield break;
        }

        public static IReadOnlyList<string> GetSampleModelsPaths()
        {
            _KhronosSampleModels ??= KhronosSampleModel.Load(_KhronosSampleAssetsDir._DefFile("Models", "model-index.json"));

            var files = _KhronosSampleModels
                .SelectMany(item => item.GetPaths())
                .ToList();

            return files;            
        }

        public static IReadOnlyList<string> GetKhronosValidationPaths()
        {
            var skip = new string[]
            {
                "empty_object.gltf", // need to look further                
                "integer_written_as_float.gltf",
                "unknown_type.gltf",
                "valid.gltf", // valid just because objects are unused
                "get_elements_sparse.gltf", // valid just because objects are unused
                "invalid_elements_float.gltf", // sure, it has invalid floats, but then the accessor is not used by the current scene. But what if the accessor is used externally, or by an unhalded extension?
                "not_found.gltf", // it fails at a tricky time
                "non_relative_uri.gltf", // absolute path pointing to a http which is not supported.
                "unrecognized_format.gltf", // might require to dig into the image
                "multiple_extensions.gltf", // it's theoretically tracked (it should give a warning) but then, objects should not be empty...
                "invalid_tangent.gltf", // it's theoretically tracked (it should give a warning) but then, objects should not be empty...
                "primitive_incompatible_mode.gltf", // it's theoretically tracked (it should give a warning) but then, objects should not be empty...
                "primitive_no_position.gltf", // it's theoretically tracked (it should give a warning) but then, objects should not be empty...
                "index_buffer_degenerate_triangle.gltf", // it's theoretically tracked (it should give a warning) but then, objects should not be empty...
                "node_skinned_mesh_without_skin.gltf", // it's theoretically tracked (it should give a warning) but then, objects should not be empty...
                "duplicate_extension_entry.gltf",
                "named_objects.gltf", // gltf-validator says valid, but Buffer should not be.
                "unused_objects.gltf",
                "ignored_animated_transform.gltf", // an channel animated a node with a skin has no effect (warning) since nodes with skin have no transform
                "ignored_local_transform.gltf", // a transform in a node with a skin has no effect (warning) since nodes with skin have no transform
                "ignored_parent_transform.gltf", // a transform in a node with a skin has no effect (warning) since nodes with skin have no transform
                "misplaced_bin_chunk.glb",
                "valid_placeholder.glb",
                "undeclared_extension.gltf",
                "unexpected_extension.gltf",
                "unresolved_source.gltf",
                "unresolved_light_empty_root_ext.gltf",
                "unresolved_light_no_root_ext.gltf",
                "invalid_image_mime_type.gltf", // actual images cannot be validated
            };

            var files = _FindModelInDirectory(_ValidationDir._DefDir("test"))
                .Where(item => skip.All(f=>!item.EndsWith(f)));
            
            return files
                .OrderBy(item => item)                
                .ToList();
        }

        public static IReadOnlyList<string> GetBabylonJSModelsPaths()
        {
            var skipAlways = new string[]
            {
                "ClearCoatTest.gltf", // validator reports errors.
                "ClearCoatTest.glb", // validator reports errors.
                "\\Elf\\Elf.gltf", // validator reports invalid inverse bind matrices.
                "\\meshes\\Tests\\AssetGenerator", // already covered separately.
                "\\meshes\\KHR_materials_volume_testing.glb", // draco compression-
                "\\meshes\\Yeti\\MayaExport\\", // validator reports out of bounds accesor
                "\\meshes\\Demos\\optimized\\", // uses MeshOpt extension
                "\\meshes\\Demos\\retargeting\\riggedMesh.glb", // validator reports errors
                "\\meshes\\Buildings\\road gap.glb", // uses KHR_Draco compression  
                "\\meshes\\Buildings\\Road corner.glb", // uses KHR_Draco compression  
                "\\meshes\\Tests\\BadDraco\\Box-draco.glb", // uses KHR_Draco compression  
            };            

            var files = _FindModelInDirectory(_BabylonJsMeshesDir);

            return files
                .Where(item => !item.ToUpperInvariant().Contains("GLTF-DRACO"))
                .Where(item => !item.ToUpperInvariant().Contains("GLTF-MESHOPT")) // not supported yet
                .Where(item => skipAlways.All(f => !item.Contains(f)))                
                .OrderBy(item => item)                
                .ToList();
        }        

        public static string GetPollyFileModelPath()
        {
            return _UsingExternalFiles("glTF-Blender-Exporter", "polly")._DefFile("project_polly.glb").FullName;
        }

        public static string GetUniVRMModelPath()
        {
            return _UsingExternalFiles("UniVRM")._DefFile("AliciaSolid_vrm-0.51.vrm").FullName;
        }

        public static IEnumerable<string> GetMeshIntancingModelPaths()
        {
            var fromBabylon = GetBabylonJSModelsPaths()
                .Where(item => item.ToUpperInvariant().Contains("TEAPOT"));

            var meshInstPath = _UsingInternalFiles("gltf-GpuMeshInstancing");

            var fromLocal = meshInstPath
                .GetFiles("*.glb", System.IO.SearchOption.AllDirectories)
                .Select(item => item.FullName);

            return fromBabylon.Concat(fromLocal);
        }

        private static IReadOnlyList<string> _FindModelInDirectory(System.IO.DirectoryInfo dinfo)
        {
            var dirPath = dinfo.FullName;

            if (dirPath.EndsWith(".zip")) dirPath = dirPath.Substring(0, dirPath.Length-4);

            // if (!System.IO.Path.IsPathFullyQualified(dirPath)) throw new ArgumentException(nameof(dirPath));
            if (!System.IO.Path.IsPathRooted(dirPath)) throw new ArgumentException(nameof(dirPath));

            var gltf = System.IO.Directory.GetFiles(dirPath, "*.gltf", System.IO.SearchOption.AllDirectories);
            var glbb = System.IO.Directory.GetFiles(dirPath, "*.glb", System.IO.SearchOption.AllDirectories);

            return gltf.Concat(glbb).ToList();
        }        

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{Name}")]
    class KhronosSampleModel
    {
        #region loaders

        public static KhronosSampleModel[] Load(System.IO.FileInfo finfo)
        {            
            var text = System.IO.File.ReadAllText(finfo.FullName);
            var result = Read(text);

            foreach (var item in result) { item._BaseDirectory = finfo.Directory; }

            return result;
        }

        public static KhronosSampleModel[] Read(string json)
        {
            var opts = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            return System.Text.Json.JsonSerializer.Deserialize<KhronosSampleModel[]>(json, opts);
        }

        #endregion

        #region data

        private System.IO.DirectoryInfo _BaseDirectory;

        public string Name { get; set; }
        public string Screenshot { get; set; }
        public Dictionary<string, string> Variants { get; set; } = new Dictionary<string, string>();

        #endregion

        #region API

        public IEnumerable<string> GetPaths()
        {
            var rootPath = _BaseDirectory.FullName;

            foreach(var variant in Variants)
            {
                if (variant.Key == "glTF-Draco") continue; // draco is not supported by SharpGLTF

                yield return System.IO.Path.Combine(rootPath, Name, variant.Key, variant.Value);
            }
        }

        #endregion
    }
}
