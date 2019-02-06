using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp.Schema2
{
    [TestFixture]
    public class CreateModelTests
    {
        [Test(Description = "Creates an empty model")]
        public void CreateEmptyScene()
        {
            var root = ModelRoot.CreateNew();

            var scene = root.UseScene("Empty Scene");

            Assert.NotNull(scene);            
            Assert.AreEqual("Empty Scene", root.DefaultScene.Name);            
        }

        [Test(Description ="Creates a model with a triangle mesh")]
        public void CreateTriangleScene()
        {
            // Notice!! Although this is a valid way of creating a gltf mesh, it will be extremely GPU inefficient.

            var root = ModelRoot.CreateNew();            

            var mesh = root.CreateMesh();

            var primitive = mesh.CreatePrimitive();
            primitive.DrawPrimitiveType = PrimitiveType.TRIANGLES;            

            var positions = root.CreateVector3Buffer
                (
                new System.Numerics.Vector3(0, 10, 0),
                new System.Numerics.Vector3(-10, -10, 0),
                new System.Numerics.Vector3(10, -10, 0)
                );

            var indices = root.CreateIndexBuffer( 0, 1, 2 );

            var positionsView = root.CreateBufferView(positions,null,null,null, BufferMode.ARRAY_BUFFER);
            var indicesView =   root.CreateBufferView(indices, null, null, null, BufferMode.ELEMENT_ARRAY_BUFFER);

            var positionsAccessor = root.CreateAccessor();
            positionsAccessor.SetVertexData(positionsView, 0, ElementType.VEC3, ComponentType.FLOAT, false, 3);

            var indicesAccessor = root.CreateAccessor();
            indicesAccessor.SetIndexData(indicesView, 0, IndexType.UNSIGNED_INT, 3);

            primitive.SetVertexAccessor("POSITION", positionsAccessor);
            primitive.IndexAccessor = indicesAccessor;

            // create a scene

            var scene = root.UseScene("Empty Scene");

            var node = scene.AddNode("Triangle");

            // assign the mesh we previously created

            node.Mesh = mesh;

            // save

            root.AttachToCurrentTest("result.glb");
            root.AttachToCurrentTest("result.gltf");
        }
    }
}
