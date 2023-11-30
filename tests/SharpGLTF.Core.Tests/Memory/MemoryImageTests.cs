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

            Assert.That(image2.GetHashCode(), Is.EqualTo(image1.GetHashCode()));
            Assert.That(image2, Is.EqualTo(image1));
            Assert.That(MemoryImage.AreEqual(image1, image2), Is.True);

            Assert.That(image3.GetHashCode(), Is.Not.EqualTo(image1.GetHashCode()));
            Assert.That(image3, Is.Not.EqualTo(image1));
            Assert.That(MemoryImage.AreEqual(image1, image3), Is.False);
        }
    }
}
