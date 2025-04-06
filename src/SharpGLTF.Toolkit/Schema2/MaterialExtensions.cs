using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Materials;

using ALPHAMODE = SharpGLTF.Materials.AlphaMode;
using ALPHAMODEGLTF2 = SharpGLTF.Schema2.AlphaMode;

namespace SharpGLTF.Schema2
{
    public static partial class Toolkit
    {
        #region Fluent API

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with default material attributes.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> instance to set.</param>
        /// <returns>This <see cref="Material"/> instance.</returns>
        public static Material WithDefault(this Material material)
        {
            Guard.NotNull(material, nameof(material));

            return material.WithPBRMetallicRoughness();
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with default material attributes.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> instance to set.</param>
        /// <param name="diffuseColor">A <see cref="Vector4"/> color where X=Red, Y=Green, Z=Blue, W=Alpha.</param>
        /// <returns>This <see cref="Material"/> instance.</returns>
        public static Material WithDefault(this Material material, Vector4 diffuseColor)
        {
            Guard.NotNull(material, nameof(material));

            var ch = material.WithPBRMetallicRoughness().FindChannel("BaseColor").Value;

            ch.Color = diffuseColor;

            return material;
        }

        public static Material WithDoubleSide(this Material material, bool enabled)
        {
            Guard.NotNull(material, nameof(material));

            material.DoubleSided = enabled;
            return material;
        }

        [Obsolete("don't use vector4 based parameter. Use WithChannelColor and WithChannelFactor instead.")]
        public static Material WithChannelParameter(this Material material, string channelName, Vector4 parameter)
        {
            Guard.NotNull(material, nameof(material));

            var channel = material.FindChannel(channelName).Value;

            channel.Parameter = parameter;

            return material;
        }

        public static Material WithChannelColor(this Material material, string channelName, Vector4 color)
        {
            Guard.NotNull(material, nameof(material));

            var channel = material.FindChannel(channelName).Value;

            channel.Color = color;

            return material;
        }

        public static Material WithChannelFactor(this Material material, string channelName, string paramName, float factor)
        {
            Guard.NotNull(material, nameof(material));

            var channel = material.FindChannel(channelName).Value;

            channel.SetFactor(paramName, factor);

            return material;
        }

        public static Material WithChannelTexture(this Material material, string channelName, int textureSet, string imageFilePath)
        {
            Guard.NotNull(material, nameof(material));

            var image = material.LogicalParent.UseImageWithFile(imageFilePath);

            return material.WithChannelTexture(channelName, textureSet, image);
        }

        public static Material WithChannelTexture(this Material material, string channelName, int textureSet, Image image)
        {
            Guard.NotNull(material, nameof(material));

            var channel = material.FindChannel(channelName).Value;

            channel.SetTexture(textureSet, image);

            return material;
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Metallic Roughness attributes.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> instance to set.</param>
        /// <returns>This <see cref="Material"/> instance.</returns>
        public static Material WithPBRMetallicRoughness(this Material material)
        {
            Guard.NotNull(material, nameof(material));

            material.InitializePBRMetallicRoughness();
            return material;
        }

        public static Material WithPBRMetallicRoughness(
            this Material material,
            Vector4 baseColor,
            string baseColorImageFilePath,
            string metallicImageFilePath = null,
            float metallicFactor = 1,
            float roughnessFactor = 1
            )
        {
            Guard.NotNull(material, nameof(material));

            material
                .WithPBRMetallicRoughness()
                .WithChannelColor("BaseColor", baseColor)
                .WithChannelFactor("MetallicRoughness", "MetallicFactor", metallicFactor)
                .WithChannelFactor("MetallicRoughness", "RoughnessFactor", roughnessFactor);

            if (!string.IsNullOrWhiteSpace(baseColorImageFilePath)) material.WithChannelTexture("BaseColor", 0, baseColorImageFilePath);
            if (!string.IsNullOrWhiteSpace(metallicImageFilePath)) material.WithChannelTexture("Metallic", 0, baseColorImageFilePath);

            return material;
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Specular Glossiness attributes.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> instance to set.</param>
        /// <returns>This <see cref="Material"/> instance.</returns>
        [Obsolete("SpecularGlossiness Extension has been declared deprecated by the Khronos Group. Use newer extensions instead.")]
        public static Material WithPBRSpecularGlossiness(this Material material)
        {
            Guard.NotNull(material, nameof(material));

            material.InitializePBRSpecularGlossiness();
            return material;
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with Unlit attributes.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> instance to set.</param>
        /// <returns>This <see cref="Material"/> instance.</returns>
        public static Material WithUnlit(this Material material)
        {
            Guard.NotNull(material, nameof(material));

            material.InitializeUnlit();
            return material;
        }

        /// <summary>
        /// Creates or reuses an <see cref="Image"/> with the file set by <paramref name="filePath"/>
        /// </summary>
        /// <param name="root">The <see cref="ModelRoot"/> root instance.</param>
        /// <param name="filePath">A valid file path pointing to a valid image</param>
        /// <returns>A <see cref="Image"/> instance.</returns>
        public static Image UseImageWithFile(this ModelRoot root, string filePath)
        {
            var content = System.IO.File.ReadAllBytes(filePath);

            return root.UseImageWithContent(content);
        }

        /// <summary>
        /// Creates or reuses an <see cref="Image"/> with the image content set by <paramref name="image"/>
        /// </summary>
        /// <param name="root">The <see cref="ModelRoot"/> root instance.</param>
        /// <param name="image">A buffer containing the bytes of the image file.</param>
        /// <returns>A <see cref="Image"/> instance.</returns>
        public static Image UseImageWithContent(this ModelRoot root, Memory.MemoryImage image)
        {
            Guard.NotNull(root, nameof(root));

            return root.UseImage(image);
        }

        #endregion

        #region creation API

        public static Material CreateMaterial(this ModelRoot root, MaterialBuilder mb)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(mb, nameof(mb));

            var m = root.CreateMaterial();

            mb.CopyTo(m);

            return m;
        }

        public static MaterialBuilder ToMaterialBuilder(this Material srcMaterial)
        {
            if (srcMaterial == null) return Materials.MaterialBuilder.CreateDefault();
            var dstMaterial = new Materials.MaterialBuilder(srcMaterial.Name);

            srcMaterial.CopyTo(dstMaterial);

            return dstMaterial;
        }

        #endregion

        #region transfer API

        public static ALPHAMODEGLTF2 ToSchema2(this ALPHAMODE alpha)
        {
            switch (alpha)
            {
                case ALPHAMODE.BLEND: return ALPHAMODEGLTF2.BLEND;
                case ALPHAMODE.MASK: return ALPHAMODEGLTF2.MASK;
                case ALPHAMODE.OPAQUE: return ALPHAMODEGLTF2.OPAQUE;
                default: throw new NotImplementedException(alpha.ToString());
            }
        }

        public static ALPHAMODE ToToolkit(this ALPHAMODEGLTF2 alpha)
        {
            switch (alpha)
            {
                case ALPHAMODEGLTF2.BLEND: return ALPHAMODE.BLEND;
                case ALPHAMODEGLTF2.MASK: return ALPHAMODE.MASK;
                case ALPHAMODEGLTF2.OPAQUE: return ALPHAMODE.OPAQUE;
                default: throw new NotImplementedException(alpha.ToString());
            }
        }

        public static void CopyTo(this Material srcMaterial, MaterialBuilder dstMaterial)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));

            _CopyDefaultTo(srcMaterial, dstMaterial);

            if (srcMaterial.Unlit)
            {
                dstMaterial.WithUnlitShader();
                srcMaterial.CopyChannelsTo(dstMaterial, "BaseColor");
                return;
            }

            if (srcMaterial.FindChannel("Diffuse") != null || srcMaterial.FindChannel("SpecularGlossiness") != null)
            {
                #pragma warning disable CS0618 // Type or member is obsolete

                dstMaterial.WithSpecularGlossinessShader();
                srcMaterial.CopyChannelsTo(dstMaterial, "Diffuse", "SpecularGlossiness");
                // srcMaterial.CopyChannelsTo(dstMaterial, "ClearCoat", "ClearCoatRoughness", "ClearCoatNormal");

                if (srcMaterial.FindChannel("BaseColor") != null || srcMaterial.FindChannel("MetallicRoughness") != null)
                {
                    var fallback = new MaterialBuilder(srcMaterial.Name);

                    _CopyDefaultTo(srcMaterial, fallback);
                    _CopyMetallicRoughnessTo(srcMaterial, fallback);

                    dstMaterial.WithFallback(fallback);
                }

                return;
                #pragma warning restore CS0618 // Type or member is obsolete
            }

            if (srcMaterial.FindChannel("BaseColor") != null || srcMaterial.FindChannel("MetallicRoughness") != null)
            {
                _CopyMetallicRoughnessTo(srcMaterial, dstMaterial);
            }
        }

