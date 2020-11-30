using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Defines a curve that can be sampled at any point.
    /// </summary>
    /// <typeparam name="T">The type of a point in the curve.</typeparam>
    public interface ICurveSampler<T>
    {
        /// <summary>
        /// Samples the curve at the given offset.
        /// </summary>
        /// <param name="offset">The curve offset to sample.</param>
        /// <returns>The value of the curve at <paramref name="offset"/>.</returns>
        T GetPoint(Single offset);
    }

    /// <summary>
    /// Defines methods that convert the current curve to a Step, Linear or Spline curve.
    /// </summary>
    /// <typeparam name="T">The type of a point of the curve</typeparam>
    public interface IConvertibleCurve<T>
    {
        /// <summary>
        /// Gets a value indicating the maximum degree of the curve, current values are:
        /// 0: STEP.
        /// 1: LINEAR.
        /// 3: CUBIC.
        /// </summary>
        int MaxDegree { get; }

        /// <summary>
        /// Gets a STEP interpolated curve. Use only when <see cref="MaxDegree"/> is 0.
        /// </summary>
        /// <returns>A Time-Value dictionary</returns>
        IReadOnlyDictionary<Single, T> ToStepCurve();

        /// <summary>
        /// Gets a LINEAR interpolated curve. Use only when <see cref="MaxDegree"/> is 1.
        /// </summary>
        /// <returns>A Time-Value dictionary</returns>
        IReadOnlyDictionary<Single, T> ToLinearCurve();

        /// <summary>
        /// Gets a CUBIC interpolated curve. Use only when <see cref="MaxDegree"/> is 3.
        /// </summary>
        /// <returns>A Time-Value dictionary</returns>
        IReadOnlyDictionary<Single, (T TangentIn, T Value, T TangentOut)> ToSplineCurve();
    }
}
