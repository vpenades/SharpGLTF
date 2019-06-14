using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using TRANSFORM = System.Numerics.Matrix4x4;
using V3 = System.Numerics.Vector3;
using V4 = System.Numerics.Vector4;

namespace SharpGLTF.Transforms
{
    /// <summary>
    /// Interface for a mesh transform object
    /// </summary>
    public interface ITransform
    {
        /// <summary>
        /// Gets a value indicating whether the current <see cref="ITransform"/> will render visible geometry.
        /// </summary>
        bool Visible { get; }

        /// <summary>
        /// Gets a value indicating whether the triangles need to be flipped to render correctly.
        /// </summary>
        bool FlipFaces { get; }

        V3 TransformPosition(V3 position, V3[] morphTargets, (int, float)[] skinWeights);
        V3 TransformNormal(V3 normal, V3[] morphTargets, (int, float)[] skinWeights);
        V4 TransformTangent(V4 tangent, V3[] morphTargets, (int, float)[] skinWeights);
    }

    public abstract class MorphTransform
    {
        #region constructor

        protected MorphTransform(IReadOnlyList<float> morphWeights)
        {
            if (morphWeights == null || morphWeights.Count == 0)
            {
                _InvWeight = 1;
                return;
            }

            var sum = morphWeights.Sum();

            if (sum == 0)
            {
                _InvWeight = 1;
                return;
            }

            _MorphWeights = new float[morphWeights.Count];
            for (int i = 0; i < morphWeights.Count; ++i) _MorphWeights[i] = morphWeights[i];

            if (sum <= 1)
            {
                _InvWeight = 1 - sum;
            }
            else
            {
                _InvWeight = 0;
                for (int i = 0; i < morphWeights.Count; ++i) _MorphWeights[i] = _MorphWeights[i] / sum;
            }
        }

        #endregion

        #region data

        private readonly float _InvWeight;
        private readonly float[] _MorphWeights;

        #endregion

        #region properties

        public float InverseWeight => _InvWeight;

        public IReadOnlyList<float> MorphWeights => _MorphWeights;

        #endregion

        #region API

        protected V3 MorphVectors(V3 value, V3[] morphTargets)
        {
            if (_InvWeight == 1 || morphTargets == null || morphTargets.Length == 0) return value;

            Guard.IsTrue(_MorphWeights.Length == morphTargets.Length, nameof(morphTargets));

            var p = value * _InvWeight;

            for (int i = 0; i < _MorphWeights.Length; ++i)
            {
                p += morphTargets[i] * _MorphWeights[i];
            }

            return p;
        }

        #endregion
    }

    public class StaticTransform : MorphTransform, ITransform
    {
        public StaticTransform(TRANSFORM xform, IReadOnlyList<float> morphWeights = null)
            : base(morphWeights)
        {
            _Transform = xform;

            // http://m-hikari.com/ija/ija-password-2009/ija-password5-8-2009/hajrizajIJA5-8-2009.pdf
            var determinant3x3 =
                +(xform.M13 * xform.M21 * xform.M32)
                +(xform.M11 * xform.M22 * xform.M33)
                +(xform.M12 * xform.M23 * xform.M31)
                -(xform.M12 * xform.M21 * xform.M33)
                -(xform.M13 * xform.M22 * xform.M31)
                -(xform.M11 * xform.M23 * xform.M32);

            _Visible = Math.Abs(determinant3x3) > float.Epsilon;
            _FlipFaces = determinant3x3 < 0;
        }

        private readonly TRANSFORM _Transform;
        private readonly Boolean _Visible;
        private readonly Boolean _FlipFaces;

        public Boolean Visible => _Visible;

        public Boolean FlipFaces => _FlipFaces;

        public V3 TransformPosition(V3 position, V3[] morphTargets, (int, float)[] skinWeights)
        {
            position = MorphVectors(position, morphTargets);

            return V3.Transform(position, _Transform);
        }

        public V3 TransformNormal(V3 normal, V3[] morphTargets, (int, float)[] skinWeights)
        {
            normal = MorphVectors(normal, morphTargets);

            return V3.Normalize(V3.Transform(normal, _Transform));
        }

        public V4 TransformTangent(V4 tangent, V3[] morphTargets, (int, float)[] skinWeights)
        {
            var n = MorphVectors(new V3(tangent.X, tangent.Y, tangent.Z), morphTargets);

            n = V3.Normalize(V3.Transform(n, _Transform));

            return new V4(n, tangent.W);
        }
    }

    public class SkinTransform : MorphTransform, ITransform
    {
        public SkinTransform(TRANSFORM[] invBindings, TRANSFORM[] xforms, IReadOnlyList<float> morphWeights = null)
            : base(morphWeights)
        {
            Guard.NotNull(invBindings, nameof(invBindings));
            Guard.NotNull(xforms, nameof(xforms));
            Guard.IsTrue(invBindings.Length == xforms.Length, nameof(xforms), $"{invBindings} and {xforms} length mismatch.");

            _JointTransforms = new TRANSFORM[invBindings.Length];

            for (int i = 0; i < _JointTransforms.Length; ++i)
            {
                _JointTransforms[i] = invBindings[i] * xforms[i];
            }
        }

        private readonly TRANSFORM[] _JointTransforms;

        public bool Visible => true;

        public bool FlipFaces => false;

        public V3 TransformPosition(V3 localPosition, V3[] morphTargets, (int, float)[] skinWeights)
        {
            localPosition = MorphVectors(localPosition, morphTargets);

            var worldPosition = V3.Zero;

            var wnrm = 1.0f / skinWeights.Sum(item => item.Item2);

            foreach (var jw in skinWeights)
            {
                worldPosition += V3.Transform(localPosition, _JointTransforms[jw.Item1]) * jw.Item2 * wnrm;
            }

            return worldPosition;
        }

        public V3 TransformNormal(V3 localNormal, V3[] morphTargets, (int, float)[] skinWeights)
        {
            localNormal = MorphVectors(localNormal, morphTargets);

            var worldNormal = V3.Zero;

            foreach (var jw in skinWeights)
            {
                worldNormal += V3.TransformNormal(localNormal, _JointTransforms[jw.Item1]) * jw.Item2;
            }

            return V3.Normalize(localNormal);
        }

        public V4 TransformTangent(V4 localTangent, V3[] morphTargets, (int, float)[] skinWeights)
        {
            var localTangentV = MorphVectors(new V3(localTangent.X, localTangent.Y, localTangent.Z), morphTargets);

            var worldTangent = V3.Zero;

            foreach (var jw in skinWeights)
            {
                worldTangent += V3.TransformNormal(localTangentV, _JointTransforms[jw.Item1]) * jw.Item2;
            }

            worldTangent = V3.Normalize(worldTangent);

            return new V4(worldTangent, localTangent.W);
        }
    }
}
