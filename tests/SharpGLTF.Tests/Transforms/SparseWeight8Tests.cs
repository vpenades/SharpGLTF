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
        public void CreateSparseWeights()
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

            // we must also support negative values.
            var sparseNegative = SparseWeight8.Create(0, 1, -1);
            Assert.AreEqual(1, sparseNegative.Index0);
            Assert.AreEqual(2, sparseNegative.Index1);
            Assert.AreEqual( 1, sparseNegative.Weight0);
            Assert.AreEqual(-1, sparseNegative.Weight1);
        }

        [Test]
        public void TestSparseEquality()
        {
            Assert.IsTrue(SparseWeight8.AreWeightsEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(0, 1)));

            Assert.IsFalse(SparseWeight8.AreWeightsEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(0, 1, 0.25f)));
            Assert.IsFalse(SparseWeight8.AreWeightsEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(1, 0)));
        }

        [Test]
        public void TestSparseWeightsLinearInterpolation1()
        {
            var x = new SparseWeight8((0,0f));
            var y = new SparseWeight8((0,1f));

            var z = SparseWeight8.InterpolateLinear(x, y, 0.5f);
        }

        [Test]
        public void TestSparseWeightsLinearInterpolation2()
        {
            var ax = new float[] { 0, 0,    0, 0,    0, 0.1f, 0.7f, 0, 0, 0, 0.1f };
            var ay = new float[] { 0, 0, 0.2f, 0, 0.1f,    0,    0, 0, 0, 0,    0, 0, 0.2f };
            var cc = Math.Min(ax.Length, ay.Length);

            var x = SparseWeight8.Create(ax); CollectionAssert.AreEqual(ax, x.Expand(ax.Length));
            var y = SparseWeight8.Create(ay); CollectionAssert.AreEqual(ay, y.Expand(ay.Length));

            var z = SparseWeight8.InterpolateLinear(x, y, 0.5f);

            for (int i=0; i < cc; ++i)
            {
                var w = (ax[i] + ay[i]) / 2;
                Assert.AreEqual(w, z[i]);
            }
        }

        [Test]
        public void TestSparseWeightsCubicInterpolation()
        {
            var a = SparseWeight8.Create(0, 0, 0.2f, 0, 0, 0, 1);
            var b = SparseWeight8.Create(1, 1, 0.4f, 0, 0, 1, 0);
            var t = SparseWeight8.Subtract(b, a);

            var lr = SparseWeight8.InterpolateLinear(a, b, 0.4f);
            var cr = SparseWeight8.InterpolateCubic(a, t, b, t, 0.4f);

            Assert.AreEqual(lr[0], cr[0], 0.000001f);
            Assert.AreEqual(lr[1], cr[1], 0.000001f);
            Assert.AreEqual(lr[2], cr[2], 0.000001f);
            Assert.AreEqual(lr[3], cr[3], 0.000001f);
            Assert.AreEqual(lr[4], cr[4], 0.000001f);
            Assert.AreEqual(lr[5], cr[5], 0.000001f);
            Assert.AreEqual(lr[6], cr[6], 0.000001f);
            Assert.AreEqual(lr[7], cr[7], 0.000001f);
        }
    }
}
