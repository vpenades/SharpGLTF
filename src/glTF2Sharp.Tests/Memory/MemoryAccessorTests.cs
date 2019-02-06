using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp.Memory
{
    [TestFixture]
    public class MemoryAccessorTests
    {
        [Test]
        public void TestFloatingAccesor()
        {
            Assert.AreEqual(17, FloatingAccessor.Create(new Byte[] { 17 }, Schema2.ComponentType.UNSIGNED_BYTE, false)[0]);
            Assert.AreEqual(17, FloatingAccessor.Create(new Byte[] { 17, 0 }, Schema2.ComponentType.UNSIGNED_SHORT, false)[0]);            

            Assert.AreEqual(17, FloatingAccessor.Create(new Byte[] { 17 }, Schema2.ComponentType.BYTE, false)[0]);
            Assert.AreEqual(17, FloatingAccessor.Create(new Byte[] { 17, 0 }, Schema2.ComponentType.SHORT, false)[0]);

            Assert.AreEqual(1, FloatingAccessor.Create(new Byte[] { 255 }, Schema2.ComponentType.UNSIGNED_BYTE, true)[0]);
            Assert.AreEqual(1, FloatingAccessor.Create(new Byte[] { 127 }, Schema2.ComponentType.BYTE, true)[0]);
            Assert.AreEqual(-1, FloatingAccessor.Create(new Byte[] { 128 }, Schema2.ComponentType.BYTE, true)[0]);

            Assert.AreEqual(1, FloatingAccessor.Create(new Byte[] { 255, 255 }, Schema2.ComponentType.UNSIGNED_SHORT, true)[0]);
            Assert.AreEqual(1, FloatingAccessor.Create(new Byte[] { 255, 127 }, Schema2.ComponentType.SHORT, true)[0]);
            Assert.AreEqual(-1, FloatingAccessor.Create(new Byte[] { 0,  128 }, Schema2.ComponentType.SHORT, true)[0]);

            Assert.AreEqual(17, FloatingAccessor.Create(new Byte[] { 17, 0, 0, 0 }, Schema2.ComponentType.UNSIGNED_INT, false)[0]);
            Assert.AreEqual(1, FloatingAccessor.Create(new Byte[] { 0,0, 0x80, 0x3f }, Schema2.ComponentType.FLOAT, false)[0]);
        }

        [Test]
        public void TestLinq()
        {
            var buffer = new Byte[] { 1, 52, 43, 6, 23, 234 };

            var accessor = new Vector2Accessor(buffer, 0, Schema2.ComponentType.BYTE, true);

            var result = accessor.ToArray();

            Assert.AreEqual(3, result.Length);
        }
    }
}
