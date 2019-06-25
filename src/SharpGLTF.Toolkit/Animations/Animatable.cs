using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Animations
{
    public class Animatable<T>
        where T : struct
    {
        #region data

        private Dictionary<string, ICurveSampler<T>> _Tracks = new Dictionary<string, ICurveSampler<T>>();

        public T Default { get; set; }

        #endregion

        #region API

        public T GetValueAt(string track, float value)
        {
            return _Tracks.TryGetValue(track, out ICurveSampler<T> sampler) ? sampler.GetSample(value) : this.Default;
        }

        #endregion
    }
}
