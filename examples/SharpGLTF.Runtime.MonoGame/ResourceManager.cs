using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SharpGLTF.Runtime
{
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

    class MaterialFactory
    {
        #region lifecycle

        public MaterialFactory(GraphicsDevice device, GraphicsResourceTracker disposables)
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

        private BasicEffect _DefaultRigid;
        private SkinnedEffect _DefaultSkinned;

        #endregion        

        #region API - Schema

        // Monogame's BasicEffect uses Phong's shading, while glTF uses PBR shading, so
        // given monogame's limitations, we try to guess the most appropiate values
        // to have a reasonably good looking renders.

        public Effect UseRigidEffect(Schema2.Material srcMaterial)
        {
            if (_Device == null) throw new InvalidOperationException();

            if (srcMaterial == null)
            {
                if (_DefaultRigid == null)
                {
                    _DefaultRigid = new BasicEffect(_Device);
                    _Disposables.AddDisposable(_DefaultRigid);
                }

                return _DefaultRigid;
            }

            if (_RigidEffects.TryGetValue(srcMaterial, out Effect dstMaterial)) return dstMaterial;

            dstMaterial = srcMaterial.Alpha == Schema2.AlphaMode.MASK ? CreateAlphaTestEffect(srcMaterial) : CreateBasicEffect(srcMaterial);

            _RigidEffects[srcMaterial] = dstMaterial;

            return dstMaterial;
        }

        private Effect CreateBasicEffect(Schema2.Material srcMaterial)
        {
            var dstMaterial = new BasicEffect(_Device);
            _Disposables.AddDisposable(dstMaterial);           


            dstMaterial.Name = srcMaterial.Name;

            dstMaterial.Alpha = GetAlphaLevel(srcMaterial);
            dstMaterial.DiffuseColor = GetDiffuseColor(srcMaterial);
            dstMaterial.SpecularColor = GetSpecularColor(srcMaterial);
            dstMaterial.SpecularPower = GetSpecularPower(srcMaterial);
            dstMaterial.EmissiveColor = GeEmissiveColor(srcMaterial);
            dstMaterial.Texture = GetDiffuseTexture(srcMaterial);

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

        private Effect CreateAlphaTestEffect(Schema2.Material srcMaterial)
        {
            var dstMaterial = new AlphaTestEffect(_Device);
            _Disposables.AddDisposable(dstMaterial);

            dstMaterial.Name = srcMaterial.Name;

            dstMaterial.Alpha = GetAlphaLevel(srcMaterial);
            //dstMaterial.AlphaFunction = CompareFunction.GreaterEqual;
            dstMaterial.ReferenceAlpha = (int)(srcMaterial.AlphaCutoff * 255);
            

            dstMaterial.DiffuseColor = GetDiffuseColor(srcMaterial);
            
            dstMaterial.Texture = GetDiffuseTexture(srcMaterial);            

            return dstMaterial;
        }

        public Effect UseSkinnedEffect(Schema2.Material srcMaterial)
        {
            if (_Device == null) throw new InvalidOperationException();

            if (srcMaterial == null)
            {
                if (_DefaultSkinned == null)
                {
                    _DefaultSkinned = new SkinnedEffect(_Device);
                    _Disposables.AddDisposable(_DefaultRigid);
                }

                return _DefaultSkinned;
            }

            if (_SkinnedEffects.TryGetValue(srcMaterial, out SkinnedEffect dstMaterial)) return dstMaterial;

            dstMaterial = new SkinnedEffect(_Device);
            _SkinnedEffects[srcMaterial] = dstMaterial;
            _Disposables.AddDisposable(dstMaterial);

            dstMaterial.Name = srcMaterial.Name;

            dstMaterial.Alpha = GetAlphaLevel(srcMaterial);
            dstMaterial.DiffuseColor = GetDiffuseColor(srcMaterial);
            dstMaterial.SpecularColor = GetSpecularColor(srcMaterial);
            dstMaterial.SpecularPower = GetSpecularPower(srcMaterial);
            dstMaterial.EmissiveColor = GeEmissiveColor(srcMaterial);
            dstMaterial.Texture = GetDiffuseTexture(srcMaterial);

            dstMaterial.WeightsPerVertex = 4;
            dstMaterial.PreferPerPixelLighting = true;
            // apparently, SkinnedEffect does not support disabling textures, so we set a white texture here.
            if (dstMaterial.Texture == null) dstMaterial.Texture = _TexFactory.UseWhiteImage();

            return dstMaterial;
        }

        private static float GetAlphaLevel(Schema2.Material srcMaterial)
        {
            if (srcMaterial.Alpha == Schema2.AlphaMode.OPAQUE) return 1;

            var baseColor = srcMaterial.FindChannel("BaseColor");

            if (baseColor == null) return 1;

            return baseColor.Value.Parameter.W;
        }

        private static Vector3 GetDiffuseColor(Schema2.Material srcMaterial)
        {
            var diffuse = srcMaterial.FindChannel("Diffuse");

            if (diffuse == null) diffuse = srcMaterial.FindChannel("BaseColor");

            if (diffuse == null) return Vector3.One;

            return new Vector3(diffuse.Value.Parameter.X, diffuse.Value.Parameter.Y, diffuse.Value.Parameter.Z);
        }

        private static Vector3 GetSpecularColor(Schema2.Material srcMaterial)
        {
            var mr = srcMaterial.FindChannel("MetallicRoughness");

            if (mr == null) return Vector3.One; // default value 16

            var diffuse = GetDiffuseColor(srcMaterial);
            var metallic = mr.Value.Parameter.X;
            var roughness = mr.Value.Parameter.Y;

            var k = Vector3.Zero;
            k += Vector3.Lerp(diffuse, Vector3.Zero, roughness);
            k += Vector3.Lerp(diffuse, Vector3.One, metallic);
            k *= 0.5f;

            return k;
        }

        private static float GetSpecularPower(Schema2.Material srcMaterial)
        {
            var mr = srcMaterial.FindChannel("MetallicRoughness");

            if (mr == null) return 16; // default value = 16

            var metallic = mr.Value.Parameter.X;
            var roughness = mr.Value.Parameter.Y;

            return 4 + 16 * metallic;
        }

        private static Vector3 GeEmissiveColor(Schema2.Material srcMaterial)
        {
            var emissive = srcMaterial.FindChannel("Emissive");

            if (emissive == null) return Vector3.Zero;

            return new Vector3(emissive.Value.Parameter.X, emissive.Value.Parameter.Y, emissive.Value.Parameter.Z);
        }

        private Texture2D GetDiffuseTexture(Schema2.Material srcMaterial)
        {
            var diffuse = srcMaterial.FindChannel("Diffuse");

            if (diffuse == null) diffuse = srcMaterial.FindChannel("BaseColor");

            if (diffuse == null) return null;

            var name = srcMaterial.Name;
            if (name == null) name = "null";
            name += "-Diffuse";            

            return _TexFactory.UseTexture(diffuse.Value.Texture?.PrimaryImage?.Content ?? default, name);
        }

        #endregion
    }


}
