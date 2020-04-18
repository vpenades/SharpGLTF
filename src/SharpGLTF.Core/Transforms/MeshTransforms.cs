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
        /// - Index of value <see cref="COMPLEMENT_INDEX"/> points to the Mesh master positions.
        /// - All other indices point to Mesh MorphTarget[index] positions.
        /// </summary>
        private SparseWeight8 _Weights;

        public const int COMPLEMENT_INDEX = 65536;

        /// <summary>
        /// True if morph targets represent absolute values.
        /// False if morph targets represent values relative to master value.
        /// </summary>
        private bool _AbsoluteMorphTargets;

        #endregion

        #region properties

        /// <summary>
        /// Gets the current morph weights to use for morph target blending. <see cref="COMPLEMENT_INDEX"/> represents the index for the base geometry.
        /// </summary>
        public SparseWeight8 MorphWeights => _Weights;

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
                _Weights = SparseWeight8.Create((COMPLEMENT_INDEX, 1));
                return;
            }

            _Weights = morphWeights.GetNormalizedWithComplement(COMPLEMENT_INDEX);
        }

        protected V3 MorphVectors(V3 value, V3[] morphTargets)
        {
            if (_Weights.Index0 == COMPLEMENT_INDEX && _Weights.Weight0 == 1) return value;

            if (morphTargets == null) return value;

            var p = V3.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Index == COMPLEMENT_INDEX ? value : morphTargets[pair.Index];
                    p += val * pair.Weight;
                }
            }
            else
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Index == COMPLEMENT_INDEX ? value : value + morphTargets[pair.Index];
                    p += val * pair.Weight;
                }
            }

            return p;
        }

        protected V4 MorphVectors(V4 value, V4[] morphTargets)
        {
            if (_Weights.Index0 == COMPLEMENT_INDEX && _Weights.Weight0 == 1) return value;

            if (morphTargets == null) return value;

            var p = V4.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Index == COMPLEMENT_INDEX ? value : morphTargets[pair.Index];
                    p += val * pair.Weight;
                }
            }
            else
            {
                foreach (var pair in _Weights.GetNonZeroWeights())
                {
                    var val = pair.Index == COMPLEMENT_INDEX ? value : value + morphTargets[pair.Index];
                    p += val * pair.Weight;
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

    public class RigidTransform : MorphTransform, IGeometryTransform
    {
        #region constructor

        public RigidTransform()
        {
            Update(TRANSFORM.Identity);
        }

        public RigidTransform(TRANSFORM worldMatrix)
        {
            Update(default, false);
            Update(worldMatrix);
        }

        public RigidTransform(TRANSFORM worldMatrix, SparseWeight8 morphWeights, bool useAbsoluteMorphs)
        {
            Update(morphWeights, useAbsoluteMorphs);
            Update(worldMatrix);
        }

        #endregion

        #region data

        private TRANSFORM _WorldMatrix;
        private Boolean _Visible;
        private Boolean _FlipFaces;

        #endregion

        #region properties

        public Boolean Visible => _Visible;

        public Boolean FlipFaces => _FlipFaces;

        public Matrix4x4 WorldMatrix => _WorldMatrix;

        #endregion

        #region API

        public void Update(TRANSFORM worldMatrix)
        {
            _WorldMatrix = worldMatrix;

            // http://m-hikari.com/ija/ija-password-2009/ija-password5-8-2009/hajrizajIJA5-8-2009.pdf

            float determinant3x3 =
                +(worldMatrix.M13 * worldMatrix.M21 * worldMatrix.M32)
                + (worldMatrix.M11 * worldMatrix.M22 * worldMatrix.M33)
                + (worldMatrix.M12 * worldMatrix.M23 * worldMatrix.M31)
                - (worldMatrix.M12 * worldMatrix.M21 * worldMatrix.M33)
                - (worldMatrix.M13 * worldMatrix.M22 * worldMatrix.M31)
                - (worldMatrix.M11 * worldMatrix.M23 * worldMatrix.M32);

            _Visible = Math.Abs(determinant3x3) > float.Epsilon;
            _FlipFaces = determinant3x3 < 0;
        }

        public V3 TransformPosition(V3 position, V3[] morphTargets, in SparseWeight8 skinWeights)
        {
            position = MorphVectors(position, morphTargets);

            return V3.Transform(position, _WorldMatrix);
        }

        public V3 TransformNormal(V3 normal, V3[] morphTargets, in SparseWeight8 skinWeights)
        {
            normal = MorphVectors(normal, morphTargets);

            return V3.Normalize(V3.TransformNormal(normal, _WorldMatrix));
        }

        public V4 TransformTangent(V4 tangent, V3[] morphTargets, in SparseWeight8 skinWeights)
        {
            var t = MorphVectors(new V3(tangent.X, tangent.Y, tangent.Z), morphTargets);

            t = V3.Normalize(V3.TransformNormal(t, _WorldMatrix));

            return new V4(t, tangent.W);
        }

        #endregion
    }

    public class SkinnedTransform : MorphTransform, IGeometryTransform
    {
        #region constructor

        public SkinnedTransform() { }

        public SkinnedTransform(TRANSFORM[] invBindMatrix, TRANSFORM[] currWorldMatrix, SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Update(morphWeights, useAbsoluteMorphTargets);
            Update(invBindMatrix, currWorldMatrix);
        }

        public SkinnedTransform(int count, Func<int, TRANSFORM> invBindMatrix, Func<int, TRANSFORM> currWorldMatrix, SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Update(morphWeights, useAbsoluteMorphTargets);
            Update(count, invBindMatrix, currWorldMatrix);
        }

        #endregion

        #region data

        private TRANSFORM[] _SkinTransforms;

        #endregion

        #region properties

        /// <summary>
        /// Gets the collection of the current, final matrices to use for skinning
        /// </summary>
        public IReadOnlyList<TRANSFORM> SkinMatrices => _SkinTransforms;

        #endregion

        #region API

        public void Update(TRANSFORM[] invBindMatrix, TRANSFORM[] currWorldMatrix)
        {
            Guard.NotNull(invBindMatrix, nameof(invBindMatrix));
            Guard.NotNull(currWorldMatrix, nameof(currWorldMatrix));
            Guard.IsTrue(invBindMatrix.Length == currWorldMatrix.Length, nameof(currWorldMatrix), $"{invBindMatrix} and {currWorldMatrix} length mismatch.");

            if (_SkinTransforms == null || _SkinTransforms.Length != invBindMatrix.Length) _SkinTransforms = new TRANSFORM[invBindMatrix.Length];

            for (int i = 0; i < _SkinTransforms.Length; ++i)
            {
                _SkinTransforms[i] = invBindMatrix[i] * currWorldMatrix[i];
            }
        }

        public void Update(int count, Func<int, TRANSFORM> invBindMatrix, Func<int, TRANSFORM> currWorldMatrix)
        {
            Guard.NotNull(invBindMatrix, nameof(invBindMatrix));
            Guard.NotNull(currWorldMatrix, nameof(currWorldMatrix));

            if (_SkinTransforms == null || _SkinTransforms.Length != count) _SkinTransforms = new TRANSFORM[count];

            for (int i = 0; i < _SkinTransforms.Length; ++i)
            {
                _SkinTransforms[i] = invBindMatrix(i) * currWorldMatrix(i);
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
                worldPosition += V3.Transform(localPosition, _SkinTransforms[jw.Index]) * jw.Weight * wnrm;
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
                worldNormal += V3.TransformNormal(localNormal, _SkinTransforms[jw.Index]) * jw.Weight;
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
                worldTangent += V3.TransformNormal(localTangentV, _SkinTransforms[jw.Index]) * jw.Weight;
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
            var invJoint = jointWorldTransform.Inverse();

            if (meshWorldTransform == TRANSFORM.Identity) return invJoint;

            return meshWorldTransform * invJoint;
        }

        public static Matrix4x4Double CalculateInverseBinding(Matrix4x4Double meshWorldTransform, Matrix4x4Double jointWorldTransform)
        {
            if (!Matrix4x4Double.Invert(jointWorldTransform, out Matrix4x4Double invJoint)) Guard.IsTrue(false, nameof(jointWorldTransform), "Matrix cannot be inverted.");

            if (meshWorldTransform == Matrix4x4Double.Identity) return invJoint;

            return meshWorldTransform * invJoint;
        }

        #endregion
    }
}
