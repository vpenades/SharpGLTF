using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    /// <summary>
    /// The alpha rendering mode of the material.
    /// </summary>
    public enum AlphaMode
    {
        OPAQUE,
        MASK,
        BLEND,
    }

    public enum KnownChannel
    {
        Normal,
        Occlusion,
        Emissive,

        BaseColor,
        MetallicRoughness,

        [Obsolete("This channel is used by KHR_materials_pbrSpecularGlossiness extension, which has been deprecated by Khronos; use BaseColor instead.")]
        Diffuse,

        [Obsolete("This channel is used by KHR_materials_pbrSpecularGlossiness extension, which has been deprecated by Khronos; use SpecularColor instead.")]
        SpecularGlossiness,

        ClearCoat,
        ClearCoatNormal,
        ClearCoatRoughness,

        Transmission,

        SheenColor,
        SheenRoughness,

        SpecularColor,
        SpecularFactor,

        VolumeThickness,
        VolumeAttenuation,

        Iridescence,
        IridescenceThickness,

        Anisotropy,

        DiffuseTransmissionColor,
        DiffuseTransmissionFactor
    }

    /// <summary>
    /// Enumeration of channel properties used in <see cref="ChannelBuilder.Parameters"/>
    /// </summary>
    /// <remarks>
    /// This enumeration must match <see cref="Schema2._MaterialParameterKey"/>
    /// </remarks>
    public enum KnownProperty
    {
        Unknown = 0,

        RGB,
        RGBA,

        Minimum, Maximum,

        NormalScale,

        OcclusionStrength,
        EmissiveStrength,

        IndexOfRefraction,

        MetallicFactor,
        RoughnessFactor,
        SpecularFactor,
        GlossinessFactor,
        ClearCoatFactor,
        ThicknessFactor,
        TransmissionFactor,
        IridescenceFactor,
        AttenuationDistance,
        DiffuseTransmissionFactor,

        AnisotropyStrength,

        AnisotropyRotation,
    }

    partial class MaterialBuilder
    {
        private IReadOnlyList<KnownChannel> _GetValidChannels()
        {

            switch (ShaderStyle)
            {
                case SHADERUNLIT: return _UnlitChannels;
                case SHADERPBRMETALLICROUGHNESS: return _MetRouChannels;

                #pragma warning disable CS0618 // Type or member is obsolete
                case SHADERPBRSPECULARGLOSSINESS: return _SpeGloChannels;
                #pragma warning restore CS0618 // Type or member is obsolete

                default: throw new NotImplementedException();
            }
        }

        private static readonly KnownChannel[] _UnlitChannels = new[]
        {
            KnownChannel.BaseColor
        };

        internal static readonly KnownChannel[] _MetRouChannels = new[]
        {
            KnownChannel.Normal,
            KnownChannel.Occlusion,
            KnownChannel.Emissive,

            KnownChannel.BaseColor,
            KnownChannel.MetallicRoughness,

            KnownChannel.ClearCoat,
            KnownChannel.ClearCoatNormal,
            KnownChannel.ClearCoatRoughness,
            KnownChannel.Transmission,
            KnownChannel.SheenColor,
            KnownChannel.SheenRoughness,
            KnownChannel.SpecularColor,
            KnownChannel.SpecularFactor,
            KnownChannel.VolumeThickness,
            KnownChannel.VolumeAttenuation,
            KnownChannel.Iridescence,
            KnownChannel.IridescenceThickness,
            KnownChannel.Anisotropy,
            KnownChannel.DiffuseTransmissionColor,
            KnownChannel.DiffuseTransmissionFactor
        };

        [Obsolete("Deprecated by Khronos")]
        private static readonly KnownChannel[] _SpeGloChannels = new[]
        {
            KnownChannel.Normal,
            KnownChannel.Occlusion,
            KnownChannel.Emissive,
            
            KnownChannel.Diffuse,
            KnownChannel.SpecularGlossiness,
        };
    }

    partial struct MaterialValue
    {
        internal static Collection CreateDefaultProperties(KnownChannel key)
        {
            var ppp = _CreateDefaultProperties(key).ToArray();
            var collection = new Collection(ppp);
            collection.Reset();
            return collection;
        }

        private static IEnumerable<_Property> _CreateDefaultProperties(KnownChannel key)
        {
            switch (key)
            {
                case KnownChannel.Emissive:
                    yield return new _Property(KnownProperty.RGB, Vector3.Zero);
                    yield return new _Property(KnownProperty.EmissiveStrength, 1f);
                    break;

                case KnownChannel.Normal: yield return new _Property(KnownProperty.NormalScale, 1f); break;
                case KnownChannel.Occlusion: yield return new _Property(KnownProperty.OcclusionStrength, 1f); break;

                #pragma warning disable CS0618 // Type or member is obsolete - We want to warn users that this is obsolete, but not to ourselves!
                case KnownChannel.Diffuse: yield return new _Property(KnownProperty.RGBA, Vector4.One); break;
                case KnownChannel.SpecularGlossiness:
                    yield return new _Property(KnownProperty.SpecularFactor, Vector3.One);
                    yield return new _Property(KnownProperty.GlossinessFactor, 1f);
                    break;
                #pragma warning restore CS0618 // Type or member is obsolete

                case KnownChannel.BaseColor: yield return new _Property(KnownProperty.RGBA, Vector4.One); break;
                case KnownChannel.MetallicRoughness:
                    yield return new _Property(KnownProperty.MetallicFactor, 1f);
                    yield return new _Property(KnownProperty.RoughnessFactor, 1f);
                    break;

                case KnownChannel.ClearCoat: yield return new _Property(KnownProperty.ClearCoatFactor, 0f); break;
                case KnownChannel.ClearCoatNormal: yield return new _Property(KnownProperty.NormalScale, 1f); break;
                case KnownChannel.ClearCoatRoughness: yield return new _Property(KnownProperty.RoughnessFactor, 0f); break;

                case KnownChannel.Transmission: yield return new _Property(KnownProperty.TransmissionFactor, 0f); break;

                case KnownChannel.SheenColor: yield return new _Property(KnownProperty.RGB, Vector3.Zero); break;
                case KnownChannel.SheenRoughness: yield return new _Property(KnownProperty.RoughnessFactor, 0f); break;

                case KnownChannel.SpecularColor: yield return new _Property(KnownProperty.RGB, Vector3.One); break;
                case KnownChannel.SpecularFactor: yield return new _Property(KnownProperty.SpecularFactor, 1f); break;

                case KnownChannel.VolumeThickness: yield return new _Property(KnownProperty.ThicknessFactor, 0f); break;
                case KnownChannel.VolumeAttenuation:
                    yield return new _Property(KnownProperty.RGB, Vector3.One);
                    yield return new _Property(KnownProperty.AttenuationDistance, float.PositiveInfinity);
                    break;

                case KnownChannel.Iridescence:
                    yield return new _Property(KnownProperty.IridescenceFactor, 0f);
                    yield return new _Property(KnownProperty.IndexOfRefraction, 1.3f);
                    break;
                case KnownChannel.IridescenceThickness:
                    yield return new _Property(KnownProperty.Minimum, 100);
                    yield return new _Property(KnownProperty.Maximum, 400);
                    break;

                case KnownChannel.Anisotropy:
                    yield return new _Property(KnownProperty.AnisotropyStrength, 0f);
                    yield return new _Property(KnownProperty.AnisotropyRotation, 0f);
                    break;

                case KnownChannel.DiffuseTransmissionFactor: yield return new _Property(KnownProperty.DiffuseTransmissionFactor, 20); break;
                case KnownChannel.DiffuseTransmissionColor: yield return new _Property(KnownProperty.RGB, Vector3.One); break;                    

                default: throw new NotImplementedException();
            }
        }
    }
}