        private static void _CopyMetallicRoughnessTo(Material srcMaterial, MaterialBuilder dstMaterial)
        {
            dstMaterial.WithMetallicRoughnessShader();

            var channels = MaterialBuilder._MetRouChannels
                .Select(item => item.ToString())
                .ToArray();

            srcMaterial.CopyChannelsTo(dstMaterial, channels);
        }

        private static void _CopyDefaultTo(Material srcMaterial, MaterialBuilder dstMaterial)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));

            dstMaterial.SetNameAndExtrasFrom(srcMaterial);

            dstMaterial.AlphaMode = srcMaterial.Alpha.ToToolkit();
            dstMaterial.AlphaCutoff = srcMaterial.AlphaCutoff;
            dstMaterial.DoubleSided = srcMaterial.DoubleSided;
            dstMaterial.Dispersion = srcMaterial.Dispersion;
            dstMaterial.IndexOfRefraction = srcMaterial.IndexOfRefraction;
            

            srcMaterial.CopyChannelsTo(dstMaterial, "Normal", "Occlusion", "Emissive");
        }

        public static void CopyChannelsTo(this Material srcMaterial, MaterialBuilder dstMaterial, params string[] channelKeys)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));
            Guard.NotNull(channelKeys, nameof(channelKeys));

            foreach (var k in channelKeys)
            {
                if (!Enum.TryParse<KnownChannel>(k, true, out var knownChannel)) continue;

                var src = srcMaterial.FindChannel(k);
                if (!src.HasValue) continue;                

                if (src.Value.HasDefaultContent) continue;                

                var dst = dstMaterial.UseChannel(knownChannel);

                src.Value.CopyTo(dst);
            }
        }

        public static void CopyTo(this MaterialChannel srcChannel, ChannelBuilder dstChannel)
        {
            Guard.NotNull(srcChannel, nameof(srcChannel));
            Guard.NotNull(dstChannel, nameof(dstChannel));

            foreach (var srcProp in srcChannel.Parameters)
            {
                dstChannel.Parameters[srcProp.Name] = MaterialValue.CreateFrom(srcProp.Value);
            }

            if (srcChannel.Texture == null) return;
            if (dstChannel.Texture == null) dstChannel.UseTexture();

            dstChannel.Texture.SetNameAndExtrasFrom(srcChannel.Texture);

            dstChannel.Texture.CoordinateSet = srcChannel.TextureCoordinate;

            if (srcChannel.TextureSampler != null)
            {
                dstChannel.Texture.MinFilter = srcChannel.TextureSampler.MinFilter;
                dstChannel.Texture.MagFilter = srcChannel.TextureSampler.MagFilter;
                dstChannel.Texture.WrapS = srcChannel.TextureSampler.WrapS;
                dstChannel.Texture.WrapT = srcChannel.TextureSampler.WrapT;
            }

            var srcXform = srcChannel.TextureTransform;

            if (srcXform != null)
            {
                dstChannel.Texture.WithTransform(srcXform.Offset, srcXform.Scale, srcXform.Rotation, srcXform.TextureCoordinateOverride);
            }

            ImageBuilder _convert(Image src)
            {
                if (src == null) return null;
                var dst = ImageBuilder.From(src.Content, src.Name, src.Extras?.DeepClone());
                dst.AlternateWriteFileName = src.AlternateWriteFileName;
                return dst;
            }

            dstChannel.Texture.PrimaryImage = _convert(srcChannel.Texture.PrimaryImage);
            dstChannel.Texture.FallbackImage = _convert(srcChannel.Texture.FallbackImage);
        }

        public static void CopyTo(this MaterialBuilder srcMaterial, Material dstMaterial)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));

            srcMaterial.ValidateForSchema2();

            srcMaterial.TryCopyNameAndExtrasTo(dstMaterial);

            dstMaterial.Alpha = srcMaterial.AlphaMode.ToSchema2();
            dstMaterial.AlphaCutoff = dstMaterial.Alpha == ALPHAMODEGLTF2.MASK ? srcMaterial.AlphaCutoff : 0.5f;
            dstMaterial.DoubleSided = srcMaterial.DoubleSided;            

            var hasClearCoat
                =  srcMaterial.GetChannel(KnownChannel.ClearCoat) != null
                || srcMaterial.GetChannel(KnownChannel.ClearCoatNormal) != null
                || srcMaterial.GetChannel(KnownChannel.ClearCoatRoughness) != null;            

            var hasSheen
                =  srcMaterial.GetChannel(KnownChannel.SheenColor) != null
                || srcMaterial.GetChannel(KnownChannel.SheenRoughness) != null;

            var hasSpecular
                =  srcMaterial.GetChannel(KnownChannel.SpecularColor) != null
                || srcMaterial.GetChannel(KnownChannel.SpecularFactor) != null;

            var hasVolume
                =  srcMaterial.GetChannel(KnownChannel.VolumeThickness) != null
                || srcMaterial.GetChannel(KnownChannel.VolumeAttenuation) != null;

            var hasIridescence
                =  srcMaterial.GetChannel(KnownChannel.Iridescence) != null
                || srcMaterial.GetChannel(KnownChannel.IridescenceThickness) != null;

            var hasAnisotropy = srcMaterial.GetChannel(KnownChannel.Anisotropy) != null;

            var hasTransmission = srcMaterial.GetChannel(KnownChannel.Transmission) != null;

            var hasDiffuseTransmission
                =  srcMaterial.GetChannel(KnownChannel.DiffuseTransmissionColor) != null
                || srcMaterial.GetChannel(KnownChannel.DiffuseTransmissionFactor) != null;
            
            srcMaterial.CopyChannelsTo(dstMaterial, KnownChannel.Normal, KnownChannel.Occlusion, KnownChannel.Emissive);

            MaterialBuilder defMaterial = null;

            if (srcMaterial.ShaderStyle == "Unlit")
            {
                dstMaterial.InitializeUnlit();
                srcMaterial.CopyChannelsTo(dstMaterial, KnownChannel.BaseColor);
                return;
            }

            if (srcMaterial.ShaderStyle == "PBRMetallicRoughness")
            {
                dstMaterial.InitializePBRMetallicRoughness
                    (                    
                    hasSheen ? "Sheen" : null,                    
                    hasVolume ? "Volume" : null,
                    hasSpecular ? "Specular" : null,
                    hasClearCoat ? "ClearCoat" : null,
                    hasAnisotropy ? "Anisotropy" : null,
                    hasIridescence ? "Iridescence" : null,                    
                    hasTransmission ? "Transmission" : null,
                    hasDiffuseTransmission ? "DiffuseTransmission" : null
                    );

                defMaterial = srcMaterial;
            }

            if (srcMaterial.ShaderStyle == "PBRSpecularGlossiness")
            {
                dstMaterial.InitializePBRSpecularGlossiness(srcMaterial.CompatibilityFallback != null);
                srcMaterial.CopyChannelsTo(dstMaterial, KnownChannel.Diffuse, KnownChannel.SpecularGlossiness);
                defMaterial = srcMaterial.CompatibilityFallback;
            }

            // Dispersion must be set after dst material initialization,
            // otherwise it's erased since it's stored in an extension
            dstMaterial.Dispersion = srcMaterial.Dispersion;

            // IOR must be set after dst material initialization,
            // otherwise it's erased since it's stored in an extension
            dstMaterial.IndexOfRefraction = srcMaterial.IndexOfRefraction;

            if (defMaterial != null)
            {
                if (defMaterial.ShaderStyle != "PBRMetallicRoughness") throw new ArgumentException(nameof(srcMaterial.CompatibilityFallback.ShaderStyle));
                defMaterial.CopyChannelsTo(dstMaterial, KnownChannel.BaseColor, KnownChannel.MetallicRoughness);
                defMaterial.CopyChannelsTo(dstMaterial, KnownChannel.ClearCoat, KnownChannel.ClearCoatNormal, KnownChannel.ClearCoatRoughness);
                defMaterial.CopyChannelsTo(dstMaterial, KnownChannel.Transmission);
                defMaterial.CopyChannelsTo(dstMaterial, KnownChannel.SheenColor, KnownChannel.SheenRoughness);
                defMaterial.CopyChannelsTo(dstMaterial, KnownChannel.SpecularColor, KnownChannel.SpecularFactor);
                defMaterial.CopyChannelsTo(dstMaterial, KnownChannel.VolumeThickness, KnownChannel.VolumeAttenuation);
                defMaterial.CopyChannelsTo(dstMaterial, KnownChannel.Iridescence, KnownChannel.IridescenceThickness);
                defMaterial.CopyChannelsTo(dstMaterial, KnownChannel.Anisotropy);
                defMaterial.CopyChannelsTo(dstMaterial, KnownChannel.DiffuseTransmissionColor, KnownChannel.DiffuseTransmissionFactor);
            }

            // final validation

            System.Diagnostics.Debug.Assert(dstMaterial.Dispersion == srcMaterial.Dispersion, "set Dispersion after dst material initialization");
            System.Diagnostics.Debug.Assert(dstMaterial.IndexOfRefraction == srcMaterial.IndexOfRefraction, "set IOR after dst material initialization");            
        }

        [Obsolete]
        public static void CopyChannelsTo(this MaterialBuilder srcMaterial, Material dstMaterial, params string[] channels)
        {
            var channelKeys = channels.Select(key => Enum.TryParse<KnownChannel>(key, out var val) ? val : default).ToArray();
            CopyChannelsTo(srcMaterial, dstMaterial, channelKeys);
        }

        public static void CopyChannelsTo(this MaterialBuilder srcMaterial, Material dstMaterial, params KnownChannel[] channels)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));
            Guard.NotNull(channels, nameof(channels));

            foreach (var k in channels)
            {
                var src = srcMaterial.GetChannel(k);
                if (src == null) continue;

                var dst = dstMaterial.FindChannel(k.ToString());
                if (dst == null) continue;

                src.CopyTo(dst.Value);
            }
        }

        public static void CopyTo(this ChannelBuilder srcChannel, MaterialChannel dstChannel)
        {
            Guard.NotNull(srcChannel, nameof(srcChannel));
            Guard.NotNull(dstChannel, nameof(dstChannel));

            foreach (var dstProp in dstChannel.Parameters)
            {
                dstProp.Value = srcChannel.Parameters[dstProp.Name].ToTypeless();
            }

            var srcTex = srcChannel.GetValidTexture();
            if (srcTex == null) return;

            Image primary = null;
            Image fallback = null;

            if (ImageBuilder.IsValid(srcTex.PrimaryImage))
            {
                primary = _ConvertToImage(dstChannel, srcTex.PrimaryImage);
            }

            if (primary == null) return;

            if (ImageBuilder.IsValid(srcTex.FallbackImage))
            {
                fallback = _ConvertToImage(dstChannel, srcTex.FallbackImage);
            }

            var dstTex = dstChannel.SetTexture(srcTex.CoordinateSet, primary, fallback, srcTex.WrapS, srcTex.WrapT, srcTex.MinFilter, srcTex.MagFilter);

            srcTex.TryCopyNameAndExtrasTo(dstTex);

            var srcXform = srcTex.Transform;

            if (srcXform != null)
            {
                dstChannel.SetTransform(srcXform.Offset, srcXform.Scale, srcXform.Rotation, srcXform.CoordinateSetOverride);
            }
        }

        private static Image _ConvertToImage(MaterialChannel dstChannel, ImageBuilder srcImage)
        {
            var dstImage = dstChannel
                    .LogicalParent
                    .LogicalParent
                    .UseImageWithContent(srcImage.Content);

            dstImage.AlternateWriteFileName = srcImage.AlternateWriteFileName;

            srcImage.TryCopyNameAndExtrasTo(dstImage);

            return dstImage;
        }

        #endregion

        #region evaluation API

        public static Vector4 GetDiffuseColor(this Material material, Vector4 defaultColor)
        {
            if (material == null) return defaultColor;

            var channel = material.FindChannel("Diffuse");
            if (channel.HasValue) return channel.Value.Color;

            channel = material.FindChannel("BaseColor");
            if (channel.HasValue) return channel.Value.Color;

            return defaultColor;
        }

        public static Texture GetDiffuseTexture(this Material material)
        {
            if (material == null) return null;

            var channel = material.FindChannel("Diffuse");
            if (channel.HasValue) return channel.Value.Texture;

            channel = material.FindChannel("BaseColor");
            if (channel.HasValue) return channel.Value.Texture;

            return null;
        }

        public static TextureTransform GetDiffuseTextureTransform(this Material material)
        {
            if (material == null) return null;

            var channel = material.FindChannel("Diffuse");
            if (channel.HasValue) return channel.Value.TextureTransform;

            channel = material.FindChannel("BaseColor");
            if (channel.HasValue) return channel.Value.TextureTransform;

            return null;
        }

        public static Matrix3x2? GetDiffuseTextureMatrix(this Material material, Animation track, float time)
        {
            var xform = material.GetDiffuseTextureTransform();

            return xform?.Matrix;

            // return xform.GetMatrix(track, time);
        }

        #endregion
    }
}

