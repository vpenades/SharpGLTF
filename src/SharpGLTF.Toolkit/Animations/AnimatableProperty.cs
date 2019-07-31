using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Represents a property value that can be animated using <see cref="Animations.ICurveSampler{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public class AnimatableProperty<T>
        where T : struct
    {
        #region lifecycle

        internal AnimatableProperty() { }

        internal AnimatableProperty(AnimatableProperty<T> other)
        {
            if (other == null) return;

            if (other._Tracks != null)
            {
                this._Tracks = new Dictionary<string, ICurveSampler<T>>();

                foreach (var kvp in other._Tracks)
                {
                    this._Tracks[kvp.Key] = CurveFactory.CreateCurveBuilder(kvp.Value);
                }
            }

            this.Value = other.Value;
        }

        #endregion

        #region data

        private Dictionary<string, ICurveSampler<T>> _Tracks;

        /// <summary>
        /// Gets or sets the default value of this instance.
        /// When animations are disabled, or there's no animation track available, this will be the returned value.
        /// </summary>
        public T Value { get; set; }

        #endregion

        #region properties

        public bool IsAnimated => Tracks.Count > 0;

        public IReadOnlyDictionary<string, ICurveSampler<T>> Tracks => _Tracks == null ? Collections.EmptyDictionary<string, ICurveSampler<T>>.Instance : _Tracks;

        #endregion

        #region API

        /// <summary>
        /// Removes the animation <paramref name="track"/>.
        /// </summary>
        /// <param name="track">The name of the track.</param>
        public void RemoveTrack(string track)
        {
            if (_Tracks == null) return;
            _Tracks.Remove(track);
            if (_Tracks.Count == 0) _Tracks = null;
        }

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

        /// <summary>
        /// Assigns an animation curve to a given track.
        /// </summary>
        /// <param name="track">The name of the track.</param>
        /// <param name="curve">A <see cref="ICurveSampler{T}"/> instance, or null to remove a track.</param>
        public void SetTrack(string track, ICurveSampler<T> curve)
        {
            Guard.NotNullOrEmpty(track, nameof(track));

            if (curve != null)
            {
                var convertible = curve as IConvertibleCurve<T>;
                Guard.NotNull(convertible, nameof(curve), $"Provided {nameof(ICurveSampler<T>)} {nameof(curve)} must implement {nameof(IConvertibleCurve<T>)} interface.");
            }

            // remove track
            if (curve == null)
            {
                if (_Tracks == null) return;
                _Tracks.Remove(track);
                if (_Tracks.Count == 0) _Tracks = null;
                return;
            }

            // insert track
            if (_Tracks == null) _Tracks = new Dictionary<string, ICurveSampler<T>>();

            _Tracks[track] = curve;
        }

        public CurveBuilder<T> UseTrackBuilder(string track)
        {
            Guard.NotNullOrEmpty(track, nameof(track));

            if (_Tracks == null || !_Tracks.TryGetValue(track, out ICurveSampler<T> sampler))
            {
                sampler = CurveFactory.CreateCurveBuilder<T>() as ICurveSampler<T>;
                SetTrack(track, sampler);
            }

            if (sampler is CurveBuilder<T> builder) return builder;

            throw new NotImplementedException();

            // TODO: CurveFactory.CreateCurveBuilder(sampler);
        }

        #endregion
    }
}
