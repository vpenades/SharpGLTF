using System;
using System.Collections.Generic;
using System.Numerics;

using TRANSFORM = SharpGLTF.Transforms.AffineTransform;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Defines a hierarchical transform node of a scene graph tree.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("[{LogicalNodeIndex}] {Name}")]
    class NodeTemplate
    {
        #region lifecycle

        internal NodeTemplate(Schema2.Node srcNode, int parentIdx, int[] childIndices, RuntimeOptions options)
        {
            _LogicalSourceIndex = srcNode.LogicalIndex;

            _ParentIndex = parentIdx;
            _ChildIndices = childIndices;

            Name = srcNode.Name;
            Extras = RuntimeOptions.ConvertExtras(srcNode, options);

            _LocalTransform = srcNode.LocalTransform;

            if (_LocalTransform.TryDecompose(out TRANSFORM lxform))
            {
                _Scale = new AnimatableProperty<Vector3>(lxform.Scale);
                _Rotation = new AnimatableProperty<Quaternion>(lxform.Rotation);
                _Translation = new AnimatableProperty<Vector3>(lxform.Translation);
            }

            var mw = Transforms.SparseWeight8.Create(srcNode.MorphWeights);
            _Morphing = new AnimatableProperty<Transforms.SparseWeight8>(mw);

            var isolateMemory = options?.IsolateMemory ?? false;

            foreach (var anim in srcNode.LogicalParent.LogicalAnimations)
            {
                var index = anim.LogicalIndex;

                var curves = srcNode.GetCurveSamplers(anim);

                _Scale.SetCurve(index, curves.Scale?.CreateCurveSampler(isolateMemory));
                _Rotation.SetCurve(index, curves.Rotation?.CreateCurveSampler(isolateMemory));
                _Translation.SetCurve(index, curves.Translation?.CreateCurveSampler(isolateMemory));
                _Morphing.SetCurve(index, curves.GetMorphingSampler<Transforms.SparseWeight8>()?.CreateCurveSampler(isolateMemory));
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

        private readonly TRANSFORM _LocalTransform;

        private readonly bool _UseAnimatedTransforms;
        private readonly AnimatableProperty<Vector3> _Scale;
        private readonly AnimatableProperty<Quaternion> _Rotation;
        private readonly AnimatableProperty<Vector3> _Translation;
        private readonly AnimatableProperty<Transforms.SparseWeight8> _Morphing;

        #endregion

        #region properties

        public string Name { get; set; }

        public Object Extras { get; set; }

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

        public Matrix4x4 LocalMatrix => _LocalTransform.Matrix;

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

        public TRANSFORM GetLocalTransform(int trackLogicalIndex, float time)
        {
            if (!_UseAnimatedTransforms || trackLogicalIndex < 0) return _LocalTransform;

            var s = _Scale?.GetValueAt(trackLogicalIndex, time);
            var r = _Rotation?.GetValueAt(trackLogicalIndex, time);
            var t = _Translation?.GetValueAt(trackLogicalIndex, time);

            return new TRANSFORM(s, r, t);
        }

        public TRANSFORM GetLocalTransform(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {
            if (!_UseAnimatedTransforms) return _LocalTransform;

            Span<TRANSFORM> xforms = stackalloc TRANSFORM[track.Length];

            for (int i = 0; i < xforms.Length; ++i)
            {
                xforms[i] = GetLocalTransform(track[i], time[i]);
            }

            return TRANSFORM.Blend(xforms, weight);
        }

        public Matrix4x4 GetLocalMatrix(int trackLogicalIndex, float time)
        {
            if (!_UseAnimatedTransforms || trackLogicalIndex < 0) return _LocalTransform.Matrix;

            return GetLocalTransform(trackLogicalIndex, time).Matrix;
        }

        public Matrix4x4 GetLocalMatrix(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {
            if (!_UseAnimatedTransforms) return _LocalTransform.Matrix;

            return GetLocalTransform(track, time, weight).Matrix;
        }

        #endregion
    }
}
