using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpGLTF.Transforms;

using XFORM = System.Numerics.Matrix4x4;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Defines a node of a scene graph in <see cref="SceneInstance"/>
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class NodeInstance
    {
        #region lifecycle

        internal NodeInstance(NodeTemplate template, NodeInstance parent)
        {
            _Template = template;
            _Parent = parent;
        }

        #endregion

        #region data

        private readonly NodeTemplate _Template;
        private readonly NodeInstance _Parent;

        private XFORM _LocalMatrix;
        private XFORM? _WorldMatrix;

        private SparseWeight8 _MorphWeights;

        #endregion

        #region properties

        public String Name => _Template.Name;

        public NodeInstance VisualParent => _Parent;

        public SparseWeight8 MorphWeights
        {
            get => _MorphWeights;
            set => _MorphWeights = value;
        }

        public XFORM LocalMatrix
        {
            get => _LocalMatrix;
            set
            {
                _LocalMatrix = value;
                _WorldMatrix = null;
            }
        }

        public XFORM WorldMatrix
        {
            get => _GetWorldMatrix();
            set => _SetWorldMatrix(value);
        }

        /// <summary>
        /// Gets a value indicating whether any of the transforms down the scene tree has been modified.
        /// </summary>
        private bool TransformChainIsDirty
        {
            get
            {
                if (!_WorldMatrix.HasValue) return true;

                return _Parent == null ? false : _Parent.TransformChainIsDirty;
            }
        }

        #endregion

        #region API

        private XFORM _GetWorldMatrix()
        {
            if (!TransformChainIsDirty) return _WorldMatrix.Value;

            _WorldMatrix = _Parent == null ? _LocalMatrix : XFORM.Multiply(_LocalMatrix, _Parent.WorldMatrix);

            return _WorldMatrix.Value;
        }

        private void _SetWorldMatrix(XFORM xform)
        {
            if (_Parent == null) { LocalMatrix = xform; return; }

            XFORM.Invert(_Parent._GetWorldMatrix(), out XFORM ipwm);

            LocalMatrix = XFORM.Multiply(xform, ipwm);
        }

        public void SetPoseTransform() { SetAnimationFrame(-1, 0); }

        public void SetAnimationFrame(int trackLogicalIndex, float time)
        {
            this.MorphWeights = _Template.GetMorphWeights(trackLogicalIndex, time);
            this.LocalMatrix = _Template.GetLocalMatrix(trackLogicalIndex, time);
        }

        public void SetAnimationFrame(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {
            this.MorphWeights = _Template.GetMorphWeights(track, time, weight);
            this.LocalMatrix = _Template.GetLocalMatrix(track, time, weight);
        }

        #endregion
    }

    /// <summary>
    /// Represents a specific and independent state of a <see cref="SceneTemplate"/>.
    /// </summary>
    public class SceneInstance
    {
        #region lifecycle

        internal SceneInstance(NodeTemplate[] nodeTemplates, DrawableReference[] drawables, Collections.NamedList<float> tracks)
        {
            _AnimationTracks = tracks;

            _NodeTemplates = nodeTemplates;
            _NodeInstances = new NodeInstance[_NodeTemplates.Length];

            for (var i = 0; i < _NodeInstances.Length; ++i)
            {
                var n = _NodeTemplates[i];
                var pidx = _NodeTemplates[i].ParentIndex;

                if (pidx >= i) throw new ArgumentException("invalid parent index", nameof(nodeTemplates));

                var p = pidx < 0 ? null : _NodeInstances[pidx];

                _NodeInstances[i] = new NodeInstance(n, p);
            }

            _DrawableReferences = drawables;
            _DrawableTransforms = new IGeometryTransform[_DrawableReferences.Length];

            for (int i = 0; i < _DrawableTransforms.Length; ++i)
            {
                _DrawableTransforms[i] = _DrawableReferences[i].CreateGeometryTransform();
            }
        }

        #endregion

        #region data

        private readonly NodeTemplate[] _NodeTemplates;
        private readonly NodeInstance[] _NodeInstances;

        private readonly DrawableReference[] _DrawableReferences;
        private readonly IGeometryTransform[] _DrawableTransforms;

        private readonly Collections.NamedList<float> _AnimationTracks;

        #endregion

        #region properties

        public IReadOnlyList<NodeInstance> LogicalNodes => _NodeInstances;

        public IEnumerable<NodeInstance> VisualNodes => _NodeInstances.Where(item => item.VisualParent == null);

        public IEnumerable<String> AnimationTracks => _AnimationTracks.Names;

        /// <summary>
        /// Gets the number of drawable references.
        /// </summary>
        public int DrawableReferencesCount => _DrawableTransforms.Length;

        /// <summary>
        /// Gets a collection of drawable references, where Item1 is the logical Index
        /// of a <see cref="Schema2.Mesh"/> in <see cref="Schema2.ModelRoot.LogicalMeshes"/>
        /// and Item2 is an <see cref="IGeometryTransform"/> that can be used to transform
        /// the <see cref="Schema2.Mesh"/> into world space.
        /// </summary>
        public IEnumerable<(int, IGeometryTransform)> DrawableReferences
        {
            get
            {
                for (int i = 0; i < _DrawableTransforms.Length; ++i)
                {
                    yield return GetDrawableReference(i);
                }
            }
        }

        #endregion

        #region API

        public void SetPoseTransforms()
        {
            foreach (var n in _NodeInstances) n.SetPoseTransform();
        }

        public float GetAnimationDuration(int trackLogicalIndex)
        {
            if (trackLogicalIndex < 0) return 0;
            if (trackLogicalIndex >= _AnimationTracks.Count) return 0;

            return _AnimationTracks[trackLogicalIndex];
        }

        public float GetAnimationDuration(string trackName)
        {
            return GetAnimationDuration(_AnimationTracks.IndexOf(trackName));
        }

        public void SetAnimationFrame(int trackLogicalIndex, float time)
        {
            foreach (var n in _NodeInstances) n.SetAnimationFrame(trackLogicalIndex, time);
        }

        public void SetAnimationFrame(string trackName, float time)
        {
            SetAnimationFrame(_AnimationTracks.IndexOf(trackName), time);
        }

        public void SetAnimationFrame(params (int, float, float)[] blended)
        {
            SetAnimationFrame(_NodeInstances, blended);
        }

        public static void SetAnimationFrame(IEnumerable<NodeInstance> nodes, params (int, float, float)[] blended)
        {
            Guard.NotNull(nodes, nameof(nodes));

            Span<int> tracks = stackalloc int[blended.Length];
            Span<float> times = stackalloc float[blended.Length];
            Span<float> weights = stackalloc float[blended.Length];

            float w = blended.Sum(item => item.Item3);

            w = w == 0 ? 1 : 1 / w;

            for (int i = 0; i < blended.Length; ++i)
            {
                tracks[i] = blended[i].Item1;
                times[i] = blended[i].Item2;
                weights[i] = blended[i].Item3 * w;
            }

            foreach (var n in nodes) n.SetAnimationFrame(tracks, times, weights);
        }

        /// <summary>
        /// Gets a drawable reference pair, where Item1 is the logical Index
        /// of a <see cref="Schema2.Mesh"/> in <see cref="Schema2.ModelRoot.LogicalMeshes"/>
        /// and Item2 is an <see cref="IGeometryTransform"/> that can be used to transform
        /// the <see cref="Schema2.Mesh"/> into world space.
        /// </summary>
        /// <param name="index">The index of the drawable reference, from 0 to <see cref="DrawableReferencesCount"/></param>
        /// <returns>A drawable reference</returns>
        public (int, IGeometryTransform) GetDrawableReference(int index)
        {
            var dref = _DrawableReferences[index];

            dref.UpdateGeometryTransform(_DrawableTransforms[index], _NodeInstances);

            return (dref.LogicalMeshIndex, _DrawableTransforms[index]);
        }

        #endregion
    }
}
