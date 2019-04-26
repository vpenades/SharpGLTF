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
                new JointWeightPair(2,0),
                new JointWeightPair(1,0.20f),
                new JointWeightPair(3,0.15f),
                new JointWeightPair(2,0.25f),
                new JointWeightPair(4,0),
                new JointWeightPair(7,0.40f)
            };

            JointWeightPair.InPlaceReverseBubbleSort(pairs);

            Assert.AreEqual(7, pairs[0].Joint);
            Assert.AreEqual(2, pairs[1].Joint);
            Assert.AreEqual(1, pairs[2].Joint);
            Assert.AreEqual(3, pairs[3].Joint);
            Assert.AreEqual(0, pairs[4].Joint);
            Assert.AreEqual(0, pairs[5].Joint);
        }
    }
}
