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
            _AnimationTracks = template.Tracks;

            _NodeTemplates = template.Nodes;
            _NodeInstances = new NodeInstance[_NodeTemplates.Length];

            for (var i = 0; i < _NodeInstances.Length; ++i)
            {
                var n = _NodeTemplates[i];
                var pidx = _NodeTemplates[i].ParentIndex;

                if (pidx >= i) throw new ArgumentException("invalid parent index", nameof(template.Nodes));

                var p = pidx < 0 ? null : _NodeInstances[pidx];

                _NodeInstances[i] = new NodeInstance(n, p);
            }
        }

        #endregion

        #region data

        private readonly NodeTemplate[] _NodeTemplates;
        private readonly NodeInstance[] _NodeInstances;

        private readonly AnimationTrackInfo[] _AnimationTracks;

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

        public int IndexOfTrack(string name)
        {
            return Array.FindIndex(_AnimationTracks, item => item.Name == name);
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
            n.WorldMatrix = worldMatrix;
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
