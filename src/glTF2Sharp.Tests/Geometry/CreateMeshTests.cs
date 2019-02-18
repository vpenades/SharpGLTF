using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp.Geometry
{
    using Memory;

    [TestFixture]
    public class CreateMeshTests
    {
        [Test]
        public void CreateMesh1Test()
        {
            var positions = new[]
            {
                new Vector3(0,1,2),
                new Vector3(4,1,3),
                new Vector3(2,3,5),
                new Vector3(2,4,9),
                new Vector3(1,3,5),
            };

            var normals = new[]
            {
                Vector3.UnitX,
                Vector3.UnitX,
                Vector3.UnitX,
                Vector3.UnitY,
                Vector3.UnitZ,
            };

            var indices = new UInt32[] { 0, 1, 2, 0, 2, 3 };

            var primitive = new MeshPrimitive();
            primitive.SetVertices(5, "POSITION", "NORMAL");
            primitive.SetIndices(6);

            primitive.Vertices[0].SetValues(0,positions);
            primitive.Vertices[1].SetValues(0, normals);
            primitive.Indices.SetValues(0, indices);            

            CollectionAssert.AreEqual(positions, primitive.Vertices[0].AsVector3Array());
            CollectionAssert.AreEqual(normals, primitive.Vertices[1].AsVector3Array());
            CollectionAssert.AreEqual(indices, primitive.Indices.AsIntegerArray());

        }
    }
}
