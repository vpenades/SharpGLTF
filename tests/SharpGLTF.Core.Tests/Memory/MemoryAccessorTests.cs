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

            Assert.That(byteStride, Is.EqualTo(24));

            Assert.That(pos.ByteOffset, Is.EqualTo(baseOffset + 0));
            Assert.That(pos.ByteStride, Is.EqualTo(24));
            Assert.That(pos.ItemsCount, Is.EqualTo(5));

            Assert.That(nrm.ByteOffset, Is.EqualTo(baseOffset + 12));
            Assert.That(nrm.ByteStride, Is.EqualTo(24));
            Assert.That(nrm.ItemsCount, Is.EqualTo(5));

            Assert.That(pos.IsValidVertexAttribute, Is.True);
            Assert.That(nrm.IsValidVertexAttribute, Is.True);
        }
    }
}
