using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Memory
{
    [Category("Core Memory")]
    public class MemoryImageTests
    {
        [Test]
        public void TestImageEquality()
        {
            // two images that are equal byte by byte, loaded from different sources
            // must be considered equal.

            var image1 = new MemoryImage(MemoryImage.DefaultPngImage, "first_reference.png");
            var image2 = new MemoryImage(MemoryImage.DefaultPngImage, "second_reference.png");
            var image3 = MemoryImage.Empty;

            Assert.AreEqual(image1.GetHashCode(), image2.GetHashCode());
            Assert.AreEqual(image1, image2);
            Assert.IsTrue(MemoryImage.AreEqual(image1, image2));

            Assert.AreNotEqual(image1.GetHashCode(), image3.GetHashCode());
            Assert.AreNotEqual(image1, image3);
            Assert.IsFalse(MemoryImage.AreEqual(image1, image3));
        }
    }
}
