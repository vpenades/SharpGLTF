using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Materials;

namespace SharpGLTF.Schema2
{
    public static partial class Schema2Toolkit
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

            ch.Parameter = diffuseColor;

            return material;
        }

        public static Material WithDoubleSide(this Material material, bool enabled)
        {
            Guard.NotNull(material, nameof(material));

            material.DoubleSided = enabled;
            return material;
        }

        public static Material WithChannelParameter(this Material material, string channelName, Vector4 parameter)
        {
            Guard.NotNull(material, nameof(material));

            var channel = material.FindChannel(channelName).Value;

            channel.Parameter = parameter;

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
            material.WithPBRMetallicRoughness();

            material.WithChannelParameter("BaseColor", baseColor);
            material.WithChannelParameter("MetallicRoughness", new Vector4(metallicFactor, roughnessFactor, 0, 0));

            if (!string.IsNullOrWhiteSpace(baseColorImageFilePath)) material.WithChannelTexture("BaseColor", 0, baseColorImageFilePath);
            if (!string.IsNullOrWhiteSpace(metallicImageFilePath)) material.WithChannelTexture("Metallic", 0, baseColorImageFilePath);

            return material;
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Specular Glossiness attributes.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> instance to set.</param>
        /// <returns>This <see cref="Material"/> instance.</returns>
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

        public static Material CreateMaterial(this ModelRoot root, Materials.MaterialBuilder mb)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(mb, nameof(mb));

            var m = root.CreateMaterial(mb.Name);

            mb.CopyTo(m);

            return m;
        }

        public static Materials.MaterialBuilder ToMaterialBuilder(this Material srcMaterial)
        {
            if (srcMaterial == null) return Materials.MaterialBuilder.CreateDefault();
            var dstMaterial = new Materials.MaterialBuilder(srcMaterial.Name);

            srcMaterial.CopyTo(dstMaterial);

            return dstMaterial;
        }

        #endregion

        #region transfer API

        public static Schema2.AlphaMode ToSchema2(this Materials.AlphaMode alpha)
        {
            switch (alpha)
            {
                case Materials.AlphaMode.BLEND: return Schema2.AlphaMode.BLEND;
                case Materials.AlphaMode.MASK: return Schema2.AlphaMode.MASK;
                case Materials.AlphaMode.OPAQUE: return Schema2.AlphaMode.OPAQUE;
                default: throw new NotImplementedException(alpha.ToString());
            }
        }

        public static Materials.AlphaMode ToToolkit(this Schema2.AlphaMode alpha)
        {
            switch (alpha)
            {
                case Schema2.AlphaMode.BLEND: return Materials.AlphaMode.BLEND;
                case Schema2.AlphaMode.MASK: return Materials.AlphaMode.MASK;
                case Schema2.AlphaMode.OPAQUE: return Materials.AlphaMode.OPAQUE;
                default: throw new NotImplementedException(alpha.ToString());
            }
        }

        public static void CopyTo(this Material srcMaterial, MaterialBuilder dstMaterial)
        {
            _CopyDefaultTo(srcMaterial, dstMaterial);

            if (srcMaterial.Unlit)
            {
                dstMaterial.WithUnlitShader();
                srcMaterial.CopyChannelsTo(dstMaterial, "BaseColor");
                return;
            }

            if (srcMaterial.FindChannel("Diffuse") != null || srcMaterial.FindChannel("SpecularGlossiness") != null)
            {
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
            }

            if (srcMaterial.FindChannel("BaseColor") != null || srcMaterial.FindChannel("MetallicRoughness") != null)
            {
                _CopyMetallicRoughnessTo(srcMaterial, dstMaterial);
            }
        }

        private static void _CopyMetallicRoughnessTo(Material srcMaterial, MaterialBuilder dstMaterial)
        {
            dstMaterial.WithMetallicRoughnessShader();
            srcMaterial.CopyChannelsTo(dstMaterial, "BaseColor", "MetallicRoughness");
            srcMaterial.CopyChannelsTo(dstMaterial, "ClearCoat", "ClearCoatRoughness", "ClearCoatNormal");
        }

        private static void _CopyDefaultTo(Material srcMaterial, MaterialBuilder dstMaterial)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));

            dstMaterial.Name = srcMaterial.Name;
            dstMaterial.AlphaMode = srcMaterial.Alpha.ToToolkit();
            dstMaterial.AlphaCutoff = srcMaterial.AlphaCutoff;
            dstMaterial.DoubleSided = srcMaterial.DoubleSided;

            srcMaterial.CopyChannelsTo(dstMaterial, "Normal", "Occlusion", "Emissive");
        }

        public static void CopyChannelsTo(this Material srcMaterial, MaterialBuilder dstMaterial, params string[] channelKeys)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));
            Guard.NotNull(channelKeys, nameof(channelKeys));

            foreach (var k in channelKeys)
            {
                var src = srcMaterial.FindChannel(k);
                if (!src.HasValue) continue;

                if (src.Value.HasDefaultContent) continue;

                var dst = dstMaterial.UseChannel(k);

                src.Value.CopyTo(dst);
            }
        }

        public static void CopyTo(this MaterialChannel srcChannel, ChannelBuilder dstChannel)
        {
            Guard.NotNull(srcChannel, nameof(srcChannel));
            Guard.NotNull(dstChannel, nameof(dstChannel));

            dstChannel.Parameter = srcChannel.Parameter;

            if (srcChannel.Texture == null) { return; }

            if (dstChannel.Texture == null) dstChannel.UseTexture();

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

            dstChannel.Texture.PrimaryImage = srcChannel.Texture.PrimaryImage?.Content ?? Memory.MemoryImage.Empty;
            dstChannel.Texture.FallbackImage = srcChannel.Texture.FallbackImage?.Content ?? Memory.MemoryImage.Empty;
        }

        public static void CopyTo(this MaterialBuilder srcMaterial, Material dstMaterial)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));

            srcMaterial.ValidateForSchema2();

            dstMaterial.Alpha = srcMaterial.AlphaMode.ToSchema2();
            dstMaterial.AlphaCutoff = srcMaterial.AlphaCutoff;
            dstMaterial.DoubleSided = srcMaterial.DoubleSided;

            var hasClearCoat = srcMaterial.GetChannel("ClearCoat") != null
                || srcMaterial.GetChannel("ClearCoatRoughness") != null
                || srcMaterial.GetChannel("ClearCoatNormal") != null;

            srcMaterial.CopyChannelsTo(dstMaterial, "Normal", "Occlusion", "Emissive");

            MaterialBuilder defMaterial = null;

            if (srcMaterial.ShaderStyle == "Unlit")
            {
                dstMaterial.InitializeUnlit();
                srcMaterial.CopyChannelsTo(dstMaterial, "BaseColor");
                return;
            }

            if (srcMaterial.ShaderStyle == "PBRMetallicRoughness")
            {
                if (hasClearCoat) dstMaterial.InitializePBRMetallicRoughnessClearCoat();
                else dstMaterial.InitializePBRMetallicRoughness();
                defMaterial = srcMaterial;
            }

            if (srcMaterial.ShaderStyle == "PBRSpecularGlossiness")
            {
                dstMaterial.InitializePBRSpecularGlossiness(srcMaterial.CompatibilityFallback != null);
                srcMaterial.CopyChannelsTo(dstMaterial, "Diffuse", "SpecularGlossiness");
                defMaterial = srcMaterial.CompatibilityFallback;
            }

            if (defMaterial != null)
            {
                if (defMaterial.ShaderStyle != "PBRMetallicRoughness") throw new ArgumentException(nameof(srcMaterial.CompatibilityFallback.ShaderStyle));
                defMaterial.CopyChannelsTo(dstMaterial, "BaseColor", "MetallicRoughness");
                defMaterial.CopyChannelsTo(dstMaterial, "ClearCoat", "ClearCoatRoughness", "ClearCoatNormal");
            }
        }

        public static void CopyChannelsTo(this MaterialBuilder srcMaterial, Material dstMaterial, params string[] channelKeys)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));

            foreach (var k in channelKeys)
            {
                var src = srcMaterial.GetChannel(k);
                if (src == null) continue;

                var dst = dstMaterial.FindChannel(k);
                if (dst == null) continue;

                src.CopyTo(dst.Value);
            }
        }

        public static void CopyTo(this ChannelBuilder srcChannel, MaterialChannel dstChannel)
        {
            Guard.NotNull(srcChannel, nameof(srcChannel));
            Guard.NotNull(dstChannel, nameof(dstChannel));

            dstChannel.Parameter = srcChannel.Parameter;

            var srcTex = srcChannel.GetValidTexture();
            if (srcTex == null) return;

            Image primary = null;
            Image fallback = null;

            if (srcTex.PrimaryImage.IsValid)
            {
                primary = dstChannel
                .LogicalParent
                .LogicalParent
                .UseImageWithContent(srcTex.PrimaryImage);
            }

            if (srcTex.FallbackImage.IsValid)
            {
                fallback = dstChannel
                .LogicalParent
                .LogicalParent
                .UseImageWithContent(srcTex.FallbackImage);
            }

            dstChannel.SetTexture(srcTex.CoordinateSet, primary, fallback, srcTex.WrapS, srcTex.WrapT, srcTex.MinFilter, srcTex.MagFilter);

            var srcXform = srcTex.Transform;

            if (srcXform != null)
            {
                dstChannel.SetTransform(srcXform.Offset, srcXform.Scale, srcXform.Rotation, srcXform.CoordinateSetOverride);
            }
        }

        #endregion

        #region evaluation API

        public static Vector4 GetDiffuseColor(this Material material, Vector4 defaultColor)
        {
            if (material == null) return defaultColor;

            var channel = material.FindChannel("Diffuse");
            if (channel.HasValue) return channel.Value.Parameter;

            channel = material.FindChannel("BaseColor");
            if (channel.HasValue) return channel.Value.Parameter;

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

        #endregion
    }
}
