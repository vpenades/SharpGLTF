using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using SharpGLTF.Schema2;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Defines a templatized representation of a <see cref="Schema2.Scene"/> that can be used
    /// to create <see cref="SceneInstance"/>, which can help render a scene on a client application.
    /// </summary>
    public class SceneTemplate
    {
        #region lifecycle

        /// <summary>
        /// Creates a new <see cref="SceneTemplate"/> from a given <see cref="Schema2.Scene"/>.
        /// </summary>
        /// <param name="srcScene">The source <see cref="Schema2.Scene"/> to templatize.</param>
        /// <param name="options">Custom processing options, or null.</param>
        /// <returns>A new <see cref="SceneTemplate"/> instance.</returns>
        public static SceneTemplate Create(Schema2.Scene srcScene, RuntimeOptions options = null)
        {
            Guard.NotNull(srcScene, nameof(srcScene));

            options ??= new RuntimeOptions();

            var armature = ArmatureTemplate.Create(srcScene, options);

            // gather scene nodes.

            var srcNodes = Schema2.Node.Flatten(srcScene)
                .Select((key, idx) => (key, idx))
                .ToDictionary(pair => pair.key, pair => pair.idx);

            int indexSolver(Schema2.Node srcNode)
            {
                if (srcNode == null) return -1;
                return srcNodes[srcNode];
            }

            // create drawables.

            var instances = srcNodes.Keys
                .Where(item => item.Mesh != null)
                .ToList();

            var drawables = new List<DrawableTemplate>();

            for (int i = 0; i < instances.Count; ++i)
            {
                var srcInstance = instances[i];

                if (srcInstance.Skin != null)
                {
                    drawables.Add(new SkinnedDrawableTemplate(srcInstance, indexSolver));
                    continue;
                }

                if (srcInstance.GetGpuInstancing() == null)
                {
                    drawables.Add(new RigidDrawableTemplate(srcInstance, indexSolver));
                    continue;
                }

                switch (options.GpuMeshInstancing)
                {
                    case MeshInstancing.Discard: break;

                    case MeshInstancing.Enabled:
                        drawables.Add(new InstancedDrawableTemplate(srcInstance, indexSolver));
                        break;

                    case MeshInstancing.SingleMesh:
                        drawables.Add(new RigidDrawableTemplate(srcInstance, indexSolver));
                        break;

                    default: throw new NotImplementedException();
                }
            }

            var extras = RuntimeOptions.ConvertExtras(srcScene, options);            

            var template = new SceneTemplate(srcScene.Name, extras, armature, drawables.ToArray());

            #pragma warning disable GLTFRT1000 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            template.SphereBounds = template.EvaluateBoundingSphere(srcScene.LogicalParent.LogicalMeshes.Decode());
            #pragma warning restore GLTFRT1000 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            return template;
        }

        private SceneTemplate(string name, Object extras, ArmatureTemplate armature, DrawableTemplate[] drawables)
        {
            _Name = name;
            _Extras = extras;
            _Armature = armature;
            _DrawableReferences = drawables;
        }

        #endregion

        #region data

        private readonly String _Name;
        private readonly Object _Extras;
        private readonly ArmatureTemplate _Armature;
        private readonly DrawableTemplate[] _DrawableReferences;        

        #endregion

        #region properties

        public String Name => _Name;

        public Object Extras => _Extras;

        #if NET8_0_OR_GREATER
        [Experimental("GLTFRT1000")] // I will probably change the return type and the way this value is accessed
        #endif
        public (System.Numerics.Vector3 center, float radius) SphereBounds { get; set; }

        /// <summary>
        /// Gets the unique indices of <see cref="Schema2.Mesh"/> instances in <see cref="Schema2.ModelRoot.LogicalMeshes"/> used by this template.
        /// </summary>
        public IEnumerable<int> LogicalMeshIds => _DrawableReferences.Select(item => item.LogicalMeshIndex).Distinct();

        #endregion

        #region API

        /// <summary>
        /// Creates a new <see cref="SceneInstance"/> of this <see cref="SceneTemplate"/>
        /// that can be used to render the scene.
        /// </summary>
        /// <returns>A new <see cref="SceneInstance"/> object.</returns>
        public SceneInstance CreateInstance()
        {
            var inst = new SceneInstance(_Armature, _DrawableReferences);

            inst.Armature.SetPoseTransforms();

            return inst;
        }

        #endregion
    }
}
