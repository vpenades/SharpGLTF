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

        private Dictionary<string, Curve<T>> _Tracks = new Dictionary<string, Curve<T>>();

        public T Default { get; set; }

        #endregion

        #region properties

        public IReadOnlyDictionary<string, Curve<T>> Tracks => _Tracks;

        #endregion

        #region API

        public T GetValueAt(string track, float value)
        {
            return _Tracks.TryGetValue(track, out Curve<T> sampler) ? sampler.GetPoint(value) : this.Default;
        }

        public Curve<T> UseCurve(string track)
        {
            if (_Tracks.TryGetValue(track, out Curve<T> curve)) return curve;

            _Tracks[track] = curve = CurveFactory.CreateSplineCurve<T>();

            return curve;
        }

        #endregion
    }
}
