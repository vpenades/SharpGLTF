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

    partial class OrthographicCameraContent
    {
        public OrthographicCameraContent(float xmag, float ymag, float znear, float zfar)
        {
            _XMag = xmag;
            _YMag = ymag;
            _ZNear = znear;
            _ZFar = zfar;
        }

        private float _XMag;
        private float _YMag;
        private float _ZNear;
        private float _ZFar;
    }

    partial class PerspectiveCameraContent
    {
        public PerspectiveCameraContent(float? aspectRatio, float fovy, float znear, float zfar = float.PositiveInfinity)
        {
            _AspectRatio = aspectRatio;
            _FovY = fovy;
            _ZNear = znear;
            _ZFar = zfar;
        }

        float? _AspectRatio;
        float _FovY;
        float _ZNear;
        float _ZFar;
    }
}
