using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Camera[{LogicalIndex}] {Name}")]
    public sealed partial class Camera
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

    public partial class ModelRoot
    {
        public Camera CreateCamera(string name = null)
        {
            var camera = new Camera
            {
                Name = name
            };

            _cameras.Add(camera);

            return camera;
        }
    }
}
