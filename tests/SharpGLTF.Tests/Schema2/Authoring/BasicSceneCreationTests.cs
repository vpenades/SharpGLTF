using System.Numerics;

using NUnit.Framework;

namespace SharpGLTF.Schema2.Authoring
{
    using VPOSNRM = Geometry.VertexBuilder<Geometry.VertexTypes.VertexPositionNormal,Geometry.VertexTypes.VertexEmpty,Geometry.VertexTypes.VertexEmpty>;


    [TestFixture]
    [Category("Model Authoring")]
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

            var json = root.GetJSON(true);

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

        [Test(Description = "Creates a model with a triangle mesh")]
        public void CreateSceneWithSolidTriangle()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

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
        public void CreateSceneWithTexturedTriangle()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

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
        public void CreateSceneWithInterleavedQuadMesh()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var vertices = new[]
            {
                VPOSNRM.Create(new Vector3(-10,  10, 0), Vector3.UnitZ),
                VPOSNRM.Create(new Vector3( 10,  10, 0), Vector3.UnitZ),
                VPOSNRM.Create(new Vector3( 10, -10, 0), Vector3.UnitZ),
                VPOSNRM.Create(new Vector3(-10, -10, 0), Vector3.UnitZ)
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

        [Test(Description = "Creates a scene with a perspective camera")]
        public void CreateSceneWithCamera()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            
            var model = ModelRoot.CreateModel();
            
            model.UseScene(0)
                .CreateNode()
                .WithLocalTranslation(new Vector3(0, 3, 10))
                .WithPerspectiveCamera(null, 1, 0.1f);                

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }
    }
}
