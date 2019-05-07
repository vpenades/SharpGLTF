using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Geometry.VertexTypes
{
    [TestFixture]
    [Category("Toolkit")]
    public class JointWeightPairTests
    {
        [Test]
        public void TestSorting()
        {
            var pairs = new[]
            {
                new BoneBinding(2,0),
                new BoneBinding(1,0.20f),
                new BoneBinding(3,0.15f),
                new BoneBinding(2,0.25f),
                new BoneBinding(4,0),
                new BoneBinding(7,0.40f)
            };

            BoneBinding.InPlaceReverseBubbleSort(pairs);

            Assert.AreEqual(7, pairs[0].Joint);
            Assert.AreEqual(2, pairs[1].Joint);
            Assert.AreEqual(1, pairs[2].Joint);
            Assert.AreEqual(3, pairs[3].Joint);
            Assert.AreEqual(0, pairs[4].Joint);
            Assert.AreEqual(0, pairs[5].Joint);
        }
    }
}
