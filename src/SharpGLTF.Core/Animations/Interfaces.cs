using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Defines a curve that can be sampled at specific points.
    /// </summary>
    /// <typeparam name="T">The type of a point in the curve.</typeparam>
    public interface ICurveSampler<T>
    {
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

        IReadOnlyDictionary<Single, T> ToStepCurve();
        IReadOnlyDictionary<Single, T> ToLinearCurve();
        IReadOnlyDictionary<Single, (T TangentIn, T Value, T TangentOut)> ToSplineCurve();
    }
}
