using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Collections
{
    [TestFixture]
    [Category("Core")]
    public class ChildrenDictionaryTests
    {
        class TestChild : IChildOfDictionary<ChildrenDictionaryTests>
        {
            public ChildrenDictionaryTests LogicalParent { get; private set; }

            public string LogicalKey { get; private set; }

            void IChildOfDictionary<ChildrenDictionaryTests>.SetLogicalParent(ChildrenDictionaryTests parent, string key)
            {
                LogicalParent = parent;
                LogicalKey = key;
            }
        }

        [Test]
        public void TestChildCollectionDictionary1()
        {
            var dict = new ChildrenDictionary<TestChild, ChildrenDictionaryTests>(this);

            Assert.That(() => dict.Add("key", null), Throws.ArgumentNullException);

            var item1 = new TestChild();
            Assert.That(item1.LogicalParent, Is.Null);
            Assert.That(item1.LogicalKey, Is.Null);

            var item2 = new TestChild();
            Assert.That(item2.LogicalParent, Is.Null);
            Assert.That(item2.LogicalKey, Is.Null);

            dict["0"] = item1;
            Assert.That(item1.LogicalParent, Is.SameAs(this));
            Assert.That(item1.LogicalKey, Is.EqualTo("0"));

            Assert.That(() => dict.Add("1", item1), Throws.ArgumentException);

            dict.Remove("0");
            Assert.That(item1.LogicalParent, Is.Null);
            Assert.That(item1.LogicalKey, Is.Null);

            dict["0"] = item1;
            Assert.That(item1.LogicalParent, Is.SameAs(this));
            Assert.That(item1.LogicalKey, Is.EqualTo("0"));

            dict["1"] = item2;
            Assert.That(item1.LogicalParent, Is.SameAs(this));
            Assert.That(item1.LogicalKey, Is.EqualTo("0"));
            Assert.That(item2.LogicalParent, Is.SameAs(this));
            Assert.That(item2.LogicalKey, Is.EqualTo("1"));
        }

    }
}
