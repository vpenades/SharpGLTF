using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


#if USINGMONOGAMEMODEL
using MODELMESH = Microsoft.Xna.Framework.Graphics.ModelMesh;
using MODELMESHPART = Microsoft.Xna.Framework.Graphics.ModelMeshPart;
#else
using MODELMESH = SharpGLTF.Runtime.Template.RuntimeModelMesh;
using MODELMESHPART = SharpGLTF.Runtime.Template.RuntimeModelMeshPart;
#endif

namespace SharpGLTF.Runtime.Template
{
    public class MonoGameModelTemplate
    {
        #region lifecycle

        public static MonoGameDeviceContent<MonoGameModelTemplate> LoadDeviceModel(GraphicsDevice device, string filePath, Pipeline.LoaderContext context = null)
        {
            var model = Schema2.ModelRoot.Load(filePath, Validation.ValidationMode.TryFix);

            return CreateDeviceModel(device, model, context);
        }

        public static MonoGameDeviceContent<MonoGameModelTemplate> CreateDeviceModel(GraphicsDevice device, Schema2.ModelRoot srcModel, Pipeline.LoaderContext context = null)
        {
            context ??= new Pipeline.BasicEffectsLoaderContext(device);

            context.Reset();

            var options = new RuntimeOptions { IsolateMemory = true };

            var templates = srcModel.LogicalScenes
                .Select(item => SceneTemplate.Create(item, options))
                .ToArray();            

            var srcMeshes = templates
                .SelectMany(item => item.LogicalMeshIds)
                .Distinct()
                .Select(idx => srcModel.LogicalMeshes[idx]);

            foreach(var srcMesh in srcMeshes)
            {
                context._WriteMesh(srcMesh);
            }

            var dstMeshes = context.CreateRuntimeModels();

            var mdl = new MonoGameModelTemplate(templates,srcModel.DefaultScene.LogicalIndex, dstMeshes);

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

            #pragma warning disable GLTFRT1000 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            _Bounds = scenes.Select(item => new BoundingSphere(item.SphereBounds.center, item.SphereBounds.radius)).ToArray();
            #pragma warning restore GLTFRT1000 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            /*
            _Bounds = scenes
                .Select(item => CalculateBounds(item))
                .ToArray();*/

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
        
        #endregion

        #region API

        public int IndexOfScene(string sceneName) => Array.FindIndex(_Scenes, item => item.Name == sceneName);

        public BoundingSphere GetBounds(int sceneIndex) => _Bounds[sceneIndex];        

        public MonoGameModelInstance CreateInstance() => CreateInstance(_DefaultSceneIndex);

        public MonoGameModelInstance CreateInstance(int sceneIndex)
        {
            return new MonoGameModelInstance(this, _Scenes[sceneIndex].CreateInstance());
        }

        private BoundingSphere CalculateBounds(SceneTemplate scene)
        {
            var instances = scene.CreateInstance();            

            var bounds = default(BoundingSphere);

            foreach (var inst in instances)
            {
                var b = _Meshes[inst.Template.LogicalMeshIndex].BoundingSphere;

                System.Diagnostics.Debug.Assert(b.Radius > 0);

                switch(inst.Transform)
                {
                    case Transforms.RigidTransform statXform:
                        b = b.Transform(statXform.WorldMatrix);
                        break;

                    case Transforms.SkinnedTransform skinXform:
                        // this is a bit agressive and probably over-reaching, but with skins you
                        // never know the actual bounds unless you calculate them frame by frame.

                        var bb = b;

                        foreach (var xb in skinXform.SkinMatrices.Select(item => bb.Transform(item)))
                        {
                            b = BoundingSphere.CreateMerged(b, xb);
                        }
                        break;
                }                

                bounds = bounds.Radius == 0 ? b : BoundingSphere.CreateMerged(bounds, b);
            }

            return bounds;
        }
        
        #endregion        
    }    
}
