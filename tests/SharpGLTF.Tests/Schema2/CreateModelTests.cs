using System;
using System.Collections.Generic;
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
            var positionsView = root.CreateBufferView(root.CreateBuffer(12 * 3), null, null, null, BufferMode.ARRAY_BUFFER);
            var positionsArray = new Memory.Vector3Array(positionsView.Content);
            positionsArray[0] = new System.Numerics.Vector3(0, 10, 0);
            positionsArray[1] = new System.Numerics.Vector3(-10, -10, 0);
            positionsArray[2] = new System.Numerics.Vector3(10, -10, 0);

            // create an index buffer and fill it
            var indicesView = root.CreateBufferView(root.CreateBuffer(4 * 3), null, null, null, BufferMode.ELEMENT_ARRAY_BUFFER);
            var indicesArray = new Memory.IntegerArray(indicesView.Content);
            indicesArray[0] = 0;
            indicesArray[1] = 1;
            indicesArray[2] = 2;

            // create a positions accessor
            var positionsAccessor = root.CreateAccessor();
            positionsAccessor.SetVertexData(positionsView, 0, ElementType.VEC3, ComponentType.FLOAT, false, 3);

            // create an indices accessor
            var indicesAccessor = root.CreateAccessor();
            indicesAccessor.SetIndexData(indicesView, 0, IndexType.UNSIGNED_INT, 3);

            // create a mesh and a mesh primitive
            var mesh = root.CreateMesh();
            var primitive = mesh.CreatePrimitive();
            primitive.DrawPrimitiveType = PrimitiveType.TRIANGLES;
            primitive.SetVertexAccessor("POSITION", positionsAccessor);
            primitive.IndexAccessor = indicesAccessor;

            // create a scene
            var scene = root.UseScene("Empty Scene");

            // create a node
            var node = scene.AddVisualNode("Triangle");

            // assign the mesh we previously created
            node.Mesh = mesh;

            // save
            root.AttachToCurrentTest("result.glb");
            root.AttachToCurrentTest("result.gltf");            
        }
    }
}
