using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

using NUnit.Framework;

namespace SharpGLTF.Schema2.Authoring
{
    using VPOS = Geometry.VertexTypes.VertexPosition;
    using VTEX = Geometry.VertexTypes.VertexTexture1;
    using VPOSNRM = Geometry.VertexTypes.VertexPositionNormal;
    

    [TestFixture]
    public class BasicSceneCreationTests
    {
        [Test(Description = "Creates an empty model")]
        public void CreateEmptyScene()
        {
            var root = ModelRoot.CreateModel();

            var scene = root.UseScene("Empty Scene");

            Assert.NotNull(scene);
            Assert.AreEqual("Empty Scene", root.DefaultScene.Name);
        }

        [Test(Description = "Creates an empty model")]
        public void CreateSceneWithExtras()
        {
            var root = ModelRoot.CreateModel();
            var scene = root.UseScene("Empty Scene");

            var dict = root.TryUseExtrasAsDictionary(true);

            dict["author"] = "me";

            dict["value1"] = 17;
            dict["array1"] = new IO.JsonList { 1, 2, 3 };
            dict["dict1"] = new IO.JsonDictionary
            {
                ["A"] = 16,
                ["B"] = "delta",
                ["C"] = new IO.JsonList { 4, 6, 7 },
                ["D"] = new IO.JsonDictionary { ["S"]= 1, ["T"] = 2 }
            };

            var json = root.GetJSON(Newtonsoft.Json.Formatting.Indented);

            var bytes = root.WriteGLB();
            var rootBis = ModelRoot.ParseGLB(bytes);

            var adict = root.TryUseExtrasAsDictionary(false);
            var bdict = rootBis.TryUseExtrasAsDictionary(false);

            CollectionAssert.AreEqual(adict, bdict);

            Assert.AreEqual(adict["author"], bdict["author"]);
            Assert.AreEqual(adict["value1"], bdict["value1"]);
            CollectionAssert.AreEqual
                (
                adict["array1"] as IO.JsonList,
                bdict["array1"] as IO.JsonList
                );

            CollectionAssert.AreEqual
                (
                adict["dict1"] as IO.JsonDictionary,
                bdict["dict1"] as IO.JsonDictionary
                );
        }

        [Test(Description = "Creates a scene with lights")]
        public void CreateSceneWithLights()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var root = ModelRoot.CreateModel();
            var scene = root.UseScene("Empty Scene");

            scene.CreateNode()
                .PunctualLight = root.CreatePunctualLight(PunctualLightType.Directional)
                .WithColor(Vector3.UnitX, 2);

            var node2 = scene.CreateNode()
                .PunctualLight = root.CreatePunctualLight(PunctualLightType.Spot)
                .WithColor(Vector3.UnitY, 3, 10)
                .WithSpotCone(0.2f, 0.3f);

            root.AttachToCurrentTest("sceneWithLight.gltf");
            root.AttachToCurrentTest("sceneWithLight.glb");
        }

        [Test(Description = "Creates a model with a triangle mesh")]
        public void CreateSolidTriangleScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            // create model
            var model = ModelRoot.CreateModel();

            // create scene
            var scene = model.DefaultScene = model.UseScene("Default");

            // create node
            var rnode = scene.CreateNode("Triangle Node");

            // create material
            var material = model.CreateMaterial("Default")
                .WithDefault(new Vector4(0, 1, 0, 1))
                .WithDoubleSide(true);

            // create mesh
            var rmesh = rnode.Mesh = model.CreateMesh("Triangle Mesh");

            // create the vertex positions
            var positions = new[]
            {
                new Vector3(0, 10, 0),
                new Vector3(-10, -10, 0),
                new Vector3(10, -10, 0),
            };

            // create an index buffer and fill it            
            var indices = new[] { 0, 1, 2 };

