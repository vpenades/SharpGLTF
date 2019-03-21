using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Geometry
{
    using Schema2;

    [TestFixture]
    public class CreateMeshTests
    {
        [Test]
        public void CreateTriangleScene()
        {
            // define the data of a triangle:

            var positions = new[]
            {
                new Vector3(10,-10,0),
                new Vector3(0,10,0),
                new Vector3(-10,-10,0)
            };

            var normals = new[]
            {
                Vector3.UnitZ,
                Vector3.UnitZ,
                Vector3.UnitZ
            };

            var indices = new UInt32[] { 0, 1, 2 };

            // create a new mesh:
            var srcMesh = new Mesh<int?>();

            // setup a mesh primitive
            var srcPrimitive = srcMesh.CreatePrimitive();
            srcPrimitive.AllocateVertices(positions.Length, "POSITION", "NORMAL");  // (#1)
            srcPrimitive.AllocateIndices(indices.Length, PrimitiveType.TRIANGLES);  // (#2)

            // assign vertices and indices
            srcPrimitive.Vertices[0].SetValues(0, positions);
            srcPrimitive.Vertices[1].SetValues(0, normals);
            srcPrimitive.Indices.SetValues(0, indices);
            srcPrimitive.Material = 0; // using material with index 0, which will be created later

            // check the values we've set match the input data
            CollectionAssert.AreEqual(positions, srcPrimitive.Vertices[0].AsVector3Array());
            CollectionAssert.AreEqual(normals, srcPrimitive.Vertices[1].AsVector3Array());
            CollectionAssert.AreEqual(indices, srcPrimitive.Indices.AsIntegerArray());

            // Notice that until now, we've been working with objects in the .Geometry namespace.

            // Now we switch to the .Schema2 namespace and we create a new scene:

            var model = ModelRoot.CreateModel();                        
            var scene = model.UseScene("default");
            var rnode = scene.CreateNode("main scene");

            var material = model.CreateMaterial("DefaultMaterial")
                .WithDefault(new Vector4(1, 0, 0, 1))
                .WithDoubleSide(true);

            rnode.Mesh = model.CreateMesh();

            // this assigns the mesh we've created before to this schema mesh.
            // Notice that the schema buffers being created will be using the
            // memory allocated by (#1) and (#2)
            srcMesh.CopyTo(rnode.Mesh);
                        
            model.AttachToCurrentTest("Triangle.gltf");
            model.AttachToCurrentTest("Triangle.glb");

            TestContext.CurrentContext.AttachShowDirLink();
        }        
    }
}
