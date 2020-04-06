using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
        /// <param name="srcScene">The source <see cref="Schema2.Scene"/> to templateize.</param>
        /// <param name="isolateMemory">True if we want to copy data instead of sharing it.</param>
        /// <returns>A new <see cref="SceneTemplate"/> instance.</returns>
        public static SceneTemplate Create(Schema2.Scene srcScene, bool isolateMemory)
        {
            Guard.NotNull(srcScene, nameof(srcScene));

            // gather scene nodes.

            var srcNodes = Schema2.Node.Flatten(srcScene)
                .Select((key, idx) => (key, idx))
                .ToDictionary(pair => pair.key, pair => pair.idx);

            int indexSolver(Schema2.Node srcNode)
            {
                if (srcNode == null) return -1;
                return srcNodes[srcNode];
            }

            // create bones.

            var dstNodes = new NodeTemplate[srcNodes.Count];

            foreach (var srcNode in srcNodes)
            {
                var nidx = srcNode.Value;

                // parent index
                var pidx = indexSolver(srcNode.Key.VisualParent);
                if (pidx >= nidx) throw new InvalidOperationException("parent indices should be below child indices");

                // child indices
                var cidx = srcNode.Key.VisualChildren
                    .Select(n => indexSolver(n))
                    .ToArray();

                dstNodes[nidx] = new NodeTemplate(srcNode.Key, pidx, cidx, isolateMemory);
            }

            // create drawables.

            var instances = srcNodes.Keys
                .Where(item => item.Mesh != null)
                .ToList();

            var drawables = new DrawableTemplate[instances.Count];

            for (int i = 0; i < drawables.Length; ++i)
            {
                var srcInstance = instances[i];

                drawables[i] = srcInstance.Skin != null
                    ?
                    (DrawableTemplate)new SkinnedDrawableTemplate(srcInstance, indexSolver)
                    :
                    (DrawableTemplate)new RigidDrawableTemplate(srcInstance, indexSolver);
            }

            // gather animation durations.

            var dstTracks = new Collections.NamedList<float>();

            foreach (var anim in srcScene.LogicalParent.LogicalAnimations)
            {
                var index = anim.LogicalIndex;
                var name = anim.Name;

                float duration = dstTracks.Count <= index ? 0 : dstTracks[index];
                duration = Math.Max(duration, anim.Duration);
                dstTracks.SetValue(index, name, anim.Duration);
            }

            return new SceneTemplate(srcScene.Name, dstNodes, drawables, dstTracks);
        }

        private SceneTemplate(string name, NodeTemplate[] nodes, DrawableTemplate[] drawables, Collections.NamedList<float> animTracks)
        {
            _Name = name;
            _NodeTemplates = nodes;
            _DrawableReferences = drawables;
            _AnimationTracks = animTracks;
        }

        #endregion

        #region data

        private readonly String _Name;

        private readonly NodeTemplate[] _NodeTemplates;
        private readonly DrawableTemplate[] _DrawableReferences;

        private readonly Collections.NamedList<float> _AnimationTracks;

        #endregion

        #region properties

        public String Name => _Name;

        /// <summary>
        /// Gets the unique indices of <see cref="Schema2.Mesh"/> instances in <see cref="Schema2.ModelRoot.LogicalMeshes"/>
        /// </summary>
        public IEnumerable<int> LogicalMeshIds => _DrawableReferences.Select(item => item.LogicalMeshIndex).Distinct();

        /// <summary>
        /// Gets A collection of animation track names.
        /// </summary>
        public IEnumerable<string> AnimationTracks => _AnimationTracks.Names;

        #endregion

        #region API

        /// <summary>
        /// Creates a new <see cref="SceneInstance"/> of this <see cref="SceneTemplate"/>
        /// that can be used to render the scene.
        /// </summary>
        /// <returns>A new <see cref="SceneInstance"/> object.</returns>
        public SceneInstance CreateInstance()
        {
            var inst = new SceneInstance(_NodeTemplates, _DrawableReferences, _AnimationTracks);

            inst.SetPoseTransforms();

            return inst;
        }

        #endregion
    }
}
