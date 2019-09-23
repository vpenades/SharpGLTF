using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoGameScene
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        #region lifecycle

        public Game1()
        {
            _Graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
        }
        
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            this.Window.Title = "SharpGLTF - MonoGame Scene";
            this.Window.AllowUserResizing = true;
            this.Window.AllowAltF4 = true;

            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion

        #region resources

        private readonly GraphicsDeviceManager _Graphics;
        
        // these are the actual hardware resources that represent every model's geometry.
        
        SharpGLTF.Runtime.MonoGameDeviceContent<SharpGLTF.Runtime.MonoGameModelTemplate> _AvodadoTemplate;
        SharpGLTF.Runtime.MonoGameDeviceContent<SharpGLTF.Runtime.MonoGameModelTemplate> _BrainStemTemplate;
        SharpGLTF.Runtime.MonoGameDeviceContent<SharpGLTF.Runtime.MonoGameModelTemplate> _CesiumManTemplate;
        SharpGLTF.Runtime.MonoGameDeviceContent<SharpGLTF.Runtime.MonoGameModelTemplate> _HauntedHouseTemplate;

        #endregion

        #region content loading
        
        protected override void LoadContent()
        {
            _AvodadoTemplate = SharpGLTF.Runtime.MonoGameModelTemplate.LoadDeviceModel(this.GraphicsDevice, "Models\\Avocado.glb");
            _BrainStemTemplate = SharpGLTF.Runtime.MonoGameModelTemplate.LoadDeviceModel(this.GraphicsDevice, "Models\\BrainStem.glb");
            _CesiumManTemplate = SharpGLTF.Runtime.MonoGameModelTemplate.LoadDeviceModel(this.GraphicsDevice, "Models\\CesiumMan.glb");
            _HauntedHouseTemplate = SharpGLTF.Runtime.MonoGameModelTemplate.LoadDeviceModel(this.GraphicsDevice, "Models\\haunted_house.glb");
        }
        
        protected override void UnloadContent()
        {
            _AvodadoTemplate?.Dispose();
            _AvodadoTemplate = null;

            _BrainStemTemplate?.Dispose();
            _BrainStemTemplate = null;

            _CesiumManTemplate?.Dispose();
            _CesiumManTemplate = null;

            _HauntedHouseTemplate?.Dispose();
            _HauntedHouseTemplate = null;
        }

        #endregion

        #region game loop

        // these are the scene instances we create for every glTF model we want to render on screen.
        // Instances are designed to be as lightweight as possible, so it should not be a problem to
        // create as many of them as you need at runtime.
        private SharpGLTF.Runtime.MonoGameModelInstance _HauntedHouse;
        private SharpGLTF.Runtime.MonoGameModelInstance _BrainStem;
        private SharpGLTF.Runtime.MonoGameModelInstance _Avocado;
        private SharpGLTF.Runtime.MonoGameModelInstance _CesiumMan1;
        private SharpGLTF.Runtime.MonoGameModelInstance _CesiumMan2;
        private SharpGLTF.Runtime.MonoGameModelInstance _CesiumMan3;
        private SharpGLTF.Runtime.MonoGameModelInstance _CesiumMan4;       


        protected override void Update(GameTime gameTime)
        {
            // For Mobile devices, this logic will close the Game when the Back button is pressed
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            // create as many instances as we need from the templates

            if (_Avocado == null) _Avocado = _AvodadoTemplate.Instance.CreateInstance();
            if (_HauntedHouse == null) _HauntedHouse = _HauntedHouseTemplate.Instance.CreateInstance();
            if (_BrainStem == null) _BrainStem = _BrainStemTemplate.Instance.CreateInstance();

            if (_CesiumMan1 == null) _CesiumMan1 = _CesiumManTemplate.Instance.CreateInstance();
            if (_CesiumMan2 == null) _CesiumMan2 = _CesiumManTemplate.Instance.CreateInstance();
            if (_CesiumMan3 == null) _CesiumMan3 = _CesiumManTemplate.Instance.CreateInstance();
            if (_CesiumMan4 == null) _CesiumMan4 = _CesiumManTemplate.Instance.CreateInstance();

            // animate each instance individually.

            var animTime = (float)gameTime.TotalGameTime.TotalSeconds;

            _BrainStem.Controller.SetAnimationFrame(0, animTime);
            _CesiumMan1.Controller.SetAnimationFrame(0, 0.3f);
            _CesiumMan2.Controller.SetAnimationFrame(0, 0.5f * animTime);
            _CesiumMan3.Controller.SetAnimationFrame(0, 1.0f * animTime);
            _CesiumMan4.Controller.SetAnimationFrame(0, 1.5f * animTime);

            base.Update(gameTime);
        }        

        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);            

            var camera = Matrix.CreateWorld(new Vector3(0, 2, 12), -Vector3.UnitZ, Vector3.UnitY);                      

            // draw all the instances.

            var ctx = new ModelDrawContext(_Graphics, camera);

            ctx.DrawModelInstance(_Avocado, Matrix.CreateScale(30) * Matrix.CreateTranslation(-5,5,-5));

            ctx.DrawModelInstance(_HauntedHouse, Matrix.CreateScale(20));

            ctx.DrawModelInstance(_BrainStem, Matrix.CreateTranslation(0,0.5f,8));

            ctx.DrawModelInstance(_CesiumMan1, Matrix.CreateTranslation(-3, 0, 5));
            ctx.DrawModelInstance(_CesiumMan2, Matrix.CreateTranslation(-2, 0, 5));
            ctx.DrawModelInstance(_CesiumMan3, Matrix.CreateTranslation( 2, 0, 5));
            ctx.DrawModelInstance(_CesiumMan4, Matrix.CreateTranslation( 3, 0, 5));
        }
        
        #endregion
    }
}
