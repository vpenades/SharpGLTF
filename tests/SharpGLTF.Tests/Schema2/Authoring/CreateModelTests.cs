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
                .SetTexture(0, model.CreateImage().WithExternalFile(imagePath) );
            
            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");            
        }

        [Test(Description = "Creates a simple scene using a mesh builder helper class")]
        public void CreateMeshBuilderScene()
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
    }
}
