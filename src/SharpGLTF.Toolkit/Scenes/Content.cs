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

        private MESHBUILDER _Mesh;

        #endregion

        #region API

        public MESHBUILDER GetGeometryAsset() => _Mesh;

        #endregion
    }

    partial class MorphableMeshContent : IRenderableContent
    {
        #region data

        private IRenderableContent _Target;

        private readonly List<Animations.AnimatableProperty<float>> _MorphWeights = new List<Animations.AnimatableProperty<float>>();

        #endregion

        #region API

        public MESHBUILDER GetGeometryAsset() => _Target?.GetGeometryAsset();

        #endregion
    }

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

        private CameraBuilder _Camera;

        #endregion
    }

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

        private LightBuilder _Light;

        #endregion
    }
}
