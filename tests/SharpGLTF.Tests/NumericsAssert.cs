using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    static class NumericsAssert
    {
        public static void IsFinite(Vector2 vector)
        {
            Assert.IsTrue(float.IsFinite(vector.X), "X");
            Assert.IsTrue(float.IsFinite(vector.Y), "Y");
        }

        public static void IsFinite(Vector3 vector)
        {
            Assert.IsTrue(float.IsFinite(vector.X), "X");
            Assert.IsTrue(float.IsFinite(vector.Y), "Y");
            Assert.IsTrue(float.IsFinite(vector.Z), "Z");
        }

        public static void IsFinite(Vector4 vector)
        {
            Assert.IsTrue(float.IsFinite(vector.X), "X");
            Assert.IsTrue(float.IsFinite(vector.Y), "Y");
            Assert.IsTrue(float.IsFinite(vector.Z), "Z");
            Assert.IsTrue(float.IsFinite(vector.W), "W");
        }

        public static void IsFinite(Quaternion quaternion)
        {
            Assert.IsTrue(float.IsFinite(quaternion.X), "X");
            Assert.IsTrue(float.IsFinite(quaternion.Y), "Y");
            Assert.IsTrue(float.IsFinite(quaternion.Z), "Z");
            Assert.IsTrue(float.IsFinite(quaternion.W), "W");
        }

        public static void AreEqual(Vector2 expected, Vector2 actual, double delta = 0)
        {
            Assert.AreEqual(expected.X, actual.X, delta, "X");
            Assert.AreEqual(expected.Y, actual.Y, delta, "Y");
        }

        public static void AreEqual(Vector3 expected, Vector3 actual, double delta = 0)
        {
            Assert.AreEqual(expected.X, actual.X, delta, "X");
            Assert.AreEqual(expected.Y, actual.Y, delta, "Y");
            Assert.AreEqual(expected.Z, actual.Z, delta, "Z");
        }

        public static void AreEqual(Vector4 expected, Vector4 actual, double delta = 0)
        {
            Assert.AreEqual(expected.X, actual.X, delta, "X");
            Assert.AreEqual(expected.Y, actual.Y, delta, "Y");
            Assert.AreEqual(expected.Z, actual.Z, delta, "Z");
            Assert.AreEqual(expected.W, actual.W, delta, "W");
        }

        public static void AreEqual(Quaternion expected, Quaternion actual, double delta = 0)
        {
            Assert.AreEqual(expected.X, actual.X, delta, "X");
            Assert.AreEqual(expected.Y, actual.Y, delta, "Y");
            Assert.AreEqual(expected.Z, actual.Z, delta, "Z");
            Assert.AreEqual(expected.W, actual.W, delta, "W");
        }

        public static void LengthIsOne(Vector2 vector, double delta = 0)
        {
            var lenSquared = vector.X * vector.X + vector.Y * vector.Y;

            Assert.AreEqual(1, lenSquared, delta * delta, "Length");
        }

        public static void LengthIsOne(Vector3 vector, double delta = 0)
        {
            var lenSquared = vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;

            Assert.AreEqual(1, lenSquared, delta * delta, "Length");
        }

        public static void LengthIsOne(Vector4 vector, double delta = 0)
        {
            var lenSquared = vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z + vector.W * vector.W;

            Assert.AreEqual(1, lenSquared, delta * delta, "Length");
        }

        public static void LengthIsOne(Quaternion vector, double delta = 0)
        {
            var lenSquared = vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z + vector.W * vector.W;

            Assert.AreEqual(1, lenSquared, delta * delta, "Length");
        }

        public static void Less(Vector2 arg1, Vector2 arg2)
        {
            Assert.Less(arg1.X, arg2.X, "X");
            Assert.Less(arg1.Y, arg2.Y, "Y");
        }

        public static void Less(Vector3 arg1, Vector3 arg2)
        {
            Assert.Less(arg1.X, arg2.X, "X");
            Assert.Less(arg1.Y, arg2.Y, "Y");
            Assert.Less(arg1.Z, arg2.Z, "Z");
        }

        public static void Less(Vector4 arg1, Vector4 arg2)
        {
            Assert.Less(arg1.X, arg2.X, "X");
            Assert.Less(arg1.Y, arg2.Y, "Y");
            Assert.Less(arg1.Z, arg2.Z, "Z");
            Assert.Less(arg1.W, arg2.W, "W");
        }

        public static void LessOrEqual(Vector2 arg1, Vector2 arg2)
        {
            Assert.LessOrEqual(arg1.X, arg2.X, "X");
            Assert.LessOrEqual(arg1.Y, arg2.Y, "Y");
        }

        public static void LessOrEqual(Vector3 arg1, Vector3 arg2)
        {
            Assert.LessOrEqual(arg1.X, arg2.X, "X");
            Assert.LessOrEqual(arg1.Y, arg2.Y, "Y");
            Assert.LessOrEqual(arg1.Z, arg2.Z, "Z");            
        }

        public static void LessOrEqual(Vector4 arg1, Vector4 arg2)
        {
            Assert.LessOrEqual(arg1.X, arg2.X, "X");
            Assert.LessOrEqual(arg1.Y, arg2.Y, "Y");
            Assert.LessOrEqual(arg1.Z, arg2.Z, "Z");
            Assert.LessOrEqual(arg1.W, arg2.W, "W");
        }

        public static void Greater(Vector2 arg1, Vector2 arg2)
        {
            Assert.Greater(arg1.X, arg2.X, "X");
            Assert.Greater(arg1.Y, arg2.Y, "Y");
        }

        public static void Greater(Vector3 arg1, Vector3 arg2)
        {
            Assert.Greater(arg1.X, arg2.X, "X");
            Assert.Greater(arg1.Y, arg2.Y, "Y");
            Assert.Greater(arg1.Z, arg2.Z, "Z");
        }

        public static void Greater(Vector4 arg1, Vector4 arg2)
        {
            Assert.Greater(arg1.X, arg2.X, "X");
            Assert.Greater(arg1.Y, arg2.Y, "Y");
            Assert.Greater(arg1.Z, arg2.Z, "Z");
            Assert.Greater(arg1.W, arg2.W, "W");
        }

        public static void GreaterOrEqual(Vector2 arg1, Vector2 arg2)
        {
            Assert.GreaterOrEqual(arg1.X, arg2.X, "X");
            Assert.GreaterOrEqual(arg1.Y, arg2.Y, "Y");
        }

        public static void GreaterOrEqual(Vector3 arg1, Vector3 arg2)
        {
            Assert.GreaterOrEqual(arg1.X, arg2.X, "X");
            Assert.GreaterOrEqual(arg1.Y, arg2.Y, "Y");
            Assert.GreaterOrEqual(arg1.Z, arg2.Z, "Z");
        }

        public static void GreaterOrEqual(Vector4 arg1, Vector4 arg2)
        {
            Assert.GreaterOrEqual(arg1.X, arg2.X, "X");
            Assert.GreaterOrEqual(arg1.Y, arg2.Y, "Y");
            Assert.GreaterOrEqual(arg1.Z, arg2.Z, "Z");
            Assert.GreaterOrEqual(arg1.W, arg2.W, "W");
        }

        public static void AngleLessOrEqual(Vector2 a, Vector2 b, float radians)
        {
            var angle = VectorsUtils.GetAngle(a, b);

            Assert.LessOrEqual(angle, radians, "Angle");
        }

        public static void AngleLessOrEqual(Vector3 a, Vector3 b, float radians)
        {
            var angle = VectorsUtils.GetAngle(a, b);

            Assert.LessOrEqual(angle, radians, "Angle");
        }

        public static void AngleLessOrEqual(Quaternion a, Quaternion b, float radians)
        {
            var angle = VectorsUtils.GetAngle(a, b);

            Assert.LessOrEqual(angle, radians, "Angle");
        }
    }
}
