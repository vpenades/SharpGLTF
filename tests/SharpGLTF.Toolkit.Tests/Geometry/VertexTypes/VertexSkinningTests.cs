using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Geometry.VertexTypes
{
    [TestFixture]
    [Category("Toolkit")]
    public class VertexSkinningTests
    {
        [Test]
        public void TestVertexBuilderDebuggerDisplay()
        {
            var v = new VertexBuilder<VertexPositionNormalTangent, VertexColor1Texture1, VertexJoints4>();

            var txt = v._GetDebuggerDisplay();
        }

        [Test]
        public void TestVertexSkinningDowngradeFrom8To4Joints()
        {
            // vertex with 5 bindings
            var v8 = new VertexJoints8
                (
                (1, 0.20f),
                (2, 0.15f),
                (3, 0.25f),
                (4, 0.10f),
                (5, 0.30f)
                );

            // we downgrade to 4 bindings; remaining bindings should be interpolated to keep weighting 1.
            var v4 = v8.ConvertToSkinning<VertexJoints4>();

            var sparse = v4.GetWeights();

            Assert.AreEqual(1, sparse.WeightSum, 0.00001f);

            Assert.AreEqual(0.333333f, sparse[5], 0.00001f);
            Assert.AreEqual(0.277777f, sparse[3], 0.00001f);
            Assert.AreEqual(0.222222f, sparse[1], 0.00001f);
            Assert.AreEqual(0.166666f, sparse[2], 0.00001f);
        }
    }
}
