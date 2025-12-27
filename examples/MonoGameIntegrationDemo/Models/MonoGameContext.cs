using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Runtime;
using SharpGLTF.Runtime.Template;


namespace MonoGameIntegrationDemo.Models
{
    public class MonoGameContext : Game
    {
        #region lifecycle
        public MonoGameContext()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _CurrentModelTemplate?.Dispose();
                _CurrentModelTemplate = null;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region data

        private readonly GraphicsDeviceManager _graphics;        

        private MonoGameDeviceContent<MonoGameModelTemplate>? _CurrentModelTemplate;

        private MonoGameModelInstance? _CurrentModelInstance;

        #endregion

        public MonoGameModelInstance SetModel(SharpGLTF.Schema2.ModelRoot model)
        {
            _CurrentModelInstance = null;
            _CurrentModelTemplate?.Dispose(); // destroy previous model, if any.

            _CurrentModelTemplate = MonoGameModelTemplate.CreateDeviceModel(this.GraphicsDevice, model);
            _CurrentModelInstance = _CurrentModelTemplate.Instance.CreateInstance();

            var bounds = _CurrentModelTemplate.Instance.Bounds;

            _CameraPos = bounds.Center.ToNumerics() + new System.Numerics.Vector3(0, 0, bounds.Radius * 4);

            return _CurrentModelInstance;
        }

        protected override void Update(GameTime gameTime)
        {
            // Your game logic here
            base.Update(gameTime);
        }

        private System.Numerics.Vector3 _CameraPos;

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);            

            // setup lights and fog for the effects in the template

            _CurrentModelInstance?.Template?.ConfigureLightsAndFog(null, null);

            // rendering

            if (_CurrentModelInstance != null)
            {
                var bounds = _CurrentModelInstance.Template.Bounds;
                
                var mdlPos = bounds.Center;                

                var camX = Matrix.CreateWorld(_CameraPos, mdlPos - _CameraPos, Vector3.UnitY);
                var mdlX = Matrix.CreateRotationY(0.25f * (float)gameTime.TotalGameTime.TotalSeconds) * Matrix.CreateTranslation(Vector3.Zero);

                var dc = new ModelDrawingContext(this.GraphicsDevice);
                dc.NearPlane = 0.1f; // for small objects, we need to set the near plane very close to the camera.
                dc.SetCamera(camX);

                // dc.DrawMesh(_LightsAndFog, _MeshCollection[0], mdlX);

                _CurrentModelInstance?.Draw(dc.GetProjectionMatrix(), dc.GetViewMatrix(), mdlX);
            }

            base.Draw(gameTime);
        }
    }
}
