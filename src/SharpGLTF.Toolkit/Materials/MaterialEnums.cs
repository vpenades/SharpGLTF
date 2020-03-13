using System;
using System.Collections.Generic;
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
    }
}
