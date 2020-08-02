using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Schema2;

namespace SharpGLTF.Runtime
{
    // tracks all the disposable objects of a model;
    // vertex buffers, index buffers, effects and textures.
    class GraphicsResourceTracker
    {
        #region data

        private readonly List<GraphicsResource> _Disposables = new List<GraphicsResource>();        

        #endregion

        #region properties

        public IReadOnlyList<GraphicsResource> Disposables => _Disposables;

        #endregion

        #region API        
        public void AddDisposable(GraphicsResource resource)
        {
            if (resource == null) throw new ArgumentNullException();
            if (_Disposables.Contains(resource)) throw new ArgumentException();
            _Disposables.Add(resource);
        }

        #endregion
    }

    class TextureFactory
    {
        #region lifecycle

        public TextureFactory(GraphicsDevice device, GraphicsResourceTracker disposables)
        {
            _Device = device;
            _Disposables = disposables;
        }

        #endregion

        #region data

        private readonly GraphicsDevice _Device;
        private readonly GraphicsResourceTracker _Disposables;

        private readonly Dictionary<Memory.MemoryImage, Texture2D> _Textures = new Dictionary<Memory.MemoryImage, Texture2D>();        

        #endregion

        #region API

        public Texture2D UseTexture(Memory.MemoryImage image, string name = null)
        {
            if (_Device == null) throw new InvalidOperationException();

            if (!image.IsValid) return null;

            if (_Textures.TryGetValue(image, out Texture2D tex)) return tex;

            using (var m = image.Open())
            {
                tex = Texture2D.FromStream(_Device, m);
                _Disposables.AddDisposable(tex);

                tex.Name = name;

                _Textures[image] = tex;

                return tex;
            }
        }

        public Texture2D UseWhiteImage()
        {
            const string solidWhitePNg = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAACXBIWXMAAA7DAAAOwwHHb6hkAAAAFHpUWHRUaXRsZQAACJkrz8gsSQUABoACIippo0oAAAAoelRYdEF1dGhvcgAACJkLy0xOzStJVQhIzUtMSS1WcCzKTc1Lzy8BAG89CQyAoFAQAAAAGklEQVQoz2P8//8/AymAiYFEMKphVMPQ0QAAVW0DHZ8uFaIAAAAASUVORK5CYII=";

            var toBytes = Convert.FromBase64String(solidWhitePNg);

            return UseTexture(new ArraySegment<byte>(toBytes), "_InternalSolidWhite");
        }

        #endregion        
    }

    class EffectsFactory
    {
        #region lifecycle

        public EffectsFactory(GraphicsDevice device, GraphicsResourceTracker disposables)
        {
            _Device = device;
            _TexFactory = new TextureFactory(device, disposables);
            _Disposables = disposables;
        }

        #endregion

        #region data

        private readonly GraphicsDevice _Device;
        private readonly TextureFactory _TexFactory;
        private readonly GraphicsResourceTracker _Disposables;

        private readonly Dictionary<Object, Effect> _RigidEffects = new Dictionary<Object, Effect>();
        private readonly Dictionary<Object, SkinnedEffect> _SkinnedEffects = new Dictionary<Object, SkinnedEffect>();
        
        #endregion

        #region API - Schema

        public void Register(Object key, bool isSkinned, Effect effect)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (effect == null) throw new ArgumentNullException(nameof(effect));

            if (isSkinned && effect is SkinnedEffect skEffect) { _SkinnedEffects[key] = skEffect; }
            else { _RigidEffects[key] = effect; }
        }        

        public Effect GetMaterial(Schema2.Material srcMaterial, bool isSkinned)
        {
            if (isSkinned)
            {
                if (_SkinnedEffects.TryGetValue(srcMaterial, out SkinnedEffect dstMaterial)) return dstMaterial;
            }
            else
            {
                if (_RigidEffects.TryGetValue(srcMaterial, out Effect dstMaterial)) return dstMaterial;
            }

            return null;
        }        

        internal Texture2D UseTexture(MaterialChannel? channel, string name)
        {
            if (!channel.HasValue) return _TexFactory.UseWhiteImage();

            if (channel.HasValue && name == null)
            {
                name = channel.Value.LogicalParent.Name;
                if (name == null) name = "null";
                name += $"-{channel.Value.Key}";
            }            

            return _TexFactory.UseTexture(channel.Value.Texture?.PrimaryImage?.Content ?? default, name);
        }

        #endregion
    }


}
