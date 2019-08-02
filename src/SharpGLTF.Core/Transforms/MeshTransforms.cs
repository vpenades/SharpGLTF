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
    public interface IGeometryTransform
    {
        /// <summary>
        /// Gets a value indicating whether the current <see cref="IGeometryTransform"/> will render visible geometry.
        /// </summary>
        /// <remarks>
        /// When this value is false, a runtime should skip rendering any geometry using
        /// this <see cref="IGeometryTransform"/> instance, since it will not be visible anyway.
        /// </remarks>
        bool Visible { get; }

        /// <summary>
        /// Gets a value indicating whether the triangles need to be flipped to render correctly.
        /// </summary>
        /// <remarks>
        /// When this value is true, a runtime rendering triangles should inverse the face culling.
        /// </remarks>
        bool FlipFaces { get; }

        V3 TransformPosition(V3 position, V3[] morphTargets, in SparseWeight8 skinWeights);
        V3 TransformNormal(V3 normal, V3[] morphTargets, in SparseWeight8 skinWeights);
        V4 TransformTangent(V4 tangent, V3[] morphTargets, in SparseWeight8 skinWeights);

        V4 MorphColors(V4 color, V4[] morphTargets);
    }

    public abstract class MorphTransform
    {
        #region constructor

        protected MorphTransform()
        {
            Update(default, false);
        }

        protected MorphTransform(SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Update(morphWeights, useAbsoluteMorphTargets);
        }

        #endregion

        #region data

        /// <summary>
        /// Represents a sparse collection of weights where:
        /// - Index of value <see cref="_COMPLEMENT_INDEX"/> points to the Mesh master positions.
        /// - All other indices point to Mesh MorphTarget[index] positions.
        /// </summary>
        private SparseWeight8 _Weights;

        private const int _COMPLEMENT_INDEX = 65536;

        /// <summary>
        /// True if morph targets represent absolute values.
        /// False if morph targets represent values relative to master value.
        /// </summary>
        private bool _AbsoluteMorphTargets;

        #endregion

        #region properties

        /// <summary>
        /// Gets a value indicating whether morph target values are absolute, and not relative to the master value.
        /// </summary>
        public bool AbsoluteMorphTargets => _AbsoluteMorphTargets;

        #endregion

        #region API

        public void Update(SparseWeight8 morphWeights, bool useAbsoluteMorphTargets = false)
        {
            _AbsoluteMorphTargets = useAbsoluteMorphTargets;

            if (morphWeights.IsWeightless)
            {
                _Weights = SparseWeight8.Create((_COMPLEMENT_INDEX, 1));
                return;
            }

            _Weights = morphWeights.GetNormalizedWithComplement(_COMPLEMENT_INDEX);
        }

        protected V3 MorphVectors(V3 value, V3[] morphTargets)
        {
            if (_Weights.Index0 == _COMPLEMENT_INDEX && _Weights.Weight0 == 1) return value;

            if (morphTargets == null) return value;

            var p = V3.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Item1 == _COMPLEMENT_INDEX ? value : morphTargets[pair.Item1];
                    p += val * pair.Item2;
                }
            }
            else
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Item1 == _COMPLEMENT_INDEX ? value : value + morphTargets[pair.Item1];
                    p += val * pair.Item2;
                }
            }

            return p;
        }

        protected V4 MorphVectors(V4 value, V4[] morphTargets)
        {
            if (_Weights.Index0 == _COMPLEMENT_INDEX && _Weights.Weight0 == 1) return value;

            if (morphTargets == null) return value;

            var p = V4.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Item1 == _COMPLEMENT_INDEX ? value : morphTargets[pair.Item1];
                    p += val * pair.Item2;
                }
            }
            else
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Item1 == _COMPLEMENT_INDEX ? value : value + morphTargets[pair.Item1];
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

    public class StaticTransform : MorphTransform, IGeometryTransform
    {
        #region constructor

        public StaticTransform()
        {
            Update(TRANSFORM.Identity);
        }

        public StaticTransform(TRANSFORM xform)
        {
            Update(default, false);
            Update(xform);
        }

        public StaticTransform(TRANSFORM xform, SparseWeight8 morphWeights, bool useAbsoluteMorphs)
        {
            Update(morphWeights, useAbsoluteMorphs);
            Update(xform);
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

        public void Update(TRANSFORM xform)
        {
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

        public V3 TransformPosition(V3 position, V3[] morphTargets, in SparseWeight8 skinWeights)
        {
            position = MorphVectors(position, morphTargets);

            return V3.Transform(position, _Transform);
        }

        public V3 TransformNormal(V3 normal, V3[] morphTargets, in SparseWeight8 skinWeights)
        {
            normal = MorphVectors(normal, morphTargets);

            return V3.Normalize(V3.Transform(normal, _Transform));
        }

        public V4 TransformTangent(V4 tangent, V3[] morphTargets, in SparseWeight8 skinWeights)
        {
            var n = MorphVectors(new V3(tangent.X, tangent.Y, tangent.Z), morphTargets);

            n = V3.Normalize(V3.Transform(n, _Transform));

            return new V4(n, tangent.W);
        }

        #endregion
    }

    public class SkinTransform : MorphTransform, IGeometryTransform
    {
        #region constructor

        public SkinTransform() { }

        public SkinTransform(TRANSFORM[] invBindMatrix, TRANSFORM[] currWorldMatrix, SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Update(morphWeights, useAbsoluteMorphTargets);
            Update(invBindMatrix, currWorldMatrix);
        }

        public SkinTransform(int count, Func<int, TRANSFORM> invBindMatrix, Func<int, TRANSFORM> currWorldMatrix, SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Update(morphWeights, useAbsoluteMorphTargets);
            Update(count, invBindMatrix, currWorldMatrix);
        }

        #endregion

        #region data

        private IList<TRANSFORM> _JointTransforms;

        #endregion

        #region API

        public void Update(TRANSFORM[] invBindMatrix, TRANSFORM[] currWorldMatrix)
        {
            Guard.NotNull(invBindMatrix, nameof(invBindMatrix));
            Guard.NotNull(currWorldMatrix, nameof(currWorldMatrix));
            Guard.IsTrue(invBindMatrix.Length == currWorldMatrix.Length, nameof(currWorldMatrix), $"{invBindMatrix} and {currWorldMatrix} length mismatch.");

            if (_JointTransforms == null || _JointTransforms.Count != invBindMatrix.Length) _JointTransforms = new TRANSFORM[invBindMatrix.Length];

            for (int i = 0; i < _JointTransforms.Count; ++i)
            {
                _JointTransforms[i] = invBindMatrix[i] * currWorldMatrix[i];
            }
        }

        public void Update(int count, Func<int, TRANSFORM> invBindMatrix, Func<int, TRANSFORM> currWorldMatrix)
        {
            Guard.NotNull(invBindMatrix, nameof(invBindMatrix));
            Guard.NotNull(currWorldMatrix, nameof(currWorldMatrix));

            if (_JointTransforms == null || _JointTransforms.Count != count) _JointTransforms = new TRANSFORM[count];

            for (int i = 0; i < _JointTransforms.Count; ++i)
            {
                _JointTransforms[i] = invBindMatrix(i) * currWorldMatrix(i);
            }
        }

        public bool Visible => true;

        public bool FlipFaces => false;

        public V3 TransformPosition(V3 localPosition, V3[] morphTargets, in SparseWeight8 skinWeights)
        {
            Guard.NotNull(skinWeights, nameof(skinWeights));

            localPosition = MorphVectors(localPosition, morphTargets);

            var worldPosition = V3.Zero;

            var wnrm = 1.0f / skinWeights.WeightSum;

            foreach (var jw in skinWeights.GetIndexedWeights())
            {
                worldPosition += V3.Transform(localPosition, _JointTransforms[jw.Item1]) * jw.Item2 * wnrm;
            }

            return worldPosition;
        }

        public V3 TransformNormal(V3 localNormal, V3[] morphTargets, in SparseWeight8 skinWeights)
        {
            Guard.NotNull(skinWeights, nameof(skinWeights));

            localNormal = MorphVectors(localNormal, morphTargets);

            var worldNormal = V3.Zero;

            foreach (var jw in skinWeights.GetIndexedWeights())
            {
                worldNormal += V3.TransformNormal(localNormal, _JointTransforms[jw.Item1]) * jw.Item2;
            }

            return V3.Normalize(localNormal);
        }

        public V4 TransformTangent(V4 localTangent, V3[] morphTargets, in SparseWeight8 skinWeights)
        {
            Guard.NotNull(skinWeights, nameof(skinWeights));

            var localTangentV = MorphVectors(new V3(localTangent.X, localTangent.Y, localTangent.Z), morphTargets);

            var worldTangent = V3.Zero;

            foreach (var jw in skinWeights.GetIndexedWeights())
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
        public static TRANSFORM CalculateInverseBinding(TRANSFORM meshWorldTransform, TRANSFORM jointWorldTransform)
        {
            // var xform = meshWorldTransform.Inverse();
            // xform = jointWorldTransform * xform;
            // return xform.Inverse();

            var invJoint = jointWorldTransform.Inverse();

            return meshWorldTransform * invJoint;
        }

        #endregion
    }
}
