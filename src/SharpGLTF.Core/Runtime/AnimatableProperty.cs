using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpGLTF.Animations;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Defines an animatable property with a default value and a collection of animation curve tracks.
    /// </summary>
    /// <typeparam name="T">A type that can be interpolated with <see cref="ICurveSampler{T}"/></typeparam>
    sealed class AnimatableProperty<T>
       where T : struct
    {
        #region lifecycle

        internal AnimatableProperty(T defval)
        {
            Value = defval;
        }

        #endregion

        #region data

        private Dictionary<string, ICurveSampler<T>> _Tracks;

        /// <summary>
        /// Gets the default value of this instance.
        /// When animations are disabled, or there's no animation track available, this will be the returned value.
        /// </summary>
        public T Value { get; private set; }

        #endregion

        #region properties

        public bool IsAnimated => Tracks.Count > 0;

        public IReadOnlyDictionary<string, ICurveSampler<T>> Tracks => _Tracks;

        #endregion

        #region API

        /// <summary>
        /// Evaluates the value of this <see cref="AnimatableProperty{T}"/> at a given <paramref name="offset"/> for a given <paramref name="track"/>.
        /// </summary>
        /// <param name="track">An animation track name, or null.</param>
        /// <param name="offset">A time offset within the given animation track.</param>
        /// <returns>The evaluated value taken from the animation <paramref name="track"/>, or <see cref="Value"/> if a track was not found.</returns>
        public T GetValueAt(string track, float offset)
        {
            if (_Tracks == null) return this.Value;

            return _Tracks.TryGetValue(track, out ICurveSampler<T> sampler) ? sampler.GetPoint(offset) : this.Value;
        }

        public void AddCurve(string name, ICurveSampler<T> sampler)
        {
            if (_Tracks == null) _Tracks = new Dictionary<string, ICurveSampler<T>>();
            _Tracks[name] = sampler;
        }

        #endregion
    }
}
