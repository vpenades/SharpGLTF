using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

using XYZ = System.Numerics.Vector3;

namespace SharpGLTF.Collections
{
    [TestFixture]
    [Category("Core")]
    public class VertexListTests
    {
        [Test]
        public void TestFloatHashCode()
        {
            // it's important to ensure that the hash or positive and negative Zero is the same.

            float positiveZero = 0f;
            float negativeZero = -positiveZero;

            var floats = new float[] { positiveZero, negativeZero };
            var integers = System.Runtime.InteropServices.MemoryMarshal.Cast<float, uint>(floats);
            Assert.AreNotEqual(integers[0], integers[1]);

            var positiveHash = positiveZero.GetHashCode();
            var negativeHash = negativeZero.GetHashCode();
            Assert.AreEqual(positiveHash, negativeHash);
        }

        [System.Diagnostics.DebuggerDisplay("{Value}")]
        struct _VertexExample
        {
            public static implicit operator _VertexExample(int value)
            {
                return new _VertexExample(value);
            }

            public _VertexExample(int val)
            {
                Value = val;
            }

            public int Value;
        }

        [Test]
        public void TestVertexListDictionary()
        {
            var list = new VertexList<_VertexExample>();

            list.Use(5);
            Assert.AreEqual(1, list.Count);

            list.Use(7);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(5, list[0].Value);
            Assert.AreEqual(7, list[1].Value);

            list.Use(5);
            Assert.AreEqual(2, list.Count);

            var list2 = new VertexList<_VertexExample>();
            list.CopyTo(list2);
            Assert.AreEqual(2, list2.Count);

        }

        [Test]
        public void TestValueListSet()
        {
            var a = new XYZ(1.1f);
            var b = new XYZ(1.2f);
            var c = new XYZ(1.3f);
            var d = new XYZ(1.4f);

            var vlist = new ValueListSet<XYZ>();

            var idx0 = vlist.Use(a); Assert.AreEqual(0, idx0);
            var idx1 = vlist.Use(b); Assert.AreEqual(1, idx1);
            var idx2 = vlist.Use(a); Assert.AreEqual(0, idx2);

            Assert.AreEqual(a, vlist[idx0]);
            Assert.AreEqual(b, vlist[idx1]);
            Assert.AreEqual(a, vlist[idx2]);

            CollectionAssert.AreEqual(new[] { a, b }, vlist.ToArray());

            vlist.Use(c);
            vlist.Use(d);
            CollectionAssert.AreEqual(new[] { a, b, c, d }, vlist.ToArray());

            var vlist2 = new ValueListSet<XYZ>();
            vlist.CopyTo(vlist2);

            Assert.AreEqual(vlist[0], vlist2[0]);
            Assert.AreEqual(vlist[1], vlist2[1]);
            Assert.AreEqual(vlist[2], vlist2[2]);
            Assert.AreEqual(vlist[3], vlist2[3]);

        }



    }
}
