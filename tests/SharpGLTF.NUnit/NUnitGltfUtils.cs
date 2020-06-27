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

        public static string AttachToCurrentTest(this Schema2.ModelRoot model, string fileName, WriteSettings settings = null)
        {
            // find the output path for the current test
            fileName = TestContext.CurrentContext.GetAttachmentPath(fileName, true);

            if (fileName.ToLower().EndsWith(".glb"))
            {
                model.SaveGLB(fileName, settings);
            }
            else if (fileName.ToLower().EndsWith(".gltf"))
            {
                if (settings == null) settings = new WriteSettings { JsonIndented = true };

                model.Save(fileName, settings);
            }
            else if (fileName.ToLower().EndsWith(".obj"))
            {
                fileName = fileName.Replace(" ", "_");
                Schema2.Schema2Toolkit.SaveAsWavefront(model, fileName);
            }
            else if (fileName.ToLower().EndsWith(".plotly"))
            {
                fileName = fileName.Replace(".plotly", ".html");

                

                var trace = model.DefaultScene.ToTrace();
                var plot = Plotly.Plot.traces(trace);

                var xaxis = Plotly.Scene.xaxis(Plotly.Xaxis.color("red"));
                var yaxis = Plotly.Scene.yaxis(Plotly.Yaxis.color("green"));
                var zaxis = Plotly.Scene.zaxis(Plotly.Zaxis.color("blue"));
                var camera = Plotly.Camera.up(Plotly.Up.x(0), Plotly.Up.y(1), Plotly.Up.z(0));
                var scene = Plotly.Layout.scene(Plotly.Scene.Aspectmode.data(), Plotly.Scene.camera(camera) ,xaxis,yaxis,zaxis );

                var layout = Plotly.Plot.layout
                    ( Plotly.Layout.autosize(true)
                    // , Plotly.Layout.width(0)
                    , Plotly.Layout.height(920)
                    // , Plotly.Layout.margin(Plotly.Margin.autoexpand(true))
                    // , Plotly.Layout.margin(Plotly.Margin.pad(5))                    
                    // , Plotly.Layout.margin(Plotly.Margin.t(5), Plotly.Margin.b(5))                    
                    , scene
                    );                

                var document = new Plotly.Plot(plot, layout);                
                var render = document.Render().ToString();

                System.IO.File.WriteAllText(fileName, render);
            }

            // Attach the saved file to the current test
            TestContext.AddTestAttachment(fileName);

            if (fileName.ToLower().EndsWith(".obj")) return fileName;

            var report = gltf_validator.ValidateFile(fileName);
            if (report == null) return fileName;

            if (report.HasErrors || report.HasWarnings)
            {
                TestContext.WriteLine(report.ToString());
            }

            Assert.IsFalse(report.HasErrors);

            return fileName;
        }
    }

    
}
