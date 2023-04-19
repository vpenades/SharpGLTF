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
            Assert.IsNull(item1.LogicalParent);
            Assert.AreEqual(-1, item1.LogicalIndex);

            var item2 = new TestChild();
            Assert.IsNull(item2.LogicalParent);
            Assert.AreEqual(-1, item2.LogicalIndex);

            list.Add(item1);
            Assert.AreSame(this, item1.LogicalParent);
            Assert.AreEqual(0, item1.LogicalIndex);

            Assert.Throws<ArgumentException>(() => list.Add(item1));

            list.Remove(item1);
            Assert.IsNull(item1.LogicalParent);
            Assert.AreEqual(-1, item1.LogicalIndex);

            list.Add(item1);
            Assert.AreSame(this, item1.LogicalParent);
            Assert.AreEqual(0, item1.LogicalIndex);

            list.Insert(0, item2);
            Assert.AreSame(this, item2.LogicalParent);
            Assert.AreEqual(0, item2.LogicalIndex);
            Assert.AreSame(this, item1.LogicalParent);
            Assert.AreEqual(1, item1.LogicalIndex);

            list.RemoveAt(0);
            Assert.IsNull(item2.LogicalParent);
            Assert.AreEqual(-1, item2.LogicalIndex);
            Assert.AreSame(this, item1.LogicalParent);
            Assert.AreEqual(0, item1.LogicalIndex);

        }

    }
}
