using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Graphics;

namespace SharpGLTF.Runtime.Pipeline
{
    /// <summary>
    /// Factory used to convert images into <see cref="Texture2D"/> objects.
    /// </summary>
    /// <remarks>
    /// By default, this factory is only able to read images using <see cref="Texture2D.FromStream(GraphicsDevice, System.IO.Stream)"/>;<br/>
    /// In order to add support for more formats, create a derived class and register it at <see cref="InstanceBuilder"/>
    /// </remarks>
    public class TextureFactory
    {
        #region lifecycle

        /// <summary>
        /// Register here your own <see cref="TextureFactory"/> derived class to override texture creation
        /// </summary>
        public static Func<GraphicsDevice, GraphicsResourceTracker, TextureFactory> InstanceBuilder { get; set; }

        public static TextureFactory Create(GraphicsDevice device, GraphicsResourceTracker disposables)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(disposables);

            var tf = InstanceBuilder?.Invoke(device, disposables);
            tf ??= new TextureFactory(device, disposables);
            return tf;
        }
        
        protected TextureFactory(GraphicsDevice device, GraphicsResourceTracker disposables)
        {
            _Device = device;
            _Disposables = disposables;
        }

        #endregion

        #region data

        private readonly GraphicsDevice _Device;
        private readonly GraphicsResourceTracker _Disposables;

        // temporary cache to prevent creating the same texture multiple times
        private readonly Dictionary<Memory.MemoryImage, Texture2D> _Textures = new Dictionary<Memory.MemoryImage, Texture2D>();

        #endregion

        #region API

        /// <summary>
        /// Some effects require a white texture
        /// </summary>
        /// <returns></returns>
        public Texture2D UseWhiteImage()
        {
            var tex = _Disposables
                .Disposables
                .OfType<Texture2D>()
                .FirstOrDefault(item => item.Name == "_InternalSolidWhite");
            if (tex != null) return tex;

            const string solidWhitePNg = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAACXBIWXMAAA7DAAAOwwHHb6hkAAAAFHpUWHRUaXRsZQAACJkrz8gsSQUABoACIippo0oAAAAoelRYdEF1dGhvcgAACJkLy0xOzStJVQhIzUtMSS1WcCzKTc1Lzy8BAG89CQyAoFAQAAAAGklEQVQoz2P8//8/AymAiYFEMKphVMPQ0QAAVW0DHZ8uFaIAAAAASUVORK5CYII=";

            var toBytes = Convert.FromBase64String(solidWhitePNg);

            return UseTexture(new ArraySegment<byte>(toBytes), "_InternalSolidWhite");
        }

        public Texture2D UseTexture(Memory.MemoryImage image, string name = null)
        {
            if (_Device == null) throw new InvalidOperationException();

            if (!image.IsValid) return null;
            if (image.MimeType == "image/webp") return UseWhiteImage();
            if (image.MimeType == "image/ktx2") return UseWhiteImage();

            if (_Textures.TryGetValue(image, out Texture2D tex)) return tex;

            using (var m = image.Open())
            {
                tex = LoadTexture(_Device, m, image.MimeType);
                _Disposables.AddDisposable(tex);

                tex.Name = name;

                _Textures[image] = tex;

                return tex;
            }
        }        

        #endregion

        #region Overridable API

        /// <summary>
        /// Override this method to support loading images not supported by MonoGame (KTX2, WebP)
        /// </summary>        
        protected virtual Texture2D LoadTexture(GraphicsDevice device, System.IO.Stream image, string imageMimeType)
        {
            if (imageMimeType == "image/webp") throw new NotSupportedException("webp images not supported");
            if (imageMimeType == "image/ktx2") throw new NotSupportedException("ktx2 images not supported");

            return Texture2D.FromStream(device, image);
        }

        #endregion
    }
}
