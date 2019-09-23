using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpGLTF.Runtime;

namespace MonoGameScene
{
    /// <summary>
    /// Small helper for rendering MonoGame models.
    /// </summary>
    class ModelDrawContext
    {
        #region lifecycle

        public ModelDrawContext(GraphicsDeviceManager graphics, Matrix cameraMatrix)
        {
            _Device = graphics.GraphicsDevice;

            _Device.DepthStencilState = DepthStencilState.Default;

            _View = Matrix.Invert(cameraMatrix);            
            
            float fieldOfView = MathHelper.PiOver4;            
            float nearClipPlane = 0.01f;            
            float farClipPlane = 1000;

            _Projection = Matrix.CreatePerspectiveFieldOfView(fieldOfView, graphics.GraphicsDevice.Viewport.AspectRatio, nearClipPlane, farClipPlane);            
        }

        #endregion

        #region data

        private GraphicsDevice _Device;
        private Matrix _Projection;
        private Matrix _View;        

        #endregion        

        #region API

        public void DrawModelInstance(MonoGameModelInstance model, Matrix world)
        {
            foreach (var e in model.Template.Effects) UpdateMaterial(e);

            model.Draw(_Projection, _View, world);
        }

        public static void UpdateMaterial(Effect effect)
        {
            if (effect is IEffectLights lights)
            {
                lights.EnableDefaultLighting();
            }

            if (effect is IEffectFog fog)
            {
                fog.FogEnabled = false;
            }
        }

        #endregion
    }
}
