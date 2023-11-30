using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Geometry.VertexTypes
{
    using VERTEXSKINNED4 = VertexBuilder<VertexPosition, VertexEmpty, VertexJoints4>;
    using VERTEXSKINNED8 = VertexBuilder<VertexPosition, VertexEmpty, VertexJoints8>;

    [Category("Toolkit")]
    public class VertexSkinningTests
    {
        [Test]
        public void TestVertexBuilderDebuggerDisplay()
        {
            var v = new VertexBuilder<VertexPositionNormalTangent, VertexColor1Texture1, VertexJoints4>();

            var txt = v._GetDebuggerDisplay();
        }

        [Test]
        public void TestSkinnedVertexEquality()
        {
            var p = new Vector3(0, 3, 0);

            var w0 = Transforms.SparseWeight8.CreateUnchecked(new Vector4(0, 1, 2, 3), new Vector4(4, 5, 6, 7), new Vector4(4, 3, 2, 1) * 0.6f, new Vector4(4, 3, 2, 1) * 0.4f);
            var w1 = Transforms.SparseWeight8.CreateUnchecked(new Vector4(3, 2, 1, 0), new Vector4(7, 6, 5, 4), new Vector4(1, 2, 3, 4) * 0.6f, new Vector4(1, 2, 3, 4) * 0.4f);

            // the index/weight pairs are ordered diferently...
            Assert.That(w1.Index0, Is.Not.EqualTo(w0.Index0));
            Assert.That(w1.Weight0, Is.Not.EqualTo(w0.Weight0));

            // but they should be effectively the same.
            Assert.That(w1, Is.EqualTo(w0));

            var v0 = new VERTEXSKINNED4()
                .WithGeometry(p)
                .WithSkinning(w0);

            var v1 = new VERTEXSKINNED4()
                .WithGeometry(p)
                .WithSkinning(w1);

            Assert.That(v1, Is.EqualTo(v0));

            var v2 = new VERTEXSKINNED8()
                .WithGeometry(p)
                .WithSkinning(w0);

            var v3 = new VERTEXSKINNED8()
                .WithGeometry(p)
                .WithSkinning(w1);

            Assert.That(v1, Is.EqualTo(v0));
        }

        [Test]
        public void TestVertexBuilderSkin8()
        {
            var v0 = new VERTEXSKINNED8()
                .WithGeometry(new Vector3(0, 3, 0))
                .WithSkinning
                    (
                    (0, 0.3f),
                    (1, 0.2f),
                    (2, 0.1f),
                    (3, 0.1f),
                    (4, 0.1f),
                    (5, 0.1f),
                    (6, 0.1f)
                    );

            v0.Validate();

            var v1 = new VERTEXSKINNED8()
                .WithGeometry(new Vector3(0, 3, 0))
                .WithSkinning
                    (
                    (0, 3),
                    (1, 2),
                    (2, 1),
                    (3, 3),
                    (4, 4),
                    (5, 5),
                    (6, 6)
                    );

            v1.Validate();
        }

        [Test]
        public void TestVertexSkinningDowngradeFrom8To4Joints()
        {
            // vertex with 5 bindings
            var v8 = new VertexJoints8
                (
                (1, 0.20f),
                (2, 0.15f),
                (3, 0.25f),
                (4, 0.10f),
                (5, 0.30f)
                );

            // we downgrade to 4 bindings; remaining bindings should be interpolated to keep weighting 1.
            var v4 = v8.ConvertToSkinning<VertexJoints4>();

            var sparse = v4.GetBindings();

            Assert.That(sparse.WeightSum, Is.EqualTo(1).Within(0.00001f));

            Assert.That(sparse[5], Is.EqualTo(0.333333f).Within(0.00001f));
            Assert.That(sparse[3], Is.EqualTo(0.277777f).Within(0.00001f));
            Assert.That(sparse[1], Is.EqualTo(0.222222f).Within(0.00001f));
            Assert.That(sparse[2], Is.EqualTo(0.166666f).Within(0.00001f));
        }
    }
}
