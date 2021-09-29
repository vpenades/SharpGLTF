using System;
using System.Collections.Generic;
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
    }

    partial class ChannelBuilder
    {
        private static Vector4 _GetDefaultParameter(KnownChannel key)
        {
            switch (key)
            {
                case KnownChannel.Emissive: return Vector4.Zero;

                case KnownChannel.Normal:
                case KnownChannel.ClearCoatNormal:
                case KnownChannel.Occlusion:
                case KnownChannel.SpecularFactor:
                    return Vector4.UnitX;

                case KnownChannel.BaseColor:
                case KnownChannel.Diffuse:
                case KnownChannel.SpecularColor:
                    return Vector4.One;

                case KnownChannel.MetallicRoughness: return new Vector4(1, 1, 0, 0);
                case KnownChannel.SpecularGlossiness: return Vector4.One;

                case KnownChannel.ClearCoat: return Vector4.Zero;
                case KnownChannel.ClearCoatRoughness: return Vector4.Zero;

                case KnownChannel.Transmission: return Vector4.Zero;

                case KnownChannel.SheenColor: return Vector4.Zero;
                case KnownChannel.SheenRoughness: return Vector4.Zero;

                default: throw new NotImplementedException();
            }
        }
    }
}
