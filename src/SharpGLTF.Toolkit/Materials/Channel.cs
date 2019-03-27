using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    [System.Diagnostics.DebuggerDisplay("{_key} {Factor}")]
    public class Channel
    {
        internal Channel(string key) { _Key = key; }

        private readonly String _Key;

        public String Key => _Key;

        public Vector4 Factor { get; set; }

        public Texture Texture { get; set; }
    }
}
