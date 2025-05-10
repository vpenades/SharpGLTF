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
            Assert.That(integers[1], Is.Not.EqualTo(integers[0]));

            var positiveHash = positiveZero.GetHashCode();
            var negativeHash = negativeZero.GetHashCode();
            Assert.That(negativeHash, Is.EqualTo(positiveHash));
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
            Assert.That(list, Has.Count.EqualTo(1));

            list.Use(7);
            Assert.That(list, Has.Count.EqualTo(2));
            Assert.That(list[0].Value, Is.EqualTo(5));
            Assert.That(list[1].Value, Is.EqualTo(7));

            list.Use(5);
            Assert.That(list, Has.Count.EqualTo(2));

            var list2 = new VertexList<_VertexExample>();
            list.CopyTo(list2);
            Assert.That(list2, Has.Count.EqualTo(2));

        }

        [Test]
        public void TestValueListSet()
        {
            var a = new XYZ(1.1f);
            var b = new XYZ(1.2f);
            var c = new XYZ(1.3f);
            var d = new XYZ(1.4f);

            var vlist = new ValueListSet<XYZ>();

            var idx0 = vlist.Use(a); Assert.That(idx0, Is.EqualTo(0));
            var idx1 = vlist.Use(b); Assert.That(idx1, Is.EqualTo(1));
            var idx2 = vlist.Use(a); Assert.That(idx2, Is.EqualTo(0));

            Assert.That(vlist[idx0], Is.EqualTo(a));
            Assert.That(vlist[idx1], Is.EqualTo(b));
            Assert.That(vlist[idx2], Is.EqualTo(a));

            Assert.That(new[] { a, b }, Is.EqualTo(vlist.ToArray()));

            vlist.Use(c);
            vlist.Use(d);
            Assert.That(new[] { a, b, c, d }, Is.EqualTo(vlist.ToArray()));

            var vlist2 = new ValueListSet<XYZ>();
            vlist.CopyTo(vlist2);

            Assert.That(vlist2[0], Is.EqualTo(vlist[0]));
            Assert.That(vlist2[1], Is.EqualTo(vlist[1]));
            Assert.That(vlist2[2], Is.EqualTo(vlist[2]));
            Assert.That(vlist2[3], Is.EqualTo(vlist[3]));

        }



    }
}
