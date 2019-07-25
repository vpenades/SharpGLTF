using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

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
        /// Creates or reuses an <see cref="Image"/> with the image content set by <paramref name="imageContent"/>
        /// </summary>
        /// <param name="root">The <see cref="ModelRoot"/> root instance.</param>
        /// <param name="imageContent">A buffer containing the bytes of the image file.</param>
        /// <returns>A <see cref="Image"/> instance.</returns>
        public static Image UseImageWithContent(this ModelRoot root, Byte[] imageContent)
        {
            Guard.NotNull(root, nameof(root));

            return root.UseImage(new ArraySegment<byte>(imageContent));
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

        public static void CopyTo(this Material srcMaterial, Materials.MaterialBuilder dstMaterial)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));

            dstMaterial.Name = srcMaterial.Name;
            dstMaterial.AlphaMode = srcMaterial.Alpha.ToToolkit();
            dstMaterial.AlphaCutoff = srcMaterial.AlphaCutoff;
            dstMaterial.DoubleSided = srcMaterial.DoubleSided;

            srcMaterial.CopyChannelsTo(dstMaterial, "Normal", "Occlusion", "Emissive");

            if (srcMaterial.Unlit) dstMaterial.WithUnlitShader();

            if (srcMaterial.FindChannel("BaseColor") != null || srcMaterial.FindChannel("MetallicRoughness") != null)
            {
                dstMaterial.WithMetallicRoughnessShader();
                srcMaterial.CopyChannelsTo(dstMaterial, "BaseColor", "MetallicRoughness");
            }

            if (srcMaterial.FindChannel("Diffuse") != null || srcMaterial.FindChannel("SpecularGlossiness") != null)
            {
                dstMaterial = new Materials.MaterialBuilder(srcMaterial.Name).WithFallback(dstMaterial);

                dstMaterial.Name = srcMaterial.Name;
                dstMaterial.AlphaMode = srcMaterial.Alpha.ToToolkit();
                dstMaterial.AlphaCutoff = srcMaterial.AlphaCutoff;
                dstMaterial.DoubleSided = srcMaterial.DoubleSided;

                srcMaterial.CopyChannelsTo(dstMaterial, "Normal", "Occlusion", "Emissive");

                dstMaterial.WithSpecularGlossinessShader();
                srcMaterial.CopyChannelsTo(dstMaterial, "Diffuse", "SpecularGlossiness");
            }
        }

        public static void CopyChannelsTo(this Material srcMaterial, Materials.MaterialBuilder dstMaterial, params string[] channelKeys)
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

        public static void CopyTo(this MaterialChannel srcChannel, Materials.ChannelBuilder dstChannel)
        {
            Guard.NotNull(srcChannel, nameof(srcChannel));
            Guard.NotNull(dstChannel, nameof(dstChannel));

            dstChannel.Parameter = srcChannel.Parameter;

            if (srcChannel.Texture == null) { return; }

            if (dstChannel.Texture == null) dstChannel.UseTexture();

            dstChannel.Texture.CoordinateSet = srcChannel.TextureCoordinate;
            dstChannel.Texture.MinFilter = srcChannel.TextureSampler.MinFilter;
            dstChannel.Texture.MagFilter = srcChannel.TextureSampler.MagFilter;
            dstChannel.Texture.WrapS = srcChannel.TextureSampler.WrapS;
            dstChannel.Texture.WrapT = srcChannel.TextureSampler.WrapT;

            var srcXform = srcChannel.TextureTransform;

            if (srcXform != null)
            {
                dstChannel.Texture.WithTransform(srcXform.Offset, srcXform.Scale, srcXform.Rotation, srcXform.TextureCoordinateOverride);
            }

            dstChannel.Texture.PrimaryImageContent = srcChannel.Texture.PrimaryImage.GetImageContent();

            if (srcChannel.Texture.FallbackImage != null)
            {
                dstChannel.Texture.FallbackImageContent = srcChannel.Texture.FallbackImage.GetImageContent();
            }
        }

        public static void CopyTo(this Materials.MaterialBuilder srcMaterial, Material dstMaterial)
        {
            Guard.NotNull(srcMaterial, nameof(srcMaterial));
            Guard.NotNull(dstMaterial, nameof(dstMaterial));

            srcMaterial.ValidateForSchema2();

            dstMaterial.Alpha = srcMaterial.AlphaMode.ToSchema2();
            dstMaterial.AlphaCutoff = srcMaterial.AlphaCutoff;
            dstMaterial.DoubleSided = srcMaterial.DoubleSided;

            srcMaterial.CopyChannelsTo(dstMaterial, "Normal", "Occlusion", "Emissive");

            Materials.MaterialBuilder defMaterial = null;

            if (srcMaterial.ShaderStyle == "Unlit")
            {
                dstMaterial.InitializePBRMetallicRoughness();
                srcMaterial.CopyChannelsTo(dstMaterial, "BaseColor");
                return;
            }

            if (srcMaterial.ShaderStyle == "PBRMetallicRoughness")
            {
                dstMaterial.InitializePBRMetallicRoughness();
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
                srcMaterial.CopyChannelsTo(dstMaterial, "BaseColor", "MetallicRoughness");
            }
        }

        public static void CopyChannelsTo(this Materials.MaterialBuilder srcMaterial, Material dstMaterial, params string[] channelKeys)
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

        public static void CopyTo(this Materials.ChannelBuilder srcChannel, MaterialChannel dstChannel)
        {
            Guard.NotNull(srcChannel, nameof(srcChannel));
            Guard.NotNull(dstChannel, nameof(dstChannel));

            dstChannel.Parameter = srcChannel.Parameter;

            var srcTex = srcChannel.Texture;
            if (srcTex == null) return;

            var primary = dstChannel
                .LogicalParent
                .LogicalParent
                .UseImageWithContent(srcTex.PrimaryImageContent.ToArray());

            Image fallback = null;

            if (srcTex.FallbackImageContent.Count > 0)
            {
                fallback = dstChannel
                .LogicalParent
                .LogicalParent
                .UseImageWithContent(srcTex.FallbackImageContent.ToArray());
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

            var channel = material.FindChannel("BaseColor");
            if (channel.HasValue) return channel.Value.Parameter;

            channel = material.FindChannel("Diffuse");
            if (channel.HasValue) return channel.Value.Parameter;

            return defaultColor;
        }

        public static Texture GetDiffuseTexture(this Material material)
        {
            if (material == null) return null;

            var channel = material.FindChannel("BaseColor");
            if (channel.HasValue) return channel.Value.Texture;

            channel = material.FindChannel("Diffuse");
            if (channel.HasValue) return channel.Value.Texture;

            return null;
        }

        #endregion
    }
}
