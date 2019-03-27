using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    [System.Diagnostics.DebuggerDisplay("Texture {CoordinateSet} {MinFilter} {MagFilter} {WrapS} {WrapT} {Rotation} {Offset} {Scale}")]
    public class Texture
    {
        #region properties

        public int CoordinateSet { get; set; }

        public Single Rotation { get; set; }

        public Vector2 Offset { get; set; }

        public Vector2 Scale { get; set; }

        public ArraySegment<Byte> ImageContent { get; set; }

        public Schema2.TextureMipMapMode MinFilter { get; set; }

        public Schema2.TextureInterpolationMode MagFilter { get; set; }

        public Schema2.TextureWrapMode WrapS { get; set; } = Schema2.TextureWrapMode.REPEAT;

        public Schema2.TextureWrapMode WrapT { get; set; } = Schema2.TextureWrapMode.REPEAT;

        #endregion
    }
}
