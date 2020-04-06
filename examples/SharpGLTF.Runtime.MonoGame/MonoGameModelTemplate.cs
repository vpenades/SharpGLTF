using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#if USINGMONOGAMEMODEL
using MODELMESH = Microsoft.Xna.Framework.Graphics.ModelMesh;
using MODELMESHPART = Microsoft.Xna.Framework.Graphics.ModelMeshPart;
#else
using MODELMESH = SharpGLTF.Runtime.ModelMeshReplacement;
using MODELMESHPART = SharpGLTF.Runtime.ModelMeshPartReplacement;
#endif

namespace SharpGLTF.Runtime
{
    public class MonoGameModelTemplate
    {
        #region lifecycle

        public static MonoGameDeviceContent<MonoGameModelTemplate> LoadDeviceModel(GraphicsDevice device, string filePath)
        {
            var model = Schema2.ModelRoot.Load(filePath, Validation.ValidationMode.TryFix);

            return CreateDeviceModel(device, model);
        }

        public static MonoGameDeviceContent<MonoGameModelTemplate> CreateDeviceModel(GraphicsDevice device, Schema2.ModelRoot srcModel)
        {
            srcModel.FixTextureSampler();

            var templates = srcModel.LogicalScenes
                .Select(item => SceneTemplate.Create(item, true))
                .ToArray();
            
            var context = new LoaderContext(device);

            var meshes = templates
                .SelectMany(item => item.LogicalMeshIds)                
                .ToDictionary(k => k, k => context.CreateMesh(srcModel.LogicalMeshes[k]));            

            var mdl = new MonoGameModelTemplate(templates,srcModel.DefaultScene.LogicalIndex, meshes);

            return new MonoGameDeviceContent<MonoGameModelTemplate>(mdl, context.Disposables.ToArray());
        }
        
        internal MonoGameModelTemplate(SceneTemplate[] scenes, int defaultSceneIndex, IReadOnlyDictionary<int, MODELMESH> meshes)
        {
            _Meshes = meshes;
            _Effects = _Meshes.Values
                .SelectMany(item => item.Effects)
                .Distinct()
                .ToArray();

            _Scenes = scenes;
            _Bounds = scenes
                .Select(item => CalculateBounds(item))
                .ToArray();

            _DefaultSceneIndex = defaultSceneIndex;
        }

        #endregion

        #region data
        
        /// <summary>
        /// Meshes shared by all the scenes.
        /// </summary>
        internal readonly IReadOnlyDictionary<int, MODELMESH> _Meshes;

        /// <summary>
        /// Effects shared by all the meshes.
        /// </summary>
        private readonly Effect[] _Effects;


        private readonly SceneTemplate[] _Scenes;
        private readonly BoundingSphere[] _Bounds;

        private readonly int _DefaultSceneIndex;

        #endregion

        #region properties

        public int SceneCount => _Scenes.Length;

        public IReadOnlyList<Effect> Effects => _Effects;

        public BoundingSphere Bounds => GetBounds(_DefaultSceneIndex);

        public IEnumerable<string> AnimationTracks => GetAnimationTracks(_DefaultSceneIndex);

        #endregion

        #region API

        public int IndexOfScene(string sceneName) => Array.FindIndex(_Scenes, item => item.Name == sceneName);

        public BoundingSphere GetBounds(int sceneIndex) => _Bounds[sceneIndex];

        public IEnumerable<string> GetAnimationTracks(int sceneIndex) => _Scenes[sceneIndex].AnimationTracks;

        public MonoGameModelInstance CreateInstance() => CreateInstance(_DefaultSceneIndex);

        public MonoGameModelInstance CreateInstance(int sceneIndex)
        {
            return new MonoGameModelInstance(this, _Scenes[sceneIndex].CreateInstance());
        }

        private BoundingSphere CalculateBounds(SceneTemplate scene)
        {
            var instance = scene.CreateInstance();
            instance.SetPoseTransforms();

            var bounds = default(BoundingSphere);

            foreach (var d in instance.DrawableInstances)
            {
                var b = _Meshes[d.Template.LogicalMeshIndex].BoundingSphere;

                if (d.Transform is Transforms.RigidTransform statXform) b = b.Transform(statXform.WorldMatrix.ToXna());

                if (d.Transform is Transforms.SkinnedTransform skinXform)
                {
                    // this is a bit agressive and probably over-reaching, but with skins you never know the actual bounds
                    // unless you calculate the bounds frame by frame.

                    var bb = b;

                    foreach (var xb in skinXform.SkinMatrices.Select(item => bb.Transform(item.ToXna())))
                    {
                        b = BoundingSphere.CreateMerged(b, xb);
                    }
                }

                bounds = bounds.Radius == 0 ? b : BoundingSphere.CreateMerged(bounds, b);
            }

            return bounds;
        }
        
        #endregion        
    }    
}
