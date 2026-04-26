using System.Linq;
using System.Numerics;

using NUnit.Framework;

namespace SharpGLTF.Schema2.Authoring
{
    [TestFixture]
    [Category("Model Authoring")]
    public class GaussianSplattingExtensionTests
    {
        private static readonly Vector3[] _Positions =
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0)
        };

        private static readonly Vector4[] _Rotations =
        {
            new Vector4(0, 0, 0, 1),
            new Vector4(0, 0, 0, 1)
        };

        private static readonly Vector3[] _Scales =
        {
            Vector3.Zero,
            Vector3.Zero
        };

        private static readonly float[] _Opacities =
        {
            1,
            0.5f
        };

        private static readonly Vector3[] _Sh0 =
        {
            new Vector3(0.1f, 0.2f, 0.3f),
            new Vector3(0.2f, 0.3f, 0.4f)
        };

        [Test]
        public void CreateRoundtripWithGaussianSplattingExtension()
        {
            var model = ModelRoot.CreateModel();
            var primitive = _CreateMinimalGaussianPrimitive(model);

            var gaussian = primitive.UseGaussianSplatting();
            gaussian.Kernel = GaussianSplatting.KernelEllipse;
            gaussian.ColorSpace = GaussianSplatting.ColorSpaceSrgbRec709Display;
            gaussian.Projection = "custom_projection";
            gaussian.SortingMethod = "custom_sorting";

            var roundtrip = ModelRoot.ParseGLB(model.WriteGLB());
            var roundtripPrimitive = roundtrip.LogicalMeshes[0].Primitives[0];
            var roundtripGaussian = roundtripPrimitive.GetGaussianSplatting();

            Assert.That(roundtripGaussian, Is.Not.Null);
            Assert.That(roundtripGaussian.Kernel, Is.EqualTo(gaussian.Kernel));
            Assert.That(roundtripGaussian.ColorSpace, Is.EqualTo(gaussian.ColorSpace));
            Assert.That(roundtripGaussian.Projection, Is.EqualTo(gaussian.Projection));
            Assert.That(roundtripGaussian.SortingMethod, Is.EqualTo(gaussian.SortingMethod));
        }

        [Test]
        public void DetectGaussianSplattingExtensionUsage()
        {
            var model = ModelRoot.CreateModel();
            var primitive = _CreateMinimalGaussianPrimitive(model);

            var gaussian = primitive.UseGaussianSplatting();
            gaussian.Kernel = GaussianSplatting.KernelEllipse;
            gaussian.ColorSpace = GaussianSplatting.ColorSpaceSrgbRec709Display;

            var used = model.GatherUsedExtensions().ToArray();
            Assert.That(used, Does.Contain(GaussianSplatting.SCHEMANAME));

            var roundtrip = ModelRoot.ParseGLB(model.WriteGLB());
            Assert.That(roundtrip.ExtensionsUsed, Does.Contain(GaussianSplatting.SCHEMANAME));
        }

        [Test]
        public void UseGetRemoveGaussianSplattingExtension()
        {
            var model = ModelRoot.CreateModel();
            var primitive = _CreateMinimalGaussianPrimitive(model);

            var extension = primitive.GetGaussianSplatting();
            Assert.That(extension, Is.Null);

            var created = primitive.UseGaussianSplatting();
            var reused = primitive.UseGaussianSplatting();

            Assert.That(reused, Is.SameAs(created));
            Assert.That(created.Projection, Is.EqualTo(GaussianSplatting.ProjectionPerspective));
            Assert.That(created.SortingMethod, Is.EqualTo(GaussianSplatting.SortingMethodCameraDistance));
            Assert.That(GaussianSplatting.GetSphericalHarmonicsAttribute(2, 4), Is.EqualTo("KHR_gaussian_splatting:SH_DEGREE_2_COEF_4"));

            primitive.RemoveGaussianSplatting();
            Assert.That(primitive.GetGaussianSplatting(), Is.Null);
        }

        [Test]
        public void WriteGaussianSplattingWithInvalidModeThrows()
        {
            var model = ModelRoot.CreateModel();
            var primitive = _CreateMinimalGaussianPrimitive(model);

            var gaussian = primitive.UseGaussianSplatting();
            gaussian.Kernel = GaussianSplatting.KernelEllipse;
            gaussian.ColorSpace = GaussianSplatting.ColorSpaceSrgbRec709Display;

            primitive.DrawPrimitiveType = PrimitiveType.TRIANGLES;

            Assert.That(() => model.WriteGLB(), Throws.TypeOf<Validation.LinkException>());
        }

        [Test]
        public void WriteGaussianSplattingWithoutRequiredAttributeThrows()
        {
            var model = ModelRoot.CreateModel();
            var primitive = _CreateMinimalGaussianPrimitive(model);

            primitive.SetVertexAccessor(GaussianSplatting.AttributeOpacity, null);

            var gaussian = primitive.UseGaussianSplatting();
            gaussian.Kernel = GaussianSplatting.KernelEllipse;
            gaussian.ColorSpace = GaussianSplatting.ColorSpaceSrgbRec709Display;

            Assert.That(() => model.WriteGLB(), Throws.TypeOf<Validation.SchemaException>());
        }

        private static MeshPrimitive _CreateMinimalGaussianPrimitive(ModelRoot model)
        {
            var scene = model.UseScene("default");
            var node = scene.CreateNode("gaussian");
            var mesh = model.CreateMesh("gaussian-mesh");

            node.Mesh = mesh;

            var primitive = mesh.CreatePrimitive().WithIndicesAutomatic(PrimitiveType.POINTS);
            primitive.WithVertexAccessor("POSITION", _Positions);
            primitive.WithVertexAccessor(GaussianSplatting.AttributeRotation, _Rotations);
            primitive.WithVertexAccessor(GaussianSplatting.AttributeScale, _Scales);
            primitive.WithVertexAccessor(GaussianSplatting.AttributeOpacity, _Opacities);
            primitive.WithVertexAccessor(GaussianSplatting.AttributeSHDegree0Coef0, _Sh0);

            return primitive;
        }
    }
}
