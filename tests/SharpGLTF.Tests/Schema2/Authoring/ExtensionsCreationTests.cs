using System.Numerics;

using NUnit.Framework;

namespace SharpGLTF.Schema2.Authoring
{
    using VPOS = Geometry.VertexTypes.VertexPosition;
    using VTEX = Geometry.VertexTypes.VertexTexture1;

    [TestFixture]
    [Category("Model Authoring")]
    public class ExtensionsCreationTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            // TestFiles.DownloadReferenceModels();
        }

        #endregion

        [Test(Description = "Creates a scene with lights")]
        public void CreateSceneWithWithLightsExtension()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var root = ModelRoot.CreateModel();
            var scene = root.UseScene("Empty Scene");

            scene.CreateNode()
                .PunctualLight = root.CreatePunctualLight(PunctualLightType.Directional)
                .WithColor(Vector3.UnitX, 2);

            scene.CreateNode()
                .PunctualLight = root.CreatePunctualLight(PunctualLightType.Spot)
                .WithColor(Vector3.UnitY, 3, 10)
                .WithSpotCone(0.2f, 0.3f);

            root.AttachToCurrentTest("sceneWithLight.gltf");
            root.AttachToCurrentTest("sceneWithLight.glb");
        }

        [Test(Description = "Creates a quad mesh with a complex material")]
        public void CreateSceneWithSpecularGlossinessExtension()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var basePath = System.IO.Path.Combine(TestFiles.RootDirectory, "glTF-Sample-Models", "2.0", "SpecGlossVsMetalRough", "glTF");

            // first, create a default material
            var material = new Materials.MaterialBuilder("material1")
                .WithMetallicRoughnessShader()
                .WithChannelImage(Materials.KnownChannel.Normal, System.IO.Path.Combine(basePath, "WaterBottle_normal.png"))
                .WithChannelImage(Materials.KnownChannel.Emissive, System.IO.Path.Combine(basePath, "WaterBottle_emissive.png"))
                .WithChannelImage(Materials.KnownChannel.Occlusion, System.IO.Path.Combine(basePath, "WaterBottle_occlusion.png"))
                .WithChannelImage(Materials.KnownChannel.BaseColor, System.IO.Path.Combine(basePath, "WaterBottle_baseColor.png"))
                .WithChannelImage(Materials.KnownChannel.MetallicRoughness, System.IO.Path.Combine(basePath, "WaterBottle_roughnessMetallic.png"));

            // wrap the fallback material with a PBR Specular Glossiness material.
            material = new Materials.MaterialBuilder("material1")
                .WithFallback(material)
                .WithSpecularGlossinessShader()
                .WithChannelImage(Materials.KnownChannel.Normal, System.IO.Path.Combine(basePath, "WaterBottle_normal.png"))
                .WithChannelImage(Materials.KnownChannel.Emissive, System.IO.Path.Combine(basePath, "WaterBottle_emissive.png"))
                .WithChannelImage(Materials.KnownChannel.Occlusion, System.IO.Path.Combine(basePath, "WaterBottle_occlusion.png"))
                .WithChannelImage(Materials.KnownChannel.Diffuse, System.IO.Path.Combine(basePath, "WaterBottle_diffuse.png"))
                .WithChannelImage(Materials.KnownChannel.SpecularGlossiness, System.IO.Path.Combine(basePath, "WaterBottle_specularGlossiness.png"));

            var mesh = new Geometry.MeshBuilder<VPOS, VTEX>("mesh1");
            mesh.UsePrimitive(material).AddQuadrangle
                ((new Vector3(-10, 10, 0), new Vector2(1, 0))
                , (new Vector3(10, 10, 0), new Vector2(0, 0))
                , (new Vector3(10, -10, 0), new Vector2(0, 1))
                , (new Vector3(-10, -10, 0), new Vector2(1, 1))
                );

            var scene = new Scenes.SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            scene.AttachToCurrentTest("result.glb");
            scene.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates a quad mesh with a complex material")]
        public void CreateSceneWithClearCoatExtension()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var basePath = System.IO.Path.Combine(TestFiles.RootDirectory, "glTF-Sample-Models", "2.0", "SpecGlossVsMetalRough", "glTF");

            // first, create a default material
            var material = new Materials.MaterialBuilder("material")
                .WithMetallicRoughnessShader()
                .WithChannelImage(Materials.KnownChannel.Normal, System.IO.Path.Combine(basePath, "WaterBottle_normal.png"))
                .WithChannelImage(Materials.KnownChannel.Emissive, System.IO.Path.Combine(basePath, "WaterBottle_emissive.png"))
                .WithChannelImage(Materials.KnownChannel.Occlusion, System.IO.Path.Combine(basePath, "WaterBottle_occlusion.png"))
                .WithChannelImage(Materials.KnownChannel.BaseColor, System.IO.Path.Combine(basePath, "WaterBottle_baseColor.png"))
                .WithChannelImage(Materials.KnownChannel.MetallicRoughness, System.IO.Path.Combine(basePath, "WaterBottle_roughnessMetallic.png"))
                .WithChannelImage(Materials.KnownChannel.ClearCoat, System.IO.Path.Combine(basePath, "WaterBottle_emissive.png"))
                .WithChannelParam(Materials.KnownChannel.ClearCoat, new Vector4(0.5f, 0, 0, 0))
                .WithChannelImage(Materials.KnownChannel.ClearCoatRoughness, System.IO.Path.Combine(basePath, "WaterBottle_roughnessMetallic.png"))
                .WithChannelImage(Materials.KnownChannel.ClearCoatNormal, System.IO.Path.Combine(basePath, "WaterBottle_normal.png"));

            var mesh = new Geometry.MeshBuilder<VPOS, VTEX>("mesh1");
            mesh.UsePrimitive(material).AddQuadrangle
                ((new Vector3(-10, 10, 0), new Vector2(1, 0))
                , (new Vector3(10, 10, 0), new Vector2(0, 0))
                , (new Vector3(10, -10, 0), new Vector2(0, 1))
                , (new Vector3(-10, -10, 0), new Vector2(1, 1))
                );

            var scene = new Scenes.SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            scene.AttachToCurrentTest("result.glb");
            scene.AttachToCurrentTest("result.gltf");
        }

        [TestCase("shannon-dxt5.dds")]
        [TestCase("shannon.webp")]
        public void CreateSceneWithTextureImageExtension(string textureFileName)
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var basePath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "Assets");

            // first, create a default material
            var material = new Materials.MaterialBuilder("material1")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelImage
                (
                    Materials.KnownChannel.BaseColor,
                    System.IO.Path.Combine(basePath, textureFileName)
                );                

            var mesh = new Geometry.MeshBuilder<VPOS, VTEX>("mesh1");

            mesh
                .UsePrimitive(material)
                .AddQuadrangle
                ((new Vector3(-10, 10, 0), new Vector2(1, 0))
                , (new Vector3(10, 10, 0), new Vector2(0, 0))
                , (new Vector3(10, -10, 0), new Vector2(0, 1))
                , (new Vector3(-10, -10, 0), new Vector2(1, 1))
                );

            var model = ModelRoot.CreateModel();

            model.CreateMeshes(mesh);

            model.UseScene("Default")
                .CreateNode("RootNode")
                .WithMesh(model.LogicalMeshes[0]);

            model.AttachToCurrentTest("result_wf.obj");
            model.AttachToCurrentTest("result_glb.glb");
            model.AttachToCurrentTest("result_gltf.gltf");            
        }        

        [Test]
        public void CrateSceneWithTextureTransformExtension()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var basePath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "Assets");

            // first, create a default material
            var material = new Materials.MaterialBuilder("material1")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelImage(Materials.KnownChannel.BaseColor, System.IO.Path.Combine(basePath, "shannon.jpg"));

            material.GetChannel(Materials.KnownChannel.BaseColor).UseTexture().WithTransform(0.40f,0.25f, 0.5f,0.5f);

            var mesh = new Geometry.MeshBuilder<VPOS, VTEX>("mesh1");

            mesh
                .UsePrimitive(material)
                .AddQuadrangle
                ((new Vector3(-10, 10, 0), new Vector2(1, 0))
                , (new Vector3(10, 10, 0), new Vector2(0, 0))
                , (new Vector3(10, -10, 0), new Vector2(0, 1))
                , (new Vector3(-10, -10, 0), new Vector2(1, 1))
                );

            var model = ModelRoot.CreateModel();

            model.CreateMeshes(mesh);

            model.UseScene("Default")
                .CreateNode("RootNode")
                .WithMesh(model.LogicalMeshes[0]);
            
            model.AttachToCurrentTest("result_glb.glb");
            model.AttachToCurrentTest("result_gltf.gltf");
        }
    }
}
