using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SharpGLTF.Runtime
{
    public class MonoGameModelTemplate
    {
        #region lifecycle

        public static MonoGameDeviceContent<MonoGameModelTemplate> LoadDeviceModel(GraphicsDevice device, string filePath)
        {
            var model = Schema2.ModelRoot.Load(filePath);

            return CreateDeviceModel(device, model);
        }

        public static MonoGameDeviceContent<MonoGameModelTemplate> CreateDeviceModel(GraphicsDevice device, Schema2.ModelRoot srcModel)
        {
            srcModel.FixTextureSampler();

            var template = Runtime.SceneTemplate.Create(srcModel.DefaultScene, false);

            var context = new LoaderContext(device);

            var meshes = template
                .LogicalMeshIds
                .ToDictionary(k => k, k => context.CreateMesh(srcModel.LogicalMeshes[k]));
            

            var mdl = new MonoGameModelTemplate(template, meshes);

            return new MonoGameDeviceContent<MonoGameModelTemplate>(mdl, context.Disposables.ToArray());
        }

        internal MonoGameModelTemplate(Runtime.SceneTemplate scene, IReadOnlyDictionary<int, ModelMesh> meshes)
        {
            _Template = scene;
            _Meshes = meshes;

            _Effects = _Meshes.Values
                .SelectMany(item => item.Effects)
                .Distinct()
                .ToArray();

            var instance = _Template.CreateInstance();
            instance.SetPoseTransforms();

            _Bounds = default(BoundingSphere);

            foreach (var d in instance.DrawableReferences)
            {
                var b = meshes[d.Item1].BoundingSphere;

                if (d.Item2 is Transforms.StaticTransform statXform) b = b.Transform(statXform.WorldMatrix.ToXna());

                if (d.Item2 is Transforms.SkinTransform skinXform)
                {
                    // this is a bit agressive and probably over-reaching, but with skins you never know the actual bounds
                    // unless you calculate the bounds frame by frame.

                    var bb = b;
                    
                    foreach(var xb in skinXform.SkinMatrices.Select(item => bb.Transform(item.ToXna())))
                    {
                        b = BoundingSphere.CreateMerged(b, xb);
                    }                    
                }

                _Bounds = _Bounds.Radius == 0 ? b : BoundingSphere.CreateMerged(_Bounds, b);
            }            
        }

        #endregion

        #region data

        private readonly Runtime.SceneTemplate _Template;
        internal readonly IReadOnlyDictionary<int, ModelMesh> _Meshes;

        private readonly Effect[] _Effects;
        private readonly BoundingSphere _Bounds;

        #endregion

        #region properties

        public IReadOnlyList<Effect> Effects => _Effects;

        public BoundingSphere Bounds => _Bounds;

        public IEnumerable<string> AnimationTracks => _Template.AnimationTracks;

        #endregion

        #region API

        public MonoGameModelInstance CreateInstance()
        {
            return new MonoGameModelInstance(this, _Template.CreateInstance());
        }
        
        #endregion        
    }    
}
