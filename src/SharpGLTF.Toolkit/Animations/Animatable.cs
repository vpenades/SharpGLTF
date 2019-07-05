using System;
using System.Collections.Generic;
using System.Numerics;
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

        #region properties

        public IReadOnlyDictionary<string, ICurveSampler<T>> Tracks => _Tracks;

        #endregion

        #region API

        public T GetValueAt(string track, float value)
        {
            return _Tracks.TryGetValue(track, out ICurveSampler<T> sampler) ? sampler.GetPoint(value) : this.Default;
        }

        public ICurveSampler<T> UseLinearCurve(string track)
        {
            if (!_Tracks.TryGetValue(track, out ICurveSampler<T> curve))
            {
                _Tracks[track] = curve = CurveFactory.CreateLinearCurve<T>();
            }

            if (curve is ILinearCurve<T> editableCurve) return editableCurve;

            throw new ArgumentException(nameof(T), "Generic argument not supported");
        }

        public ICurveSampler<T> UseSplineCurve(string track)
        {
            if (!_Tracks.TryGetValue(track, out ICurveSampler<T> curve))
            {
                _Tracks[track] = curve = CurveFactory.CreateSplineCurve<T>();
            }

            if (curve is ISplineCurve<T> editableCurve) return editableCurve;

            throw new ArgumentException(nameof(T), "Generic argument not supported");
        }

        #endregion
    }
}
