using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using TRANSFORM = SharpGLTF.Transforms.AffineTransform;

namespace SharpGLTF.Runtime
{
    public interface IDrawableTemplate
    {
        string NodeName { get; }

        int LogicalMeshIndex { get; }
    }

    /// <summary>
    /// Defines a reference to a drawable mesh
    /// </summary>
    abstract class DrawableTemplate : IDrawableTemplate
    {
        #region lifecycle

        protected DrawableTemplate(Schema2.Node node)
        {
            _LogicalMeshIndex = node.Mesh.LogicalIndex;

            _NodeName = node.Name;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly String _NodeName;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly int _LogicalMeshIndex;

        #endregion

        #region properties

        public String NodeName => _NodeName;

        /// <summary>
        /// Gets the index of a <see cref="Schema2.Mesh"/> in <see cref="Schema2.ModelRoot.LogicalMeshes"/>
        /// </summary>
        public int LogicalMeshIndex => _LogicalMeshIndex;

        #endregion

        #region API

        public abstract Transforms.IGeometryTransform CreateGeometryTransform();

        public abstract void UpdateGeometryTransform(Transforms.IGeometryTransform geoxform, ArmatureInstance armature);

        #endregion
    }

    /// <summary>
    /// Defines a reference to a drawable rigid mesh
    /// </summary>
    class RigidDrawableTemplate : DrawableTemplate
    {
        #region lifecycle

        internal RigidDrawableTemplate(Schema2.Node node, Func<Schema2.Node, int> indexFunc)
            : base(node)
        {
            _NodeIndex = indexFunc(node);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly int _NodeIndex;

        #endregion

        #region API

        public override Transforms.IGeometryTransform CreateGeometryTransform() { return new Transforms.RigidTransform(); }

        public override void UpdateGeometryTransform(Transforms.IGeometryTransform rigidTransform, ArmatureInstance armature)
        {
            var node = armature.LogicalNodes[_NodeIndex];

            var statxform = (Transforms.RigidTransform)rigidTransform;
            statxform.Update(node.ModelMatrix);
            statxform.Update(node.MorphWeights, false);
        }

        #endregion
    }

    class InstancedDrawableTemplate : RigidDrawableTemplate
    {
        #region lifecycle

        internal InstancedDrawableTemplate(Schema2.Node node, Func<Schema2.Node, int> indexFunc)
            : base(node, indexFunc)
        {
            var instancing = node.GetGpuInstancing();

            _Instances = new TRANSFORM[instancing.Count];

            for (int i = 0; i < _Instances.Length; ++i)
            {
                _Instances[i] = instancing.GetLocalTransform(i);
            }
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly TRANSFORM[] _Instances;

        #endregion

        #region properties

        public IReadOnlyList<TRANSFORM> Instances => _Instances;

        #endregion

        #region API

        public override Transforms.IGeometryTransform CreateGeometryTransform() { return new Transforms.InstancingTransform(_Instances); }

        public override void UpdateGeometryTransform(Transforms.IGeometryTransform rigidTransform, ArmatureInstance armature)
        {
            base.UpdateGeometryTransform(rigidTransform, armature);
            (rigidTransform as Transforms.InstancingTransform).UpdateInstances();
        }

        #endregion
    }

    /// <summary>
    /// Defines a reference to a drawable skinned mesh
    /// </summary>
    sealed class SkinnedDrawableTemplate : DrawableTemplate
    {
        #region lifecycle

        internal SkinnedDrawableTemplate(Schema2.Node node, Func<Schema2.Node, int> indexFunc)
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

        public override void UpdateGeometryTransform(Transforms.IGeometryTransform skinnedTransform, ArmatureInstance armature)
        {
            var skinxform = (Transforms.SkinnedTransform)skinnedTransform;
            skinxform.Update(_JointsNodeIndices.Length, idx => _BindMatrices[idx], idx => armature.LogicalNodes[_JointsNodeIndices[idx]].ModelMatrix);
            skinxform.Update(armature.LogicalNodes[_MorphNodeIndex].MorphWeights, false);
        }

        #endregion
    }
}
