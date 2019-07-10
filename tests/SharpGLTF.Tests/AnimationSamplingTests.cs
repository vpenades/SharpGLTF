using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    [TestFixture]
    [Category("Core")]
    public class AnimationSamplingTests
    {
        [Test]
        public void TestHermiteInterpolation1()
        {
            var p1 = new Vector2(0, 0);
            var p2 = new Vector2(0, 1);
            var p3 = new Vector2(1, 1);
            var p4 = new Vector2(1, 0);

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
        public void TestHermiteInterpolation2()
        {
            var p1 = new Vector2(0, 0);
            var p2 = new Vector2(0.1f, 5);
            var p3 = new Vector2(0.7f, 3);
            var p4 = new Vector2(1, 0);            

            var ppp = new List<Vector2>();

            for (float amount = 0; amount <= 1; amount += 0.01f)
            {
                var hermite = Animations.SamplerFactory.CreateHermitePointWeights(amount);

                var p = Vector2.Zero;

                p += p1 * hermite.Item1;
                p += p4 * hermite.Item2;
                p += (p2-p1) * 4 * hermite.Item3;
                p += (p4-p3) * 4 * hermite.Item4;

                ppp.Add(p);
            }

            var series1 = ppp.ToPointSeries();
            var series2 = new[] { p1, p2, p3, p4 }.ToLineSeries();

            new[] { series1, series2 }.AttachToCurrentTest("plot.png");
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
