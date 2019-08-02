using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    public static class NumericsAssert
    {
        public static void IsFinite(Single value, string message = null)
        {
            Assert.IsTrue(float.IsFinite(value), message);
        }

        public static void IsFinite(Double value, string message = null)
        {
            Assert.IsTrue(double.IsFinite(value), message);
        }

        public static void IsFinite(Vector2 vector)
        {
            IsFinite(vector.X, "X");
            IsFinite(vector.Y, "Y");
        }

        public static void IsFinite(Vector3 vector)
        {
            IsFinite(vector.X, "X");
            IsFinite(vector.Y, "Y");
            IsFinite(vector.Z, "Z");
        }

        public static void IsFinite(Vector4 vector)
        {
            IsFinite(vector.X, "X");
            IsFinite(vector.Y, "Y");
            IsFinite(vector.Z, "Z");
            IsFinite(vector.W, "W");
        }

        public static void IsFinite(Quaternion quaternion)
        {
            IsFinite(quaternion.X, "X");
            IsFinite(quaternion.Y, "Y");
            IsFinite(quaternion.Z, "Z");
            IsFinite(quaternion.W, "W");
        }

        public static void IsFinite(Plane plane)
        {
            IsFinite(plane.Normal.X, "Normal.X");
            IsFinite(plane.Normal.Y, "Normal.Y");
            IsFinite(plane.Normal.Z, "Normal.Z");
            IsFinite(plane.D, "D");
        }

        public static void IsFinite(Matrix4x4 matrix)
        {
            IsFinite(matrix.M11, "M11");
            IsFinite(matrix.M12, "M12");
            IsFinite(matrix.M13, "M13");
            IsFinite(matrix.M14, "M14");

            IsFinite(matrix.M21, "M21");
            IsFinite(matrix.M22, "M22");
            IsFinite(matrix.M23, "M23");
            IsFinite(matrix.M24, "M24");

            IsFinite(matrix.M31, "M31");
            IsFinite(matrix.M32, "M32");
            IsFinite(matrix.M33, "M33");
            IsFinite(matrix.M34, "M34");

            IsFinite(matrix.M41, "M41");
            IsFinite(matrix.M42, "M42");
            IsFinite(matrix.M43, "M43");
            IsFinite(matrix.M44, "M44");
        }

        public static void AreEqual(BigInteger expected, BigInteger actual, double tolerance = 0)
        {
            Assert.AreEqual(0, (double)BigInteger.Abs(actual - expected), tolerance);
        }

        public static void AreEqual(Vector2 expected, Vector2 actual, double tolerance = 0)
        {
            Assert.AreEqual(expected.X, actual.X, tolerance, "X");
            Assert.AreEqual(expected.Y, actual.Y, tolerance, "Y");
        }

        public static void AreEqual(Vector3 expected, Vector3 actual, double tolerance = 0)
        {
            Assert.AreEqual(expected.X, actual.X, tolerance, "X");
            Assert.AreEqual(expected.Y, actual.Y, tolerance, "Y");
            Assert.AreEqual(expected.Z, actual.Z, tolerance, "Z");
        }

        public static void AreEqual(Vector4 expected, Vector4 actual, double tolerance = 0)
        {
            Assert.AreEqual(expected.X, actual.X, tolerance, "X");
            Assert.AreEqual(expected.Y, actual.Y, tolerance, "Y");
            Assert.AreEqual(expected.Z, actual.Z, tolerance, "Z");
            Assert.AreEqual(expected.W, actual.W, tolerance, "W");
        }

        public static void AreEqual(Quaternion expected, Quaternion actual, double tolerance = 0)
        {
            Assert.AreEqual(expected.X, actual.X, tolerance, "X");
            Assert.AreEqual(expected.Y, actual.Y, tolerance, "Y");
            Assert.AreEqual(expected.Z, actual.Z, tolerance, "Z");
            Assert.AreEqual(expected.W, actual.W, tolerance, "W");
        }

        public static void AreEqual(Matrix4x4 expected, Matrix4x4 actual, double delta = 0)
        {
            Assert.AreEqual(expected.M11, actual.M11, delta, "M11");
            Assert.AreEqual(expected.M12, actual.M12, delta, "M12");
            Assert.AreEqual(expected.M13, actual.M13, delta, "M13");
            Assert.AreEqual(expected.M14, actual.M14, delta, "M14");

            Assert.AreEqual(expected.M21, actual.M21, delta, "M21");
            Assert.AreEqual(expected.M22, actual.M22, delta, "M22");
            Assert.AreEqual(expected.M23, actual.M23, delta, "M23");
            Assert.AreEqual(expected.M24, actual.M24, delta, "M24");

            Assert.AreEqual(expected.M31, actual.M31, delta, "M31");
            Assert.AreEqual(expected.M32, actual.M32, delta, "M32");
            Assert.AreEqual(expected.M33, actual.M33, delta, "M33");
            Assert.AreEqual(expected.M34, actual.M34, delta, "M34");

            Assert.AreEqual(expected.M41, actual.M41, delta, "M41");
            Assert.AreEqual(expected.M42, actual.M42, delta, "M42");
            Assert.AreEqual(expected.M43, actual.M43, delta, "M43");
            Assert.AreEqual(expected.M44, actual.M44, delta, "M44");
        }

        public static void IsInvertible(Matrix4x4 matrix)
        {
            IsFinite(matrix);
            Assert.IsTrue(Matrix4x4.Invert(matrix, out Matrix4x4 inverted));
        }

        public static void IsOrthogonal3x3(Matrix4x4 matrix, double tolerance = 0)
        {
            IsFinite(matrix);

            Assert.AreEqual(0, matrix.M41);
            Assert.AreEqual(0, matrix.M42);
            Assert.AreEqual(0, matrix.M43);
            Assert.AreEqual(1, matrix.M44);

            var cx = new Vector3(matrix.M11, matrix.M21, matrix.M31);
            var cy = new Vector3(matrix.M12, matrix.M22, matrix.M32);
            var cz = new Vector3(matrix.M13, matrix.M23, matrix.M33);            

            Assert.AreEqual(0, Vector3.Dot(cx, cy), tolerance);
            Assert.AreEqual(0, Vector3.Dot(cx, cz), tolerance);
            Assert.AreEqual(0, Vector3.Dot(cy, cz), tolerance);
        }

        public static void IsNormalized(Vector2 actual, double delta = 0)
        {
            IsFinite(actual);
            AreEqual(Vector2.Normalize(actual), actual, delta);
        }

        public static void IsNormalized(Vector3 actual, double delta = 0)
        {
            IsFinite(actual);
            AreEqual(Vector3.Normalize(actual), actual, delta);
        }

        public static void IsNormalized(Vector4 actual, double delta = 0)
        {
            IsFinite(actual);
            AreEqual(Vector4.Normalize(actual), actual, delta);
        }

        public static void IsNormalized(Quaternion actual, double delta = 0)
        {
            IsFinite(actual);
            AreEqual(Quaternion.Normalize(actual), actual, delta);
        }

        public static void InRange(BigInteger value, BigInteger min, BigInteger max)
        {
            GreaterOrEqual(value, min);
            LessOrEqual(value, max);
        }

        public static void InRange(Vector2 value, Vector2 min, Vector2 max)
        {
            GreaterOrEqual(value, min);
            LessOrEqual(value, max);
        }

        public static void InRange(Vector3 value, Vector3 min, Vector3 max)
        {
            GreaterOrEqual(value, min);
            LessOrEqual(value, max);
        }

        public static void InRange(Vector4 value, Vector4 min, Vector4 max)
        {
            GreaterOrEqual(value, min);
            LessOrEqual(value, max);
        }

        public static void Less(BigInteger arg1, BigInteger arg2)
        {
            Assert.Less(arg1.CompareTo(arg2), 0);
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

        public static void LessOrEqual(BigInteger arg1, BigInteger arg2)
        {
            Assert.LessOrEqual(arg1.CompareTo(arg2), 0);
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

        public static void Greater(BigInteger arg1, BigInteger arg2)
        {
            Assert.Greater(arg1.CompareTo(arg2), 0);
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

        public static void GreaterOrEqual(BigInteger arg1, BigInteger arg2)
        {
            Assert.GreaterOrEqual(arg1.CompareTo(arg2), 0);
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

        public static void AngleLessOrEqual(Vector2 a, Vector2 b, double radians)
        {
            var angle = VectorsUtils.GetAngle(a, b);

            Assert.LessOrEqual(angle, radians, "Angle");
        }

        public static void AngleLessOrEqual(Vector3 a, Vector3 b, double radians)
        {
            var angle = VectorsUtils.GetAngle(a, b);

            Assert.LessOrEqual(angle, radians, "Angle");
        }

        public static void AngleLessOrEqual(Quaternion a, Quaternion b, double radians)
        {
            var angle = VectorsUtils.GetAngle(a, b);

            Assert.LessOrEqual(angle, radians, "Angle");
        }
    }
}
