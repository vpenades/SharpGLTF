using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

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

        

    }
}
