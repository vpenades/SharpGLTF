using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SharpGLTF
{
    public class BinaryTableTests
    {

        [Test]
        public void TestGetArrayOffset()
        {
            // arrange
            var list0 = new List<string>(){ "hello", "world!"};
            var list1 = new List<string>(){"test", "testtest"};
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
            Assert.That(arrayOffsets[2], Is.EqualTo(l0+l1));

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
            
            //bytes = BinaryTable.GetBytes(new List<string>() { "a", "b" });
            //Assert.That(bytes.Length, Is.EqualTo(2));

            bytes  = BinaryTable.GetBytes(new List<bool>() { true, false });
            Assert.That(bytes.Length, Is.EqualTo(1));
            // create a bit arrat frin the byte array
            var bits = new System.Collections.BitArray(bytes);
            Assert.That(bits[0] == true);
            Assert.That(bits[1] == false);

            var ints = new List<List<int>>();
            ints.Add(new List<int>() { 0, 1 });
            Assert.Throws<NotImplementedException>(() => BinaryTable.GetBytes(ints));
        }

        private static List<T> GetTestArray<T>()
        {
            var l = new List<T>();
            l.Add((T)Convert.ChangeType(0, typeof(T),CultureInfo.InvariantCulture));
            l.Add((T)Convert.ChangeType(1, typeof(T), CultureInfo.InvariantCulture));
            return l;
        }

    }
}
