using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

using SharpGLTF.Scenes;

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
            Assert.That(convertible, Is.Not.Null);

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
                .AttachToCurrentTest("plot.html");
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
            Assert.That(convertible, Is.Not.Null);

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
                .AttachToCurrentTest("plot.html");
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
            Assert.That(convertible, Is.Not.Null);

            var linear = convertible.ToLinearCurve().Select(kvp => (kvp.Key, kvp.Value)).CreateSampler();
            var spline = convertible.ToSplineCurve().Select(kvp => (kvp.Key, kvp.Value)).CreateSampler();

            // check if both curves are roughly the same.

            for (float t = 0; t < 2; t += 0.01f)
            {
                var cc = curve.GetPoint(t);
                var ls = linear.GetPoint(t);
                var ss = spline.GetPoint(t);

                Assert.That(ls[0], Is.EqualTo(cc[0]).Within(0.02f));
                Assert.That(ls[1], Is.EqualTo(cc[1]).Within(0.02f));

                Assert.That(ss[0], Is.EqualTo(cc[0]).Within(0.02f));
                Assert.That(ss[1], Is.EqualTo(cc[1]).Within(0.02f));

                Assert.That(ss[0], Is.EqualTo(ls[0]).Within(0.02f));
                Assert.That(ss[1], Is.EqualTo(ls[1]).Within(0.02f));
            }

            // plot the curve.

            convertible
                .ToLinearCurve()
                .Select(kvp => new Vector2(kvp.Value[0], kvp.Value[1]))
                .ToPointSeries()
                .WithLineType(Plotting.LineType.Continuous)
                .AttachToCurrentTest("plot.html");
        }

        [Test]
        public void CreateVisibilityCurve1()
        {
            // Create a Vector3 curve

            var curve = CurveFactory.CreateCurveBuilder<Boolean>();

            curve.WithPoint(0, false, false);
            curve.WithPoint(1, true, false);
            curve.WithPoint(2, false, false);

            var _0_6 = curve.GetPoint(0.6f);
            Assert.That(_0_6, Is.EqualTo(false));
        }
    }

    [Category("Toolkit.Animations")]
    public class TrackBuilderTests
    {
        [Test]
        public void CreateOneKey()
        {
            var node = new Scenes.NodeBuilder("someNode");

            // define translation curve
            var tb_trans = node.UseTranslation().UseTrackBuilder("track1");
            tb_trans.SetPoint(0, new Vector3(1,2,3));

            // define visibility curve
            var tb_vis = node.UseVisibility().UseTrackBuilder("track1");
            tb_vis.SetPoint(0, true, false);
            tb_vis.SetPoint(2, false,false);
            tb_vis.SetPoint(3, true, false);

            // create gltf scene

            var scene = new Scenes.SceneBuilder();
            scene.AddNode(node);

            var gltfSettings = new SceneBuilderSchema2Settings();            
            var glTF = scene.ToGltf2(gltfSettings);

            var rootNode = glTF.DefaultScene.VisualChildren.FirstOrDefault();

            var rootNodeSamplers = rootNode.GetCurveSamplers(glTF.LogicalAnimations[0]);

            var rootNodeTrans = rootNodeSamplers.Translation.CreateCurveSampler().GetPoint(2.5f);
            Assert.That(rootNodeTrans, Is.EqualTo(new Vector3(1, 2, 3)));

            var rootNodeVis = rootNodeSamplers.Visibility.CreateCurveSampler().GetPoint(2.5f);
            Assert.That(rootNodeVis, Is.EqualTo(false));

            // create runtime template

            var options = new Runtime.RuntimeOptions { IsolateMemory = true };
            var template = Runtime.SceneTemplate.Create(glTF.DefaultScene, options);

            // create runtime instance       

            var instance = template.CreateInstance();

            var instanceNode = instance.Armature.LogicalNodes.First(n => n.Name == "someNode");

            instance.Armature.SetAnimationFrame(0, 0);
            var nodeMatrix = instanceNode.LocalMatrix;
            Assert.That(nodeMatrix.Translation, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(instanceNode.IsVisible, Is.EqualTo(true));

            instance.Armature.SetAnimationFrame(0, 2.5f);
            nodeMatrix = instanceNode.LocalMatrix;
            Assert.That(nodeMatrix.Translation, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(instanceNode.IsVisible, Is.EqualTo(false));
        }

    }
}
