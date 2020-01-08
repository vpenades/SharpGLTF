using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Collections
{
    [TestFixture]
    [Category("Core")]
    public class ChildrenCollectionTests
    {
        class TestChild : IChildOf<ChildrenCollectionTests>
        {
            public ChildrenCollectionTests LogicalParent { get; private set; }

            public void _SetLogicalParent(ChildrenCollectionTests parent)
            {
                LogicalParent = parent;
            }
        }

        [Test]
        public void TestChildCollectionList1()
        {
            if (System.Diagnostics.Debugger.IsAttached) return;

            var list = new ChildrenCollection<TestChild, ChildrenCollectionTests>(this);
            
            Assert.Throws<ArgumentNullException>(() => list.Add(null));

            var item = new TestChild();
            Assert.IsNull(item.LogicalParent);

            list.Add(item);
            Assert.AreSame(item.LogicalParent, this);

            Assert.Throws<ArgumentException>(() => list.Add(item));

            list.Remove(item);
            Assert.IsNull(item.LogicalParent);
        }

    }
}
