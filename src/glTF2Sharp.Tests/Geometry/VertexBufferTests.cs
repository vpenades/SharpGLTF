using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp.Geometry
{
    [TestFixture]
    public class VertexBufferTests
    {
        [Test]
        public void CreateVertexBuffer()
        {
            var positions = new[]
            {
                new Vector3(0,1,2),
                new Vector3(4,1,3),
                new Vector3(2,3,5),
            };

            var normals = new[]
            {
                Vector3.UnitX,
                Vector3.UnitX,
                Vector3.UnitX,
            };

            var vb = new VertexBuffer(3, "POSITION", "NORMAL");

            vb.SetVector3Column("POSITION", positions);
            vb.SetVector3Column("NORMAL", normals);

            var p2 = vb.GetVector3Column("POSITION");
            var n2 = vb.GetVector3Column("NORMAL");

            CollectionAssert.AreEqual(positions, p2);
            CollectionAssert.AreEqual(normals, n2);
        }

        [Test]
        public void CreateVertexBufferTest()
        {
            var vbdecl = new MeshBuffers.VertexDeclaration()
                .WithVector3("POSITION")
                .WithVector3("NORMAL");

            Assert.AreEqual(6, vbdecl.Stride);

            var vertex1 = vbdecl.CreateVertex();
            var vertex2 = vbdecl.CreateVertex();
            var vertex3 = vbdecl.CreateVertex();

            var vbuffer = new MeshBuffers.TriangleBufferBuilder(vbdecl);

            vertex1.Position = new Vector3(1, 2, 3);
            vertex1.Normal = Vector3.UnitX;
            vertex2.Position = new Vector3(4, 2, 3);
            vertex2.Normal = Vector3.UnitY;
            vertex3.Position = new Vector3(1, 5, 3);
            vertex3.Normal = Vector3.UnitZ;

            vbuffer.AddTriangle(vertex1, vertex2, vertex3);
            vbuffer.AddTriangle(vertex1, vertex2, vertex3);

            var data = EncodeVertexBuffer(vbuffer.Vertices);
        }

        static VertexBuffer EncodeVertexBuffer(MeshBuffers.VertexBuffer srcBuffer)
        {
            var attributes = srcBuffer.Declaration.Attributes.ToArray();

            var dstBuffer = new glTF2Sharp.Geometry.VertexBuffer(srcBuffer.Count, attributes);

            foreach (var attr in attributes)
            {
                var srcDim = srcBuffer.Declaration.GetDimensions(attr);

                if (srcDim == 1)
                {
                    var srcColumn = srcBuffer.GetScalarColumn(attr);
                    dstBuffer.SetScalarColumn(attr, srcColumn);
                }

                if (srcDim == 2)
                {
                    var srcColumn = srcBuffer.GetVector2Column(attr);
                    dstBuffer.SetVector2Column(attr, srcColumn);
                }

                if (srcDim == 3)
                {
                    var srcColumn = srcBuffer.GetVector3Column(attr);
                    dstBuffer.SetVector3Column(attr, srcColumn);
                }

                if (srcDim == 4)
                {
                    var srcColumn = srcBuffer.GetVector4Column(attr);
                    dstBuffer.SetVector4Column(attr, srcColumn);
                }
            }

            return dstBuffer;
        }
    }
}
