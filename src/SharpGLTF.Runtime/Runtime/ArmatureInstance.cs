using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XFORM = System.Numerics.Matrix4x4;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Represents the transform states of a collection of bones.
    /// </summary>
    public class ArmatureInstance
    {
        #region constructor

        internal ArmatureInstance(ArmatureTemplate template)
        {
            _Template = template;
            _NodeTemplates = template.Nodes;

            var ni = new NodeInstance[_NodeTemplates.Count];

            for (var i = 0; i < ni.Length; ++i)
            {
                var n = _NodeTemplates[i];
                var p = n.ParentIndex < 0 ? null : ni[n.ParentIndex];

                ni[i] = new NodeInstance(n, p);
            }

            _NodeInstances = ni;

            _MaterialTemplates = template.Materials;

            _AnimationTracks = template.Tracks;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        internal readonly ArmatureTemplate _Template;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<NodeTemplate> _NodeTemplates;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<NodeInstance> _NodeInstances;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<MaterialTemplate> _MaterialTemplates;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<AnimationTrackInfo> _AnimationTracks;

        #endregion

        #region properties

        /// <summary>
        /// Gets a flattened collection of all the nodes of this armature.
        /// </summary>
        public IReadOnlyList<NodeInstance> LogicalNodes => _NodeInstances;

        /// <summary>
        /// Gets all the <see cref="NodeInstance"/> roots used by this <see cref="SceneInstance"/>.
        /// </summary>
        public IEnumerable<NodeInstance> VisualNodes => _NodeInstances.Where(item => item.VisualParent == null);

        /// <summary>
        /// Gets the total number of animation tracks for this instance.
        /// </summary>
        public IReadOnlyList<AnimationTrackInfo> AnimationTracks => _AnimationTracks;

        #endregion

        #region API

        /// <summary>
        /// Sets the matrix of a bone.
        /// </summary>
        /// <param name="name">The name of the node to be set.</param>
        /// <param name="localMatrix">A matrix relative to its parent bone.</param>
        public void SetLocalMatrix(string name, XFORM localMatrix)
        {
            var n = LogicalNodes.FirstOrDefault(item => item.Name == name);
            if (n == null) throw new ArgumentException($"{name} not found", nameof(name));
            n.LocalMatrix = localMatrix;
        }

        /// <summary>
        /// Sets the matrix of a bone.
        /// </summary>
        /// <param name="name">The name of the node to be set.</param>
        /// <param name="modelMatrix">A matrix relative to the model.</param>
        public void SetModelMatrix(string name, XFORM modelMatrix)
        {
            var n = LogicalNodes.FirstOrDefault(item => item.Name == name);
            if (n == null) throw new ArgumentException($"{name} not found", nameof(name));
            n.ModelMatrix = modelMatrix;
        }

        /// <summary>
        /// Resets the bone transforms to their default positions.
        /// </summary>
        public void SetPoseTransforms()
        {
            _Template.ApplyAnimationTo(this, -1, 0);
        }

        /// <summary>
        /// Sets the bone transforms from an animation frame.
        /// </summary>
        /// <param name="trackLogicalIndex">The animation track index.</param>
        /// <param name="time">The animation time frame.</param>
        /// <param name="looped">True to use the animation as a looped animation.</param>
        public void SetAnimationFrame(int trackLogicalIndex, float time, bool looped = true)
        {
            _Template.ApplyAnimationTo(this, trackLogicalIndex, time, looped);
        }

        public void SetAnimationFrame(params (int TrackIdx, float Time, float Weight)[] blended)
        {
            _Template.ApplyAnimationTo(this, blended);
        }        

        #endregion
    }
}
