using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    public static class VectorUtils
    {
        public static Single NextSingle(this Random rnd)
        {
            return (Single)rnd.NextDouble();
        }

        public static Vector2 NextVector2(this Random rnd)
        {
            return new Vector2(rnd.NextSingle(), rnd.NextSingle());
        }

        public static Vector3 NextVector3(this Random rnd)
        {
            return new Vector3(rnd.NextSingle(), rnd.NextSingle(), rnd.NextSingle());
        }

        public static Vector4 NextVector4(this Random rnd)
        {
            return new Vector4(rnd.NextSingle(), rnd.NextSingle(), rnd.NextSingle(), rnd.NextSingle());
        }

        public static void AreEqual(Vector4 a, Vector4 b, double delta = 0)
        {
            Assert.AreEqual(a.X, b.X, delta);
            Assert.AreEqual(a.Y, b.Y, delta);
            Assert.AreEqual(a.Z, b.Z, delta);
            Assert.AreEqual(a.W, b.W, delta);
        }
    }
}
