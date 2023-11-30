using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Collections
{
    [TestFixture]
    [Category("Core")]
    public class ChildrenListTests
    {
        class TestChild : IChildOfList<ChildrenListTests>
        {
            public ChildrenListTests LogicalParent { get; private set; }

            public int LogicalIndex { get; private set; } = -1;

            void IChildOfList<ChildrenListTests>.SetLogicalParent(ChildrenListTests parent, int index)
            {
                LogicalParent = parent;
                LogicalIndex = index;
            }
        }

        [Test]
        public void TestChildCollectionList1()
        {
            if (System.Diagnostics.Debugger.IsAttached) return;

            var list = new ChildrenList<TestChild, ChildrenListTests>(this);
            
            Assert.Throws<ArgumentNullException>(() => list.Add(null));

            var item1 = new TestChild();
            Assert.That(item1.LogicalParent, Is.Null);
            Assert.That(item1.LogicalIndex, Is.EqualTo(-1));

            var item2 = new TestChild();
            Assert.That(item2.LogicalParent, Is.Null);
            Assert.That(item2.LogicalIndex, Is.EqualTo(-1));

            list.Add(item1);
            Assert.That(item1.LogicalParent, Is.SameAs(this));
            Assert.That(item1.LogicalIndex, Is.EqualTo(0));

            Assert.Throws<ArgumentException>(() => list.Add(item1));

            list.Remove(item1);
            Assert.That(item1.LogicalParent, Is.Null);
            Assert.That(item1.LogicalIndex, Is.EqualTo(-1));

            list.Add(item1);
            Assert.That(item1.LogicalParent, Is.SameAs(this));
            Assert.That(item1.LogicalIndex, Is.EqualTo(0));

            list.Insert(0, item2);
            Assert.That(item2.LogicalParent, Is.SameAs(this));
            Assert.That(item2.LogicalIndex, Is.EqualTo(0));
            Assert.That(item1.LogicalParent, Is.SameAs(this));
            Assert.That(item1.LogicalIndex, Is.EqualTo(1));

            list.RemoveAt(0);
            Assert.That(item2.LogicalParent, Is.Null);
            Assert.That(item2.LogicalIndex, Is.EqualTo(-1));
            Assert.That(item1.LogicalParent, Is.SameAs(this));
            Assert.That(item1.LogicalIndex, Is.EqualTo(0));

        }

    }
}
