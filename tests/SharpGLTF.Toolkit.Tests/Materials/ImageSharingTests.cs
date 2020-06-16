using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

using SharpGLTF.Geometry.Parametric;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;

namespace SharpGLTF.Materials
{
    [Category("Toolkit.Materials")]
    public class ImageSharingTests
    {
        [Test]
        public void WriteTwoModelsWithSharedTexture()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // get the texture from its original location and save it in our test directory.
            var assetsPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "Assets");
            

            var tex1Bytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(assetsPath, "shannon.png"));
            var tex2Bytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(assetsPath, "Texture1.jpg"));

            var tex1 = tex1Bytes.AttachToCurrentTest("shared-shannon.png");
            var tex2 = tex2Bytes.AttachToCurrentTest("subdir\\shared-in-dir-Texture1.jpg");

            // create a material using our shared texture
            var material1 = new MaterialBuilder()                
                .WithUnlitShader()
                .WithBaseColor(tex1);

            // create a material using our shared texture
            var material2 = new MaterialBuilder()
                .WithUnlitShader()
                .WithBaseColor(tex2);

            // create a simple cube mesh
            var mesh1 = new Cube<MaterialBuilder>(material1).ToMesh(Matrix4x4.Identity);
            var mesh2 = new Cube<MaterialBuilder>(material2).ToMesh(Matrix4x4.Identity);
            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh1, Matrix4x4.CreateTranslation(-2, 0, 0));
            scene.AddRigidMesh(mesh2, Matrix4x4.CreateTranslation(2, 0, 0));

            var gltf = scene.ToGltf2();

            // define the texture sharing hook; this is a pretty naive approach, but it's good
            // enough to demonstrate how it works.

            string imageSharingHook(IO.WriteContext ctx, string uri, Memory.MemoryImage image)
            {
                Assert.IsTrue(new string[] { tex1, tex2 }.Contains(image.SourcePath) );

                if (File.Exists(image.SourcePath))
                {
                    // image.SourcePath is an absolute path, we must make it relative to ctx.CurrentDirectory

                    var currDir = ctx.CurrentDirectory.FullName + "\\";

                    // if the shared texture can be reached by the model in its directory, reuse the texture.
                    if (image.SourcePath.StartsWith(currDir, StringComparison.OrdinalIgnoreCase))
                    {
                        // we've found the shared texture!, return the uri relative to the model:
                        return image.SourcePath.Substring(currDir.Length);
                    }

                    // TODO: Here we could also try to find a texture equivalent to MemoryImage in the
                    // CurrentDirectory even if it has a different name, to minimize texture duplication.
                }

                // we were unable to reuse the shared texture,
                // default to write our own texture.

                image.SaveToFile(Path.Combine(ctx.CurrentDirectory.FullName, uri));

                return uri;
            }

            var settings = new WriteSettings();            
            settings.ImageWriting = ResourceWriteMode.SatelliteFile;
            settings.ImageWriteCallback = imageSharingHook;

            // save the model several times:           

            var path1 = gltf.AttachToCurrentTest("model1.glb", settings);
            var path2 = gltf.AttachToCurrentTest("model2.glb", settings);
            var path3 = gltf.AttachToCurrentTest("model3.gltf", settings);

            var satellites1 = ModelRoot.GetSatellitePaths(path1);
            var satellites2 = ModelRoot.GetSatellitePaths(path2);
            var satellites3 = ModelRoot.GetSatellitePaths(path3);

            Assert.IsTrue(satellites1.Contains("shared-shannon.png"));
            Assert.IsTrue(satellites1.Contains("subdir/shared-in-dir-Texture1.jpg"));

            Assert.IsTrue(satellites2.Contains("shared-shannon.png"));
            Assert.IsTrue(satellites2.Contains("subdir/shared-in-dir-Texture1.jpg"));

            Assert.IsTrue(satellites3.Contains("shared-shannon.png"));
            Assert.IsTrue(satellites3.Contains("subdir/shared-in-dir-Texture1.jpg"));
        }

    }
}
