using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Runtime
{
    [System.Diagnostics.DebuggerDisplay("{Name} {Duration}s")]
    public class AnimationTrackInfo
    {
        internal AnimationTrackInfo(string name, Object extras, float duration)
        {
            Name = name;
            Extras = extras;
            Duration = duration;
        }

        public string Name { get; private set; }
        public Object Extras { get; private set; }
        public float Duration { get; private set; }
    }
}
