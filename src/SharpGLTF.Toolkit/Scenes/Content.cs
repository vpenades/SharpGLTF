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

    partial class MeshContent : IRenderableContent
    {
        #region lifecycle

        public MeshContent(MESHBUILDER mesh)
        {
            _Mesh = mesh;
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

    partial class CameraContent
    {
        #region lifecycle

        public CameraContent(CameraBuilder camera)
        {
            _Camera = camera;
        }

        #endregion

        #region data

        private CameraBuilder _Camera;

        #endregion
    }

    partial class LightContent
    {
        #region lifecycle

        public LightContent(LightBuilder light)
        {
            _Light = light;
        }

        #endregion

        #region data

        private LightBuilder _Light;

        #endregion
    }
}
