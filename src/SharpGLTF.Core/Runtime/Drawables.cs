using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Defines a reference to a drawable mesh
    /// </summary>
    abstract class DrawableReference
    {
        #region lifecycle

        protected DrawableReference(Schema2.Node node)
        {
            _LogicalMeshIndex = node.Mesh.LogicalIndex;
        }

        #endregion

        #region data

        private readonly int _LogicalMeshIndex;

        #endregion

        #region properties

        /// <summary>
        /// Gets the index of a <see cref="Schema2.Mesh"/> in <see cref="Schema2.ModelRoot.LogicalMeshes"/>
        /// </summary>
        public int LogicalMeshIndex => _LogicalMeshIndex;

        #endregion

        #region API

        public abstract Transforms.IGeometryTransform CreateGeometryTransform();

        public abstract void UpdateGeometryTransform(Transforms.IGeometryTransform geoxform, IReadOnlyList<NodeInstance> instances);

        #endregion
    }

    /// <summary>
    /// Defines a reference to a drawable rigid mesh
    /// </summary>
    sealed class RigidDrawableReference : DrawableReference
    {
        #region lifecycle

        internal RigidDrawableReference(Schema2.Node node, Func<Schema2.Node, int> indexFunc)
            : base(node)
        {
            _NodeIndex = indexFunc(node);
        }

        #endregion

        #region data

        private readonly int _NodeIndex;

        #endregion

        #region API

        public override Transforms.IGeometryTransform CreateGeometryTransform() { return new Transforms.RigidTransform(); }

        public override void UpdateGeometryTransform(Transforms.IGeometryTransform rigidTransform, IReadOnlyList<NodeInstance> instances)
        {
            var node = instances[_NodeIndex];

            var statxform = (Transforms.RigidTransform)rigidTransform;
            statxform.Update(node.WorldMatrix);
            statxform.Update(node.MorphWeights, false);
        }

        #endregion
    }

    /// <summary>
    /// Defines a reference to a drawable skinned mesh
    /// </summary>
    sealed class SkinnedDrawableReference : DrawableReference
    {
        #region lifecycle

        internal SkinnedDrawableReference(Schema2.Node node, Func<Schema2.Node, int> indexFunc)
            : base(node)
        {
            var skin = node.Skin;

            _MorphNodeIndex = indexFunc(node);

            _JointsNodeIndices = new int[skin.JointsCount];
            _BindMatrices = new Matrix4x4[skin.JointsCount];

            for (int i = 0; i < _JointsNodeIndices.Length; ++i)
            {
                var (j, ibm) = skin.GetJoint(i);

                _JointsNodeIndices[i] = indexFunc(j);
                _BindMatrices[i] = ibm;
            }
        }

        #endregion

        #region data

        private readonly int _MorphNodeIndex;
        private readonly int[] _JointsNodeIndices;
        private readonly Matrix4x4[] _BindMatrices;

        #endregion

        #region API

        public override Transforms.IGeometryTransform CreateGeometryTransform() { return new Transforms.SkinnedTransform(); }

        public override void UpdateGeometryTransform(Transforms.IGeometryTransform skinnedTransform, IReadOnlyList<NodeInstance> instances)
        {
            var skinxform = (Transforms.SkinnedTransform)skinnedTransform;
            skinxform.Update(_JointsNodeIndices.Length, idx => _BindMatrices[idx], idx => instances[_JointsNodeIndices[idx]].WorldMatrix);
            skinxform.Update(instances[_MorphNodeIndex].MorphWeights, false);
        }

        #endregion
    }
}
