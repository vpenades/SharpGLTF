using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using SharpGLTF.Runtime;
using SharpGLTF.Runtime.Pipeline;
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
            // dispose of previous model, if any.
            _CurrentModelInstance = null;
            _CurrentModelTemplate?.Dispose(); 

            // load template

            _CurrentModelTemplate = TemplateFactory.CreateDeviceModel(this.GraphicsDevice, model);            

            _CurrentModelInstance = _CurrentModelTemplate.Instance.CreateInstance();

            var bounds = _CurrentModelTemplate.Instance.Bounds;

            _CameraPos = new System.Numerics.Vector3(0, 0, bounds.Radius * 4);

            return _CurrentModelInstance;
        }

        protected override void Update(GameTime gameTime)
        {            
            base.Update(gameTime);

            var mouse = Mouse.GetState(this.Window);
            var pos = mouse.Position.ToVector2().ToNumerics();

            var delta = pos - _MousePos;
            _MousePos = pos;

            if (mouse.LeftButton == ButtonState.Pressed)
            {
                var camX = System.Numerics.Matrix4x4.CreateWorld(_CameraPos, -_CameraPos, System.Numerics.Vector3.UnitY);
                var delta3D = System.Numerics.Vector3.TransformNormal(new System.Numerics.Vector3(delta.X, delta.Y, 0), camX);

                var r = _CameraPos.Length();
                _CameraPos -= delta3D * r / 40;
                _CameraPos *= r / _CameraPos.Length();
            }            
        }

        private System.Numerics.Vector2 _MousePos;

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
                
                // var mdlPos = bounds.Center;                

                var camX = Matrix.CreateWorld(_CameraPos, -_CameraPos, Vector3.UnitY);
                var mdlX = Matrix.CreateTranslation(-bounds.Center);

                var dc = new ModelDrawingContext(this.GraphicsDevice);
                dc.NearPlane = bounds.Radius / 4; // for small objects, we need to set the near plane very close to the camera.
                dc.SetCamera(camX);

                // dc.DrawMesh(_LightsAndFog, _MeshCollection[0], mdlX);

                _CurrentModelInstance?.Draw(dc.GetProjectionMatrix(), dc.GetViewMatrix(), mdlX);
            }

            base.Draw(gameTime);
        }
    }
}
