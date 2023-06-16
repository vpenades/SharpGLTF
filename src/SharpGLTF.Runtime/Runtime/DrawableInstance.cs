using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Runtime
{
    [System.Diagnostics.DebuggerDisplay("{_ToDebuggerDisplayString(),nq}")]
    public readonly struct DrawableInstance
    {
        #region diagnostics

        private string _ToDebuggerDisplayString()
        {
            if (Template == null || Transform == null) return "⚠ Empty";

            var text = string.Empty;

            if (Transform.Visible) text += "👁 ";

            text += "[";
            if (Template.NodeName != null) text += Template.NodeName + " ";
            text += Template.LogicalMeshIndex;
            text += "] ";

            if (Transform is Transforms.RigidTransform rigid)
            {
                text += "Rigid";
            }

            if (Transform is Transforms.SkinnedTransform skinned)
            {
                text += $"Skinned 🦴={skinned.SkinMatrices.Count}";
            }

            if (Transform is Transforms.InstancingTransform instanced)
            {
                text += $"Instanced 🏠={instanced.LocalMatrices.Count}";
            }

            return text;
        }

        #endregion

        #region constructor

        internal DrawableInstance(IDrawableTemplate t, Transforms.IGeometryTransform xform)
        {
            Template = t;
            Transform = xform;
        }

        #endregion

        #region data

        /// <summary>
        /// Represents WHAT to draw.
        /// </summary>
        public readonly IDrawableTemplate Template;

        /// <summary>
        /// Represents WHERE to draw the <see cref="Template"/>.
        /// </summary>
        /// <remarks>
        /// This value can be casted to any of:
        /// <list type="table">
        /// <item><see cref="Transforms.MorphTransform"/></item>
        /// <item><see cref="Transforms.RigidTransform"/></item>
        /// <item><see cref="Transforms.SkinnedTransform"/></item>
        /// <item><see cref="Transforms.InstancingTransform"/></item>
        /// <item><see cref="Transforms.IGeometryInstancing"/></item>
        /// <item><see cref="Transforms.IMaterialTransform"/></item>
        /// </list>
        /// </remarks>
        public readonly Transforms.IGeometryTransform Transform;

        #endregion

        #region properties

        /// <summary>
        /// If <see cref="Transform"/> has instancing support,
        /// it gets the number of times we need to render <see cref="Template"/>
        /// </summary>
        public int InstanceCount => this.Transform is Transforms.IGeometryInstancing instancing ? instancing.InstancesCount : 1;

        #endregion
    }
}
