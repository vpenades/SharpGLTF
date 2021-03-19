using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Transforms
{
    [Category("Core.Transforms")]
    public class SparseWeight8Tests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(0,0.0001f)]
        [TestCase(2, -2, 2, -2)]
        [TestCase(0.2f, 0.15f, 0.25f, 0.10f, 0.30f)]
        [TestCase(0, 0, 1, 0, 2, 0, 3, 4, 5, 0, 6, 0, 7, 0, 6, 0, 9, 0, 11)]
        [TestCase(9, -9, 8, -8, 7, -7, 6, -6, 5, -5, 4, -4, 3, -3, 2, -2, 1, -1)]
        [TestCase(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1)]
        [TestCase(0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1)]        
        public void TestSparseCreation(params float[] array1)
        {
            var array2 = CreateSparseCompatibleArray(array1);
            
            var array3 = array1
                .Select((val, idx) => (idx, val))
                .Where(item => item.val != 0)
                .Reverse()
                .ToArray();

            // creation mode 1
            var sparse = SparseWeight8.Create(array1);
            Assert.AreEqual(array2.Sum(), sparse.WeightSum);
            CollectionAssert.AreEqual(array2, sparse.Expand(array2.Length));            

            // creation mode 2
            var indexedSparse = SparseWeight8.Create(array3);
            Assert.AreEqual(array2.Sum(), indexedSparse.WeightSum, 0.000001f);
            CollectionAssert.AreEqual(array2, indexedSparse.Expand(array2.Length));

            Assert.IsTrue(SparseWeight8.AreEqual(sparse, indexedSparse));

            // sort by weights
            var sByWeights = SparseWeight8.OrderedByWeight(sparse);
            Assert.AreEqual(array2.Sum(), sByWeights.WeightSum);
            CollectionAssert.AreEqual(array2, sByWeights.Expand(array2.Length));
            CheckWeightOrdered(sByWeights);

            // sort by indices
            var sByIndices = SparseWeight8.OrderedByIndex(sByWeights);
            Assert.AreEqual(array2.Sum(), sByIndices.WeightSum);
            CollectionAssert.AreEqual(array2, sByIndices.Expand(array2.Length));
            CheckIndexOrdered(sByWeights);

            // equality
            Assert.IsTrue(SparseWeight8.AreEqual(sByIndices, sByWeights));
            Assert.AreEqual(sByIndices.GetHashCode(), sByWeights.GetHashCode());

            // sum
            var sum = SparseWeight8.Add(sByIndices, sByWeights);
            Assert.AreEqual(array2.Sum() * 2, sum.WeightSum);

            // complement normalization
            if (!array2.Any(item => item<0))
            {
                Assert.GreaterOrEqual(sparse.GetNormalizedWithComplement(int.MaxValue).WeightSum, 1);
            }
        }


        [Test]
        public void TestSparseCreation()
        {
            var sparse = SparseWeight8.Create
                (
                (9, 9),
                (8, 2),
                (5, 1), // we set these weights separately
                (5, 1), // to check that 5 will pass 8
                (5, 1), // in the sorted set.
                (7, 1)
                );

            Assert.AreEqual(3, sparse[5]);
            Assert.AreEqual(1, sparse[7]);
            Assert.AreEqual(2, sparse[8]);
            Assert.AreEqual(9, sparse[9]);
        }

        [Test]
        public void TestCreateSparseFromVectors()
        {
            CollectionAssert.AreEqual
                (
                SparseWeight8.Create(new System.Numerics.Vector4(0, 1, 2, 3), new System.Numerics.Vector4(1, 1, 1, 1)).Expand(4),
                SparseWeight8.Create(1, 1, 1, 1).Expand(4)
                );

            CollectionAssert.AreEqual
                (
                SparseWeight8.Create(new System.Numerics.Vector4(0, 1, 2, 3), new System.Numerics.Vector4(1, 2, 3, 4)).Expand(4),
                SparseWeight8.Create(1, 2, 3, 4).Expand(4)
                );

            CollectionAssert.AreEqual
                (
                SparseWeight8.Create(new System.Numerics.Vector4(0, 1, 2, 3), new System.Numerics.Vector4(4, 3, 2, 1)).Expand(4),
                SparseWeight8.Create(4, 3, 2, 1).Expand(4)
                );

            CollectionAssert.AreEqual
                (
                SparseWeight8.Create(new System.Numerics.Vector4(0, 2, 2, 3), new System.Numerics.Vector4(4, 3, 2, 1)).Expand(4),
                SparseWeight8.Create(4, 0, 5, 1).Expand(4)
                );

            CollectionAssert.AreEqual
                (
                SparseWeight8.Create(new System.Numerics.Vector4(1, 1, 1, 1), new System.Numerics.Vector4(1, 1, 1, 1)).Expand(4),
                SparseWeight8.Create(0, 4, 0, 0).Expand(4)
                );
        }

        /// <summary>
        /// Creates a new array with only the 8 most relevant weights.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        static float[] CreateSparseCompatibleArray(params float[] array)
        {
            const int MAXWEIGHTS = 8;

            if (array == null) return null;

            var threshold =array
                .Select(item => Math.Abs(item))
                .OrderByDescending(item => item)
                .Take(MAXWEIGHTS)
                .Min();

            var array2 = new float[array.Length];

            var c = 0;

            for(int i=0; i < array2.Length; ++i)
            {
                var v = array[i];
                if (v == 0) continue;

                if (Math.Abs(v) >= threshold)
                {
                    array2[i] = v;
                    ++c;

                    if (c >= MAXWEIGHTS) return array2;
                }
            }

            return array2;
        }        

        static void CheckWeightOrdered(SparseWeight8 sparse)
        {
            Assert.GreaterOrEqual(Math.Abs(sparse.Weight0), Math.Abs(sparse.Weight1));
            Assert.GreaterOrEqual(Math.Abs(sparse.Weight1), Math.Abs(sparse.Weight2));
            Assert.GreaterOrEqual(Math.Abs(sparse.Weight2), Math.Abs(sparse.Weight3));
            Assert.GreaterOrEqual(Math.Abs(sparse.Weight3), Math.Abs(sparse.Weight4));
            Assert.GreaterOrEqual(Math.Abs(sparse.Weight4), Math.Abs(sparse.Weight5));
            Assert.GreaterOrEqual(Math.Abs(sparse.Weight5), Math.Abs(sparse.Weight6));
            Assert.GreaterOrEqual(Math.Abs(sparse.Weight6), Math.Abs(sparse.Weight7));
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
        public void TestSparseNormalization()
        {
            var sparse1 = SparseWeight8
                .Create(0, 0, 0, 0, 0, 0.1f, 0.7f, 0, 0, 0, 0.1f)
                .GetNormalizedWithComplement(int.MaxValue);
            
            Assert.AreEqual(0.1f, sparse1[5]);
            Assert.AreEqual(0.7f, sparse1[6]);
            Assert.AreEqual(0.1f, sparse1[10]);
            Assert.AreEqual(0.1f, sparse1[int.MaxValue], 0.0000001f);
            Assert.AreEqual(1, sparse1.WeightSum);
        }

        [Test]
        public void TestSparseEquality()
        {
            Assert.IsTrue(SparseWeight8.AreEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(0, 1)));

            Assert.IsFalse(SparseWeight8.AreEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(0, 1, 0.25f)));
            Assert.IsFalse(SparseWeight8.AreEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(1, 0)));

            // check if two "half weights" are equal to one "full weight"
            //Assert.IsTrue(SparseWeight8.AreWeightsEqual(SparseWeight8.Create((3, 5), (3, 5)), SparseWeight8.Create((3, 10))));
        }

        [Test]
        public void TestSparseWeightsLinearInterpolation1()
        {
            var x = SparseWeight8.Create(0,0,1,2); CollectionAssert.AreEqual(new[] { 0f, 0f, 1f, 2f }, x.Expand(4));
            var y = SparseWeight8.Create(1,2,0,0); CollectionAssert.AreEqual(new[] { 1f, 2f, 0f, 0f }, y.Expand(4));

            var z = SparseWeight8.InterpolateLinear(x, y, 0.5f);
            Assert.AreEqual(0.5f, z[0]);
            Assert.AreEqual(1, z[1]);
            Assert.AreEqual(0.5f, z[2]);
            Assert.AreEqual(1, z[3]);
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

        [Test]
        public void TestSparseWeightReduction()
        {
            var a = SparseWeight8.Create(5, 3, 2, 4, 0, 4, 2, 6, 3, 6, 1);

            var b = a.GetTrimmed(4);

            Assert.AreEqual(4, b.GetNonZeroWeights().Count());
            
            Assert.AreEqual(a[0], b[0]);
            Assert.AreEqual(a[3], b[3]);
            Assert.AreEqual(a[7], b[7]);
            Assert.AreEqual(a[9], b[9]);

            Assert.AreEqual(0, b.Weight4);
            Assert.AreEqual(0, b.Weight5);
            Assert.AreEqual(0, b.Weight6);
            Assert.AreEqual(0, b.Weight7);            
        }
    }
}
