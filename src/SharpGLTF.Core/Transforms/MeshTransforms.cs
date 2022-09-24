using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using TRANSFORM = System.Numerics.Matrix4x4;
using V2 = System.Numerics.Vector2;
using V3 = System.Numerics.Vector3;
using V4 = System.Numerics.Vector4;

namespace SharpGLTF.Transforms
{
    public interface IMaterialTransform
    {
        V2 MorphTexCoord(V2 texCoord, IReadOnlyList<V2> morphTargets);

        V4 MorphColors(V4 color, IReadOnlyList<V4> morphTargets);
    }

    /// <summary>
    /// Interface for a vertex transform object
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

        /// <summary>
        /// Transforms a vertex position from local mesh space to world space.
        /// </summary>
        /// <param name="localPosition">The local position of the vertex.</param>
        /// <param name="positionDeltas">The local position deltas of the vertex, one for each morph target, or null.</param>
        /// <param name="skinWeights">The skin weights of the vertex, or default.</param>
        /// <returns>A position in world space.</returns>
        V3 TransformPosition(V3 localPosition, IReadOnlyList<V3> positionDeltas, in SparseWeight8 skinWeights);

        /// <summary>
        /// Transforms a vertex normal from local mesh space to world space.
        /// </summary>
        /// <param name="localNormal">The local normal of the vertex.</param>
        /// <param name="normalDeltas">The local normal deltas of the vertex, one for each morph target, or null.</param>
        /// <param name="skinWeights">The skin weights of the vertex, or default.</param>
        /// <returns>A normal in world space.</returns>
        V3 TransformNormal(V3 localNormal, IReadOnlyList<V3> normalDeltas, in SparseWeight8 skinWeights);

