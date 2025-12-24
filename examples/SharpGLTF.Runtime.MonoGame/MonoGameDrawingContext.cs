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


        /*
        public void DrawMesh(PBREnvironment environment, Mesh mesh, Matrix worldMatrix)
        {
            var proj = GetProjectionMatrix();

            PushState();

            foreach (var e in mesh.OpaqueEffects)
            {
                ModelInstance.UpdateProjViewTransforms(e, proj, _View);
                ModelInstance.UpdateWorldTransforms(e, worldMatrix);
                environment.ApplyTo(e);
            }

            mesh.DrawOpaque();

            foreach (var e in mesh.TranslucidEffects)
            {
                ModelInstance.UpdateProjViewTransforms(e, proj, _View);
                ModelInstance.UpdateWorldTransforms(e, worldMatrix);
                environment.ApplyTo(e);
            }

            mesh.DrawTranslucid();

            PopState();
        }

        /// <summary>
        /// Draw a single model instance
        /// </summary>
        /// <param name="environment">Defines the athmospheric and lighting environment to use for the render.</param>
        /// <param name="modelInstance">Defines the instance that is going to be rendered.</param>
        /// <remarks>
        /// Rendering models one by one is accepted, but some features like translucent parts sortings will not work
        /// unless you manually render the models in the correct order.
        /// </remarks>
        public void DrawModelInstance(PBREnvironment environment, MODELINST modelInstance)
        {
            PushState();

            var proj = GetProjectionMatrix();

            foreach (var e in modelInstance.SharedEffects)
            {
                environment.ApplyTo(e);
                ModelInstance.UpdateProjViewTransforms(e, proj, _View);
            }

            modelInstance.DrawAllParts(proj, _View);

            PopState();
        }

        /// <summary>
        /// Draws a batch of model instances.
        /// </summary>
        /// <param name="environment">Defines the athmospheric and lighting environment to use for the render.</param>
        /// <param name="modelInstances">A batch of model instances.</param>
        /// <remarks>
        /// Rendering multiple models in a batch has a number of advantages over rendering models one by one:
        /// - It allows splitting the rendering between opaque and translucent parts, which are rendered in the correct
        ///   order to preserve rendering correctness.
        /// - Less redundant calls.
        /// - Futher optimizations are possible, like batching instances that share the same template model in a single
        ///   drawing call.
        /// - Possibility to add shadows, where some instances cast shadows over others.
        /// </remarks>
        public void DrawSceneInstances(PBREnvironment environment, params MODELINST[] modelInstances)
        {
            PushState();

            // todo: fustrum culling goes here

            var proj = GetProjectionMatrix();

            _SceneInstances.Clear();
            _SceneInstances.AddRange(modelInstances);
            _SceneInstances.Sort(_DistanceComparer);

            // gather all effects from all visible instances.
            _SceneEffects.Clear();
            _SceneEffects.UnionWith(_SceneInstances.SelectMany(item => item.SharedEffects));

            // set Projection & View on all visible effects.

            foreach (var e in _SceneEffects)
            {
                ModelInstance.UpdateProjViewTransforms(e, proj, _View);
                // todo: set env.Exposure
                // todo: set env.AmbientLight
            }

            // todo: find the closest lights for each visible instance.

            // render opaque parts from closest to farthest

            foreach (var instance in _SceneInstances)
            {
                foreach (var e in instance.SharedEffects) environment.ApplyTo(e);
                instance.DrawOpaqueParts();
            }

            // render translucid parts from farthest to closest

            _SceneInstances.Reverse();

            foreach (var instance in _SceneInstances)
            {
                foreach (var e in instance.SharedEffects) environment.ApplyTo(e);
                instance.DrawTranslucidParts();
            }

            PopState();
        }*/

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
