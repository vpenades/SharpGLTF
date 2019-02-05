using System;
using System.Collections.Generic;
using System.Text;

namespace glTF2Sharp.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Camera[{LogicalIndex}] {Name}")]
    public partial class Camera
    {
        #region lifecycle

        internal Camera() { }

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent.LogicalCameras.IndexOfReference(this);

        public CameraType Type
        {
            get => this._type;
            set => this._type = value;
        }

        #endregion        
    }
}
