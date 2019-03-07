using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2.Authoring
{
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

            root.Extras["author"] = "me";

            root.Extras["value1"] = 17;
            root.Extras["array1"] = new Single[] { 1, 2, 3 };
            root.Extras["dict1"] = new Dictionary<string, Object>
            {
                ["A"] = 16,
                ["B"] = "delta",
                ["C"] = new Single[] { 4, 6, 7 },
                ["D"] = new Dictionary<string, Object> { ["S"]= 1, ["T"] = 2 }
            };

            var json = root.GetJSON(Newtonsoft.Json.Formatting.Indented);
            var bytes = root.GetGLB();

            var rootBis = ModelRoot.ParseGLB(bytes);

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
                );
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
            // create mesh
            var rmesh = rnode.Mesh = model.CreateMesh("Triangle Mesh");

            // create a vertex buffer with positions and fill it
            var positionsView = model.UseBufferView(new Byte[12 * 3], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var positionsArray = new Memory.Vector3Array(positionsView.Content);
            positionsArray[0] = new Vector3(0, 10, 0);
            positionsArray[1] = new Vector3(-10, -10, 0);
            positionsArray[2] = new Vector3(10, -10, 0);

            // create an index buffer and fill it
            var indicesView = model.UseBufferView(new Byte[4 * 3], 0, null, 0, BufferMode.ELEMENT_ARRAY_BUFFER);
            var indicesArray = new Memory.IntegerArray(indicesView.Content);
            indicesArray[0] = 0;
            indicesArray[1] = 1;
            indicesArray[2] = 2;

            // create a positions accessor
            var positionsAccessor = model
                .CreateAccessor()
                .WithVertexData(positionsView, 0, 3, ElementType.VEC3, ComponentType.FLOAT, false);

            // create an indices accessor
            var indicesAccessor = model
                .CreateAccessor()
                .WithIndexData(indicesView, 0, 3, IndexType.UNSIGNED_INT);
            
            // create mesh primitive
            var primitive = rmesh.CreatePrimitive();
            primitive.DrawPrimitiveType = PrimitiveType.TRIANGLES;
            primitive.SetVertexAccessor("POSITION", positionsAccessor);
            primitive.IndexAccessor = indicesAccessor;
            primitive.Material = model.CreateMaterial("Default").WithDefault(new Vector4(0, 1, 0, 1));
            primitive.Material.DoubleSided = true;

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

            // define the triangle positions
            var sourcePositions = new[]
            {
                new Vector3(0, 10, 0),
                new Vector3(-10, -10, 0),
                new Vector3(10, -10, 0)
            };

            // define the triangle UV coordinates
            var sourceTextures = new[]
            {
                new Vector2(0.5f, -0.8f),
                new Vector2(-0.5f, 1.2f),
                new Vector2(1.5f, 1.2f)
            };

            // create a vertex buffer
            int byteStride = (3 + 2) * 4;
            var vbuffer = model.UseBufferView(new Byte[byteStride * 3], byteStride, BufferMode.ARRAY_BUFFER);

            // create positions accessor and fill it
            var vpositions = model
                .CreateAccessor("Triangle Positions")
                .WithVertexData(vbuffer, 0, sourcePositions);

            // create texcoord accessor and fill it
            var vtextures = model
                .CreateAccessor("Triangle texture coords")
                .WithVertexData(vbuffer, 12, sourceTextures);

            // create a mesh primitive and assgin the accessors and other properties
            var primitive = rmesh.CreatePrimitive();
            primitive.SetVertexAccessor("POSITION", vpositions);
            primitive.SetVertexAccessor("TEXCOORD_0", vtextures);
            primitive.DrawPrimitiveType = PrimitiveType.TRIANGLES;

            // create and assign a material
            primitive.Material = model
                .CreateMaterial("Default")
                .WithPBRMetallicRoughness();

            primitive.Material.DoubleSided = true;

            // PBRMetallicRoughness has a "BaseColor" and a "Metallic" and a "Roughness" channels.
            primitive.Material
                .FindChannel("BaseColor")
                .SetTexture(0, model.CreateImage().WithSatelliteFile(imagePath) );
            
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
                var material = model.CreateMaterial().WithDefault(color);
                material.DoubleSided = true;
                return material;
            };

            // fill our node with the mesh
            meshBuilder.CopyToNode(rnode, createMaterialForColor);
            
            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }


        struct mySimpleVertex
        {
            public mySimpleVertex(float px, float py, float pz, float nx, float ny, float nz)
            {
                Position = new Vector3(px, py, pz);
                Normal = Vector3.Normalize(new Vector3(nx, ny, nz));
            }

            public Vector3 Position;
            public Vector3 Normal;
        }

        [Test(Description = "Creates an interleaved scene using a mesh builder helper class")]
        public void CreateInterleavedMeshBuilderScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var meshBuilder = new InterleavedMeshBuilder<mySimpleVertex, Vector4>();

            var v1 = new mySimpleVertex(-10, 10, 0, -10, 10, 15);
            var v2 = new mySimpleVertex( 10, 10, 0, 10, 10, 15);
            var v3 = new mySimpleVertex( 10,-10, 0, 10, -10, 15);
            var v4 = new mySimpleVertex(-10,-10, 0, -10, -10, 15);            
            meshBuilder.AddPolygon(new Vector4(1, 1, 1, 1), v1, v2, v3, v4);

            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");
            var rnode = scene.CreateNode("RootNode");

            // setup a lambda function that creates a material for a given color
            Material createMaterialForColor(Vector4 color)
            {
                var material = model.CreateMaterial().WithDefault(color);
                material.DoubleSided = true;
                return material;
            };

            // fill our node with the mesh
            meshBuilder.CopyToNode(rnode, createMaterialForColor);

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates an animated scene using a mesh builder helper class")]
        public void CreateAnimatedMeshBuilderScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var meshBuilder = new InterleavedMeshBuilder<mySimpleVertex, Vector4>();

            var v1 = new mySimpleVertex(-10, 10, 0, -10, 10, 15);
            var v2 = new mySimpleVertex(10, 10, 0, 10, 10, 15);
            var v3 = new mySimpleVertex(10, -10, 0, 10, -10, 15);
            var v4 = new mySimpleVertex(-10, -10, 0, -10, -10, 15);
            meshBuilder.AddPolygon(new Vector4(1, 1, 1, 1), v1, v2, v3, v4);

            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");
            var rnode = scene.CreateNode("RootNode");
            rnode.LocalTransform = new Transforms.AffineTransform(null, null, null, Vector3.Zero);

            // setup a lambda function that creates a material for a given color
            Material createMaterialForColor(Vector4 color)
            {
                var material = model.CreateMaterial().WithDefault(color);
                material.DoubleSided = true;
                return material;
            };

            // fill our node with the mesh
            meshBuilder.CopyToNode(rnode, createMaterialForColor);

            // create animation sequence with 4 frames
            var keyframes = new Dictionary<Single, Vector3>()
            {
                [1] = new Vector3(0, 0, 0),
                [2] = new Vector3(50, 0, 0),
                [3] = new Vector3(0, 50, 0),
                [4] = new Vector3(0, 0, 0),
            };

            var animation = model.CreateAnimation("Animation");
            animation.CreateTranslationChannel(rnode, keyframes);
            

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        struct mySkinnedVertex
        {
            public mySkinnedVertex(float px, float py, float pz, int jointIndex)
            {
                Position = new Vector3(px, py, pz);
                Joints_0 = new Vector4(jointIndex);
                Weights_0 = Vector4.UnitX;
            }

            public mySkinnedVertex(float px, float py, float pz, int jointIndex1, int jointIndex2)
            {
                Position = new Vector3(px, py, pz);
                Joints_0 = new Vector4(jointIndex1, jointIndex2,0,0);
                Weights_0 = new Vector4(0.5f, 0.5f, 0, 0);
            }

            public Vector3 Position;            
            public Vector4 Joints_0;
            public Vector4 Weights_0;
        }

        [Test(Description = "Creates a skinned animated scene using a mesh builder helper class")]
        public void CreateSkinnedAnimatedMeshBuilderScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            // create base model
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");
            var snode = scene.CreateNode("RootNode");

            // create the three joints that will affect the mesh
            var skelet = scene.CreateNode("Skeleton");
            var jnode1 = skelet.CreateNode("Joint 1").WithLocalTranslation(new Vector3(0, 0, 0));
            var jnode2 = jnode1.CreateNode("Joint 2").WithLocalTranslation(new Vector3(0, 40, 0));
            var jnode3 = jnode2.CreateNode("Joint 3").WithLocalTranslation(new Vector3(0, 40, 0));

            // setup skin
            snode.Skin = model.CreateSkin();
            snode.Skin.Skeleton = skelet;
            snode.Skin.BindJoints(jnode1, jnode2, jnode3);

            // create the mesh
            var meshBuilder = new InterleavedMeshBuilder<mySkinnedVertex, Vector4>();

            var v1 = new mySkinnedVertex(-10, 0, +10, 0);
            var v2 = new mySkinnedVertex(+10, 0, +10, 0);
            var v3 = new mySkinnedVertex(+10, 0, -10, 0);
            var v4 = new mySkinnedVertex(-10, 0, -10, 0);

            var v5 = new mySkinnedVertex(-10, 40, +10, 0, 1);
            var v6 = new mySkinnedVertex(+10, 40, +10, 0, 1);
            var v7 = new mySkinnedVertex(+10, 40, -10, 0, 1);
            var v8 = new mySkinnedVertex(-10, 40, -10, 0, 1);

            var v9  = new mySkinnedVertex(-5, 80, +5, 2);
            var v10 = new mySkinnedVertex(+5, 80, +5, 2);
            var v11 = new mySkinnedVertex(+5, 80, -5, 2);
            var v12 = new mySkinnedVertex(-5, 80, -5, 2);

            meshBuilder.AddPolygon(new Vector4(1, 0, 1, 1), v1, v2, v6, v5);
            meshBuilder.AddPolygon(new Vector4(1, 0, 1, 1), v2, v3, v7, v6);
            meshBuilder.AddPolygon(new Vector4(1, 0, 1, 1), v3, v4, v8, v7);
            meshBuilder.AddPolygon(new Vector4(1, 0, 1, 1), v4, v1, v5, v8);

            meshBuilder.AddPolygon(new Vector4(1, 1, 0, 1), v5, v6, v10, v9);
            meshBuilder.AddPolygon(new Vector4(1, 1, 0, 1), v6, v7, v11, v10);
            meshBuilder.AddPolygon(new Vector4(1, 1, 0, 1), v7, v8, v12, v11);
            meshBuilder.AddPolygon(new Vector4(1, 1, 0, 1), v8, v5, v9, v12);

            // setup a lambda function that creates a material for a given color
            Material createMaterialForColor(Vector4 color)
            {
                var material = model.CreateMaterial().WithDefault(color);
                material.DoubleSided = true;
                return material;
            };

            // fill our node with the mesh
            meshBuilder.CopyToNode(snode, createMaterialForColor);

            // create animation sequence with 4 frames
            var keyframes = new Dictionary<Single, Quaternion>
            {
                [1] = Quaternion.Identity,
                [2] = Quaternion.CreateFromYawPitchRoll(0, 1, 0),
                [3] = Quaternion.CreateFromYawPitchRoll(0, 0, 1),
                [4] = Quaternion.Identity,
            };

            model.CreateAnimation("Animation")
                .CreateRotationChannel(jnode2, keyframes);

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }
    }
}
