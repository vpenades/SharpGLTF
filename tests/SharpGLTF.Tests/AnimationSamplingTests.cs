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
                var hermite = Animations.SamplerFactory.CreateHermitePointWeights(amount);

                var p = Vector2.Zero;

                p += p1 * hermite.Item1;
                p += p4 * hermite.Item2;
                p += (p2 - p1) * 4 * hermite.Item3;
                p += (p4 - p3) * 4 * hermite.Item4;

                ppp.Add(p);
            }

            // now lets calculate an arbitrary point and tangent

            float k = 0.3f;

            var hb = Animations.SamplerFactory.CreateHermitePointWeights(k);
            var ht = Animations.SamplerFactory.CreateHermiteTangentWeights(k);

            var pp = p1 * hb.Item1 + p4 * hb.Item2 + (p2 - p1) * 4 * hb.Item3 + (p4 - p3) * 4 * hb.Item4;
            var pt = p1 * ht.Item1 + p4 * ht.Item2 + (p2 - p1) * 4 * ht.Item3 + (p4 - p3) * 4 * ht.Item4;

            // plotting

            var series1 = ppp.ToPointSeries();
            var series2 = new[] { p1, p2, p3, p4 }.ToLineSeries();
            var series3 = new[] { pp, pp + pt }.ToLineSeries();

            new[] { series1, series2, series3 }.AttachToCurrentTest("plot.png");
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
                var hermite = Animations.SamplerFactory.CreateHermitePointWeights(amount);

                var p = Vector2.Zero;

                p += p1 * hermite.Item1;
                p += p2 * hermite.Item2;
                p += t * hermite.Item3;
                p += t * hermite.Item4;

                ppp.Add(p);
            }

            var series1 = ppp.ToPointSeries().WithLineType(Plotting.LineType.Star);

            new[] { series1 }.AttachToCurrentTest("plot.png");
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
                hq += q1 * hermite.Item1;
                hq += q2 * hermite.Item2;
                hq += qt * hermite.Item3;
                hq += qt * hermite.Item4;
                hq = Quaternion.Normalize(hq);
                
                // check
                NumericsAssert.AreEqual(sq, hq, 0.1f);
                NumericsAssert.AngleLessOrEqual(sq, hq, 0.22f);

                // diff
                var a = VectorsUtils.GetAngle(sq, hq) * 180.0f / 3.141592f;
                angles.Add(new Vector2(amount, a));                          
            }

            angles.ToPointSeries()
                .WithLineType(Plotting.LineType.Continuous)
                .AttachToCurrentTest("plot.png");
        
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
                .AttachToCurrentTest("plot.png");            
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
