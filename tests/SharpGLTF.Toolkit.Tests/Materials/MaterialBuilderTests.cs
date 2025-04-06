using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;

namespace SharpGLTF.Materials
{
    [Category("Toolkit.Materials")]
    public class MaterialBuilderTests
    {
        [Test]
        public void TestMaterialEquality()
        {
            // Checking if two materials are the same or not is conceptually ambiguous.
            // The static method AreEqualByContent allows to check if two materials represent
            // the same physical material, even if they're two different references.
            // ... And we could use it for general equality checks, but then, since
            // MaterialBuilder is NOT inmutable, it can mean that two materials can be equal
            // at a given time, and non equal at another. Furthermore, it would imply having
            // a hash code that changes over time. As a consequence, using MaterialBuilder as
            // a dictionary key is possible, but dangerous if not carefully handled.

            var srcMaterial = _CreateUnlitMaterial();

            var clnMaterial = srcMaterial.Clone();

            // srcMaterial and clnMaterial are two different objects, so plain equality checks must apply to reference checks
            Assert.That(clnMaterial != srcMaterial);
            Assert.That(clnMaterial, Is.Not.EqualTo(srcMaterial));
            Assert.That(clnMaterial.GetHashCode(), Is.Not.EqualTo(srcMaterial.GetHashCode()));

            // checking the materials represent the same "material" must be made with AreEqualByContent method.
            Assert.That(MaterialBuilder.AreEqualByContent(srcMaterial, clnMaterial));

            var bag = new HashSet<MaterialBuilder>();
            bag.Add(srcMaterial);
            bag.Add(clnMaterial);

            Assert.That(bag, Has.Count.EqualTo(2));
        }        

