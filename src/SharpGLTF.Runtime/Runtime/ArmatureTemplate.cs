using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Represents a flattened collection of nodes/joints/bones, which define a Skeleton when correlated.
    /// </summary>
    /// /// <remarks>
    /// Only the nodes used by a given <see cref="Schema2.Scene"/> will be copied.
    /// Also, nodes will be reordered so children nodes always come after their parents (for fast evaluation),
    /// so it's important to keep in mind that <see cref="NodeTemplate"/> indices will differ from those
    /// in <see cref="Schema2.Scene.VisualChildren"/>.
    /// </remarks>
    class ArmatureTemplate
    {
        #region lifecycle

        /// <summary>
        /// Creates a new <see cref="ArmatureTemplate"/> based on the nodes of <see cref="Schema2.Scene"/>.
        /// </summary>
        /// <param name="srcScene">The source <see cref="Schema2.Scene"/> from where to take the nodes.</param>
        /// <param name="options">Custom processing options, or null.</param>
        /// <returns>A new <see cref="ArmatureTemplate"/> instance.</returns>
        internal static ArmatureTemplate Create(Schema2.Scene srcScene, RuntimeOptions options)
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

                // child indices
                var cidx = srcNode.Key.VisualChildren
                    .Select(n => indexSolver(n))
                    .ToArray();

                dstNodes[nidx] = new NodeTemplate(srcNode.Key, pidx, cidx, options);
            }

            // gather materials

            var dstMaterials = srcNodes
                .Keys
                .Select(item => item.Mesh)
                .Where(item => item != null)
                .SelectMany(mesh => mesh.Primitives)
                .Select(prim => prim.Material)
                .Where(mat => mat != null)
                .Select(mat => new MaterialTemplate(mat, options))
                .ToArray();

            // gather animation durations.

            var dstTracks = srcScene.LogicalParent
                .LogicalAnimations
                .Select(item => new AnimationTrackInfo(item.Name, RuntimeOptions.ConvertExtras(item, options), item.Duration))
                .ToArray();

            return new ArmatureTemplate(dstNodes, dstMaterials, dstTracks);
        }

        private ArmatureTemplate(NodeTemplate[] nodes, MaterialTemplate[] materials, AnimationTrackInfo[] animTracks)
        {
            // check that child nodes always follow parent nodes

            for (int idx = 0; idx < nodes.Length; ++idx)
            {
                var n = nodes[idx];

                if (n == null) throw new ArgumentNullException(nameof(nodes));
                if (n.ParentIndex >= idx) throw new ArgumentOutOfRangeException(nameof(nodes), $"[{idx}].ParentIndex must be lower than {idx}, but found {n.ParentIndex}");

                for (int j = 0; j < n.ChildIndices.Count; ++j)
                {
                    var cidx = n.ChildIndices[j];
                    if (cidx >= nodes.Length) throw new ArgumentOutOfRangeException(nameof(nodes), $"[{idx}].ChildIndices[{j}] must be lower than {nodes.Length}, but found {cidx}");
                    if (cidx <= idx) throw new ArgumentOutOfRangeException(nameof(nodes), $"[{idx}].ChildIndices[{j}] must be heigher than {idx}, but found {cidx}");
                }
            }

            _NodeTemplates = nodes;
            _MaterialTemplates = materials;
            _AnimationTracks = animTracks;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly NodeTemplate[] _NodeTemplates;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly MaterialTemplate[] _MaterialTemplates;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly AnimationTrackInfo[] _AnimationTracks;

        #endregion

        #region properties

        /// <summary>
        /// Gets the flattened list of nodes, ordered so parents appear before children.
        /// </summary>
        public IReadOnlyList<NodeTemplate> Nodes => _NodeTemplates;

        /// <summary>
        /// Gets the list of materials used by the meshes of this model
        /// </summary>
        public IReadOnlyList<MaterialTemplate> Materials => _MaterialTemplates;

        /// <summary>
        /// Gets the animations tracks info.
        /// </summary>
        public IReadOnlyList<AnimationTrackInfo> Tracks => _AnimationTracks;

        #endregion
    }
}
