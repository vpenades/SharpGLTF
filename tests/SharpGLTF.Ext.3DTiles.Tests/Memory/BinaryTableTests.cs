using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SharpGLTF.Memory
{
    public class BinaryTableTests
    {
        [Test]
        public void ConvertJaggedListOfBoolsToBytes()
        {
            var values = new List<List<bool>>();
            values.Add(new List<bool>() { true, false });
            values.Add(new List<bool>() { false, true });
            var bytes = BinaryTable.ConvertJaggedListToBytes(values);
            // Check size, should be 1 byte for 4 bits
            Assert.That(bytes.Count, Is.EqualTo(1));

            // read the bits back
            var bits = new System.Collections.BitArray(bytes.ToArray());
            Assert.That(bits[0], Is.EqualTo(true));
            Assert.That(bits[1], Is.EqualTo(false));
            Assert.That(bits[2], Is.EqualTo(false));
            Assert.That(bits[3], Is.EqualTo(true));
        }

        [Test]
        public void ConvertJaggedListOfIntsToBytes()
        {
            var values = new List<List<int>>();
            values.Add(new List<int>() { 0, 1 });
            values.Add(new List<int>() { 2, 3 });
            var bytes = BinaryTable.ConvertJaggedListToBytes(values);
            Assert.That(bytes.Count, Is.EqualTo(BinaryTable.GetSize<int>() * 4));
        }

        [Test]
        public void ConvertVector2ToBytes()
        {
            var values = new List<System.Numerics.Vector2>();
            values.Add(new System.Numerics.Vector2(0, 1));
            values.Add(new System.Numerics.Vector2(2, 3));
            var bytes = BinaryTable.GetBytes(values);
            Assert.That(bytes.Length, Is.EqualTo(BinaryTable.GetSize<System.Numerics.Vector2>() * 2));
        }

        [Test]
        public void ConvertVector3ToBytes()
        {
            var values = new List<System.Numerics.Vector3>();
            values.Add(new System.Numerics.Vector3(0, 1, 2));
            values.Add(new System.Numerics.Vector3(3, 4, 5));
            var bytes = BinaryTable.GetBytes(values);
            Assert.That(bytes.Length, Is.EqualTo(BinaryTable.GetSize<System.Numerics.Vector3>() * 2));
        }

        [Test]
        public void ConvertVector4ToBytes()
        {
            var values = new List<System.Numerics.Vector4>();
            values.Add(new System.Numerics.Vector4(0, 1, 2, 3));
            values.Add(new System.Numerics.Vector4(4, 5, 6, 7));
            var bytes = BinaryTable.GetBytes(values);
            Assert.That(bytes.Length, Is.EqualTo(BinaryTable.GetSize<System.Numerics.Vector4>() * 2));
        }

        [Test]
        public void ConvertMatrix4x4ToBytes()
        {
            // create list of identity matrices
            var values = new List<System.Numerics.Matrix4x4>();
            values.Add(System.Numerics.Matrix4x4.Identity);

            // convert to bytes
            var bytes = BinaryTable.GetBytes(values);

            // check size
            Assert.That(bytes.Length, Is.EqualTo(BinaryTable.GetSize<System.Numerics.Matrix4x4>()));

        }

        [Test]
        public void TestGetArrayOffset()
        {
            // arrange
            var list0 = new List<string>() { "hello", "world!" };
            var list1 = new List<string>() { "test", "testtest" };
            var arrays = new List<List<string>>() { list0, list1 };

            // act
            var arrayOffsets = BinaryTable.GetArrayOffsets(arrays);
            var stringOffsets = BinaryTable.GetStringOffsets(arrays);

            // assert
            Assert.That(arrayOffsets.Count, Is.EqualTo(arrays.Count + 1));
            Assert.That(arrayOffsets[0], Is.EqualTo(0));
            var l0 = list0.Count;
            var l1 = list1.Count;

            Assert.That(arrayOffsets[1], Is.EqualTo(l0));
            Assert.That(arrayOffsets[2], Is.EqualTo(l0 + l1));

            Assert.That(stringOffsets.Count, Is.EqualTo(list0.Count + list1.Count + 1));
            Assert.That(stringOffsets[0], Is.EqualTo(0));
            Assert.That(stringOffsets[1], Is.EqualTo(list0[0].Length));
            Assert.That(stringOffsets[2], Is.EqualTo(list0[0].Length + list0[1].Length));
            Assert.That(stringOffsets[3], Is.EqualTo(list0[0].Length + list0[1].Length + list1[0].Length));
            Assert.That(stringOffsets[4], Is.EqualTo(list0[0].Length + list0[1].Length + list1[0].Length + list1[1].Length));
        }

        [Test]
        public void TestBinaryConversion()
        {
            var bytes = BinaryTable.GetBytes(GetTestArray<byte>());
            Assert.That(bytes.Length, Is.EqualTo(BinaryTable.GetSize<byte>() * 2));

            bytes = BinaryTable.GetBytes(GetTestArray<int>());
            Assert.That(bytes.Length, Is.EqualTo(BinaryTable.GetSize<int>() * 2));

            bytes = BinaryTable.GetBytes(new List<string>() { "a", "b" });
            Assert.That(bytes.Length, Is.EqualTo(2));

            bytes = BinaryTable.GetBytes(new List<bool>() { true, false });
            Assert.That(bytes.Length, Is.EqualTo(1));

            var bits = new System.Collections.BitArray(bytes);
            Assert.That(bits[0] == true);
            Assert.That(bits[1] == false);

            // test exception for datetime
            var dates = new List<DateTime>();
            dates.Add(new DateTime());
            Assert.Throws<NotImplementedException>(() => BinaryTable.GetBytes(dates));
        }

        private static List<T> GetTestArray<T>()
        {
            var l = new List<T>();
            l.Add((T)Convert.ChangeType(0, typeof(T), CultureInfo.InvariantCulture));
            l.Add((T)Convert.ChangeType(1, typeof(T), CultureInfo.InvariantCulture));
            return l;
        }

    }
}
