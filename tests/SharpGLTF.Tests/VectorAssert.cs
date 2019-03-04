using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    public static class VectorAssert
    {
        public static void AreEqual(Vector4 a, Vector4 b, double delta = 0)
        {
            Assert.AreEqual(a.X, b.X, delta);
            Assert.AreEqual(a.Y, b.Y, delta);
            Assert.AreEqual(a.Z, b.Z, delta);
            Assert.AreEqual(a.W, b.W, delta);
        }
    }
}
