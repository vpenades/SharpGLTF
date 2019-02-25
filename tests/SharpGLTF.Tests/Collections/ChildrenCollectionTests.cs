using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Collections
{
    [TestFixture]
    public class ChildrenCollectionTests
    {
        public TestContext TestContext { get; set; }

        class TestChild : IChildOf<ChildrenCollectionTests>
        {
            public ChildrenCollectionTests LogicalParent { get; private set; }

            public void _SetLogicalParent(ChildrenCollectionTests parent)
            {
                LogicalParent = parent;
            }
        }

        [Test]
        public void ListTest1()
        {
            var list = new ChildrenCollection<TestChild, ChildrenCollectionTests>(this);

            var item1 = new TestChild();
            Assert.IsNull(item1.LogicalParent);

            list.Add(item1);
            Assert.AreSame(item1.LogicalParent, this);

            list.Remove(item1);
            Assert.IsNull(item1.LogicalParent);
        }

    }
}