        /// <summary>
        /// Transforms a vertex tangent from local mesh space to world space.
        /// </summary>
        /// <param name="tangent">The tangent normal of the vertex.</param>
        /// <param name="tangentDeltas">The local tangent deltas of the vertex, one for each morph target, or null.</param>
        /// <param name="skinWeights">The skin weights of the vertex, or default.</param>
        /// <returns>A tangent in world space.</returns>
        V4 TransformTangent(V4 tangent, IReadOnlyList<V3> tangentDeltas, in SparseWeight8 skinWeights);
    }

    public interface IGeometryInstancing
    {
        int InstancesCount { get; }

        /// <summary>
        /// Gets the list of instances produced by this transform.
        /// </summary>
        IReadOnlyList<RigidTransform> WorldTransforms { get; }
    }

    public abstract class MorphTransform : IMaterialTransform
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
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private SparseWeight8 _Weights;

        public const int COMPLEMENT_INDEX = 65536;

        /// <summary>
        /// True if morph targets represent absolute values.
        /// False if morph targets represent values relative to master value.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
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

        public void Update(in SparseWeight8 morphWeights, bool useAbsoluteMorphTargets = false)
        {
            _AbsoluteMorphTargets = useAbsoluteMorphTargets;

            if (morphWeights.IsWeightless)
            {
                _Weights = SparseWeight8.Create((COMPLEMENT_INDEX, 1));
                return;
            }

            _Weights = morphWeights.GetNormalizedWithComplement(COMPLEMENT_INDEX);
        }

        protected V2 MorphVectors(V2 value, IReadOnlyList<V2> morphTargets)
        {
            if (morphTargets == null || morphTargets.Count == 0) return value;

            if (_Weights.Index0 == COMPLEMENT_INDEX && _Weights.Weight0 == 1) return value;

            var p = V2.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var (index, weight) in _Weights.GetNonZeroWeights())
                {
                    var val = index == COMPLEMENT_INDEX ? value : morphTargets[index];
                    p += val * weight;
                }
            }
            else
            {
                foreach (var (index, weight) in _Weights.GetNonZeroWeights())
                {
                    var val = index == COMPLEMENT_INDEX ? value : value + morphTargets[index];
                    p += val * weight;
                }
            }

            return p;
        }

        protected V3 MorphVectors(V3 value, IReadOnlyList<V3> morphTargets)
        {
            if (morphTargets == null || morphTargets.Count == 0) return value;

            if (_Weights.Index0 == COMPLEMENT_INDEX && _Weights.Weight0 == 1) return value;

            var p = V3.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var (index, weight) in _Weights.GetNonZeroWeights())
                {
                    var val = index == COMPLEMENT_INDEX ? value : morphTargets[index];
                    p += val * weight;
                }
            }
            else
            {
                foreach (var (index, weight) in _Weights.GetNonZeroWeights())
                {
                    var val = index == COMPLEMENT_INDEX ? value : value + morphTargets[index];
                    p += val * weight;
                }
            }

            return p;
        }

        protected V4 MorphVectors(V4 value, IReadOnlyList<V4> morphTargets)
        {
            if (morphTargets == null || morphTargets.Count == 0) return value;

            if (_Weights.Index0 == COMPLEMENT_INDEX && _Weights.Weight0 == 1) return value;

            var p = V4.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var (index, weight) in _Weights.GetNonZeroWeights())
                {
                    var val = index == COMPLEMENT_INDEX ? value : morphTargets[index];
                    p += val * weight;
                }
            }
            else
            {
                foreach (var (index, weight) in _Weights.GetNonZeroWeights())
                {
                    var val = index == COMPLEMENT_INDEX ? value : value + morphTargets[index];
                    p += val * weight;
                }
            }

            return p;
        }

        public V4 MorphColors(V4 color, IReadOnlyList<V4> morphTargets)
        {
            return MorphVectors(color, morphTargets);
        }

        public V2 MorphTexCoord(V2 texCoord, IReadOnlyList<V2> morphTargets)
        {
            return MorphVectors(texCoord, morphTargets);
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

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private TRANSFORM _WorldMatrix;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private Boolean _Visible;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private Boolean _FlipFaces;

        #endregion

        #region properties

        public Boolean Visible => _Visible;

        public Boolean FlipFaces => _FlipFaces;

        public TRANSFORM WorldMatrix => _WorldMatrix;

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

        public V3 TransformPosition(V3 localPosition, IReadOnlyList<V3> positionDeltas, in SparseWeight8 skinWeights)
        {
            localPosition = MorphVectors(localPosition, positionDeltas);

            return V3.Transform(localPosition, _WorldMatrix);
        }

        public V3 TransformNormal(V3 localNormal, IReadOnlyList<V3> normalDeltas, in SparseWeight8 skinWeights)
        {
            localNormal = MorphVectors(localNormal, normalDeltas);

            return V3.Normalize(V3.TransformNormal(localNormal, _WorldMatrix));
        }

        public V4 TransformTangent(V4 tangent, IReadOnlyList<V3> tangentDeltas, in SparseWeight8 skinWeights)
        {
            var t = MorphVectors(new V3(tangent.X, tangent.Y, tangent.Z), tangentDeltas);

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

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private TRANSFORM[] _SkinTransforms;

        #endregion

        #region properties

        public bool Visible => true;

        public bool FlipFaces => false;

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

        public V3 TransformPosition(V3 localPosition, IReadOnlyList<V3> positionDeltas, in SparseWeight8 skinWeights)
        {
            localPosition = MorphVectors(localPosition, positionDeltas);

            var worldPosition = V3.Zero;

            var wnrm = 1.0f / skinWeights.WeightSum;

            foreach (var (jidx, jweight) in skinWeights.GetIndexedWeights())
            {
                worldPosition += V3.Transform(localPosition, _SkinTransforms[jidx]) * jweight * wnrm;
            }

            return worldPosition;
        }

        public V3 TransformNormal(V3 localNormal, IReadOnlyList<V3> normalDeltas, in SparseWeight8 skinWeights)
        {
            localNormal = MorphVectors(localNormal, normalDeltas);

            var worldNormal = V3.Zero;

            foreach (var (jidx, jweight) in skinWeights.GetIndexedWeights())
            {
                worldNormal += V3.TransformNormal(localNormal, _SkinTransforms[jidx]) * jweight;
            }

            return V3.Normalize(localNormal);
        }

        public V4 TransformTangent(V4 tangent, IReadOnlyList<V3> tangentDeltas, in SparseWeight8 skinWeights)
        {
            var localTangentV = MorphVectors(new V3(tangent.X, tangent.Y, tangent.Z), tangentDeltas);

            var worldTangent = V3.Zero;

            foreach (var (jidx, jweight) in skinWeights.GetIndexedWeights())
            {
                worldTangent += V3.TransformNormal(localTangentV, _SkinTransforms[jidx]) * jweight;
            }

            worldTangent = V3.Normalize(worldTangent);

            return new V4(worldTangent, tangent.W);
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

            if (jointWorldTransform.M44 == 1) invJoint.M44 = 1; // fix precission loss;

            if (meshWorldTransform == Matrix4x4Double.Identity) return invJoint;

            return meshWorldTransform * invJoint;
        }

        #endregion
    }

    public class InstancingTransform : RigidTransform, IGeometryInstancing
    {
        #region lifecycle

        public InstancingTransform(AffineTransform[] instances)
        {
            Guard.NotNull(instances, nameof(instances));

            _LocalMatrices = new TRANSFORM[instances.Length];

            for (int i = 0; i < _LocalMatrices.Length; ++i)
            {
                _LocalMatrices[i] = instances[i].Matrix;
            }

            _WorldTransforms = new Lazy<RigidTransform[]>(_CreateTransforms);
        }

        #endregion

        #region data

        private readonly Matrix4x4[] _LocalMatrices;

        private Lazy<RigidTransform[]> _WorldTransforms;

        #endregion

        #region properties

        /// <inheritdoc/>
        public int InstancesCount => _LocalMatrices.Length;

        /// <summary>
        /// Gets the local matrices for every instanced mesh
        /// </summary>
        public IReadOnlyList<TRANSFORM> LocalMatrices => _LocalMatrices;

        /// <inheritdoc/>
        public IReadOnlyList<RigidTransform> WorldTransforms => UpdateInstances();        

        #endregion

        #region API

        private RigidTransform[] _CreateTransforms()
        {
            var xforms = new RigidTransform[_LocalMatrices.Length];

            for (int i = 0; i < xforms.Length; ++i)
            {
                xforms[i] = new RigidTransform();
            }

            return xforms;
        }

        public RigidTransform[] UpdateInstances()
        {
            var xforms = _WorldTransforms.Value;

            for (int i = 0; i < xforms.Length; ++i)
            {
                var xform = AffineTransform
                    .Multiply(_LocalMatrices[i], this.WorldMatrix)
                    .Matrix;

                xforms[i].Update(xform);
            }

            return xforms;
        }

        public static IEnumerable<IGeometryTransform> Evaluate(IGeometryTransform xform)
        {
            if (xform is IGeometryInstancing instanced)
            {
                foreach (var xinst in instanced.WorldTransforms) yield return xinst;
                yield break;
            }

            yield return xform;
        }

        #endregion
    }
}
