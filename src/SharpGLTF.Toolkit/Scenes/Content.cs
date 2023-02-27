using System;
using System.Collections.Generic;

using MESHBUILDER = SharpGLTF.Geometry.IMeshBuilder<SharpGLTF.Materials.MaterialBuilder>;

namespace SharpGLTF.Scenes
{
    interface IRenderableContent
    {
        MESHBUILDER GetGeometryAsset();
    }

    /// <summary>
    /// Represents a dummy, empty content of <see cref="ContentTransformer.Content"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("EmptyContent")]
    partial class EmptyContent : ICloneable
    {
        #region lifecycle

        public EmptyContent() { }

        public Object Clone() { return new EmptyContent(this); }

        private EmptyContent(EmptyContent other) { }

        #endregion
    }

    /// <summary>
    /// Represents a <see cref="MESHBUILDER"/> content of <see cref="ContentTransformer.Content"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("MeshContent => {_Mesh}")]
    partial class MeshContent :
        IRenderableContent,
        ICloneable,
        IEquatable<IRenderableContent>
    {
        #region lifecycle

        public MeshContent(MESHBUILDER mesh)
        {
            this._Mesh = mesh;
        }

        public Object Clone()
        {
            return new MeshContent(this);
        }

        private MeshContent(MeshContent other)
        {
            this._Mesh = other._Mesh;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private MESHBUILDER _Mesh;

        public override int GetHashCode() { return _Mesh.GetHashCode(); }

        public override bool Equals(object obj) { return obj is IRenderableContent other && this.Equals(other); }

        public bool Equals(IRenderableContent other)
        {
            if (other is MeshContent otherMesh) return this._Mesh == otherMesh._Mesh;
            throw new ArgumentException("Type mismatch", nameof(other));
        }

        #endregion

        #region properties

        public MESHBUILDER Mesh
        {
            get => _Mesh;
            set => _Mesh = value;
        }

        #endregion

        #region API

        public MESHBUILDER GetGeometryAsset() => _Mesh;

        #endregion
    }

    /// <summary>
    /// Represents a <see cref="CameraBuilder"/> content of <see cref="ContentTransformer.Content"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("CameraContent => {_Camera}")]
    partial class CameraContent : ICloneable
    {
        #region lifecycle

        public CameraContent(CameraBuilder camera)
        {
            _Camera = camera;
        }

        public Object Clone()
        {
            return new CameraContent(this);
        }

        private CameraContent(CameraContent other)
        {
            this._Camera = other._Camera?.Clone();
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private CameraBuilder _Camera;

        #endregion

        #region properties

        public CameraBuilder Camera
        {
            get => _Camera;
            set => _Camera = value;
        }

        #endregion
    }

    /// <summary>
    /// Represents a <see cref="LightBuilder"/> content of <see cref="ContentTransformer.Content"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("LightContent => {_Light}")]
    partial class LightContent : ICloneable
    {
        #region lifecycle

        public LightContent(LightBuilder light)
        {
            _Light = light;
        }

        public Object Clone()
        {
            return new LightContent(this);
        }

        private LightContent(LightContent other)
        {
            this._Light = other._Light?.Clone();
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private LightBuilder _Light;

        #endregion

        #region properties

        public LightBuilder Light
        {
            get => _Light;
            set => _Light = value;
        }

        #endregion
    }
}
