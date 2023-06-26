using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

using SharpGLTF.Schema2;

namespace SharpGLTF
{
    public static class NUnitGltfUtils
    {
        public static void AttachGltfValidatorLinks(this TestContext context)
        {
            // new AttachmentInfo(context, "🌍 Khronos Validator.lnk").WriteLink("http://github.khronos.org/glTF-Validator/");
            // new AttachmentInfo(context, "🌍 BabylonJS Sandbox.lnk").WriteLink("https://sandbox.babylonjs.com/");
            // new AttachmentInfo(context, "🌍 Don McCurdy Sandbox.lnk").WriteLink("https://gltf-viewer.donmccurdy.com/");
            // new AttachmentInfo(context, "🌍 VirtualGIS Cesium Sandbox.lnk").WriteLink("https://www.virtualgis.io/gltfviewer/");
        }        

        public static void AttachToCurrentTest(this Scenes.SceneBuilder scene, string fileName)
        {
            var model = scene.ToGltf2();

            model.AttachToCurrentTest(fileName);
        }

        public static void AttachToCurrentTest(this ModelRoot model, string fileName, Animation animation, float time)
        {
            // wavefront does't like files paths with spaces because
            // some implementations would not find the material file
            fileName = fileName.Replace(" ", "_");

            AttachmentInfo
                .From(fileName)
                .WriteObject(f => model.SaveAsWavefront(f, animation, time));
        }

        public static string AttachToCurrentTest<TvG, TvM, TvS>(this Geometry.MeshBuilder<TvG, TvM, TvS> mesh, string fileName)
            where TvG : struct, Geometry.VertexTypes.IVertexGeometry
            where TvM : struct, Geometry.VertexTypes.IVertexMaterial
            where TvS : struct, Geometry.VertexTypes.IVertexSkinning
        {
            var gl2model = ModelRoot.CreateModel();

            var gl2mesh = gl2model.CreateMeshes(mesh).First();

            var node = gl2model.UseScene(0).CreateNode();
            node.Mesh = gl2mesh;

            return gl2model.AttachToCurrentTest(fileName);
        }

        public static string AttachToCurrentTest(this ModelRoot model, string fileName, WriteSettings settings = null)
        {
            string validationPath = null;

            if (fileName.ToLowerInvariant().EndsWith(".glb"))
            {
                validationPath = fileName = AttachmentInfo
                    .From(fileName)
                    .WriteObject(f => model.SaveGLB(f, settings))
                    .FullName;
            }
            else if (fileName.ToLowerInvariant().EndsWith(".gltf"))
            {
                if (settings == null) settings = new WriteSettings { JsonIndented = true };

                validationPath = fileName = AttachmentInfo
                    .From(fileName)
                    .WriteObject(f => model.Save(f, settings))
                    .FullName;
            }
            else if (fileName.ToLowerInvariant().EndsWith(".obj"))
            {
                // skip exporting to obj if gpu instancing is there
                if (Node.Flatten(model.DefaultScene).Any(n => n.GetGpuInstancing() != null)) return fileName;                

                fileName = fileName.Replace(" ", "_");

                fileName = AttachmentInfo
                    .From(fileName)
                    .WriteObject(f => model.SaveAsWavefront(f))
                    .FullName;
            }
            else if (fileName.ToLowerInvariant().EndsWith(".plotly"))
            {
                fileName = fileName.Replace(".plotly", ".html");

                var html = model.DefaultScene
                    .ToPlotly()
                    .ToHtml();

                fileName = AttachmentInfo
                    .From(fileName)
                    .WriteAllText(html)
                    .FullName;
            }           

            if (validationPath != null)
            {
                var report = GltfValidator.ValidationReport.ValidateAsync(fileName, System.Threading.CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();                

                if (report == null) return fileName;

                if (report.Severity == GltfValidator.Severity.Error || report.Severity == GltfValidator.Severity.Warning)
                {
                    TestContext.WriteLine(report.ToString());
                }

                Assert.AreNotEqual(GltfValidator.Severity.Error, report.Severity);
            }

            return fileName;
        }
    }

    
}
