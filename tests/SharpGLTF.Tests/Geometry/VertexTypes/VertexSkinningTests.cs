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
        public void TestVertexSkinningDowngradeFrom8To4Joints()
        {
            // vertex with 5 bindings
            var v8 = new VertexJoints8x8();
            v8.SetJointBinding(0, 1, 0.2f);
            v8.SetJointBinding(1, 2, 0.15f);
            v8.SetJointBinding(2, 3, 0.25f);
            v8.SetJointBinding(3, 4, 0.10f);
            v8.SetJointBinding(4, 5, 0.30f);

            // we downgrade to 4 bindings; remaining bindings should be interpolated to keep weighting 1.
            var v4 = v8.ConvertTo<VertexJoints8x4>();

            Assert.AreEqual(5, v4.GetJointBinding(0).Joint);
            Assert.AreEqual(3, v4.GetJointBinding(1).Joint);
            Assert.AreEqual(1, v4.GetJointBinding(2).Joint);
            Assert.AreEqual(2, v4.GetJointBinding(3).Joint);

            Assert.AreEqual(0.333333f, v4.GetJointBinding(0).Weight, 0.0001f);
            Assert.AreEqual(0.277777f, v4.GetJointBinding(1).Weight, 0.0001f);
            Assert.AreEqual(0.222222f, v4.GetJointBinding(2).Weight, 0.0001f);
            Assert.AreEqual(0.166666f, v4.GetJointBinding(3).Weight, 0.0001f);
        }
    }
}
