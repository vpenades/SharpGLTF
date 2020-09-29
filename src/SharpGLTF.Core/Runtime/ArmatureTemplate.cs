using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Runtime
{
    class ArmatureTemplate
    {
        #region lifecycle

        /// <summary>
        /// Creates a new <see cref="ArmatureTemplate"/> from a given <see cref="Schema2.Scene"/>.
        /// </summary>
        /// <param name="srcScene">The source <see cref="Schema2.Scene"/> to templatize.</param>
        /// <param name="isolateMemory">True if we want to copy data instead of sharing it.</param>
        /// <returns>A new <see cref="ArmatureTemplate"/> instance.</returns>
        public static ArmatureTemplate Create(Schema2.Scene srcScene, bool isolateMemory)
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

            // gather animation durations.

            var dstTracks = srcScene.LogicalParent
                .LogicalAnimations
                .Select(item => new AnimationTrackInfo(item.Name, item.Duration))
                .ToArray();

            return new ArmatureTemplate(dstNodes, dstTracks);
        }

        private ArmatureTemplate(NodeTemplate[] nodes, AnimationTrackInfo[] animTracks)
        {
            _NodeTemplates = nodes;
            _AnimationTracks = animTracks;
        }

        #endregion

        #region data

        private readonly NodeTemplate[] _NodeTemplates;
        private readonly AnimationTrackInfo[] _AnimationTracks;

        #endregion

        #region properties

        public NodeTemplate[] Nodes => _NodeTemplates;

        public AnimationTrackInfo[] Tracks => _AnimationTracks;

        #endregion

    }
}
