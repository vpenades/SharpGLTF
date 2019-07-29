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

        V4 MorphColors(V4 color, V4[] morphTargets);
    }

    public abstract class MorphTransform
    {
        #region constructor

        protected MorphTransform()
        {
            Update(SparseWeight8.Create((0, 1)), false);
        }

        protected MorphTransform(SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Update(morphWeights, useAbsoluteMorphTargets);
        }

        #endregion

        #region data

        /// <summary>
        /// Represents a normalized sparse collection of weights where:
        /// - Indices with value zero point to the master mesh
        /// - Indices with value over zero point to MorphTarget[index-1].
        /// </summary>
        private SparseWeight8 _Weights;

        /// <summary>
        /// True if morph targets represent absolute values.
        /// False if morph targets represent values relative to master value.
        /// </summary>
        private bool _AbsoluteMorphTargets;

        #endregion

        #region API

        public void Update(SparseWeight8 morphWeights, bool useAbsoluteMorphTargets = false)
        {
            _AbsoluteMorphTargets = useAbsoluteMorphTargets;

            if (morphWeights.IsWeightless)
            {
                _Weights = SparseWeight8.Create((0, 1));
                return;
            }

            _Weights = morphWeights.GetNormalizedWithComplement();
        }

        protected V3 MorphVectors(V3 value, V3[] morphTargets)
        {
            if (morphTargets == null) return value;

            if (_Weights.Index0 == 0 && _Weights.Weight0 == 1) return value;

            var p = V3.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Item1 == 0 ? value : morphTargets[pair.Item1 - 1];
                    p += val * pair.Item2;
                }
            }
            else
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Item1 == 0 ? value : value + morphTargets[pair.Item1 - 1];
                    p += val * pair.Item2;
                }
            }

            return p;
        }

        protected V4 MorphVectors(V4 value, V4[] morphTargets)
        {
            if (morphTargets == null) return value;

            if (_Weights.Index0 == 0 && _Weights.Weight0 == 1) return value;

            var p = V4.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Item1 == 0 ? value : morphTargets[pair.Item1 - 1];
                    p += val * pair.Item2;
                }
            }
            else
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Item1 == 0 ? value : value + morphTargets[pair.Item1 - 1];
                    p += val * pair.Item2;
                }
            }

            return p;
        }

        public V4 MorphColors(V4 color, V4[] morphTargets)
        {
            return MorphVectors(color, morphTargets);
        }

        #endregion
    }

    public class StaticTransform : MorphTransform, ITransform
    {
        #region constructor

        public StaticTransform(TRANSFORM xform, SparseWeight8 morphWeights, bool useAbsoluteMorphs)
        {
            Update(xform, morphWeights, useAbsoluteMorphs);
        }

        #endregion

        #region data

        private TRANSFORM _Transform;
        private Boolean _Visible;
        private Boolean _FlipFaces;

        #endregion

        #region properties

        public Boolean Visible => _Visible;

        public Boolean FlipFaces => _FlipFaces;

        #endregion

        #region API

        public void Update(TRANSFORM xform, SparseWeight8 morphWeights, bool useAbsoluteMorphs)
        {
            Update(morphWeights, useAbsoluteMorphs);

            _Transform = xform;

            // http://m-hikari.com/ija/ija-password-2009/ija-password5-8-2009/hajrizajIJA5-8-2009.pdf

            float determinant3x3 =
                +(xform.M13 * xform.M21 * xform.M32)
                + (xform.M11 * xform.M22 * xform.M33)
                + (xform.M12 * xform.M23 * xform.M31)
                - (xform.M12 * xform.M21 * xform.M33)
                - (xform.M13 * xform.M22 * xform.M31)
                - (xform.M11 * xform.M23 * xform.M32);

            _Visible = Math.Abs(determinant3x3) > float.Epsilon;
            _FlipFaces = determinant3x3 < 0;
        }

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

        #endregion
    }

    public class SkinTransform : MorphTransform, ITransform
    {
        #region constructor

        public SkinTransform(TRANSFORM[] invBindings, TRANSFORM[] xforms, SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Update(invBindings, xforms, morphWeights, useAbsoluteMorphTargets);
        }

        #endregion

        #region data

        private TRANSFORM[] _JointTransforms;

        #endregion

        #region API

        public void Update(TRANSFORM[] invBindings, TRANSFORM[] xforms, SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Guard.NotNull(invBindings, nameof(invBindings));
            Guard.NotNull(xforms, nameof(xforms));
            Guard.IsTrue(invBindings.Length == xforms.Length, nameof(xforms), $"{invBindings} and {xforms} length mismatch.");

            Update(morphWeights, useAbsoluteMorphTargets);

            if (_JointTransforms == null || _JointTransforms.Length != invBindings.Length) _JointTransforms = new TRANSFORM[invBindings.Length];

            for (int i = 0; i < _JointTransforms.Length; ++i)
            {
                _JointTransforms[i] = invBindings[i] * xforms[i];
            }
        }

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

        #endregion

        #region helper utilities

        /// <summary>
        /// Calculates the inverse bind matrix to use for runtime skinning.
        /// </summary>
        /// <param name="meshWorldTransform">The world space <see cref="TRANSFORM"/> of the mesh at the time of binding (POSE).</param>
        /// <param name="jointWorldTransform">The world space <see cref="TRANSFORM"/> of the given bone joint at the time of binding (POSE).</param>
        /// <returns>A <see cref="TRANSFORM"/> representing the inverse bind transform.</returns>
        public static Matrix4x4 CalculateInverseBinding(Matrix4x4 meshWorldTransform, Matrix4x4 jointWorldTransform)
        {
            var xform = meshWorldTransform.Inverse();

            xform = jointWorldTransform * xform;

            return xform.Inverse();
        }

        #endregion
    }
}
