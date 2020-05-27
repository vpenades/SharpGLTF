using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

using SharpGLTF.Schema2;
using SharpGLTF.Validation;

namespace SharpGLTF.Materials
{
    [Category("Toolkit.Materials")]
    public class MaterialBuilderTests
    {
        [Test]
        public void CreateUnlit()
        {
            var srcMaterial = new MaterialBuilder()
                .WithDoubleSide(true) // notice that DoubleSide enables double face rendering. This is an example, but it's usually NOT NECCESARY.
                .WithAlpha(AlphaMode.MASK, 0.7f)
                .WithUnlitShader()
                .WithBaseColor(Memory.MemoryImage.DefaultPngImage, new Vector4(0.7f, 0, 0f, 0.8f));

            Assert.IsTrue(MaterialBuilder.AreEqualByContent(srcMaterial, Schema2Roundtrip(srcMaterial)));
            Assert.IsTrue(MaterialBuilder.AreEqualByContent(srcMaterial, srcMaterial.Clone()));
        }

        [Test]
        public void CreateMetallicRoughness()
        {
            var srcMaterial = new MaterialBuilder()
                .WithDoubleSide(true) // notice that DoubleSide enables double face rendering. This is an example, but it's usually NOT NECCESARY.
                .WithAlpha(AlphaMode.MASK, 0.6f)
                .WithEmissive(Memory.MemoryImage.DefaultPngImage, new Vector3(0.2f, 0.3f, 0.1f))
                .WithNormal(Memory.MemoryImage.DefaultPngImage, 0.3f)
                .WithOcclusion(Memory.MemoryImage.DefaultPngImage, 0.4f)

                .WithMetallicRoughnessShader()
                    .WithBaseColor(Memory.MemoryImage.DefaultPngImage, new Vector4(0.7f, 0, 0f, 0.8f))
                    .WithMetallicRoughness(Memory.MemoryImage.DefaultPngImage, 0.2f, 0.4f);

            // example of setting additional additional parameters for a given channel.
            srcMaterial.GetChannel(KnownChannel.BaseColor)
                .Texture
                .WithCoordinateSet(1)
                .WithSampler(TextureWrapMode.CLAMP_TO_EDGE, TextureWrapMode.MIRRORED_REPEAT, TextureMipMapFilter.LINEAR_MIPMAP_LINEAR, TextureInterpolationFilter.NEAREST)
                .WithTransform(Vector2.One*0.2f, Vector2.One*0.3f,0.1f, 2);
                

            Assert.IsTrue(MaterialBuilder.AreEqualByContent(srcMaterial, Schema2Roundtrip(srcMaterial)));
            Assert.IsTrue(MaterialBuilder.AreEqualByContent(srcMaterial, srcMaterial.Clone()));            
        }

        [Test]
        public void CreateClearCoat()
        {
            var srcMaterial = new MaterialBuilder()
                .WithDoubleSide(true) // notice that DoubleSide enables double face rendering. This is an example, but it's usually NOT NECCESARY.
                .WithAlpha(AlphaMode.MASK, 0.6f)
                .WithEmissive(Memory.MemoryImage.DefaultPngImage, new Vector3(0.2f, 0.3f, 0.1f))
                .WithNormal(Memory.MemoryImage.DefaultPngImage, 0.3f)
                .WithOcclusion(Memory.MemoryImage.DefaultPngImage, 0.4f)

                .WithMetallicRoughnessShader()
                    .WithBaseColor(Memory.MemoryImage.DefaultPngImage, new Vector4(0.7f, 0, 0f, 0.8f))
                    .WithMetallicRoughness(Memory.MemoryImage.DefaultPngImage, 0.2f, 0.4f)

                .WithClearCoat(Memory.MemoryImage.DefaultPngImage, 1)
                .WithClearCoatNormal(Memory.MemoryImage.DefaultPngImage)
                .WithClearCoatRoughness(Memory.MemoryImage.DefaultPngImage, 1);


            Assert.IsTrue(MaterialBuilder.AreEqualByContent(srcMaterial, Schema2Roundtrip(srcMaterial)));
            Assert.IsTrue(MaterialBuilder.AreEqualByContent(srcMaterial, srcMaterial.Clone()));
        }

