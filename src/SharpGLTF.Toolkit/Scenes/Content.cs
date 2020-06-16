using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using MESHBUILDER = SharpGLTF.Geometry.IMeshBuilder<SharpGLTF.Materials.MaterialBuilder>;

namespace SharpGLTF.Scenes
{
    interface IRenderableContent
    {
        MESHBUILDER GetGeometryAsset();
    }

    [System.Diagnostics.DebuggerDisplay("Mesh")]
    partial class MeshContent
        : IRenderableContent
        , ICloneable
    {
        #region lifecycle

        public MeshContent(MESHBUILDER mesh)
        {
            _Mesh = mesh;
        }

        public Object Clone()
        {
            return new MeshContent(this);
        }

        private MeshContent(MeshContent other)
        {
            this._Mesh = other._Mesh?.Clone(m => new Materials.MaterialBuilder(m));
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private MESHBUILDER _Mesh;

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

    partial class MorphableMeshContent : IRenderableContent
    {
        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private IRenderableContent _Target;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly List<Animations.AnimatableProperty<float>> _MorphWeights = new List<Animations.AnimatableProperty<float>>();

        #endregion

        #region API

        public MESHBUILDER GetGeometryAsset() => _Target?.GetGeometryAsset();

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Camera")]
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

    [System.Diagnostics.DebuggerDisplay("Light")]
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
