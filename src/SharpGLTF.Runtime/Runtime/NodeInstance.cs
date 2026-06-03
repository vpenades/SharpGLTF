using System;
using System.Collections.Generic;
using System.Text;

using SharpGLTF.Transforms;

using M4x4XFORM = System.Numerics.Matrix4x4;
using TRANSFORM = SharpGLTF.Transforms.AffineTransform;

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

            _LocalVisible = true;
            _ModelMatrixIsDirty = true;
        }

        #endregion

        #region data

        private readonly NodeTemplate _Template;
        private readonly NodeInstance _Parent;

        private SparseWeight8 _MorphWeights;        

        private TRANSFORM _LocalTransform;        
        private M4x4XFORM _ModelMatrix;
        private bool _ModelMatrixIsDirty;

        private bool _LocalVisible;        

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
        public M4x4XFORM LocalMatrix
        {
            get => _LocalTransform.Matrix;
            set
            {
                _LocalTransform = value;
                _ModelMatrixIsDirty = true;
            }
        }

        /// <summary>
        /// Gets or sets the transform matrix of this node in Local Space.
        /// </summary>
        public TRANSFORM LocalTransform
        {
            get => _LocalTransform;
            set
            {
                _LocalTransform = value;
                _ModelMatrixIsDirty = true;
            }
        }

        /// <summary>
        /// Gets or sets the transform matrix of this node in Model Space.
        /// </summary>
        public M4x4XFORM ModelMatrix
        {
            get => _GetModelMatrix();
            set => _SetModelMatrix(value);
        }

        /// <summary>
        /// Gets or sets whether this node, and all its children are visible
        /// </summary>
        /// <remarks>
        /// In order for this node to be visible not only this property must be true;
        /// all the parent nodes up to the root must also be visible
        /// </remarks>
        public bool IsVisible
        {
            get => _LocalVisible && (_Parent?.IsVisible ?? true);
            set => _LocalVisible = value;
        }        

        #endregion

        #region API             

        public void SetAnimationFrame(int trackLogicalIndex, float time)
        {
            _Template.ApplyAnimationFrame(this, trackLogicalIndex, time);
        }

        private void _SetModelMatrix(M4x4XFORM xform)
        {
            if (_Parent == null) { LocalMatrix = xform; return; }

            M4x4XFORM.Invert(_Parent._GetModelMatrix(), out M4x4XFORM ipwm);

            LocalMatrix = M4x4XFORM.Multiply(xform, ipwm);
        }

        private M4x4XFORM _GetModelMatrix()
        {
            UpdateTransformChain();

            return _ModelMatrix;
        }        

        private bool UpdateTransformChain()
        {
            var isDirty = _ModelMatrixIsDirty;
            _ModelMatrixIsDirty = false;

            if (_Parent != null)
            {
                isDirty |= _Parent.UpdateTransformChain();

                if (isDirty)
                {
                    _ModelMatrix = M4x4XFORM.Multiply(_LocalTransform.Matrix, _Parent.ModelMatrix);
                }
            }
            else if (isDirty)
            {
                _ModelMatrix = _LocalTransform.Matrix;
            }

            return isDirty;
        }        

        #endregion
    }
}
