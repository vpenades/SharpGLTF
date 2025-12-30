using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Schema2;

using GLTFMATERIAL = SharpGLTF.Schema2.Material;

namespace SharpGLTF.Runtime.Pipeline
{
    /// <summary>
    /// Converts glTF materials into MonoGame effects
    /// </summary>
    public abstract class EffectsFactory
    {
        #region lifecycle

        /// <summary>
        /// Register here your own <see cref="EffectsFactory"/> derived class to override effects creation
        /// </summary>
        public static Func<GraphicsDevice, GraphicsResourceTracker, TextureFactory, EffectsFactory> InstanceBuilder { get; set; }

        public static EffectsFactory Create(GraphicsDevice device, GraphicsResourceTracker disposables, TextureFactory textureFactory = null)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(disposables);

            textureFactory ??= TextureFactory.Create(device, disposables);

            var ef = InstanceBuilder?.Invoke(device, disposables, textureFactory);
            ef ??= new DefaultEffectsFactory(device, disposables, textureFactory);
            return ef;
        }

        protected EffectsFactory(GraphicsDevice device, GraphicsResourceTracker disposables, TextureFactory textureFactory)
        {
            _Device = device;
            _TexFactory = textureFactory;
            _Disposables = disposables;
        }

        #endregion

        #region data

        private readonly GraphicsDevice _Device;
        private readonly TextureFactory _TexFactory;
        private readonly GraphicsResourceTracker _Disposables;

        // effects cache
        private readonly Dictionary<Object, Effect> _RigidEffects = new Dictionary<Object, Effect>();
        private readonly Dictionary<Object, SkinnedEffect> _SkinnedEffects = new Dictionary<Object, SkinnedEffect>();

        #endregion

        #region API - Schema

        protected GraphicsDevice Device => _Device;

        public Effect UseEffect(GLTFMATERIAL srcMaterial, bool isSkinned)
        {
            ArgumentNullException.ThrowIfNull(srcMaterial);

            var effect = _GetMaterialOrDefault(srcMaterial, isSkinned);
            if (effect != null) return effect;

            effect = CreateEffect(srcMaterial, isSkinned);

            if (isSkinned && effect is SkinnedEffect skEffect) { _SkinnedEffects[srcMaterial] = skEffect; }
            else { _RigidEffects[srcMaterial] = effect; }

            return effect;
        }        

        private Effect _GetMaterialOrDefault(GLTFMATERIAL srcMaterial, bool isSkinned)
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

        protected abstract Effect CreateEffect(GLTFMATERIAL srcMaterial, bool isSkinned);

        protected Texture2D UseTexture(MaterialChannel? channel, string name)
        {
            if (!channel.HasValue) return UseWhiteTexture();

            if (channel.HasValue && name == null)
            {
                name = channel.Value.LogicalParent.Name;
                name ??= "null";
                name += $"-{channel.Value.Key}";
            }

            if (channel.Value.Texture?.PrimaryImage?.Content.IsEmpty ?? true) return UseWhiteTexture();

            return _TexFactory.UseTexture(channel.Value.Texture?.PrimaryImage?.Content ?? default, name);
        }

        protected Texture2D UseWhiteTexture()
        {
            return _TexFactory.UseWhiteImage();
        }

