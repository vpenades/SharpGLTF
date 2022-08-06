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

        private static void _EnsureInitialized()
        {
            if (_TestFilesDir != null) return;

            var wdir = TestContext.CurrentContext.WorkDirectory;

            var examplesFound = false;

            while (wdir.Length > 3)
            {
                _TestFilesDir = System.IO.Path.Combine(wdir, "TestFiles");

                if (wdir.ToLowerInvariant().EndsWith("tests") && System.IO.Directory.Exists(_TestFilesDir))
                {
                    examplesFound = true;
                    break;
                }

                wdir = System.IO.Path.GetDirectoryName(wdir);
            }

            Assert.IsTrue(examplesFound, "TestFiles directory not found; please, run '1_DownloadTestFiles.cmd' before running the tests.");            

            _AssetFilesDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_TestFilesDir), "Assets");
        }

        private static string _UsingExternalFiles(params string[] subPath)
        {
            _EnsureInitialized();           

            return System.IO.Path.Combine(new string[] { _TestFilesDir }.Concat(subPath).ToArray());
        }

        private static string _UsingInternalFiles(params string[] subPath)
        {
            _EnsureInitialized();

            return System.IO.Path.Combine(new string[] { _AssetFilesDir }.Concat(subPath).ToArray());
        }

        #endregion

        #region data

        /// <summary>
        /// Path to Tests/Assets/
        /// </summary>
        private static string _AssetFilesDir;

        /// <summary>
        /// Path to Tests/TestFiles/
        /// </summary>
        private static string _TestFilesDir;

        private static readonly string _SchemaDir = _UsingExternalFiles("glTF-Schema");
        private static readonly string _ValidationDir = _UsingExternalFiles("glTF-Validator");
        internal static readonly string _SampleModelsDir = _UsingExternalFiles("glTF-Sample-Models");
        
        private static readonly string _BabylonJsMeshesDir = _UsingExternalFiles("BabylonJS-Assets");
        private static readonly string _GeneratedModelsDir = _UsingExternalFiles("GeneratedReferenceModels", "v_0_6_1");

        #endregion

        #region properties        

        public static string KhronosSampleModelsDirectory => _SampleModelsDir;

        #endregion

        #region API

        

        public static IReadOnlyList<string> GetSchemaExtensionsModelsPaths()
        {
            return GetModelPathsInDirectory(_SchemaDir, "extensions", "2.0");         
        }

        public static IEnumerable<string> GetReferenceModelPaths(bool useNegative = false)
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

                    yield return mdlPath;
                }
            }

            yield break;
        }

        public static IReadOnlyList<string> GetSampleModelsPaths()
        {
            var entries = KhronosSampleModel.Load();

            var files = entries
                .SelectMany(item => item.GetPaths(_SampleModelsDir, "2.0"))
                .ToList();

            return files;            
        }

        public static IReadOnlyList<string> GetKhronosValidationPaths()
        {
            var skip = new string[]
            {
                "empty_object.gltf", // need to look further
                "custom_property.gltf",
                "integer_written_as_float.gltf",
                "unknown_type.gltf",
                "valid.gltf", // valid just because objects are unused
                "get_elements_sparse.gltf", // valid just because objects are unused
                "invalid_elements_float.gltf", // sure, it has invalid floats, but then the accessor is not used.
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

            var files = GetModelPathsInDirectory(_ValidationDir, "test")
                .Where(item => skip.All(f=>!item.EndsWith(f)));
            
            return files
                .OrderBy(item => item)                
                .ToList();
        }

        public static IReadOnlyList<string> GetBabylonJSModelsPaths()
        {
            var skipAlways = new string[]
            {
                "\\Elf\\Elf.gltf", // validator reports invalid inverse bind matrices.
                "\\meshes\\Tests\\AssetGenerator", // already covered separately.
                "\\meshes\\KHR_materials_volume_testing.glb", // draco compression-
                "\\meshes\\Yeti\\MayaExport\\", // validator reports out of bounds accesor
                "\\meshes\\Demos\\retargeting\\riggedMesh.glb", // validator reports errors
            };            

            var files = GetModelPathsInDirectory(_BabylonJsMeshesDir);

            return files
                .Where(item => !item.ToLowerInvariant().Contains("gltf-draco"))
                .Where(item => !item.ToLowerInvariant().Contains("gltf-meshopt")) // not supported yet
                .Where(item => skipAlways.All(f => !item.Contains(f)))                
                .OrderBy(item => item)                
                .ToList();
        }        

        public static string GetPollyFileModelPath()
        {
            return _UsingExternalFiles("glTF-Blender-Exporter", "polly", "project_polly.glb");
        }

        public static string GetUniVRMModelPath()
        {
            return _UsingExternalFiles("UniVRM", "AliciaSolid_vrm-0.51.vrm");
        }

        public static IEnumerable<string> GetMeshIntancingModelPaths()
        {
            var fromBabylon = GetBabylonJSModelsPaths()
                .Where(item => item.ToLowerInvariant().Contains("teapot"));

            var meshInstPath = _UsingInternalFiles("gltf-GpuMeshInstancing");

            var fromLocal = System.IO.Directory.GetFiles(meshInstPath, "*.glb", System.IO.SearchOption.AllDirectories);

            return fromBabylon.Concat(fromLocal);
        }

        private static IReadOnlyList<string> GetModelPathsInDirectory(params string[] paths)
        {
            var dirPath = System.IO.Path.Combine(paths);

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

        public static KhronosSampleModel[] Load()
        {
            var path = System.IO.Path.Combine(TestFiles._SampleModelsDir, "2.0", "model-index.json");
            var text = System.IO.File.ReadAllText(path);
            return Read(text);
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

        public string Name { get; set; }
        public string Screenshot { get; set; }
        public Dictionary<string, string> Variants { get; set; } = new Dictionary<string, string>();

        #endregion

        #region API

        public IEnumerable<string> GetPaths(params string[] basePath)
        {
            var rootPath = System.IO.Path.Combine(basePath);

            foreach(var variant in Variants)
            {
                if (variant.Key == "glTF-Draco") continue; // draco is not supported by SharpGLTF

                yield return System.IO.Path.Combine(rootPath, Name, variant.Key, variant.Value);
            }
        }

        #endregion
    }
}
