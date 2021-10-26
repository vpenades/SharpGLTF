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

        Diffuse,
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
    }

    /// <summary>
    /// Enumeration of channel properties used in <see cref="ChannelBuilder.Parameters"/>
    /// </summary>
    /// <remarks>
    /// This enumeration must match <see cref="Schema2.MaterialParameter.Key"/>
    /// </remarks>
    public enum KnownProperty
    {
        RGB,
        RGBA,

        NormalScale,
        OcclusionStrength,

        MetallicFactor,
        RoughnessFactor,
        SpecularFactor,
        GlossinessFactor,
        ClearCoatFactor,
        ThicknessFactor,
        TransmissionFactor,
        AttenuationDistance,
    }

    partial class MaterialBuilder
    {
        private IReadOnlyList<KnownChannel> _GetValidChannels()
        {
            switch (ShaderStyle)
            {
                case SHADERUNLIT: return _UnlitChannels;
                case SHADERPBRMETALLICROUGHNESS: return _MetRouChannels;
                case SHADERPBRSPECULARGLOSSINESS: return _SpeGloChannels;
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
            KnownChannel.VolumeAttenuation
        };

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
                case KnownChannel.Emissive: yield return new _Property(KnownProperty.RGB, Vector3.Zero); break;
                case KnownChannel.Normal: yield return new _Property(KnownProperty.NormalScale, 1f); break;
                case KnownChannel.Occlusion: yield return new _Property(KnownProperty.OcclusionStrength, 1f); break;

                case KnownChannel.Diffuse: yield return new _Property(KnownProperty.RGBA, Vector4.One); break;
                case KnownChannel.SpecularGlossiness:
                    yield return new _Property(KnownProperty.SpecularFactor, Vector3.One);
                    yield return new _Property(KnownProperty.GlossinessFactor, 1f);
                    break;

                case KnownChannel.BaseColor: yield return new _Property(KnownProperty.RGBA, Vector4.One);break;
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
                    yield return new _Property(KnownProperty.AttenuationDistance, 0f);
                    break;

                default: throw new NotImplementedException();
            }
        }
    }
}
