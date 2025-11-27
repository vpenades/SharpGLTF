using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace SharpGLTF.Schema2
{
    partial class _NodeVisibility
    {
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
        public bool TryGetVisibility(out bool isVisible)
        {
            var ext = this.GetExtension<_NodeVisibility>();
            if (ext == null) { isVisible = true; return false; }
            isVisible = ext.IsVisible;
            return true;
        }

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
