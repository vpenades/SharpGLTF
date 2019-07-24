using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Animations
{
    [Category("Toolkit.Animations")]
    public class CurveBuilderTests
    {
        [Test]
        public void CreateTranslationCurve1()
        {
            // Create a Vector3 curve

            var curve = CurveFactory.CreateCurveBuilder<Vector3>();

            curve
                .WithPoint(0, 0, 0, 0);

            curve
                .WithPoint(1, 1, 1, 1)
                .WithOutgoingTangent(1, 0, 4, 0);

            curve
                .WithPoint(2, 2, 1, 1)
                .WithIncomingTangent(2, 0, -4, 0);

            // convert and resample the curve to a linear and cubic curves.

            var convertible = curve as IConvertibleCurve<Vector3>;
            Assert.NotNull(convertible);

            var linear = convertible.ToLinearCurve().Select(kvp => (kvp.Key, kvp.Value)).CreateSampler();
            var spline = convertible.ToSplineCurve().Select(kvp => (kvp.Key, kvp.Value)).CreateSampler();

            // check if both curves are roughly the same.

            for (float t=0; t < 2; t+=0.01f)
            {
                var cc = curve.GetPoint(t);
                var ls = linear.GetPoint(t);
                var ss = spline.GetPoint(t);

                NumericsAssert.AreEqual(cc, ls, 0.002f);
                NumericsAssert.AreEqual(cc, ss, 0.002f);
                NumericsAssert.AreEqual(ls, ss, 0.002f);
            }

            // plot the curve.

            convertible
                .ToLinearCurve()
                .Select(kvp => new Vector2(kvp.Value.X, kvp.Value.Y))
                .ToPointSeries()
                .WithLineType(Plotting.LineType.Continuous)
                .AttachToCurrentTest("plot.png");
        }

        [Test]
        public void CreateRotationCurve1()
        {
            // Create a Quaternion curve

            var curve = CurveFactory.CreateCurveBuilder<Quaternion>();

            curve
                .WithPoint(0, Quaternion.Identity);

            curve
                .WithPoint(1, Quaternion.CreateFromAxisAngle(Vector3.UnitX,1) )
                .WithOutgoingTangent(1, Quaternion.CreateFromAxisAngle(Vector3.UnitX, 1) );

            curve
                .WithPoint(2, Quaternion.CreateFromAxisAngle(Vector3.UnitX, 1) )
                .WithIncomingTangent(2, Quaternion.CreateFromAxisAngle(Vector3.UnitX, -1) );

            // convert and resample the curve to a linear and cubic curves.

            var convertible = curve as IConvertibleCurve<Quaternion>;
            Assert.NotNull(convertible);

            var linear = convertible.ToLinearCurve().Select(kvp => (kvp.Key, kvp.Value)).CreateSampler();
            var spline = convertible.ToSplineCurve().Select(kvp => (kvp.Key, kvp.Value)).CreateSampler();

            // check if both curves are roughly the same.

            for (float t = 0; t < 2; t += 0.01f)
            {
                var cc = curve.GetPoint(t);
                var ls = linear.GetPoint(t);
                var ss = spline.GetPoint(t);

                NumericsAssert.AreEqual(cc, ss, 0.05f);
                NumericsAssert.AreEqual(cc, ls, 0.05f);
                NumericsAssert.AreEqual(ls, ss, 0.05f);
            }

            // plot the curve.

            convertible
                .ToLinearCurve()
                .Select(kvp => new Vector2(kvp.Key, kvp.Value.W))
                .ToPointSeries()
                .WithLineType(Plotting.LineType.Continuous)
                .AttachToCurrentTest("plot.png");
        }

        [Test]
        public void CreateMorphCurve1()
        {
            // Create a Transforms.SparseWeight8 curve

            var curve = CurveFactory.CreateCurveBuilder<Transforms.SparseWeight8>();

            curve
                .WithPoint(0, 0f, 0f);

            curve
                .WithPoint(1, 1f, 1f)
                .WithOutgoingTangent(1, 0f, 4f);

            curve
                .WithPoint(2, 2f, 1f)
                .WithIncomingTangent(2, 0f, -4f);

            // convert and resample the curve to a linear and cubic curves.

            var convertible = curve as IConvertibleCurve<Transforms.SparseWeight8>;
            Assert.NotNull(convertible);

            var linear = convertible.ToLinearCurve().Select(kvp => (kvp.Key, kvp.Value)).CreateSampler();
            var spline = convertible.ToSplineCurve().Select(kvp => (kvp.Key, kvp.Value)).CreateSampler();

            // check if both curves are roughly the same.

            for (float t = 0; t < 2; t += 0.01f)
            {
                var cc = curve.GetPoint(t);
                var ls = linear.GetPoint(t);
                var ss = spline.GetPoint(t);

                Assert.AreEqual(cc[0], ls[0], 0.02f);
                Assert.AreEqual(cc[1], ls[1], 0.02f);

                Assert.AreEqual(cc[0], ss[0], 0.02f);
                Assert.AreEqual(cc[1], ss[1], 0.02f);

                Assert.AreEqual(ls[0], ss[0], 0.02f);
                Assert.AreEqual(ls[1], ss[1], 0.02f);
            }

            // plot the curve.

            convertible
                .ToLinearCurve()
                .Select(kvp => new Vector2(kvp.Value[0], kvp.Value[1]))
                .ToPointSeries()
                .WithLineType(Plotting.LineType.Continuous)
                .AttachToCurrentTest("plot.png");
        }
    }
}
