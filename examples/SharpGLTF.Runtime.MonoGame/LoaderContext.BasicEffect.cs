using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GLTFMATERIAL = SharpGLTF.Schema2.Material;

namespace SharpGLTF.Runtime
{
    class BasicEffectsLoaderContext : LoaderContext
    {
        #region lifecycle

        public BasicEffectsLoaderContext(GraphicsDevice device) : base(device) { }

        #endregion

        #region effects creation

        // Monogame's BasicEffect uses Phong's shading, while glTF uses PBR shading, so
        // given monogame's limitations, we try to guess the most appropiate values
        // to have a reasonably good looking renders.

        protected override Effect CreateEffect(GLTFMATERIAL srcMaterial, bool isSkinned)
        {
            return isSkinned ? CreateSkinnedEffect(srcMaterial) : CreateRigidEffect(srcMaterial);
        }

        protected virtual Effect CreateRigidEffect(GLTFMATERIAL srcMaterial)
        {
            var dstMaterial = srcMaterial.Alpha == Schema2.AlphaMode.MASK
                ? CreateAlphaTestEffect(srcMaterial)
                : CreateBasicEffect(srcMaterial);

            return dstMaterial;
        }

        protected virtual Effect CreateBasicEffect(GLTFMATERIAL srcMaterial)
        {
            var dstMaterial = new BasicEffect(Device);

            dstMaterial.Name = srcMaterial.Name;

            dstMaterial.Alpha = GetAlphaLevel(srcMaterial);
            dstMaterial.DiffuseColor = GetDiffuseColor(srcMaterial);
            dstMaterial.SpecularColor = GetSpecularColor(srcMaterial);
            dstMaterial.SpecularPower = GetSpecularPower(srcMaterial);
            dstMaterial.EmissiveColor = GeEmissiveColor(srcMaterial);
            dstMaterial.Texture = UseDiffuseTexture(srcMaterial);

            if (srcMaterial.Unlit)
            {
                dstMaterial.EmissiveColor = dstMaterial.DiffuseColor;
                dstMaterial.SpecularColor = Vector3.Zero;
                dstMaterial.SpecularPower = 16;
            }

            dstMaterial.PreferPerPixelLighting = true;
            dstMaterial.TextureEnabled = dstMaterial.Texture != null;

            return dstMaterial;
        }

        protected virtual Effect CreateAlphaTestEffect(GLTFMATERIAL srcMaterial)
        {
            var dstMaterial = new AlphaTestEffect(Device);

            dstMaterial.Name = srcMaterial.Name;

            dstMaterial.Alpha = GetAlphaLevel(srcMaterial);
            //dstMaterial.AlphaFunction = CompareFunction.GreaterEqual;
            dstMaterial.ReferenceAlpha = (int)(srcMaterial.AlphaCutoff * 255);

            dstMaterial.DiffuseColor = GetDiffuseColor(srcMaterial);

            dstMaterial.Texture = UseDiffuseTexture(srcMaterial);

            return dstMaterial;
        }

        protected virtual Effect CreateSkinnedEffect(GLTFMATERIAL srcMaterial)
        {
            var dstMaterial = new SkinnedEffect(Device);            

            dstMaterial.Name = srcMaterial.Name;

            dstMaterial.Alpha = GetAlphaLevel(srcMaterial);
            dstMaterial.DiffuseColor = GetDiffuseColor(srcMaterial);
            dstMaterial.SpecularColor = GetSpecularColor(srcMaterial);
            dstMaterial.SpecularPower = GetSpecularPower(srcMaterial);
            dstMaterial.EmissiveColor = GeEmissiveColor(srcMaterial);
            dstMaterial.Texture = UseDiffuseTexture(srcMaterial);

            dstMaterial.WeightsPerVertex = 4;
            dstMaterial.PreferPerPixelLighting = true;

            // apparently, SkinnedEffect does not support disabling textures, so we set a white texture here.
            if (dstMaterial.Texture == null) dstMaterial.Texture = UseTexture(null, null); // creates a dummy white texture.

            return dstMaterial;
        }

        #endregion

        #region meshes creation

        protected override void WriteMeshPrimitive(MeshPrimitiveReader srcPrimitive, Effect effect)
        {
            if (srcPrimitive.IsSkinned) WriteMeshPrimitive<VertexSkinned>(effect, srcPrimitive);
            else WriteMeshPrimitive<VertexPositionNormalTexture>(effect, srcPrimitive);
        }

        #endregion

        #region gltf helpers

        private static float GetAlphaLevel(GLTFMATERIAL srcMaterial)
        {
            if (srcMaterial.Alpha == Schema2.AlphaMode.OPAQUE) return 1;

            var baseColor = srcMaterial.FindChannel("BaseColor");

            if (baseColor == null) return 1;

            return baseColor.Value.Color.W;
        }

        private static Vector3 GetDiffuseColor(GLTFMATERIAL srcMaterial)
        {
            var diffuse = srcMaterial.FindChannel("Diffuse");

            if (diffuse == null) diffuse = srcMaterial.FindChannel("BaseColor");

            if (diffuse == null) return Vector3.One;
            
            return new Vector3(diffuse.Value.Color.X, diffuse.Value.Color.Y, diffuse.Value.Color.Z);
        }

        private static Vector3 GetSpecularColor(GLTFMATERIAL srcMaterial)
        {
            var mr = srcMaterial.FindChannel("MetallicRoughness");

            if (mr == null) return Vector3.One; // default value 16

            var diffuse = GetDiffuseColor(srcMaterial);
            var metallic = mr.Value.GetFactor("MetallicFactor");
            var roughness = mr.Value.GetFactor("RoughnessFactor");

            var k = Vector3.Zero;
            k += Vector3.Lerp(diffuse, Vector3.Zero, roughness);
            k += Vector3.Lerp(diffuse, Vector3.One, metallic);
            k *= 0.5f;

            return k;
        }

        private static float GetSpecularPower(GLTFMATERIAL srcMaterial)
        {
            var mr = srcMaterial.FindChannel("MetallicRoughness");

            if (mr == null) return 16; // default value = 16

            var metallic = mr.Value.GetFactor("MetallicFactor");
            var roughness = mr.Value.GetFactor("RoughnessFactor");

            return 4 + 16 * metallic;
        }

        private static Vector3 GeEmissiveColor(GLTFMATERIAL srcMaterial)
        {
            var emissive = srcMaterial.FindChannel("Emissive");

            if (emissive == null) return Vector3.Zero;

            return new Vector3(emissive.Value.Color.X, emissive.Value.Color.Y, emissive.Value.Color.Z);
        }

        private Texture2D UseDiffuseTexture(GLTFMATERIAL srcMaterial)
        {
            var diffuse = srcMaterial.FindChannel("Diffuse");

            if (diffuse == null) diffuse = srcMaterial.FindChannel("BaseColor");
            if (diffuse == null) return null;

            return UseTexture(diffuse, null);
        }

        #endregion
    }
}
