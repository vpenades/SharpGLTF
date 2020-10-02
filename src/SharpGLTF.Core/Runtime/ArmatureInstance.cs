using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Transforms;

using XFORM = System.Numerics.Matrix4x4;

namespace SharpGLTF.Runtime
{
    public class ArmatureInstance
    {
        #region constructor

        internal ArmatureInstance(ArmatureTemplate template)
        {
            _NodeTemplates = template.Nodes;

            var ni = new NodeInstance[_NodeTemplates.Count];

            for (var i = 0; i < ni.Length; ++i)
            {
                var n = _NodeTemplates[i];
                var p = n.ParentIndex < 0 ? null : ni[n.ParentIndex];

                ni[i] = new NodeInstance(n, p);
            }

            _NodeInstances = ni;

            _AnimationTracks = template.Tracks;
        }

        #endregion

        #region data

        private readonly IReadOnlyList<NodeTemplate> _NodeTemplates;
        private readonly IReadOnlyList<NodeInstance> _NodeInstances;

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
        /// Finds the index of a named animation track.
        /// </summary>
        /// <param name="name">The animation name</param>
        /// <returns>The index of the animation track, or -1 if not found.</returns>
        public int IndexOfTrack(string name)
        {
            for (int i = 0; i < _AnimationTracks.Count; ++i)
            {
                if (_AnimationTracks[i].Name == name) return i;
            }

            return -1;
        }

        public void SetLocalMatrix(string name, XFORM localMatrix)
        {
            var n = LogicalNodes.FirstOrDefault(item => item.Name == name);
            if (n == null) return;
            n.LocalMatrix = localMatrix;
        }

        public void SetModelMatrix(string name, XFORM worldMatrix)
        {
            var n = LogicalNodes.FirstOrDefault(item => item.Name == name);
            if (n == null) return;
            n.ModelMatrix = worldMatrix;
        }

        public void SetPoseTransforms()
        {
            foreach (var n in _NodeInstances) n.SetPoseTransform();
        }

        public void SetAnimationFrame(int trackLogicalIndex, float time, bool looped = true)
        {
            if (looped)
            {
                var duration = AnimationTracks[trackLogicalIndex].Duration;
                if (duration > 0) time %= duration;
            }

            foreach (var n in _NodeInstances) n.SetAnimationFrame(trackLogicalIndex, time);
        }

        public void SetAnimationFrame(params (int TrackIdx, float Time, float Weight)[] blended)
        {
            SetAnimationFrame(_NodeInstances, blended);
        }

        public static void SetAnimationFrame(IEnumerable<NodeInstance> nodes, params (int TrackIdx, float Time, float Weight)[] blended)
        {
            Guard.NotNull(nodes, nameof(nodes));

            Span<int> tracks = stackalloc int[blended.Length];
            Span<float> times = stackalloc float[blended.Length];
            Span<float> weights = stackalloc float[blended.Length];

            float w = blended.Sum(item => item.Weight);

            w = w == 0 ? 1 : 1 / w;

            for (int i = 0; i < blended.Length; ++i)
            {
                tracks[i] = blended[i].TrackIdx;
                times[i] = blended[i].Time;
                weights[i] = blended[i].Weight * w;
            }

            foreach (var n in nodes) n.SetAnimationFrame(tracks, times, weights);
        }

        #endregion
    }
}
