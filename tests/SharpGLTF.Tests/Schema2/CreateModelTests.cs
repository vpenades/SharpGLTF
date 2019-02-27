using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2
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
        public void CreateTriangleScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            // Although this is a valid way of creating a gltf mesh, it will be extremely GPU inefficient.            

            var root = ModelRoot.CreateModel();
            
            // create a vertex buffer with positions and fill it
            var positionsView = root.UseBufferView(new Byte[12 * 3], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var positionsArray = new Memory.Vector3Array(positionsView.Content);
            positionsArray[0] = new System.Numerics.Vector3(0, 10, 0);
            positionsArray[1] = new System.Numerics.Vector3(-10, -10, 0);
            positionsArray[2] = new System.Numerics.Vector3(10, -10, 0);

            // create an index buffer and fill it
            var indicesView = root.UseBufferView(new Byte[4 * 3], 0, null, 0, BufferMode.ELEMENT_ARRAY_BUFFER);
            var indicesArray = new Memory.IntegerArray(indicesView.Content);
            indicesArray[0] = 0;
            indicesArray[1] = 1;
            indicesArray[2] = 2;

            // create a positions accessor
            var positionsAccessor = root
                .CreateAccessor()
                .WithVertexData(positionsView, 0, 3, ElementType.VEC3, ComponentType.FLOAT, false);

            // create an indices accessor
            var indicesAccessor = root
                .CreateAccessor()
                .WithIndexData(indicesView, 0, 3, IndexType.UNSIGNED_INT);

            // create a mesh and a mesh primitive
            var mesh = root.CreateMesh();
            var primitive = mesh.CreatePrimitive();
            primitive.DrawPrimitiveType = PrimitiveType.TRIANGLES;
            primitive.SetVertexAccessor("POSITION", positionsAccessor);
            primitive.IndexAccessor = indicesAccessor;

            // create a scene
            var scene = root.DefaultScene = root.UseScene("Empty Scene");

            // create a node
            var node = scene.CreateNode("Triangle");

            // assign the mesh we previously created
            node.Mesh = mesh;

            // save
            root.AttachToCurrentTest("result.glb");
            root.AttachToCurrentTest("result.gltf");            
        }

        [Test(Description = "Creates a simple scene using a helper class")]
        public void CreateManyTrianglesScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var builder = new SimpleSceneBuilder();

            builder.AddPolygon(new Vector4(1, 1, 1, 1), (-10, 10,  0), (10, 10,  0), (10, -10,  0), (-10, -10,  0));
            builder.AddPolygon(new Vector4(1, 1, 0, 1), (-10, 10, 10), (10, 10, 10), (10, -10, 10), (-10, -10, 10));
            builder.AddPolygon(new Vector4(1, 0, 0, 1), (-10, 10, 20), (10, 10, 20), (10, -10, 20), (-10, -10, 20));

            var model = builder.ToModel();

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }
    }
}
