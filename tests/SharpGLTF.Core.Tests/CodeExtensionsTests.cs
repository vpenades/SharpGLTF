using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

using NUnit.Framework;


namespace SharpGLTF
{
    using System.Numerics;

    using Schema2;

    [TestFixture]
    [Category("Core")]
    public class CodeExtensionsTests
    {
        internal static int _WordPadded(int length)
        {
            var padding = (length & 3);

            return length + (padding == 0 ? 0 : 4 - padding);
        }

        [Test]
        public void TestPadding()
        {
            Assert.That(_WordPadded(1), Is.EqualTo(4));
            Assert.That(_WordPadded(2), Is.EqualTo(4));
            Assert.That(_WordPadded(3), Is.EqualTo(4));
            Assert.That(_WordPadded(4), Is.EqualTo(4));
            Assert.That(_WordPadded(5), Is.EqualTo(8));
        }

        [Test]
        public void TestAsNullableExtensions()
        {
            // the AsNullable extensions are a bit tricky;
            // they should default to null regardless of the value being inside or outside the bounds of min-max
            // but if after the min.max has affected the value, the default-to-null check still applies.

            Assert.That(5.AsNullable(5), Is.EqualTo(null));

            Assert.That(0.AsNullable(3, 1, 5), Is.EqualTo(1));
            Assert.That(1.AsNullable(3, 1, 5), Is.EqualTo(1));
            Assert.That(2.AsNullable(3, 1, 5), Is.EqualTo(2));
            Assert.That(3.AsNullable(3, 1, 5), Is.EqualTo(null));
            Assert.That(4.AsNullable(3, 1, 5), Is.EqualTo(4));
            Assert.That(5.AsNullable(3, 1, 5), Is.EqualTo(5));
            Assert.That(6.AsNullable(3, 1, 5), Is.EqualTo(5));            

            // vectors

            Assert.That(Vector2.Zero.AsNullable(Vector2.One, Vector2.One, Vector2.One * 2), Is.EqualTo(null));
            Assert.That(new Vector2(3).AsNullable(Vector2.One, Vector2.One, Vector2.One * 2), Is.EqualTo(new Vector2(2)));

            Assert.That(Vector3.Zero.AsNullable(Vector3.One, Vector3.One, Vector3.One * 2), Is.EqualTo(null));
            Assert.That(new Vector3(3).AsNullable(Vector3.One, Vector3.One, Vector3.One * 2), Is.EqualTo(new Vector3(2)));

            Assert.That(Vector4.Zero.AsNullable(Vector4.One, Vector4.One, Vector4.One * 2), Is.EqualTo(null));
            Assert.That(new Vector4(3).AsNullable(Vector4.One, Vector4.One, Vector4.One * 2), Is.EqualTo(new Vector4(2)));


            // special case: default values outside the min-max range should also return null
            Assert.That(0.AsNullable( 0, 1, 5), Is.EqualTo(null));
            Assert.That(10.AsNullable(10, 1, 5), Is.EqualTo(null));

            Assert.That(Vector2.Zero.AsNullable(Vector2.Zero, Vector2.One, Vector2.One * 5), Is.EqualTo(null));
            Assert.That(Vector3.Zero.AsNullable(Vector3.Zero, Vector3.One, Vector3.One * 5), Is.EqualTo(null));
            Assert.That(Vector4.Zero.AsNullable(Vector4.Zero, Vector4.One, Vector4.One * 5), Is.EqualTo(null));
        }        
    }
}
