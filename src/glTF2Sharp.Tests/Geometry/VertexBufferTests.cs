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

            var vb = new VertexArray(5, "POSITION", "NORMAL");

            vb.SetVector3Column("POSITION", 0, positions);
            vb.SetVector3Column("NORMAL", 0, normals);

            var p2 = vb.GetVector3Column("POSITION");
            var n2 = vb.GetVector3Column("NORMAL");
            CollectionAssert.AreEqual(positions, p2);
            CollectionAssert.AreEqual(normals, n2);

            var vbs = new VertexArraySegment(vb, 1, 2);

            p2 = vbs.GetVector3Column("POSITION");
            n2 = vbs.GetVector3Column("NORMAL");
            CollectionAssert.AreEqual(positions.Skip(1).Take(2), p2);
            CollectionAssert.AreEqual(normals.Skip(1).Take(2), n2);            

            var mdl = new Schema2.ModelRoot();
            var mdlbv = mdl.CreateVertexBufferView(vb.Data, vb.ByteStride);

            var mdlmesh = mdl.CreateMesh();
            var mdlprim = mdlmesh.CreatePrimitive();
            mdlprim.SetVertexAccessors(mdlbv, vbs.ByteOffset, vbs.Count, vbs.Attributes);
            Memory.EncodedArrayUtils.CopyTo(vbs.GetVector3Column("POSITION"), mdlprim.VertexAccessors["POSITION"].AsVector3Array());
            Memory.EncodedArrayUtils.CopyTo(vbs.GetVector3Column("NORMAL"), mdlprim.VertexAccessors["NORMAL"].AsVector3Array());

            mdl.AttachToCurrentTest("result.glb");
            mdl.AttachToCurrentTest("result.gltf");
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

            var dstBuffer = new VertexArray(srcBuffer.Count, attributes);

            foreach (var attr in attributes)
            {
                var srcDim = srcBuffer.Declaration.GetDimensions(attr);

                if (srcDim == 1)
                {
                    var srcColumn = srcBuffer.GetScalarColumn(attr);
                    dstBuffer.SetScalarColumn(attr, 0, srcColumn);
                }

                if (srcDim == 2)
                {
                    var srcColumn = srcBuffer.GetVector2Column(attr);
                    dstBuffer.SetVector2Column(attr, 0, srcColumn);
                }

                if (srcDim == 3)
                {
                    var srcColumn = srcBuffer.GetVector3Column(attr);
                    dstBuffer.SetVector3Column(attr, 0, srcColumn);
                }

                if (srcDim == 4)
                {
                    var srcColumn = srcBuffer.GetVector4Column(attr);
                    dstBuffer.SetVector4Column(attr, 0, srcColumn);
                }
            }

            return dstBuffer;
        }
    }
}
