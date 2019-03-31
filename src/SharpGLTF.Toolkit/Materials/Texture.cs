using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    using BYTES = ArraySegment<Byte>;

    using TEXLERP = Schema2.TextureInterpolationFilter;
    using TEXMIPMAP = Schema2.TextureMipMapFilter;
    using TEXWRAP = Schema2.TextureWrapMode;

    [System.Diagnostics.DebuggerDisplay("Texture {CoordinateSet} {MinFilter} {MagFilter} {WrapS} {WrapT} {Rotation} {Offset} {Scale}")]
    public class TextureBuilder
    {
        #region lifecycle

        internal TextureBuilder() { }

        #endregion

        #region properties

        public int CoordinateSet { get; set; } = 0;

        /*
        public Single Rotation { get; set; } = 0;

        public Vector2 Offset { get; set; } = Vector2.Zero;

        public Vector2 Scale { get; set; } = Vector2.One;
        */

        public Single Amount { get; set; } = 1;

        public BYTES ImageContent { get; set; }

        public TEXMIPMAP MinFilter { get; set; } = TEXMIPMAP.DEFAULT;

        public TEXLERP MagFilter { get; set; } = TEXLERP.DEFAULT;

        public TEXWRAP WrapS { get; set; } = TEXWRAP.REPEAT;

        public TEXWRAP WrapT { get; set; } = TEXWRAP.REPEAT;

        #endregion

        #region API

        public TextureBuilder WithCoordinateSet(int cset) { CoordinateSet = cset; return this; }

        public TextureBuilder WithImage(string imageFilePath)
        {
            var data = System.IO.File.ReadAllBytes(imageFilePath).Slice(0);
            return WithImage(data);
        }

        public TextureBuilder WithImage(BYTES image) { this.ImageContent = image; return this; }

        public TextureBuilder WithSampler(TEXMIPMAP min = TEXMIPMAP.LINEAR, TEXLERP mag = TEXLERP.LINEAR, TEXWRAP ws = TEXWRAP.REPEAT, TEXWRAP wt = TEXWRAP.REPEAT)
        {
            this.MinFilter = min;
            this.MagFilter = mag;
            this.WrapS = ws;
            this.WrapT = wt;

            return this;
        }

        #endregion
    }
}
