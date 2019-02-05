using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp.Schema2
{
    [TestFixture]
    public class CreateModelTests
    {
        [Test]
        public void CreateEmptyScene()
        {
            var root = ModelRoot.CreateNew();

            var scene = root.UseScene("Empty Scene");

            Assert.NotNull(scene);
            Assert.AreEqual("Empty Scene", scene.Name);
            Assert.AreEqual("Empty Scene", root.DefaultScene.Name);            
        }

        [Test]
        public void CreateTriangleScene()
        {
            var root = ModelRoot.CreateNew();            

            // create a mesh with a triangle

            var mesh = root.CreateMesh();

            var primitive = mesh.CreatePrimitive();
            primitive.DrawPrimitiveType = PrimitiveType.TRIANGLES;            

            var positions = root.CreateVector3Buffer
                (
                new System.Numerics.Vector3(0, 10, 0),
                new System.Numerics.Vector3(-10, -10, 0),
                new System.Numerics.Vector3(10, -10, 0)
                );

            var indices = root.CreateIndexBuffer(0, 1, 2);

            var positionsView = root.CreateBufferView(positions,null,null,null, BufferMode.ARRAY_BUFFER);
            var indicesView =   root.CreateBufferView(indices, null, null, null, BufferMode.ELEMENT_ARRAY_BUFFER);

            var positionsAccessor = root.CreateAccessor();
            positionsAccessor.SetVertexData(positionsView, ComponentType.FLOAT, ElementType.VEC3, false, 0, 3);

            var indicesAccessor = root.CreateAccessor();
            indicesAccessor.SetIndexData(indicesView, IndexType.UNSIGNED_INT, 0, 3);

            primitive.SetVertexAccessor("POSITION", positionsAccessor);
            primitive.IndexAccessor = indicesAccessor;

            // create a scene

            var scene = root.UseScene("Empty Scene");

            var triangleNode = scene.AddNode("Triangle");

            triangleNode.Mesh = mesh;

            // save

            root.AttachToCurrentTest("result.glb");
            root.AttachToCurrentTest("result.gltf");
        }
    }
}