        [Test]
        public void CreateSpecularGlossiness()
        {
            var srcMaterial = new MaterialBuilder()
                .WithDoubleSide(true) // notice that DoubleSide enables double face rendering. This is an example, but it's usually NOT NECCESARY.
                .WithAlpha(AlphaMode.MASK, 0.6f)
                .WithEmissive(Memory.MemoryImage.DefaultPngImage, new Vector3(0.2f, 0.3f, 0.1f))
                .WithNormal(Memory.MemoryImage.DefaultPngImage, 0.3f)
                .WithOcclusion(Memory.MemoryImage.DefaultPngImage, 0.4f)

                .WithSpecularGlossinessShader()
                    .WithDiffuse(Memory.MemoryImage.DefaultPngImage, new Vector4(0.7f, 0, 0f, 0.8f))
                    .WithSpecularGlossiness(Memory.MemoryImage.DefaultPngImage, new Vector3(0.7f, 0, 0f), 0.8f);
                
            
            Assert.IsTrue(MaterialBuilder.AreEqualByContent(srcMaterial, Schema2Roundtrip(srcMaterial)));
            Assert.IsTrue(MaterialBuilder.AreEqualByContent(srcMaterial, srcMaterial.Clone()));
        }

        [Test]
        public void CreateSpecularGlossinessWithFallback()
        {
            var primary = new MaterialBuilder("primary")

                // fallback and primary material must have exactly the same properties
                .WithDoubleSide(true)
                .WithAlpha(AlphaMode.MASK, 0.75f)
                .WithEmissive(Memory.MemoryImage.DefaultPngImage, new Vector3(0.2f, 0.3f, 0.1f))
                .WithNormal(Memory.MemoryImage.DefaultPngImage, 0.3f)
                .WithOcclusion(Memory.MemoryImage.DefaultPngImage, 0.4f)

                // primary must use Specular Glossiness shader.
                .WithSpecularGlossinessShader()
                    .WithDiffuse(Memory.MemoryImage.DefaultPngImage, new Vector4(0.7f, 0, 0f, 1.0f))
                    .WithSpecularGlossiness(Memory.MemoryImage.DefaultPngImage, new Vector3(0.7f, 0, 0f), 0.8f)                

                .WithMetallicRoughnessFallback(Memory.MemoryImage.DefaultPngImage, new Vector4(0.7f, 0, 0, 1), String.Empty, 0.6f, 0.7f);

            Assert.IsTrue(MaterialBuilder.AreEqualByContent(primary, Schema2Roundtrip(primary)));
            Assert.IsTrue(MaterialBuilder.AreEqualByContent(primary, primary.Clone()));
        }

        private static MaterialBuilder Schema2Roundtrip(MaterialBuilder srcMaterial)
        {
            // converts a MaterialBuilder to a Schema2.Material and back to a MaterialBuilder

            var dstModel = Schema2.ModelRoot.CreateModel();
            var dstMaterial = dstModel.CreateMaterial(srcMaterial.Name);

            srcMaterial.CopyTo(dstMaterial); // copy MaterialBuilder to Schema2.Material.

            var ctx = new ValidationResult(dstModel,ValidationMode.Strict, true);
            dstModel.ValidateReferences(ctx.GetContext());
            dstModel.ValidateContent(ctx.GetContext());

            var rtpMaterial = new MaterialBuilder(dstMaterial.Name);

            dstMaterial.CopyTo(rtpMaterial);// copy Schema2.Material to MaterialBuilder.

            return rtpMaterial;
        }
    }
}
