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

        public static void AttachLink(this TestContext context, string linkPath, string targetPath)
        {
            linkPath = context.GetAttachmentPath(linkPath);

            linkPath = ShortcutUtils.CreateLink(linkPath, targetPath);

            TestContext.AddTestAttachment(linkPath);
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

    static class NUnitGltfUtils
    {
        public static void AttachGltfValidatorLinks(this TestContext context)
        {
            context.AttachLink("🌍 Khronos Validator", "http://github.khronos.org/glTF-Validator/");
            context.AttachLink("🌍 BabylonJS Sandbox", "https://sandbox.babylonjs.com/");
            context.AttachLink("🌍 Don McCurdy Sandbox", "https://gltf-viewer.donmccurdy.com/");
            context.AttachLink("🌍 VirtualGIS Cesium Sandbox", "https://www.virtualgis.io/gltfviewer/");
        }

        

        public static void AttachToCurrentTest(this Scenes.SceneBuilder scene, string fileName)
        {
            var model = scene.ToGltf2();

            model.AttachToCurrentTest(fileName);
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
                model.Save(fileName, new Schema2.WriteSettings { JsonIndented = true });
            }
            else if (fileName.ToLower().EndsWith(".obj"))
            {
                fileName = fileName.Replace(" ", "_");
                Schema2.Schema2Toolkit.SaveAsWavefront(model, fileName);
            }

            // Attach the saved file to the current test
            TestContext.AddTestAttachment(fileName);

            if (fileName.ToLower().EndsWith(".obj")) return;

            var report = gltf_validator.ValidateFile(fileName);
            if (report == null) return;

            if (report.HasErrors || report.HasWarnings)
            {
                TestContext.WriteLine(report.ToString());
            }

            Assert.IsFalse(report.HasErrors);
        }
    }

    static class VectorsUtils
    {
        public static bool IsFinite(this float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

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

        public static float GetAngle(Quaternion a, Quaternion b)
        {
            var w = Quaternion.Concatenate(b, Quaternion.Inverse(a)).W;

            if (w < -1) w = -1;
            if (w > 1) w = 1;

            return (float)Math.Acos(w) * 2;
        }

        public static float GetAngle(Vector3 a, Vector3 b)
        {
            a = Vector3.Normalize(a);
            b = Vector3.Normalize(b);

            var c = Vector3.Dot(a, b);
            if (c > 1) c = 1;
            if (c < -1) c = -1;

            return (float)Math.Acos(c);
        }

        public static float GetAngle(Vector2 a, Vector2 b)
        {
            a = Vector2.Normalize(a);
            b = Vector2.Normalize(b);

            var c = Vector2.Dot(a, b);
            if (c > 1) c = 1;
            if (c < -1) c = -1;

            return (float)Math.Acos(c);
        }

        public static (Vector3, Vector3) GetBounds(this IEnumerable<Vector3> collection)
        {
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);

            foreach (var v in collection)
            {
                min = Vector3.Min(v, min);
                max = Vector3.Max(v, max);
            }

            return (min, max);
        }

        public static Vector3 GetMin(this IEnumerable<Vector3> collection)
        {
            var min = new Vector3(float.MaxValue);            

            foreach (var v in collection)
            {
                min = Vector3.Min(v, min);                
            }

            return min;
        }

        public static Vector3 GetMax(this IEnumerable<Vector3> collection)
        {            
            var max = new Vector3(float.MinValue);

            foreach (var v in collection)
            {
                max = Vector3.Max(v, max);
            }

            return max;
        }
    }

    
}
