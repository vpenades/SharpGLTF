using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Transforms
{
    [Category("Core.Transforms")]
    public class SparseWeight8Tests
    {
        [Test]
        public void CreateSparse()
        {
            var sparse1 = SparseWeight8.Create(0, 0, 0, 0, 0, 0.1f, 0.7f, 0, 0, 0, 0.1f);
            Assert.AreEqual(6, sparse1.Index0);
            Assert.AreEqual(5, sparse1.Index1);
            Assert.AreEqual(10, sparse1.Index2);

            Assert.AreEqual(0.7f, sparse1.Weight0);
            Assert.AreEqual(0.1f, sparse1.Weight1);
            Assert.AreEqual(0.1f, sparse1.Weight2);
            Assert.AreEqual(0, sparse1.Weight3);

            var sparse1Nrm = MorphTransform.Normalize(sparse1);
            Assert.AreEqual(7, sparse1Nrm.Index0);
            Assert.AreEqual(6, sparse1Nrm.Index1);
            Assert.AreEqual(11, sparse1Nrm.Index2);
            Assert.AreEqual(0, sparse1Nrm.Index3);

            Assert.AreEqual(0.7f, sparse1Nrm.Weight0);
            Assert.AreEqual(0.1f, sparse1Nrm.Weight1);
            Assert.AreEqual(0.1f, sparse1Nrm.Weight2);
            Assert.AreEqual(0.1f, sparse1Nrm.Weight3, 0.00001f); // funny enough, 0.8f + 0.1f = 0.90000036f
            Assert.AreEqual(0, sparse1Nrm.Weight4);
        }

        [Test]
        public void TestLinearInterpolation1()
        {
            var x = new SparseWeight8((0,0f));
            var y = new SparseWeight8((0,1f));

            var z = SparseWeight8.InterpolateLinear(x, y, 0.5f);
        }

        [Test]
        public void TestLinearInterpolation2()
        {
            var ax = new float[] { 0, 0, 0, 0, 0, 0.1f, 0.7f, 0, 0, 0, 0.1f };
            var ay = new float[] { 0, 0, 0.2f, 0, 0.1f, 0, 0, 0, 0, 0, 0 };

            var x = SparseWeight8.Create(ax); CollectionAssert.AreEqual(ax, x.Expand(ax.Length));
            var y = SparseWeight8.Create(ay); CollectionAssert.AreEqual(ay, y.Expand(ay.Length));

            var z = SparseWeight8.InterpolateLinear(x, y, 0.5f);
            Assert.AreEqual(6, z.Index0);
            Assert.AreEqual(2, z.Index1);
            Assert.AreEqual(5, z.Index2);
            Assert.AreEqual(10, z.Index3);
            Assert.AreEqual(4, z.Index4);

            Assert.AreEqual(0.35f, z.Weight0);
            Assert.AreEqual(0.1f, z.Weight1);
            Assert.AreEqual(0.05f, z.Weight2);
            Assert.AreEqual(0.05f, z.Weight3);
            Assert.AreEqual(0.05f, z.Weight4);
            Assert.AreEqual(0, z.Weight5);
        }
    }
}
