using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using GLTFMATERIAL = SharpGLTF.Schema2.Material;

namespace SharpGLTF.Runtime.Pipeline
{
    internal static class _GltfMaterialBasicProperties
    {
        public static float GetAlphaLevel(GLTFMATERIAL srcMaterial)
        {
            if (srcMaterial.Alpha == Schema2.AlphaMode.OPAQUE) return 1;

            var baseColor = srcMaterial.FindChannel("BaseColor");

            if (baseColor == null) return 1;

            return baseColor.Value.Color.W;
        }

        public static Vector3 GetDiffuseColor(GLTFMATERIAL srcMaterial)
        {
            var diffuse = srcMaterial.FindChannel("Diffuse");

            if (diffuse == null) diffuse = srcMaterial.FindChannel("BaseColor");

            if (diffuse == null) return Vector3.One;

            return new Vector3(diffuse.Value.Color.X, diffuse.Value.Color.Y, diffuse.Value.Color.Z);
        }

        public static Vector3 GetSpecularColor(GLTFMATERIAL srcMaterial)
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

        public static float GetSpecularPower(GLTFMATERIAL srcMaterial)
        {
            var mr = srcMaterial.FindChannel("MetallicRoughness");

            if (mr == null) return 16; // default value = 16

            var metallic = mr.Value.GetFactor("MetallicFactor");
            var roughness = mr.Value.GetFactor("RoughnessFactor");

            return 4 + 16 * metallic;
        }

        public static Vector3 GeEmissiveColor(GLTFMATERIAL srcMaterial)
        {
            var emissive = srcMaterial.FindChannel("Emissive");

            if (emissive == null) return Vector3.Zero;

            return new Vector3(emissive.Value.Color.X, emissive.Value.Color.Y, emissive.Value.Color.Z);
        }
    }
}
