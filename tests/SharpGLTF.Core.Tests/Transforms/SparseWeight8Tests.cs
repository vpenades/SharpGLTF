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
            Assert.That(sparse.WeightSum, Is.EqualTo(array2.Sum()));
            Assert.That(sparse.Expand(array2.Length), Is.EqualTo(array2));            

            // creation mode 2
            var indexedSparse = SparseWeight8.Create(array3);
            Assert.That(indexedSparse.WeightSum, Is.EqualTo(array2.Sum()).Within(0.000001f));
            Assert.That(indexedSparse.Expand(array2.Length), Is.EqualTo(array2));

            Assert.That(SparseWeight8.AreEqual(sparse, indexedSparse));

            // sort by weights
            var sByWeights = SparseWeight8.OrderedByWeight(sparse);
            Assert.That(sByWeights.WeightSum, Is.EqualTo(array2.Sum()));
            Assert.That(sByWeights.Expand(array2.Length), Is.EqualTo(array2));
            CheckWeightOrdered(sByWeights);

            // sort by indices
            var sByIndices = SparseWeight8.OrderedByIndex(sByWeights);
            CheckIndexOrdered(sByIndices);
            Assert.That(sByIndices.WeightSum, Is.EqualTo(array2.Sum()));
            Assert.That(sByIndices.Expand(array2.Length), Is.EqualTo(array2));
            

            // equality
            Assert.That(SparseWeight8.AreEqual(sByIndices, sByWeights), Is.True);
            Assert.That(sByWeights.GetHashCode(), Is.EqualTo(sByIndices.GetHashCode()));

            // sum
            var sum = SparseWeight8.Add(sByIndices, sByWeights);
            Assert.That(sum.WeightSum, Is.EqualTo(array2.Sum() * 2));

            // complement normalization
            if (!array2.Any(item => item<0))
            {
                Assert.That(sparse.GetNormalizedWithComplement(int.MaxValue).WeightSum, Is.GreaterThanOrEqualTo(1));
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

            Assert.That(sparse[5], Is.EqualTo(3));
            Assert.That(sparse[7], Is.EqualTo(1));
            Assert.That(sparse[8], Is.EqualTo(2));
            Assert.That(sparse[9], Is.EqualTo(9));
        }

        [Test]
        public void TestCreateSparseFromVectors()
        {
            Assert.That
                (
                SparseWeight8.Create(new System.Numerics.Vector4(0, 1, 2, 3), new System.Numerics.Vector4(1, 1, 1, 1)).Expand(4),
                Is.EqualTo(SparseWeight8.Create(1, 1, 1, 1).Expand(4))
                );

            Assert.That
                (
                SparseWeight8.Create(new System.Numerics.Vector4(0, 1, 2, 3), new System.Numerics.Vector4(1, 2, 3, 4)).Expand(4),
                Is.EqualTo(SparseWeight8.Create(1, 2, 3, 4).Expand(4))
                );

            Assert.That
                (
                SparseWeight8.Create(new System.Numerics.Vector4(0, 1, 2, 3), new System.Numerics.Vector4(4, 3, 2, 1)).Expand(4),
                Is.EqualTo(SparseWeight8.Create(4, 3, 2, 1).Expand(4))
                );

            Assert.That
                (
                SparseWeight8.Create(new System.Numerics.Vector4(0, 2, 2, 3), new System.Numerics.Vector4(4, 3, 2, 1)).Expand(4),
                Is.EqualTo(SparseWeight8.Create(4, 0, 5, 1).Expand(4))
                );

            Assert.That
                (
                SparseWeight8.Create(new System.Numerics.Vector4(1, 1, 1, 1), new System.Numerics.Vector4(1, 1, 1, 1)).Expand(4),
                Is.EqualTo(SparseWeight8.Create(0, 4, 0, 0).Expand(4))
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
            Assert.Multiple(() =>
            {
                Assert.That(Math.Abs(sparse.Weight0), Is.GreaterThanOrEqualTo(Math.Abs(sparse.Weight1)));
                Assert.That(Math.Abs(sparse.Weight1), Is.GreaterThanOrEqualTo(Math.Abs(sparse.Weight2)));
                Assert.That(Math.Abs(sparse.Weight2), Is.GreaterThanOrEqualTo(Math.Abs(sparse.Weight3)));
                Assert.That(Math.Abs(sparse.Weight3), Is.GreaterThanOrEqualTo(Math.Abs(sparse.Weight4)));
                Assert.That(Math.Abs(sparse.Weight4), Is.GreaterThanOrEqualTo(Math.Abs(sparse.Weight5)));
                Assert.That(Math.Abs(sparse.Weight5), Is.GreaterThanOrEqualTo(Math.Abs(sparse.Weight6)));
                Assert.That(Math.Abs(sparse.Weight6), Is.GreaterThanOrEqualTo(Math.Abs(sparse.Weight7)));
            });
        }

        static void CheckIndexOrdered(SparseWeight8 sparse)
        {
            var pairs = sparse.GetIndexedWeights();

            bool zeroFound = false;
            long lastIndex = long.MinValue;

            foreach(var (index,weight) in pairs)
            {
                if (weight == 0) zeroFound = true;

                if (zeroFound)
                {
                    Assert.That(index, Is.EqualTo(0));
                    Assert.That(weight, Is.EqualTo(0));
                    continue;
                }

                Assert.That(index, Is.GreaterThan(lastIndex));

                lastIndex = index;
            }            
        }

        [Test]
        public void TestSparseNormalization()
        {
            var sparse1 = SparseWeight8
                .Create(0, 0, 0, 0, 0, 0.1f, 0.7f, 0, 0, 0, 0.1f)
                .GetNormalizedWithComplement(int.MaxValue);
            
            Assert.That(sparse1[5], Is.EqualTo(0.1f));
            Assert.That(sparse1[6], Is.EqualTo(0.7f));
            Assert.That(sparse1[10], Is.EqualTo(0.1f));
            Assert.That(sparse1[int.MaxValue], Is.EqualTo(0.1f).Within(0.0000001f));
            Assert.That(sparse1.WeightSum, Is.EqualTo(1));
        }

        [Test]
        public void TestSparseEquality()
        {
            Assert.That(SparseWeight8.AreEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(0, 1)), Is.True);

            Assert.That(SparseWeight8.AreEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(0, 1, 0.25f)), Is.False);
            Assert.That(SparseWeight8.AreEqual(SparseWeight8.Create(0, 1), SparseWeight8.Create(1, 0)), Is.False);

            // check if two "half weights" are equal to one "full weight"
            //Assert.IsTrue(SparseWeight8.AreWeightsEqual(SparseWeight8.Create((3, 5), (3, 5)), SparseWeight8.Create((3, 10))));
        }

        [Test]
        public void TestSparseWeightsLinearInterpolation1()
        {
            var x = SparseWeight8.Create(0,0,1,2); Assert.That(x.Expand(4), Is.EqualTo(new[] { 0f, 0f, 1f, 2f }));
            var y = SparseWeight8.Create(1,2,0,0); Assert.That(y.Expand(4), Is.EqualTo(new[] { 1f, 2f, 0f, 0f }));

            var z = SparseWeight8.InterpolateLinear(x, y, 0.5f);
            Assert.That(z[0], Is.EqualTo(0.5f));
            Assert.That(z[1], Is.EqualTo(1));
            Assert.That(z[2], Is.EqualTo(0.5f));
            Assert.That(z[3], Is.EqualTo(1));
        }

        [Test]
        public void TestSparseWeightsLinearInterpolation2()
        {
            var ax = new float[] { 0, 0,    0, 0,    0, 0.1f, 0.7f, 0, 0, 0, 0.1f };
            var ay = new float[] { 0, 0, 0.2f, 0, 0.1f,    0,    0, 0, 0, 0,    0, 0, 0.2f };
            var cc = Math.Min(ax.Length, ay.Length);

            var x = SparseWeight8.Create(ax); Assert.That(x.Expand(ax.Length), Is.EqualTo(ax));
            var y = SparseWeight8.Create(ay); Assert.That(y.Expand(ay.Length), Is.EqualTo(ay));

            var z = SparseWeight8.InterpolateLinear(x, y, 0.5f);

            for (int i=0; i < cc; ++i)
            {
                var w = (ax[i] + ay[i]) / 2;
                Assert.That(z[i], Is.EqualTo(w));
            }
        }

        [Test]
        public void TestSparseWeightsCubicInterpolation()
        {
            var a = SparseWeight8.Create(0, 0, 0.2f, 0, 0, 0, 1);
            var b = SparseWeight8.Create(1, 1, 0.4f, 0, 0, 1, 0);
            var t = SparseWeight8.Subtract(b, a);
            Assert.That(t[0], Is.EqualTo(1));
            Assert.That(t[1], Is.EqualTo(1));
            Assert.That(t[2], Is.EqualTo(0.2f));
            Assert.That(t[3], Is.EqualTo(0));
            Assert.That(t[4], Is.EqualTo(0));
            Assert.That(t[5], Is.EqualTo(1));
            Assert.That(t[6], Is.EqualTo(-1));

            var lr = SparseWeight8.InterpolateLinear(a, b, 0.4f);
            var cr = SparseWeight8.InterpolateCubic(a, t, b, t, 0.4f);

            Assert.That(cr[0], Is.EqualTo(lr[0]).Within(0.000001f));
            Assert.That(cr[1], Is.EqualTo(lr[1]).Within(0.000001f));
            Assert.That(cr[2], Is.EqualTo(lr[2]).Within(0.000001f));
            Assert.That(cr[3], Is.EqualTo(lr[3]).Within(0.000001f));
            Assert.That(cr[4], Is.EqualTo(lr[4]).Within(0.000001f));
            Assert.That(cr[5], Is.EqualTo(lr[5]).Within(0.000001f));
            Assert.That(cr[6], Is.EqualTo(lr[6]).Within(0.000001f));
            Assert.That(cr[7], Is.EqualTo(lr[7]).Within(0.000001f));
        }

        [Test]
        public void TestSparseWeightReduction()
        {
            var a = SparseWeight8.Create(5, 3, 2, 4, 0, 4, 2, 6, 3, 6, 1);

            var b = a.GetTrimmed(4);

            Assert.That(b.GetNonZeroWeights().Count(), Is.EqualTo(4));
            
            Assert.That(b[0], Is.EqualTo(a[0]));
            Assert.That(b[3], Is.EqualTo(a[3]));
            Assert.That(b[7], Is.EqualTo(a[7]));
            Assert.That(b[9], Is.EqualTo(a[9]));

            Assert.That(b.Weight4, Is.EqualTo(0));
            Assert.That(b.Weight5, Is.EqualTo(0));
            Assert.That(b.Weight6, Is.EqualTo(0));
            Assert.That(b.Weight7, Is.EqualTo(0));            
        }
    }
}
