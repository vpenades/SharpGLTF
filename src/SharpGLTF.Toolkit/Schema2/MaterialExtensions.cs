using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public static partial class Toolkit
    {
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
            material.WithPBRMetallicRoughness()
                .FindChannel("BaseColor")
                .SetFactor(diffuseColor);

            return material;
        }

        public static Material WithDoubleSide(this Material material, bool enabled)
        {
            material.DoubleSided = enabled;
            return material;
        }

        public static Material WithChannelTexture(this Material material, string channelName, int textureSet, string imageFilePath)
        {
            material.FindChannel(channelName).SetTexture(textureSet, material.LogicalParent.UseImageWithFile(imageFilePath));
            return material;
        }

        public static Material WithChannelTexture(this Material material, string channelName, int textureSet, Image image)
        {
            material.FindChannel(channelName).SetTexture(textureSet, image);
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

        public static Material WithPBRMetallicRoughness(this Material material,
            Vector4 baseColorFactor, string baseColorImageFilePath,
            float metallicFactor = 1, string metallicImageFilePath = null,
            float roughnessFactor = 1, string roughtnessImageFilePath = null
            )
        {
            material.WithPBRMetallicRoughness();

            if (!string.IsNullOrWhiteSpace(baseColorImageFilePath)) material.WithChannelTexture("BaseColor", 0, baseColorImageFilePath);
            if (!string.IsNullOrWhiteSpace(metallicImageFilePath)) material.WithChannelTexture("Metallic", 0, baseColorImageFilePath);
            if (!string.IsNullOrWhiteSpace(roughtnessImageFilePath)) material.WithChannelTexture("Roughness", 0, baseColorImageFilePath);

            material.FindChannel("BaseColor").SetFactor(baseColorFactor);
            material.FindChannel("Metallic").SetFactor(metallicFactor);
            material.FindChannel("Roughness").SetFactor(roughnessFactor);

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
    }
}
