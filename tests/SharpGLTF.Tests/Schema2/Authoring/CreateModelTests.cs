using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;


namespace SharpGLTF.Schema2.Authoring
{
    using Geometry;

    using VEMPTY = Geometry.VertexTypes.VertexEmpty;
    using STATICVERTEX = Geometry.VertexTypes.VertexPositionNormal;
    using VPOS = Geometry.VertexTypes.VertexPosition;
    using SKIN4 = Geometry.VertexTypes.VertexJoints4;

    [TestFixture]
    public class CreateModelTests
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

        [Test(Description ="Creates a scene with lights")]
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

        [Test(Description ="Creates a model with a triangle mesh")]
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

        [Test(Description ="Creates a model with a textured triangle mesh")]
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
            model.AttachToCurrentTest("result.gltf");            
        }

        [Test(Description = "Creates a simple scene using a mesh builder helper class")]
        public void CreateSimpleMeshBuilderScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var meshBuilder = new SimpleSceneBuilder<Vector4>();
            meshBuilder.AddPolygon(new Vector4(1, 1, 1, 1), (-10, 10,  0), (10, 10,  0), (10, -10,  0), (-10, -10,  0));
            meshBuilder.AddPolygon(new Vector4(1, 1, 0, 1), (-10, 10, 10), (10, 10, 10), (10, -10, 10), (-10, -10, 10));
            meshBuilder.AddPolygon(new Vector4(1, 0, 0, 1), (-10, 10, 20), (10, 10, 20), (10, -10, 20), (-10, -10, 20));

            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");
            var rnode = scene.CreateNode("RootNode");            

            // setup a lambda function that creates a material for a given color
            Material createMaterialForColor(Vector4 color)
            {
                return model.CreateMaterial().WithDefault(color).WithDoubleSide(true);
            };

            // fill our node with the mesh
            meshBuilder.CopyToNode(rnode, createMaterialForColor);

            model.AttachToCurrentTest("result.gltf");
            model.AttachToCurrentTest("result.glb");            
        }


        [Test(Description = "Creates an interleaved scene using a toolkit utilities")]
        public void CreateInterleavedQuadScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var vertices = new[]
            {
                new STATICVERTEX(-10,  10, 0, -10,  10, 15),
                new STATICVERTEX( 10,  10, 0,  10,  10, 15),
                new STATICVERTEX( 10, -10, 0,  10, -10, 15),
                new STATICVERTEX(-10, -10, 0, -10, -10, 15)
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

        [Test(Description = "Creates an interleaved scene using a mesh builder helper class")]
        public void CreateInterleavedMeshBuilderScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var meshBuilder = new MeshBuilder<Vector4, STATICVERTEX>("mesh1");

            var v1 = new STATICVERTEX(-10, 10, 0, -10, 10, 15);
            var v2 = new STATICVERTEX( 10, 10, 0, 10, 10, 15);
            var v3 = new STATICVERTEX( 10,-10, 0, 10, -10, 15);
            var v4 = new STATICVERTEX(-10,-10, 0, -10, -10, 15);            
            meshBuilder.AddPolygon(Vector4.One, v1, v2, v3, v4);

            var model = ModelRoot.CreateModel();

            // setup a lambda function that creates a material for a given color
            Material createMaterialForColor(Vector4 color) => model.CreateMaterial().WithDefault(color).WithDoubleSide(true);

            model.UseScene("Default")
                .CreateNode("RootNode")
                .WithMesh( model.CreateMesh(createMaterialForColor, meshBuilder));

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates a scene with 4 meshes, where the meshes have been initialized so they can share the same vertex and index buffers")]
        public void CreateSharedBuffersScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            // create several meshes
            var meshBuilder1 = new MeshBuilder<Vector4, STATICVERTEX>("mesh1");
            var meshBuilder2 = new MeshBuilder<Vector4, STATICVERTEX>("mesh2");
            var meshBuilder3 = new MeshBuilder<Vector4, STATICVERTEX>("mesh3");
            var meshBuilder4 = new MeshBuilder<Vector4, STATICVERTEX>("mesh4");

            meshBuilder1.AddCube(new Vector4(1, 1, 0, 1), Matrix4x4.Identity);
            meshBuilder2.AddCube(new Vector4(1, 0, 1, 1), Matrix4x4.Identity);
            meshBuilder3.AddSphere(new Vector4(0, 1, 1, 1), 0.5f, Matrix4x4.Identity);
            meshBuilder4.AddSphere(new Vector4(1, 1, 0, 1), 0.5f, Matrix4x4.Identity);

            // create the gltf model
            var model = ModelRoot.CreateModel();

            // setup a lambda function that creates a material for a given color
            Material createMaterialForColor(Vector4 color) => model.CreateMaterial().WithDefault(color).WithDoubleSide(true);

            var meshes = model.CreateMeshes(createMaterialForColor, meshBuilder1, meshBuilder2, meshBuilder3, meshBuilder4);

            model.UseScene("Default").CreateNode("Cube1").WithMesh(meshes[0]).WithLocalTranslation(new Vector3(-5, 0, 0));
            model.UseScene("Default").CreateNode("Cube2").WithMesh(meshes[1]).WithLocalTranslation(new Vector3(0, 5, 0));
            model.UseScene("Default").CreateNode("Sphere1").WithMesh(meshes[2]).WithLocalTranslation(new Vector3(+5, 0, 0));
            model.UseScene("Default").CreateNode("Sphere2").WithMesh(meshes[3]).WithLocalTranslation(new Vector3(0, -5, 0));

            model.MergeBuffers();

            Assert.AreEqual(1, model.LogicalBuffers.Count);
            Assert.AreEqual(2, model.LogicalBufferViews.Count);
            Assert.AreEqual(BufferMode.ARRAY_BUFFER, model.LogicalBufferViews[0].DeviceBufferTarget);
            Assert.AreEqual(BufferMode.ELEMENT_ARRAY_BUFFER, model.LogicalBufferViews[1].DeviceBufferTarget);
            Assert.AreEqual(3, model.LogicalMaterials.Count);

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates an animated scene using a mesh builder helper class")]
        public void CreateAnimatedMeshBuilderScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            // create animation sequence with 4 frames
            var keyframes = new Dictionary<Single, Vector3>()
            {
                [1] = new Vector3(0, 0, 0),
                [2] = new Vector3(50, 0, 0),
                [3] = new Vector3(0, 50, 0),
                [4] = new Vector3(0, 0, 0),
            };

            // create a mesh
            var meshBuilder = new MeshBuilder<Vector4, STATICVERTEX>("mesh1");
            meshBuilder.AddCube(Vector4.One, Matrix4x4.Identity);            

            // create the gltf model
            var model = ModelRoot.CreateModel();

            model.UseScene("Default")
                .CreateNode("RootNode")
                .WithTranslationAnimation("track1", keyframes)
                .WithMesh(model.CreateMesh(c => model.CreateMaterial().WithDefault(c), meshBuilder));

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }        

        [Test(Description = "Creates a skinned animated scene using a mesh builder helper class")]
        public void CreateSkinnedAnimatedMeshBuilderScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();
            
            // create animation sequence with 4 frames
            var keyframes = new Dictionary<Single, Quaternion>
            {
                [1] = Quaternion.Identity,
                [2] = Quaternion.CreateFromYawPitchRoll(0, 1, 0),
                [3] = Quaternion.CreateFromYawPitchRoll(0, 0, 1),
                [4] = Quaternion.Identity,
            };
            
            // create the mesh
            var meshBuilder = new MeshBuilder<Vector4, VPOS, VEMPTY, SKIN4>("mesh1");

            var v1 = (new VPOS(-10, 0, +10), new SKIN4(0));
            var v2 = (new VPOS(+10, 0, +10), new SKIN4(0));
            var v3 = (new VPOS(+10, 0, -10), new SKIN4(0));
            var v4 = (new VPOS(-10, 0, -10), new SKIN4(0));

            var v5 = (new VPOS(-10, 40, +10), new SKIN4(0, 1));
            var v6 = (new VPOS(+10, 40, +10), new SKIN4(0, 1));
            var v7 = (new VPOS(+10, 40, -10), new SKIN4(0, 1));
            var v8 = (new VPOS(-10, 40, -10), new SKIN4(0, 1));

            var v9  = (new VPOS(-5, 80, +5), new SKIN4(2));
            var v10 = (new VPOS(+5, 80, +5), new SKIN4(2));
            var v11 = (new VPOS(+5, 80, -5), new SKIN4(2));
            var v12 = (new VPOS(-5, 80, -5), new SKIN4(2));

            var pink = new Vector4(1, 0, 1, 1);
            var yellow = new Vector4(1, 1, 0, 1);

            meshBuilder.AddPolygon(pink, v1, v2, v6, v5);
            meshBuilder.AddPolygon(pink, v2, v3, v7, v6);
            meshBuilder.AddPolygon(pink, v3, v4, v8, v7);
            meshBuilder.AddPolygon(pink, v4, v1, v5, v8);

            meshBuilder.AddPolygon(yellow, v5, v6, v10, v9);
            meshBuilder.AddPolygon(yellow, v6, v7, v11, v10);
            meshBuilder.AddPolygon(yellow, v7, v8, v12, v11);
            meshBuilder.AddPolygon(yellow, v8, v5, v9, v12);

            // create base model
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");            

            // create the three joints that will affect the mesh
            var skelet = scene.CreateNode("Skeleton");
            var joint1 = skelet.CreateNode("Joint 1").WithLocalTranslation(new Vector3(0, 0, 0));
            var joint2 = joint1.CreateNode("Joint 2").WithLocalTranslation(new Vector3(0, 40, 0)).WithRotationAnimation("Base Track", keyframes);
            var joint3 = joint2.CreateNode("Joint 3").WithLocalTranslation(new Vector3(0, 40, 0));

            // setup skin
            var snode = scene.CreateNode("Skeleton Node");
            snode.Skin = model.CreateSkin();
            snode.Skin.Skeleton = skelet;
            snode.Skin.BindJoints(joint1, joint2, joint3);

            // setup a lambda function that creates a material for a given color
            Material createMaterialForColor(Vector4 color) => model.CreateMaterial().WithDefault(color).WithDoubleSide(true);

            snode.WithMesh(model.CreateMesh(createMaterialForColor, meshBuilder));

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }
    }
}
