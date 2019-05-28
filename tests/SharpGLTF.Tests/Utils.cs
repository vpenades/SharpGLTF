using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    static class NUnitUtils
    {
        public static string ToShortDisplayPath(this string path)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            var fxt = System.IO.Path.GetFileName(path);

            const int maxdir = 12;

            if (dir.Length > maxdir)
            {
                dir = "..." + dir.Substring(dir.Length - maxdir);
            }

            return System.IO.Path.Combine(dir, fxt);
        }        

        public static string GetAttachmentPath(this TestContext context, string fileName, bool ensureDirectoryExists = false)
        {
            var path = System.IO.Path.Combine(context.TestDirectory, "TestResults", $"{context.Test.ID}");
            var dir = path;

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                if (System.IO.Path.IsPathRooted(fileName)) throw new ArgumentException(nameof(fileName), "path must be a relative path");
                path = System.IO.Path.Combine(path, fileName);

                dir = System.IO.Path.GetDirectoryName(path);
            }

            System.IO.Directory.CreateDirectory(dir);

            return path;
        }

        public static void AttachToCurrentTest(this Schema2.ModelRoot model, string fileName)
        {
            // find the output path for the current test
            fileName = TestContext.CurrentContext.GetAttachmentPath(fileName, true);
            
            if (fileName.ToLower().EndsWith(".glb"))
            {
                model.SaveGLB(fileName);
            }
            else if (fileName.ToLower().EndsWith(".gltf"))
            {
                model.SaveGLTF(fileName, Newtonsoft.Json.Formatting.Indented);
            }
            else if (fileName.ToLower().EndsWith(".obj"))
            {
                fileName = fileName.Replace(" ", "_");
                Schema2.Schema2Toolkit.SaveAsWavefront(model, fileName);
            }

            // Attach the saved file to the current test
            TestContext.AddTestAttachment(fileName);
        }

        public static void AttachToCurrentTest(this Schema2.ModelRoot model, string fileName, Schema2.Animation animation, float time)
        {
            fileName = fileName.Replace(" ", "_");

            // find the output path for the current test
            fileName = TestContext.CurrentContext.GetAttachmentPath(fileName, true);
            
            Schema2.Schema2Toolkit.SaveAsWavefront(model, fileName, animation, time);

            // Attach the saved file to the current test
            TestContext.AddTestAttachment(fileName);
        }

        public static void AttachToCurrentTest<TvG, TvM, TvS>(this Geometry.MeshBuilder<TvG, TvM, TvS> mesh, string fileName)
            where TvG : struct, Geometry.VertexTypes.IVertexGeometry
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvS : struct, Geometry.VertexTypes.IVertexSkinning
        {
            var gl2model = Schema2.ModelRoot.CreateModel();

            var gl2mesh = Schema2.Schema2Toolkit.CreateMeshes(gl2model, mesh).First();

            var node = gl2model.UseScene(0).CreateNode();
            node.Mesh = gl2mesh;

            gl2model.AttachToCurrentTest(fileName);
        }

        public static void AttachText(this TestContext context, string fileName, string[] lines)
        {
            fileName = context.GetAttachmentPath(fileName, true);

            System.IO.File.WriteAllLines(fileName, lines.ToArray());

            TestContext.AddTestAttachment(fileName);
        }

        public static void AttachShowDirLink(this TestContext context)
        {
            context.AttachLink("📂 Show Directory", context.GetAttachmentPath(string.Empty));
        }

        public static void AttachGltfValidatorLinks(this TestContext context)
        {
            context.AttachLink("🌍 Khronos Validator", "http://github.khronos.org/glTF-Validator/");
            context.AttachLink("🌍 BabylonJS Sandbox", "https://sandbox.babylonjs.com/");
            context.AttachLink("🌍 Don McCurdy Sandbox", "https://gltf-viewer.donmccurdy.com/");
            context.AttachLink("🌍 VirtualGIS Cesium Sandbox", "https://www.virtualgis.io/gltfviewer/");
        }

        public static void AttachLink(this TestContext context, string linkPath, string targetPath)
        {
            linkPath = context.GetAttachmentPath(linkPath);

            linkPath = ShortcutUtils.CreateLink(linkPath, targetPath);

            TestContext.AddTestAttachment(linkPath);
        }        
    }

    static class DownloadUtils
    {
        private static readonly Object _DownloadMutex = new object();

        public static void SyncronizeGitRepository(string remoteUrl, string localDirectoryPath)
        {
            if (!System.IO.Path.IsPathRooted(localDirectoryPath)) throw new ArgumentException(nameof(localDirectoryPath));

            lock (_DownloadMutex)
            {
                if (LibGit2Sharp.Repository.Discover(localDirectoryPath) == null)
                {
                    TestContext.Progress.WriteLine($"Cloning {remoteUrl} can take several minutes; Please wait...");

                    LibGit2Sharp.Repository.Clone(remoteUrl, localDirectoryPath);

                    TestContext.Progress.WriteLine($"... Clone Completed");

                    return;
                }

                using (var repo = new LibGit2Sharp.Repository(localDirectoryPath))
                {
                    var options = new LibGit2Sharp.PullOptions
                    {
                        FetchOptions = new LibGit2Sharp.FetchOptions()
                    };

                    var r = LibGit2Sharp.Commands.Pull(repo, new LibGit2Sharp.Signature("Anonymous", "anon@anon.com", new DateTimeOffset(DateTime.Now)), options);

                    TestContext.Progress.WriteLine($"{remoteUrl} is {r.Status}");
                }
            }
        }

        public static string DownloadFile(string remoteUri, string localFilePath)
        {
            if (!System.IO.Path.IsPathRooted(localFilePath)) throw new ArgumentException(nameof(localFilePath));

            lock (_DownloadMutex)
            {
                if (System.IO.File.Exists(localFilePath)) return localFilePath; // we check again because we could have downloaded the file while waiting.

                TestContext.Progress.WriteLine($"Downloading {remoteUri}... Please Wait...");

                var dir = System.IO.Path.GetDirectoryName(localFilePath);
                System.IO.Directory.CreateDirectory(dir);

                using (var wc = new System.Net.WebClient())
                {
                    wc.DownloadFile(remoteUri, localFilePath);
                }

                if (localFilePath.ToLower().EndsWith(".zip"))
                {
                    TestContext.Progress.WriteLine($"Extracting {localFilePath}...");

                    var extractPath = System.IO.Path.Combine(dir, System.IO.Path.GetFileNameWithoutExtension(localFilePath));

                    System.IO.Compression.ZipFile.ExtractToDirectory(localFilePath, extractPath);
                }

                return localFilePath;
            }
        }
    }

    static class ShortcutUtils
    {
        public static string CreateLink(string localLinkPath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(localLinkPath)) throw new ArgumentNullException(nameof(localLinkPath));
            if (string.IsNullOrWhiteSpace(targetPath)) throw new ArgumentNullException(nameof(targetPath));

            if (!Uri.TryCreate(targetPath, UriKind.Absolute, out Uri uri)) throw new UriFormatException(nameof(targetPath));

            var sb = new StringBuilder();

            sb.AppendLine("[{000214A0-0000-0000-C000-000000000046}]");
            sb.AppendLine("Prop3=19,11");
            sb.AppendLine("[InternetShortcut]");
            sb.AppendLine("IDList=");
            sb.AppendLine($"URL={uri.AbsoluteUri}");

            if (uri.IsFile)
            {                
                sb.AppendLine("IconIndex=1");
                string icon = targetPath.Replace('\\', '/');
                sb.AppendLine("IconFile=" + icon);
            }
            else
            {
                sb.AppendLine("IconIndex=0");
            }

            localLinkPath = System.IO.Path.ChangeExtension(localLinkPath, ".url");

            System.IO.File.WriteAllText(localLinkPath, sb.ToString());

            return localLinkPath;
        }        
    }

    static class VectorUtils
    {
        public static Single NextSingle(this Random rnd)
        {
            return (Single)rnd.NextDouble();
        }

        public static Vector2 NextVector2(this Random rnd)
        {
            return new Vector2(rnd.NextSingle(), rnd.NextSingle());
        }

        public static Vector3 NextVector3(this Random rnd)
        {
            return new Vector3(rnd.NextSingle(), rnd.NextSingle(), rnd.NextSingle());
        }

        public static Vector4 NextVector4(this Random rnd)
        {
            return new Vector4(rnd.NextSingle(), rnd.NextSingle(), rnd.NextSingle(), rnd.NextSingle());
        }

        public static void AreEqual(Vector4 a, Vector4 b, double delta = 0)
        {
            Assert.AreEqual(a.X, b.X, delta);
            Assert.AreEqual(a.Y, b.Y, delta);
            Assert.AreEqual(a.Z, b.Z, delta);
            Assert.AreEqual(a.W, b.W, delta);
        }
    }
}