        [Test]
        public void CreateUnlit()
        {
            var material = _CreateUnlitMaterial();

            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)));
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()));
        }

        private static MaterialBuilder _CreateUnlitMaterial()
        {
            var tex1 = ResourceInfo.From("shannon.png").FilePath;

            var material = new MaterialBuilder("Unlit Material")
                .WithDoubleSide(true) // notice that DoubleSide enables double face rendering. This is an example, but it's usually NOT NECCESARY.
                .WithAlpha(AlphaMode.MASK, 0.7f);

            material.WithUnlitShader()
                .WithBaseColor(tex1, new Vector4(0.7f, 0, 0f, 0.8f));

            return material;
        }

        [Test]
        public void CreateMetallicRoughness()
        {
            var material = _CreateMetallicRoughnessMaterial();

            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)));
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()));
        }        

        private static MaterialBuilder _CreateMetallicRoughnessMaterial()
        {
            var tex1 = ResourceInfo.From("shannon.png").FilePath;

            var material = new MaterialBuilder("Metallic Roughness Material")
                .WithAlpha(AlphaMode.MASK, 0.6f) // Note: this is just an example, for a default opaque material, use WithAlpha(AlphaMode.OPAQUE, 0.5)
                .WithEmissive(tex1, new Vector3(0.2f, 0.3f, 0.1f), 6)
                .WithNormal(tex1, 0.3f)
                .WithOcclusion(tex1, 0.4f);

            material.WithMetallicRoughnessShader()
                .WithBaseColor(tex1, new Vector4(0.7f, 0, 0f, 0.8f))
                .WithMetallicRoughness(tex1, 0.2f, 0.4f);                

            // example of setting additional parameters for a given channel.
            material.GetChannel(KnownChannel.BaseColor)
                .Texture
                .WithCoordinateSet(1)
                .WithSampler(TextureWrapMode.CLAMP_TO_EDGE, TextureWrapMode.MIRRORED_REPEAT, TextureMipMapFilter.LINEAR_MIPMAP_LINEAR, TextureInterpolationFilter.NEAREST)
                .WithTransform(Vector2.One * 0.2f, Vector2.One * 0.3f, 0.1f, 2);

            return material;
        }

        [Test]
        public void CreateMaterialWithExtensions()
        {
            var material = _CreateMetallicRoughnessMaterial();

            // https://github.com/vpenades/SharpGLTF/issues/246
            material.IndexOfRefraction = 7;

            material.Dispersion = 2;

            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)));
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()));

            _AddClearCoat(material);

            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)));
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()));

            _AddVolume(material);

            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)));
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()));                        

            _AddIridescence(material);

            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)));
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()));

            _AddAnisotropy(material);

            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)));
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()));

            _AddTransmission(material);

            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)));
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()));

            _AddDiffuseTransmission(material);

            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)));
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()));
        }

        private static void _AddVolume(MaterialBuilder material)
        {
            var tex1 = ResourceInfo.From("shannon.png").FilePath;            

            material.WithVolumeAttenuation(Vector3.One * 0.3f, 0.6f)
                .WithVolumeThickness(tex1, 0.4f);            
        }        

        private static void _AddClearCoat(MaterialBuilder material)
        {            
            var tex1 = ResourceInfo.From("shannon.png").FilePath;            

            material.WithClearCoat(tex1, 0.9f)
                .WithClearCoatNormal(tex1)
                .WithClearCoatRoughness(tex1, 0.9f);
        }       

        private static void _AddIridescence(MaterialBuilder material)
        {
            material
                .WithIridescence(default, 0.2f, 1.5f)
                .WithIridescenceThickness(default, 120, 330);
        }

        private static void _AddAnisotropy(MaterialBuilder material)
        {
            var tex1 = ResourceInfo.From("shannon.png").FilePath;

            material.WithAnisotropy(tex1, 0.2f, 3f);
        }

        private static void _AddTransmission(MaterialBuilder material)
        {
            var tex1 = ResourceInfo.From("shannon.png").FilePath;

            material.WithTransmission(tex1, 0.9f);
        }

        private static void _AddDiffuseTransmission(MaterialBuilder material)
        {
            var tex1 = ResourceInfo.From("shannon.png").FilePath;

            material.WithDiffuseTransmissionFactor(tex1, 0.9f);
            material.WithDiffuseTransmissionColor(tex1, Vector3.One * 0.2f);
        }        

        private static MaterialBuilder _Schema2Roundtrip(MaterialBuilder srcMaterial)
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

        #region obsolete (kept for backwards compatibility)

        [Test]
        public void CreateSpecularGlossiness()
        {
            var material = _CreateSpecularGlossinessMaterial();

            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)));
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()));
        }

        [Test]
        public void CreateSpecularGlossinessWithFallback()
        {
            var material = _CreateSpecularGlossinessMaterialWithFallback();

            // check
            Assert.That(MaterialBuilder.AreEqualByContent(material, _Schema2Roundtrip(material)), Is.True);
            Assert.That(MaterialBuilder.AreEqualByContent(material, material.Clone()), Is.True);
        }

        [Obsolete("SpecularGlossiness has been deprecated by Khronos")]
        private static MaterialBuilder _CreateSpecularGlossinessMaterialWithFallback()
        {

            var tex1 = ResourceInfo.From("shannon.webp").FilePath;
            var tex2 = ResourceInfo.From("shannon.png").FilePath;

            var primary = new MaterialBuilder("primary")

                // fallback and primary material must have exactly the same properties
                .WithDoubleSide(true)
                .WithAlpha(AlphaMode.MASK, 0.75f)
                .WithEmissive(tex1, new Vector3(0.2f, 0.3f, 0.1f), 4)
                .WithNormal(tex1, 0.3f)
                .WithOcclusion(tex1, 0.4f);

            // primary must use Specular Glossiness shader.
            primary.WithSpecularGlossinessShader()
                .WithDiffuse(tex1, new Vector4(0.7f, 0, 0f, 1.0f))
                .WithSpecularGlossiness(tex1, new Vector3(0.7f, 0, 0f), 0.8f);

            // set fallback textures for engines that don't support WEBP texture format
            primary.GetChannel(KnownChannel.Normal).Texture.FallbackImage = tex2;
            primary.GetChannel(KnownChannel.Emissive).Texture.FallbackImage = tex2;
            primary.GetChannel(KnownChannel.Occlusion).Texture.FallbackImage = tex2;
            primary.GetChannel(KnownChannel.Diffuse).Texture.FallbackImage = tex2;
            primary.GetChannel(KnownChannel.SpecularGlossiness).Texture.FallbackImage = tex2;

            // set fallback material for engines that don't support Specular Glossiness shader.
            primary.WithMetallicRoughnessFallback(tex1, new Vector4(0.7f, 0, 0, 1), String.Empty, 0.6f, 0.7f);
            primary.CompatibilityFallback.GetChannel(KnownChannel.BaseColor).Texture.FallbackImage = tex2;

            return primary;
        }

        [Obsolete("SpecularGlossiness has been deprecated by Khronos")]
        private static MaterialBuilder _CreateSpecularGlossinessMaterial()
        {

            var tex1 = ResourceInfo.From("shannon.png").FilePath;

            var material = new MaterialBuilder()
                .WithAlpha(AlphaMode.MASK, 0.6f)
                .WithEmissive(tex1, new Vector3(0.2f, 0.3f, 0.1f))
                .WithNormal(tex1, 0.3f)
                .WithOcclusion(tex1, 0.4f);

            material.WithSpecularGlossinessShader()
                .WithDiffuse(tex1, new Vector4(0.7f, 0, 0f, 0.8f))
                .WithSpecularGlossiness(tex1, new Vector3(0.7f, 0, 0f), 0.8f);

            return material;
        }

        #endregion
    }
}
