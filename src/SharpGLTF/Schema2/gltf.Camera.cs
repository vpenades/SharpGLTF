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

        /// <summary>
        /// Gets the zero-based index of this <see cref="Camera"/> at <see cref="ModelRoot.LogicalCameras"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalCameras.IndexOfReference(this);

        public CameraType Type
        {
            get => this._type;
            set => this._type = value;
        }

        #endregion

        #region API

        /// <inheritdoc />
        protected override IEnumerable<glTFProperty> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_orthographic, _perspective);
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Camera"/> instance
        /// and adds it to <see cref="ModelRoot.LogicalCameras"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Camera"/> instance.</returns>
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
