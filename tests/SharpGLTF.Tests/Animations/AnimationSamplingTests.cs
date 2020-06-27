using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    [TestFixture]
    [Category("Core.Animations")]
    public class AnimationSamplingTests
    {
        [Test]
        public void TestAnimationSplit()
        {
            var anim0 = new[]
            {
                (0.1f, 1),                
            };

            var anim1 = new[]
            {
                (0.1f, 1),
                (0.2f, 2)
            };

            var anim2 = new[]
            {
                (0.1f, 1),
                (0.2f, 2),
                (3.2f, 2),
                (3.3f, 2)
            };

            var anim3 = new[]
            {
                (2.1f, 1),
                (2.2f, 2),
                (3.2f, 3),
                (3.3f, 4),
                (4.0f, 5),
                (4.1f, 6),
                (5.0f, 7),
            };

            void checkSegment(int time, (float,int)[] segment)
            {
                // should check all times are incremental
                Assert.Greater(segment.Length, 1);
                Assert.LessOrEqual(segment.First().Item1, time);
                Assert.Greater(segment.Last().Item1, time);
            }


            var r0 = Animations.SamplerFactory.SplitByTime(anim0).ToArray();
            Assert.AreEqual(1, r0.Length);
            Assert.AreEqual(1, r0[0].Length);

            var r1 = Animations.SamplerFactory.SplitByTime(anim1).ToArray();
            Assert.AreEqual(1, r1.Length);
            Assert.AreEqual(2, r1[0].Length);

            var r2 = Animations.SamplerFactory.SplitByTime(anim2).ToArray();
            Assert.AreEqual(4, r2.Length);
            Assert.AreEqual(3, r2[0].Length); 
            Assert.AreEqual(2, r2[1].Length); checkSegment(1, r2[1]);
            Assert.AreEqual(2, r2[2].Length); checkSegment(2, r2[2]);
            Assert.AreEqual(3, r2[3].Length); checkSegment(3, r2[3]);

            var r3 = Animations.SamplerFactory.SplitByTime(anim3).ToArray();
            Assert.AreEqual(6, r3.Length);
            Assert.AreEqual(1, r3[0].Length); 
            Assert.AreEqual(1, r3[1].Length); 
            Assert.AreEqual(3, r3[2].Length); 
            Assert.AreEqual(4, r3[3].Length); checkSegment(3, r3[3]);
            Assert.AreEqual(4, r3[3].Length); checkSegment(4, r3[4]);
            Assert.AreEqual(1, r3[5].Length);
        }

        [Test]
        public void TestFastSampler()
        {
            var curve = Enumerable
                .Range(0, 1000)
                .Select(idx => (0.1f * (float)idx, new Vector3(idx, idx, idx)))
                .ToArray();

            var defaultSampler = new Animations.Vector3LinearSampler(curve, true);
            var fastSampler = defaultSampler.ToFastSampler();

            foreach(var k in curve)
            {
                Assert.AreEqual(k.Item2, defaultSampler.GetPoint(k.Item1));
                Assert.AreEqual(k.Item2, fastSampler.GetPoint(k.Item1));
            }

            for(float t=0; t < 100; t+=0.232f)
            {
                var dv = defaultSampler.GetPoint(t);
                var fv = fastSampler.GetPoint(t);

                Assert.AreEqual(dv, fv);
            }
        }

        [TestCase(0, 0, 0, 1, 1, 1, 1, 0)]
        [TestCase(0, 0, 0.1f, 5, 0.7f, 3, 1, 0)]
        public void TestHermiteInterpolation1(float p1x, float p1y, float p2x, float p2y, float p3x, float p3y, float p4x, float p4y)
        {
            var p1 = new Vector2(p1x, p1y);
            var p2 = new Vector2(p2x, p2y);
            var p3 = new Vector2(p3x, p3y);
            var p4 = new Vector2(p4x, p4y);

            var ppp = new List<Vector2>();

            for (float amount = 0; amount <= 1; amount += 0.01f)
            {
                var (startPosition, endPosition, startTangent, endTangent) = Animations.SamplerFactory.CreateHermitePointWeights(amount);

                var p = Vector2.Zero;

                p += p1 * startPosition;
                p += p4 * endPosition;
                p += (p2 - p1) * 4 * startTangent;
                p += (p4 - p3) * 4 * endTangent;

                ppp.Add(p);
            }

            // now lets calculate an arbitrary point and tangent

            float k = 0.3f;

            var hb = Animations.SamplerFactory.CreateHermitePointWeights(k);
            var ht = Animations.SamplerFactory.CreateHermiteTangentWeights(k);

            var pp = p1 * hb.StartPosition + p4 * hb.EndPosition + (p2 - p1) * 4 * hb.StartTangent + (p4 - p3) * 4 * hb.EndTangent;
            var pt = p1 * ht.StartPosition + p4 * ht.EndPosition + (p2 - p1) * 4 * ht.StartTangent + (p4 - p3) * 4 * ht.EndTangent;

            // plotting

            var series1 = ppp.ToPointSeries("sampling");
            var series2 = new[] { p1, p2, p3, p4 }.ToLineSeries("source");
            var series3 = new[] { pp, pp + pt }.ToLineSeries("tangent");

            new[] { series1, series2, series3 }.AttachToCurrentTest("plot.html");
        }

        [Test]
        public void TestHermiteAsLinearInterpolation()
        {
            var p1 = new Vector2(1, 0);
            var p2 = new Vector2(3, 1);
            var t = p2 - p1;

            var ppp = new List<Vector2>();

            for (float amount = 0; amount <= 1; amount += 0.1f)
            {
                var (startPosition, endPosition, startTangent, endTangent) = Animations.SamplerFactory.CreateHermitePointWeights(amount);

                var p = Vector2.Zero;

                p += p1 * startPosition;
                p += p2 * endPosition;
                p += t * startTangent;
                p += t * endTangent;

                ppp.Add(p);
            }

            var series1 = ppp.ToPointSeries().WithLineType(Plotting.LineType.Star);

            new[] { series1 }.AttachToCurrentTest("plot.html");
        }

        [Test]
        public void TestHermiteAsSphericalInterpolation()
        {
            // given two quaternions, we must find a tangent quaternion so that the quaternion
            // hermite interpolation gives roughly the same results as a plain spherical interpolation.

            // reference implementation with matrices
            var m1 = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, 1);            
            var m2 = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, 2);            
            var mt = Matrix4x4.Multiply(m2, Matrix4x4.Transpose(m1));            
            var m2bis = Matrix4x4.Multiply(mt, m1); // roundtrip; M2 == M2BIS

            // implementation with quaternions
            var q1 = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 1);
            var q2 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 2);
            var qt = Quaternion.Concatenate(q2, Quaternion.Conjugate(q1));            
            var q2bis = Quaternion.Concatenate(qt, q1); // roundtrip; Q2 == Q2BIS

            NumericsAssert.AreEqual(qt, Animations.SamplerFactory.CreateTangent(q1, q2), 0.000001f);

            var angles = new List<Vector2>();

            for (float amount = 0; amount <= 1; amount += 0.025f)
            {
                // slerp interpolation
                var sq = Quaternion.Normalize(Quaternion.Slerp(q1, q2, amount));

                // hermite interpolation with a unit tangent
                var hermite = Animations.SamplerFactory.CreateHermitePointWeights(amount);
                var hq = default(Quaternion);
                hq += q1 * hermite.StartPosition;
                hq += q2 * hermite.EndPosition;
                hq += qt * hermite.StartTangent;
                hq += qt * hermite.EndTangent;
                hq = Quaternion.Normalize(hq);
                
                // check
                NumericsAssert.AreEqual(sq, hq, 0.1f);
                NumericsAssert.AngleLessOrEqual(sq, hq, 0.22f);

                // diff
                var a = (sq, hq).GetAngle() * 180.0f / 3.141592f;
                angles.Add(new Vector2(amount, a));                          
            }

            angles.ToPointSeries()
                .WithLineType(Plotting.LineType.Continuous)
                .AttachToCurrentTest("plot.html");
        
        }

        private static (float, (Vector3, Vector3, Vector3))[] _TransAnim = new []
        {
            (0.0f, (        Vector3.Zero, new Vector3(0, 0, 0),new Vector3(0, 0, 0))),
            (1.0f, (new Vector3(0, 0, 0), new Vector3(1, 0, 0),new Vector3(0, 1, 0))),
            (2.0f, (new Vector3(0, -1, 0), new Vector3(2, 0, 0),new Vector3(0, 0, 0))),
            (3.0f, (new Vector3(0, 0, 0), new Vector3(3, 0, 0),       Vector3.Zero ))
        };

        private static (float, (Quaternion, Quaternion, Quaternion))[] _RotAnim = new[]
        {
            (0.0f, (new Quaternion(0,0,0,0), Quaternion.CreateFromYawPitchRoll(+1.6f, 0, 0), new Quaternion(0,0,0,0))),
            (1.0f, (new Quaternion(0,0,0,0), Quaternion.Identity, new Quaternion(0,0,0,0))),
            (2.0f, (new Quaternion(0,0,0,0), Quaternion.CreateFromYawPitchRoll(-1.6f, 0, 0), new Quaternion(0,0,0,0))),
            (3.0f, (new Quaternion(0,0,0,0), Quaternion.Identity, new Quaternion(0,0,0,0))),
            (4.0f, (new Quaternion(0,0,0,0), Quaternion.CreateFromYawPitchRoll(+1.6f, 0, 0), new Quaternion(0,0,0,0))),
        };

        [Test]
        public void TestVector3CubicSplineSampling()
        {
            var sampler = Animations.SamplerFactory.CreateSampler(_TransAnim);

            var points = new List<Vector3>();

            for(int i=0; i < 300; ++i)
            {
                var sample = sampler.GetPoint(((float)i) / 100.0f);
                points.Add( sample );
            }

            points
                .Select(p => new Vector2(p.X, p.Y))
                .ToPointSeries()                
                .AttachToCurrentTest("plot.html");            
        }

        [Test]
        public void TestQuaternionCubicSplineSampling()
        {
            var sampler = Animations.SamplerFactory.CreateSampler(_RotAnim);

            var a = sampler.GetPoint(0);
            var b = sampler.GetPoint(1);
            var bc = sampler.GetPoint(1.5f);
            var c = sampler.GetPoint(2);
        }

    }
}
