using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Defines a hierarchical transform node of a scene graph tree.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("[{LogicalNodeIndex}] {Name}")]
    class NodeTemplate
    {
        #region lifecycle

        internal NodeTemplate(Schema2.Node srcNode, int parentIdx, int[] childIndices, bool isolateMemory)
        {
            _LogicalSourceIndex = srcNode.LogicalIndex;

            _ParentIndex = parentIdx;
            _ChildIndices = childIndices;

            Name = srcNode.Name;

            _LocalMatrix = srcNode.LocalMatrix;
            _LocalTransform = srcNode.LocalTransform;

            _Scale = new AnimatableProperty<Vector3>(_LocalTransform.Scale);
            _Rotation = new AnimatableProperty<Quaternion>(_LocalTransform.Rotation);
            _Translation = new AnimatableProperty<Vector3>(_LocalTransform.Translation);

            var mw = Transforms.SparseWeight8.Create(srcNode.MorphWeights);
            _Morphing = new AnimatableProperty<Transforms.SparseWeight8>(mw);

            foreach (var anim in srcNode.LogicalParent.LogicalAnimations)
            {
                var index = anim.LogicalIndex;

                var curves = srcNode.GetCurveSamplers(anim);

                _Scale.SetCurve(index, curves.Scale?.CreateCurveSampler(isolateMemory));
                _Rotation.SetCurve(index, curves.Rotation?.CreateCurveSampler(isolateMemory));
                _Translation.SetCurve(index, curves.Translation?.CreateCurveSampler(isolateMemory));
                _Morphing.SetCurve(index, curves.MorphingSparse?.CreateCurveSampler(isolateMemory));
            }

            _UseAnimatedTransforms = _Scale.IsAnimated | _Rotation.IsAnimated | _Translation.IsAnimated;

            if (!_UseAnimatedTransforms)
            {
                _Scale = null;
                _Rotation = null;
                _Translation = null;
            }
        }

        #endregion

        #region data

        /// <summary>
        /// the index of this node within <see cref="SceneTemplate._Armature"/>
        /// </summary>
        private readonly int _LogicalSourceIndex;

        /// <summary>
        /// the index of the parent node within <see cref="SceneTemplate._Armature"/>
        /// </summary>
        private readonly int _ParentIndex;
        private readonly int[] _ChildIndices;

        private readonly Matrix4x4 _LocalMatrix;
        private readonly Transforms.AffineTransform _LocalTransform;

        private readonly bool _UseAnimatedTransforms;
        private readonly AnimatableProperty<Vector3> _Scale;
        private readonly AnimatableProperty<Quaternion> _Rotation;
        private readonly AnimatableProperty<Vector3> _Translation;
        private readonly AnimatableProperty<Transforms.SparseWeight8> _Morphing;

        #endregion

        #region properties

        public string Name { get; set; }

        /// <summary>
        /// Gets the index of the source <see cref="Schema2.Node"/> in <see cref="Schema2.ModelRoot.LogicalNodes"/>
        /// </summary>
        public int LogicalNodeIndex => _LogicalSourceIndex;

        /// <summary>
        /// Gets the index of the parent <see cref="NodeTemplate"/> in <see cref="SceneTemplate._Armature"/>
        /// </summary>
        public int ParentIndex => _ParentIndex;

        /// <summary>
        /// Gets the list of indices of the children <see cref="NodeTemplate"/> in <see cref="SceneTemplate._Armature"/>
        /// </summary>
        public IReadOnlyList<int> ChildIndices => _ChildIndices;

        public Matrix4x4 LocalMatrix => _LocalMatrix;

        #endregion

        #region API

        public Transforms.SparseWeight8 GetMorphWeights(int trackLogicalIndex, float time)
        {
            if (trackLogicalIndex < 0) return _Morphing.Value;

            return _Morphing.GetValueAt(trackLogicalIndex, time);
        }

        public Transforms.SparseWeight8 GetMorphWeights(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {
            if (!_Morphing.IsAnimated) return _Morphing.Value;

            Span<Transforms.SparseWeight8> xforms = stackalloc Transforms.SparseWeight8[track.Length];

            for (int i = 0; i < xforms.Length; ++i)
            {
                xforms[i] = GetMorphWeights(track[i], time[i]);
            }

            return Transforms.SparseWeight8.Blend(xforms, weight);
        }

        public Transforms.AffineTransform GetLocalTransform(int trackLogicalIndex, float time)
        {
            if (!_UseAnimatedTransforms || trackLogicalIndex < 0) return _LocalTransform;

            var s = _Scale?.GetValueAt(trackLogicalIndex, time);
            var r = _Rotation?.GetValueAt(trackLogicalIndex, time);
            var t = _Translation?.GetValueAt(trackLogicalIndex, time);

            return new Transforms.AffineTransform(s, r, t);
        }

        public Transforms.AffineTransform GetLocalTransform(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {
            if (!_UseAnimatedTransforms) return _LocalTransform;

            Span<Transforms.AffineTransform> xforms = stackalloc Transforms.AffineTransform[track.Length];

            for (int i = 0; i < xforms.Length; ++i)
            {
                xforms[i] = GetLocalTransform(track[i], time[i]);
            }

            return Transforms.AffineTransform.Blend(xforms, weight);
        }

        public Matrix4x4 GetLocalMatrix(int trackLogicalIndex, float time)
        {
            if (!_UseAnimatedTransforms || trackLogicalIndex < 0) return _LocalMatrix;

            return GetLocalTransform(trackLogicalIndex, time).Matrix;
        }

        public Matrix4x4 GetLocalMatrix(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {
            if (!_UseAnimatedTransforms) return _LocalMatrix;

            return GetLocalTransform(track, time, weight).Matrix;
        }

        #endregion
    }
}