            // create mesh primitive
            var primitive = rmesh.CreatePrimitive()
                .WithVertexAccessor("POSITION", positions)
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices)
                .WithMaterial(material);

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates a model with a textured triangle mesh")]
        public void CreateTextureTriangleScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            // we'll use our icon as the source texture
            var imagePath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "..\\..\\..\\..\\..\\build\\Icons\\glTF2Sharp.png");

            // create a basic scene
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");
            var rnode = scene.CreateNode("Triangle Node");
            var rmesh = rnode.Mesh = model.CreateMesh("Triangle Mesh");

            var material = model.CreateMaterial("Default")
                .WithPBRMetallicRoughness(Vector4.One, imagePath)
                .WithDoubleSide(true);

            // define the triangle positions
            var positions = new[]
            {
                new Vector3(0, 10, 0),
                new Vector3(-10, -10, 0),
                new Vector3(10, -10, 0)
            };

            // define the triangle UV coordinates
            var texCoords = new[]
            {
                new Vector2(0.5f, -0.8f),
                new Vector2(-0.5f, 1.2f),
                new Vector2(1.5f, 1.2f)
            };

            // create a mesh primitive and assgin the accessors and other properties
            var primitive = rmesh.CreatePrimitive()
                .WithVertexAccessor("POSITION", positions)
                .WithVertexAccessor("TEXCOORD_0", texCoords)
                .WithIndicesAutomatic(PrimitiveType.TRIANGLES)
                .WithMaterial(material);

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.obj");
            model.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates an interleaved scene using a toolkit utilities")]
        public void CreateInterleavedQuadScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var vertices = new[]
            {
                new VPOSNRM(-10,  10, 0, -10,  10, 15),
                new VPOSNRM( 10,  10, 0,  10,  10, 15),
                new VPOSNRM( 10, -10, 0,  10, -10, 15),
                new VPOSNRM(-10, -10, 0, -10, -10, 15)
            };

            var model = ModelRoot.CreateModel();

            var mesh = model.CreateMesh("mesh1");

            mesh.CreatePrimitive()
                .WithMaterial(model.CreateMaterial("Default").WithDefault(Vector4.One).WithDoubleSide(true))
                .WithVertexAccessors(vertices)
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, new int[] { 0, 1, 2, 0, 2, 3 });

            var scene = model.UseScene("Default");
            var rnode = scene.CreateNode("RootNode").WithMesh(mesh);

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates a quad mesh with a complex material")]
        public void CreateFallbackMaterialScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var basePath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "glTF-Sample-Models", "2.0", "SpecGlossVsMetalRough", "glTF");

            var material = new Materials.MaterialBuilder("material1");
            material.ShaderStyle = "PBRSpecularGlossiness";
            material.UseChannel("Normal").UseTexture().WithImage(System.IO.Path.Combine(basePath, "WaterBottle_normal.png"));
            material.UseChannel("Emissive").UseTexture().WithImage(System.IO.Path.Combine(basePath, "WaterBottle_emissive.png"));
            material.UseChannel("Occlusion").UseTexture().WithImage(System.IO.Path.Combine(basePath, "WaterBottle_occlusion.png"));
            material.UseChannel("Diffuse").UseTexture().WithImage(System.IO.Path.Combine(basePath, "WaterBottle_diffuse.png"));
            material.UseChannel("Glossiness").UseTexture().WithImage(System.IO.Path.Combine(basePath, "WaterBottle_specularGlossiness.png"));
            material.UseChannel("Specular").Color = Vector4.One * 0.3f;

            var fallback = material.CompatibilityFallback = new Materials.MaterialBuilder("material1 fallback");
            fallback.UseChannel("Normal").UseTexture().WithImage(System.IO.Path.Combine(basePath, "WaterBottle_normal.png"));
            fallback.UseChannel("Emissive").UseTexture().WithImage(System.IO.Path.Combine(basePath, "WaterBottle_emissive.png"));
            fallback.UseChannel("Occlusion").UseTexture().WithImage(System.IO.Path.Combine(basePath, "WaterBottle_occlusion.png"));
            fallback.UseChannel("BaseColor").UseTexture().WithImage(System.IO.Path.Combine(basePath, "WaterBottle_baseColor.png"));
            fallback.UseChannel("Metallic").UseTexture().WithImage(System.IO.Path.Combine(basePath, "WaterBottle_roughnessMetallic.png"));
            fallback.UseChannel("Roughness").Amount = 0.3f;


            var mesh = new Geometry.MeshBuilder<VPOS, VTEX>("mesh1");
            mesh.UsePrimitive(material).AddPolygon
                ( (new Vector3(-10, 10, 0), new Vector2(0, 0))
                , (new Vector3( 10, 10, 0), new Vector2(1, 0))
                , (new Vector3( 10, -10, 0), new Vector2(1, 1))
                , (new Vector3(-10, -10, 0), new Vector2(0, 1))
                );

            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");
            var rnode = scene.CreateNode("RootNode").WithMesh( model.CreateMesh(mesh) );

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");

        }
    }
}
