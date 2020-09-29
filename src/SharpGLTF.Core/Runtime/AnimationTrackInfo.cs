using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Runtime
{
    public class AnimationTrackInfo
    {
        public AnimationTrackInfo(string name, float duration)
        {
            Name = name;
            Duration = duration;
        }

        public string Name { get; private set; }
        public float Duration { get; private set; }
    }
}
