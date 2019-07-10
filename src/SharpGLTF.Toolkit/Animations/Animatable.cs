using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Represents a value that can be animated using <see cref="Animations.ICurveSampler{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public class Animatable<T>
    {
        #region data

        private Dictionary<string, ICurveSampler<T>> _Tracks;

        public T Default { get; set; }

        #endregion

        #region properties

        public IReadOnlyDictionary<string, ICurveSampler<T>> Tracks => _Tracks == null ? Collections.EmptyDictionary<string, ICurveSampler<T>>.Instance : _Tracks;

        #endregion

        #region API

        public T GetValueAt(string track, float value)
        {
            if (_Tracks == null) return this.Default;

            return _Tracks.TryGetValue(track, out ICurveSampler<T> sampler) ? sampler.GetPoint(value) : this.Default;
        }

        public void SetTrack(string track, ICurveSampler<T> curve)
        {
            Guard.NotNullOrEmpty(track, nameof(track));
            if (curve == null) { _Tracks.Remove(track); return; }
        }

        #endregion
    }
}
