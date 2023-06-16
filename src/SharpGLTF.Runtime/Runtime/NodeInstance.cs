using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.Transforms;

using XFORM = System.Numerics.Matrix4x4;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Defines a node of a scene graph in <see cref="SceneInstance"/>
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public sealed class NodeInstance
    {
        #region lifecycle

        internal NodeInstance(NodeTemplate template, NodeInstance parent)
        {
            _Template = template;
            _Parent = parent;
        }

        #endregion

        #region data

        private readonly NodeTemplate _Template;
        private readonly NodeInstance _Parent;

        private XFORM _LocalMatrix;
        private XFORM? _WorldMatrix;

        private SparseWeight8 _MorphWeights;

        #endregion

        #region properties

        public String Name => _Template.Name;

        public Object Extras => _Template.Extras;

        public NodeInstance VisualParent => _Parent;

        public SparseWeight8 MorphWeights
        {
            get => _MorphWeights;
            set => _MorphWeights = value;
        }

        /// <summary>
        /// Gets or sets the transform matrix of this node in Local Space.
        /// </summary>
        public XFORM LocalMatrix
        {
            get => _LocalMatrix;
            set
            {
                _LocalMatrix = value;
                _WorldMatrix = null;
            }
        }

        /// <summary>
        /// Gets or sets the transform matrix of this node in Model Space.
        /// </summary>
        public XFORM ModelMatrix
        {
            get => _GetModelMatrix();
            set => _SetModelMatrix(value);
        }

        /// <summary>
        /// Gets a value indicating whether any of the transforms down the scene tree has been modified.
        /// </summary>
        private bool TransformChainIsDirty
        {
            get
            {
                if (!_WorldMatrix.HasValue) return true;

                return _Parent == null ? false : _Parent.TransformChainIsDirty;
            }
        }

        #endregion

        #region API

        private XFORM _GetModelMatrix()
        {
            if (!TransformChainIsDirty) return _WorldMatrix.Value;

            _WorldMatrix = _Parent == null ? _LocalMatrix : XFORM.Multiply(_LocalMatrix, _Parent.ModelMatrix);

            return _WorldMatrix.Value;
        }

        private void _SetModelMatrix(XFORM xform)
        {
            if (_Parent == null) { LocalMatrix = xform; return; }

            XFORM.Invert(_Parent._GetModelMatrix(), out XFORM ipwm);

            LocalMatrix = XFORM.Multiply(xform, ipwm);
        }

        public void SetPoseTransform() { SetAnimationFrame(-1, 0); }

        public void SetAnimationFrame(int trackLogicalIndex, float time)
        {
            this.MorphWeights = _Template.GetMorphWeights(trackLogicalIndex, time);
            this.LocalMatrix = _Template.GetLocalMatrix(trackLogicalIndex, time);
        }

        public void SetAnimationFrame(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {
            this.MorphWeights = _Template.GetMorphWeights(track, time, weight);
            this.LocalMatrix = _Template.GetLocalMatrix(track, time, weight);
        }

        #endregion
    }
}
