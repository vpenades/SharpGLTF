using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Memory
{
    [TestFixture]
    [Category("Core Memory")]
    public class MemoryAccessorTests
    {
        [Test]
        public void CreateInterleaved1()
        {
            var pos = MemoryAccessInfo.CreateDefaultElement("POSITION");
            var nrm = MemoryAccessInfo.CreateDefaultElement("NORMAL");

            var attributes = new[] { pos, nrm };

            const int baseOffset = 8;

            var byteStride = MemoryAccessInfo.SetInterleavedInfo(attributes, baseOffset, 5);

            pos = attributes[0];
            nrm = attributes[1];

            Assert.AreEqual(24, byteStride);

            Assert.AreEqual(baseOffset + 0, pos.ByteOffset);
            Assert.AreEqual(24, pos.ByteStride);
            Assert.AreEqual(5, pos.ItemsCount);

            Assert.AreEqual(baseOffset + 12, nrm.ByteOffset);
            Assert.AreEqual(24, nrm.ByteStride);
            Assert.AreEqual(5, nrm.ItemsCount);

            Assert.IsTrue(pos.IsValidVertexAttribute);
            Assert.IsTrue(nrm.IsValidVertexAttribute);
        }
    }
}
