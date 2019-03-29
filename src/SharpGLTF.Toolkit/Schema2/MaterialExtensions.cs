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
            var ch = material.WithPBRMetallicRoughness().FindChannel("BaseColor").Value;

            ch.Color = diffuseColor;

            return material;
        }

        public static Material WithDoubleSide(this Material material, bool enabled)
        {
            material.DoubleSided = enabled;
            return material;
        }

        public static Material WithChannelColor(this Material material, string channelName, Vector4 color)
        {
            var channel = material.FindChannel(channelName).Value;

            channel.Color = color;

            return material;
        }

        public static Material WithChannelTexture(this Material material, string channelName, int textureSet, string imageFilePath, float amount = 1)
        {
            var image = material.LogicalParent.UseImageWithFile(imageFilePath);

            return material.WithChannelTexture(channelName, textureSet, image, amount);
        }

        public static Material WithChannelTexture(this Material material, string channelName, int textureSet, Image image, float amount = 1)
        {
            var channel = material.FindChannel(channelName).Value;

            channel.SetTexture(textureSet, image);

            channel.TextureAmount = amount;

            return material;
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Metallic Roughness attributes.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> instance to set.</param>
        /// <returns>This <see cref="Material"/> instance.</returns>
        public static Material WithPBRMetallicRoughness(this Material material)
        {
            material.InitializePBRMetallicRoughness();
            return material;
        }

        public static Material WithPBRMetallicRoughness(
            this Material material,
            Vector4 baseColor, string baseColorImageFilePath,
            float metallicAmount = 1, string metallicImageFilePath = null,
            float roughnessAmount = 1, string roughtnessImageFilePath = null
            )
        {
            material.WithPBRMetallicRoughness();

            if (!string.IsNullOrWhiteSpace(baseColorImageFilePath)) material.WithChannelColor("BaseColor", baseColor).WithChannelTexture("BaseColor", 0, baseColorImageFilePath);
            if (!string.IsNullOrWhiteSpace(metallicImageFilePath)) material.WithChannelTexture("Metallic", 0, baseColorImageFilePath, metallicAmount);
            if (!string.IsNullOrWhiteSpace(roughtnessImageFilePath)) material.WithChannelTexture("Roughness", 0, baseColorImageFilePath, roughnessAmount);

            return material;
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Specular Glossiness attributes.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> instance to set.</param>
        /// <returns>This <see cref="Material"/> instance.</returns>
        public static Material WithPBRSpecularGlossiness(this Material material)
        {
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
            foreach (var img in root.LogicalImages)
            {
                var existingContent = img.GetImageContent();
                if (Enumerable.SequenceEqual(existingContent, imageContent)) return img;
            }

            var image = root.CreateImage();
            image.SetSatelliteContent(imageContent);
            return image;
        }

        #endregion

        #region creation API

        public static Material CreateMaterial(this ModelRoot root, Materials.MaterialBuilder mb)
        {
            var m = root.CreateMaterial(mb.Name);

            mb.CopyTo(m);

            return m;
        }

        #endregion

        #region transfer API

        public static void CopyTo(this Material srcMaterial, Materials.MaterialBuilder mb)
        {
            mb.Name = srcMaterial.Name;
            mb.Unlit = srcMaterial.Unlit;
            mb.AlphaMode = srcMaterial.Alpha;
            mb.AlphaCutoff = srcMaterial.AlphaCutoff;
            mb.DoubleSided = srcMaterial.DoubleSided;

            foreach (var channel in srcMaterial.Channels)
            {
                var ch = mb.UseChannel(channel.Key);
                channel.CopyTo(ch);
            }
        }

        public static void CopyTo(this MaterialChannel srcChannel, Materials.MaterialChannelBuilder dstChannel)
        {
            dstChannel.Color = srcChannel.Color;

            if (srcChannel.Texture == null) { dstChannel.Texture = null; return; }

            if (dstChannel.Texture == null) dstChannel.Texture = new Materials.TextureBuilder();

            dstChannel.Texture.Amount = srcChannel.TextureAmount;

            dstChannel.Texture.CoordinateSet = srcChannel.TextureCoordinate;
            dstChannel.Texture.MinFilter = srcChannel.TextureSampler.MinFilter;
            dstChannel.Texture.MagFilter = srcChannel.TextureSampler.MagFilter;
            dstChannel.Texture.WrapS = srcChannel.TextureSampler.WrapS;
            dstChannel.Texture.WrapT = srcChannel.TextureSampler.WrapT;

            /*
            dstChannel.Texture.Rotation = srcChannel.Transform?.Rotation ?? 0;
            dstChannel.Texture.Offset = srcChannel.Transform?.Offset ?? Vector2.Zero;
            dstChannel.Texture.Scale = srcChannel.Transform?.Scale ?? Vector2.One;
            */

            dstChannel.Texture.ImageContent = srcChannel.Texture.Image.GetImageContent();
        }

        public static void CopyTo(this Materials.MaterialBuilder srcMaterial, Material dstMaterial)
        {
            // dstMaterial.Name = srcMaterial.Name;

            dstMaterial.Alpha = srcMaterial.AlphaMode;
            dstMaterial.AlphaCutoff = srcMaterial.AlphaCutoff;
            dstMaterial.DoubleSided = srcMaterial.DoubleSided;

            srcMaterial.GetChannel("Normal").CopyTo(dstMaterial.FindChannel("Normal").Value);
            srcMaterial.GetChannel("Occlusion").CopyTo(dstMaterial.FindChannel("Occlusion").Value);
            srcMaterial.GetChannel("Emissive").CopyTo(dstMaterial.FindChannel("Emissive").Value);

            if (srcMaterial.Style == "PBRMetallicRoughness")
            {
                dstMaterial.InitializePBRMetallicRoughness();

                srcMaterial.GetChannel("BaseColor").CopyTo(dstMaterial.FindChannel("BaseColor").Value);
                srcMaterial.GetChannel("Metallic").CopyTo(dstMaterial.FindChannel("Metallic").Value);
                srcMaterial.GetChannel("Roughness").CopyTo(dstMaterial.FindChannel("Roughness").Value);
            }
            else if (srcMaterial.Style == "PBRSpecularGlossiness")
            {
                dstMaterial.InitializePBRSpecularGlossiness();

                srcMaterial.GetChannel("Diffuse").CopyTo(dstMaterial.FindChannel("Diffuse").Value);
                srcMaterial.GetChannel("Specular").CopyTo(dstMaterial.FindChannel("Specular").Value);
                srcMaterial.GetChannel("Glossiness").CopyTo(dstMaterial.FindChannel("Glossiness").Value);
            }
        }

        public static void CopyTo(this Materials.MaterialChannelBuilder srcChannel, MaterialChannel dstChannel)
        {
            if (srcChannel == null) return;

            dstChannel.Color = srcChannel.Color;

            var srcTex = srcChannel.Texture;

            if (srcTex == null) return;

            var image = dstChannel.LogicalParent.LogicalParent.UseImageWithContent(srcTex.ImageContent.ToArray());

            dstChannel.SetTexture(srcTex.CoordinateSet, image, srcTex.MinFilter, srcTex.MagFilter, srcTex.WrapS, srcTex.WrapT);

            // dstChannel.SetTransform(srcTex.CoordinateSet, srcTex.Offset, srcTex.Scale, srcTex.Rotation);
        }

        #endregion

        #region evaluation API

        public static Vector4 GetDiffuseColor(this Material material, Vector4 defaultColor)
        {
            if (material == null) return defaultColor;

            var channel = material.FindChannel("BaseColor");
            if (channel.HasValue) return channel.Value.Color;

            channel = material.FindChannel("Diffuse");
            if (channel.HasValue) return channel.Value.Color;

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
