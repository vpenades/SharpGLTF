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
        }

    }
}
