using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

using NUnit.Framework;

namespace SharpGLTF.Schema2.Authoring
{
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

            /*

            root.Extras["author"] = "me";

            root.Extras["value1"] = 17;
            root.Extras["array1"] = new Single[] { 1, 2, 3 };
            root.Extras["dict1"] = new Dictionary<String, Object>
            {
                ["A"] = 16,
                ["B"] = "delta",
                ["C"] = new Single[] { 4, 6, 7 },
                ["D"] = new Dictionary<String, Object> { ["S"]= 1, ["T"] = 2 }
            };

            var json = root.GetJSON(Newtonsoft.Json.Formatting.Indented);
            var bytes = root.WriteGLB();

            var rootBis = ModelRoot.ParseGLB(bytes);

            CollectionAssert.AreEqual(root.Extras, rootBis.Extras);

            Assert.AreEqual(root.Extras["author"], rootBis.Extras["author"]);
            Assert.AreEqual(root.Extras["value1"], rootBis.Extras["value1"]);
            CollectionAssert.AreEqual
                (
                root.Extras["array1"] as Array,
                rootBis.Extras["array1"] as Array
                );

            CollectionAssert.AreEqual
                (
                root.Extras["dict1"] as Dictionary<string, Object>,
                rootBis.Extras["dict1"] as Dictionary<string, Object>
                );*/
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

            material.FindChannel("BaseColor").SetTransform(0, Vector2.Zero, Vector2.One, 1.5f);
            model.AttachToCurrentTest("result_withTransform.glb");
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
    }
}
