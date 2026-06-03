using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace SharpGLTF.Schema2
{
    partial class _NodeVisibility
    {
        // https://github.com/KhronosGroup/glTF/issues/1314

        #region lifecycle
        internal _NodeVisibility(Node node)
        {
            _Owner = node;            
        }

        #endregion

        #region data (not serializable)

        private readonly Node _Owner;

        #endregion

        #region properties        

        public bool IsVisible
        {
            get => this._visible ?? _visibleDefault;
            set => this._visible = value == _visibleDefault ? null : value;
        }

        #endregion
    }

    partial class Node
    {
        /// <summary>
        /// Gets the evaluated visibility state of this node.
        /// </summary>
        public bool IsVisible
        {
            get
            {
                if (!this.VisualParent.IsVisible) return false;
                return GetVisibility() ?? true;
            }
        }

        public bool GetVisibility(Animation animation, float time)
        {
            if (animation == null) return IsVisible;

            if (!this.VisualParent.GetVisibility(animation,time)) return false;            

            return this.GetCurveSamplers(animation).GetVisibility(time);
        }        

        /// <summary>
        /// Gets the visibility value of this node.
        /// </summary>
        /// <returns>if null, it inherits the value from the parent node.</returns>
        public bool? GetVisibility()
        {
            return this.GetExtension<_NodeVisibility>()?.IsVisible ?? null;
        }

        /// <summary>
        /// Sets the visibility value of this node.
        /// </summary>
        /// <param name="isVisible">true/false or null to inherit from the parent node.</param>
        public void SetVisibility(bool? isVisible)
        {
            if (isVisible == null)
            {
                this.RemoveExtensions<_NodeVisibility>();
                return;
            }
            else
            {
                this.UseExtension<_NodeVisibility>().IsVisible = isVisible == true;
            }                
        }
    }
}