        #endregion
    }

    /// <summary>
    /// Converts glTF materials into MonoGame effects
    /// </summary>
    /// <remarks>
    /// This will use default effects:<br/>
    /// <see cref="BasicEffect"/><br/>
    /// <see cref="AlphaTestEffect"/><br/>
    /// <see cref="SkinnedEffect"/><br/>
    /// Notice that these effects are very basic and limiting, so this factory does a best effort to try advanced glTF materials to fit in.
    /// </remarks>
    public class DefaultEffectsFactory : EffectsFactory
    {
        #region lifecycle
        public DefaultEffectsFactory(GraphicsDevice device, GraphicsResourceTracker disposables, TextureFactory textureFactory)
            : base(device, disposables, textureFactory) { }

        #endregion

        #region API

        // Monogame's BasicEffect uses Phong's shading, while glTF uses PBR shading,
        // so given monogame's limitations, we try to guess the most appropiate values
        // to have acceptable looking renders.

        protected override Effect CreateEffect(GLTFMATERIAL srcMaterial, bool isSkinned)
        {
            return isSkinned
                ? CreateSkinnedEffect(srcMaterial)
                : CreateRigidEffect(srcMaterial);
        }

        protected virtual Effect CreateRigidEffect(GLTFMATERIAL srcMaterial)
        {
            var dstMaterial = srcMaterial.Alpha == Schema2.AlphaMode.MASK
                ? CreateAlphaTestEffect(srcMaterial)
                : CreateBasicEffect(srcMaterial);

            return dstMaterial;
        }

        protected virtual Effect CreateBasicEffect(GLTFMATERIAL srcMaterial)
        {
            var dstMaterial = new BasicEffect(Device);

            dstMaterial.Name = srcMaterial.Name;

            dstMaterial.Alpha = _GltfMaterialBasicProperties.GetAlphaLevel(srcMaterial);
            dstMaterial.DiffuseColor = _GltfMaterialBasicProperties.GetDiffuseColor(srcMaterial);
            dstMaterial.SpecularColor = _GltfMaterialBasicProperties.GetSpecularColor(srcMaterial);
            dstMaterial.SpecularPower = _GltfMaterialBasicProperties.GetSpecularPower(srcMaterial);
            dstMaterial.EmissiveColor = _GltfMaterialBasicProperties.GeEmissiveColor(srcMaterial);
            dstMaterial.Texture = UseDiffuseTexture(srcMaterial);

            if (srcMaterial.Unlit)
            {
                dstMaterial.EmissiveColor = dstMaterial.DiffuseColor;
                dstMaterial.SpecularColor = Vector3.Zero;
                dstMaterial.SpecularPower = 16;
            }

            dstMaterial.PreferPerPixelLighting = true;
            dstMaterial.TextureEnabled = dstMaterial.Texture != null;

            return dstMaterial;
        }

        protected virtual Effect CreateAlphaTestEffect(GLTFMATERIAL srcMaterial)
        {
            var dstMaterial = new AlphaTestEffect(Device);

            dstMaterial.Name = srcMaterial.Name;

            dstMaterial.Alpha = _GltfMaterialBasicProperties.GetAlphaLevel(srcMaterial);
            //dstMaterial.AlphaFunction = CompareFunction.GreaterEqual;
            dstMaterial.ReferenceAlpha = (int)(srcMaterial.AlphaCutoff * 255);

            dstMaterial.DiffuseColor = _GltfMaterialBasicProperties.GetDiffuseColor(srcMaterial);

            dstMaterial.Texture = UseDiffuseTexture(srcMaterial);

            return dstMaterial;
        }

        protected virtual Effect CreateSkinnedEffect(GLTFMATERIAL srcMaterial)
        {
            var dstMaterial = new SkinnedEffect(Device);

            dstMaterial.Name = srcMaterial.Name;

            dstMaterial.Alpha = _GltfMaterialBasicProperties.GetAlphaLevel(srcMaterial);
            dstMaterial.DiffuseColor = _GltfMaterialBasicProperties.GetDiffuseColor(srcMaterial);
            dstMaterial.SpecularColor = _GltfMaterialBasicProperties.GetSpecularColor(srcMaterial);
            dstMaterial.SpecularPower = _GltfMaterialBasicProperties.GetSpecularPower(srcMaterial);
            dstMaterial.EmissiveColor = _GltfMaterialBasicProperties.GeEmissiveColor(srcMaterial);
            dstMaterial.Texture = UseDiffuseTexture(srcMaterial);

            dstMaterial.WeightsPerVertex = 4;
            dstMaterial.PreferPerPixelLighting = true;

            // apparently, SkinnedEffect does not support disabling textures, so we set a white texture here.
            dstMaterial.Texture ??= UseWhiteTexture(); // creates a dummy white texture.

            return dstMaterial;
        }

        protected virtual Texture2D UseDiffuseTexture(GLTFMATERIAL srcMaterial)
        {
            var diffuse = srcMaterial.FindChannel("Diffuse")
                ?? srcMaterial.FindChannel("BaseColor");

            return diffuse == null
                ? null
                : UseTexture(diffuse, null);
        }

        #endregion
    }
}
