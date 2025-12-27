using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Schema2;

using MODELINST = SharpGLTF.Runtime.MonoGameModelInstance;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Helper class for rendering <see cref="ModelInstance"/> models.
    /// </summary>
    public class ModelDrawingContext
    {
        #region lifecycle

        public ModelDrawingContext(GraphicsDevice graphics)
        {
            _Device = graphics;
            _Device.DepthStencilState = DepthStencilState.Default;
            _View = Matrix.Invert(Matrix.Identity);
            _Projection = SceneUtils.CreatePerspectiveFieldOfView(_FieldOfView, _Device.Viewport.AspectRatio, _NearPlane);
            // _DistanceComparer = MODELINST.GetDistanceComparer(-_View.Translation);
        }

        #endregion

        #region data

        private GraphicsDevice _Device;

        private readonly Stack<_GraphicsState> _PreserveState = new Stack<_GraphicsState>();

        private float _FieldOfView = MathHelper.PiOver4;
        private float _NearPlane = 1f;

        private Matrix _View, _Projection;
        private IComparer<MODELINST> _DistanceComparer;

        private static readonly HashSet<Effect> _SceneEffects = new HashSet<Effect>();
        private static readonly List<MODELINST> _SceneInstances = new List<MODELINST>();

        #endregion

        #region properties

        public float FieldOfView
        {
            get => _FieldOfView;
            set => _FieldOfView = value;
        }

        public float NearPlane
        {
            get => _NearPlane;
            set => _NearPlane = value;
        }


        #endregion

        #region API

        protected void PushState()
        {
            var state = new _GraphicsState(_Device);
            _PreserveState.Push(state);
        }

        protected void PopState()
        {
            var state = _PreserveState.Pop();
            state.Apply(_Device);
        }

        public Matrix GetProjectionMatrix()
        {
            return _Projection;
        }

        public Matrix GetViewMatrix()
        {
            return _View;
        }

        public void SetProjectionMatrix(Matrix projectionMatrix)
        {
            _Projection = projectionMatrix;
        }

        public void SetCamera(Matrix cameraMatrix)
        {
            _View = Matrix.Invert(cameraMatrix);
            _Projection = SceneUtils.CreatePerspectiveFieldOfView(_FieldOfView, _Device.Viewport.AspectRatio, _NearPlane);
            // _DistanceComparer = MODELINST.GetDistanceComparer(-_View.Translation);
        }        

        #endregion        
    }

    /// <summary>
    /// Preserves all the monogame states that might be
    /// modified when rendering a model.
    /// </summary>
    readonly struct _GraphicsState
    {
        public _GraphicsState(GraphicsDevice graphics)
        {
            _Rasterizer = graphics.RasterizerState;
            _Blend = graphics.BlendState;

            _Sampler0 = graphics.SamplerStates[0];
            _Sampler1 = graphics.SamplerStates[1];
        }

        private readonly RasterizerState _Rasterizer;
        private readonly BlendState _Blend;

        private readonly SamplerState _Sampler0;
        private readonly SamplerState _Sampler1;

        public void Apply(GraphicsDevice graphics)
        {
            graphics.RasterizerState = _Rasterizer;
            graphics.BlendState = _Blend;

            graphics.SamplerStates[0] = _Sampler0;
            graphics.SamplerStates[1] = _Sampler1;
        }
    }
}
