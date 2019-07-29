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

            var sparse9to2 = SparseWeight8.Create(0, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f);
            Assert.AreEqual(9, sparse9to2.Index0);
            Assert.AreEqual(8, sparse9to2.Index1);
            Assert.AreEqual(7, sparse9to2.Index2);
            Assert.AreEqual(6, sparse9to2.Index3);
            Assert.AreEqual(5, sparse9to2.Index4);
            Assert.AreEqual(4, sparse9to2.Index5);
            Assert.AreEqual(3, sparse9to2.Index6);
            Assert.AreEqual(2, sparse9to2.Index7);
            Assert.AreEqual(0.9f, sparse9to2.Weight0);
            Assert.AreEqual(0.8f, sparse9to2.Weight1);
            Assert.AreEqual(0.7f, sparse9to2.Weight2);
            Assert.AreEqual(0.6f, sparse9to2.Weight3);
            Assert.AreEqual(0.5f, sparse9to2.Weight4);
            Assert.AreEqual(0.4f, sparse9to2.Weight5);
            Assert.AreEqual(0.3f, sparse9to2.Weight6);
            Assert.AreEqual(0.2f, sparse9to2.Weight7);



            Assert.AreEqual(0.7f, sparse1.Weight0);
            Assert.AreEqual(0.1f, sparse1.Weight1);
            Assert.AreEqual(0.1f, sparse1.Weight2);
            Assert.AreEqual(0, sparse1.Weight3);

            var sparse1Nrm = sparse1.GetNormalizedWithComplement();
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
        public void TestSparseOrdering1()
        {
            var array1 = new float[] { 0.2f, 0.15f, 0.25f, 0.10f, 0.30f };

            var s5 = SparseWeight8.Create(array1);
            CollectionAssert.AreEqual(array1, s5.Expand(5));

            var s5byWeights = SparseWeight8.OrderedByWeight(s5);
            CollectionAssert.AreEqual(array1, s5byWeights.Expand(5));
            CheckWeightOrdered(s5byWeights);
            var s5byIndices = SparseWeight8.OrderedByIndex(s5byWeights);
            CollectionAssert.AreEqual(array1, s5byIndices.Expand(5));
            CheckIndexOrdered(s5byWeights);
        }

        [Test]
        public void TestSparseOrdering2()
        {
            var expanded = new float[] { 0,0,1,0,2,0,3,4,5,0,6,0,7,0,6,0,9,0,11 };

            var sparse = SparseWeight8.Create(expanded);

            Assert.AreEqual(11, sparse[18]);
            Assert.AreEqual(9, sparse[16]);
            Assert.AreEqual(7, sparse[12]);
            Assert.AreEqual(6, sparse[10]);
            Assert.AreEqual(6, sparse[14]);
            Assert.AreEqual(5, sparse[8]);
            Assert.AreEqual(4, sparse[7]);
            Assert.AreEqual(3, sparse[6]);
        }

        static void CheckWeightOrdered(SparseWeight8 sparse)
        {
            Assert.GreaterOrEqual(sparse.Weight0, sparse.Weight1);
            Assert.GreaterOrEqual(sparse.Weight1, sparse.Weight2);
            Assert.GreaterOrEqual(sparse.Weight2, sparse.Weight3);
            Assert.GreaterOrEqual(sparse.Weight3, sparse.Weight4);
            Assert.GreaterOrEqual(sparse.Weight4, sparse.Weight5);
            Assert.GreaterOrEqual(sparse.Weight5, sparse.Weight6);
            Assert.GreaterOrEqual(sparse.Weight6, sparse.Weight7);
        }

        static void CheckIndexOrdered(SparseWeight8 sparse)
        {
            Assert.LessOrEqual(sparse.Index0, sparse.Index0);
            Assert.LessOrEqual(sparse.Index1, sparse.Index1);
            Assert.LessOrEqual(sparse.Index2, sparse.Index2);
            Assert.LessOrEqual(sparse.Index3, sparse.Index3);
            Assert.LessOrEqual(sparse.Index4, sparse.Index4);
            Assert.LessOrEqual(sparse.Index5, sparse.Index5);
            Assert.LessOrEqual(sparse.Index6, sparse.Index6);
        }

        [Test]
        public void TestSparseEquality()
        {
            Assert.IsTrue(SparseWeight8.AreWeightsEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(0, 1)));

            Assert.IsFalse(SparseWeight8.AreWeightsEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(0, 1, 0.25f)));
            Assert.IsFalse(SparseWeight8.AreWeightsEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(1, 0)));

            // check if two "half weights" are equal to one "full weight"
            Assert.IsTrue(SparseWeight8.AreWeightsEqual(SparseWeight8.Create((3, 5), (3, 5)), SparseWeight8.Create((3, 10))));
        }

        [Test]
        public void TestSparseWeightsLinearInterpolation1()
        {
            var x = SparseWeight8.Create((0,0f));
            var y = SparseWeight8.Create((0,1f));

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
            Assert.AreEqual(1, t[0]);
            Assert.AreEqual(1, t[1]);
            Assert.AreEqual(0.2f, t[2]);
            Assert.AreEqual(0, t[3]);
            Assert.AreEqual(0, t[4]);
            Assert.AreEqual(1, t[5]);
            Assert.AreEqual(-1, t[6]);

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
