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
    public class ExtensionsTests
    {
        internal static int _WordPadded(int length)
        {
            var padding = (length & 3);

            return length + (padding == 0 ? 0 : 4 - padding);
        }

        [Test]
        public void TestPadding()
        {
            Assert.AreEqual(4, _WordPadded(1));
            Assert.AreEqual(4, _WordPadded(2));
            Assert.AreEqual(4, _WordPadded(3));
            Assert.AreEqual(4, _WordPadded(4));
            Assert.AreEqual(8, _WordPadded(5));
        }

        [Test]
        public void TestAsNullableExtensions()
        {
            // the AsNullable extensions are a bit tricky;
            // they should default to null regardless of the value being inside or outside the bounds of min-max
            // but if after the min.max has affected the value, the default-to-null check still applies.

            Assert.AreEqual(null, 5.AsNullable(5));

            Assert.AreEqual(1,    0.AsNullable(3, 1, 5));
            Assert.AreEqual(1,    1.AsNullable(3, 1, 5));
            Assert.AreEqual(2,    2.AsNullable(3, 1, 5));
            Assert.AreEqual(null, 3.AsNullable(3, 1, 5));
            Assert.AreEqual(4,    4.AsNullable(3, 1, 5));
            Assert.AreEqual(5,    5.AsNullable(3, 1, 5));
            Assert.AreEqual(5,    6.AsNullable(3, 1, 5));            

            // vectors

            Assert.AreEqual(null, Vector2.Zero.AsNullable(Vector2.One, Vector2.One, Vector2.One * 2));
            Assert.AreEqual(new Vector2(2), new Vector2(3).AsNullable(Vector2.One, Vector2.One, Vector2.One * 2));

            Assert.AreEqual(null, Vector3.Zero.AsNullable(Vector3.One, Vector3.One, Vector3.One * 2));
            Assert.AreEqual(new Vector3(2), new Vector3(3).AsNullable(Vector3.One, Vector3.One, Vector3.One * 2));

            Assert.AreEqual(null, Vector4.Zero.AsNullable(Vector4.One, Vector4.One, Vector4.One * 2));
            Assert.AreEqual(new Vector4(2), new Vector4(3).AsNullable(Vector4.One, Vector4.One, Vector4.One * 2));


            // special case: default values outside the min-max range should also return null
            Assert.AreEqual(null,  0.AsNullable( 0, 1, 5));
            Assert.AreEqual(null, 10.AsNullable(10, 1, 5));

            Assert.AreEqual(null, Vector2.Zero.AsNullable(Vector2.Zero, Vector2.One, Vector2.One * 5));
            Assert.AreEqual(null, Vector3.Zero.AsNullable(Vector3.Zero, Vector3.One, Vector3.One * 5));
            Assert.AreEqual(null, Vector4.Zero.AsNullable(Vector4.Zero, Vector4.One, Vector4.One * 5));
        }        
    }
}
