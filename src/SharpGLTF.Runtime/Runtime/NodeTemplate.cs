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
            var isolateMemory = options?.IsolateMemory ?? false;

            _LogicalSourceIndex = srcNode.LogicalIndex;

            _ParentIndex = parentIdx;
            _ChildIndices = childIndices;

            Name = srcNode.Name;
            Extras = RuntimeOptions.ConvertExtras(srcNode, options);

            _IsVisible = srcNode.TryGetVisibility(out var visibility) ? visibility : null;
            _Visibility = new AnimatableProperty<bool>(_IsVisible ?? true);

            _LocalTransform = srcNode.LocalTransform;
            _LocalTransformAnimation = new _NodeTemplateTransforms(srcNode);            

            var mw = Transforms.SparseWeight8.Create(srcNode.MorphWeights);
            _Morphing = new AnimatableProperty<Transforms.SparseWeight8>(mw);            

            foreach (var anim in srcNode.LogicalParent.LogicalAnimations)
            {
                var index = anim.LogicalIndex;

                var curves = srcNode.GetCurveSamplers(anim);

                _LocalTransformAnimation.SetCurves(curves, index, isolateMemory);

                _Morphing.SetCurve(index, curves.GetMorphingSampler<Transforms.SparseWeight8>()?.CreateCurveSampler(isolateMemory));

                _Visibility.SetCurve(index, curves.Visibility?.CreateCurveSampler(isolateMemory));
            }            

            if (!_LocalTransformAnimation.IsAnimated) _LocalTransformAnimation = null;

            if (!_Visibility.IsAnimated) _Visibility = null;
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
        private readonly _NodeTemplateTransforms _LocalTransformAnimation;

        private readonly AnimatableProperty<Transforms.SparseWeight8> _Morphing;

        private readonly Boolean? _IsVisible;
        private readonly AnimatableProperty<Boolean> _Visibility;

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
            return _LocalTransformAnimation == null || trackLogicalIndex < 0
                ? _LocalTransform
                : _LocalTransformAnimation.GetTransform(trackLogicalIndex, time);
        }

        public TRANSFORM GetLocalTransform(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {
            if (_LocalTransformAnimation == null) return _LocalTransform;

            Span<TRANSFORM> xforms = stackalloc TRANSFORM[track.Length];

            for (int i = 0; i < xforms.Length; ++i)
            {
                xforms[i] = GetLocalTransform(track[i], time[i]);
            }

            return TRANSFORM.Blend(xforms, weight);
        }

        public Matrix4x4 GetLocalMatrix(int trackLogicalIndex, float time)
        {
            return _LocalTransformAnimation == null || trackLogicalIndex < 0
                ? _LocalTransform.Matrix
                : GetLocalTransform(trackLogicalIndex, time).Matrix;
        }

        public Matrix4x4 GetLocalMatrix(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {
            return _LocalTransformAnimation == null
                ? _LocalTransform.Matrix
                : GetLocalTransform(track, time, weight).Matrix;
        }

        public bool? GetVisibility(int trackLogicalIndex, float time)
        {
            return _Visibility == null
                ? _IsVisible
                : _Visibility.GetValueAt(trackLogicalIndex, time);
        }

        public bool? GetVisibility(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {
            if (_Visibility == null) return _IsVisible;

            float falseCount = 0;
            float trueCount = 0;

            for (int i = 0; i < track.Length; ++i)
            {
                var val = _Visibility.GetValueAt(track[i], time[i]);
                if (val) trueCount++;
                else falseCount++;
            }

            return trueCount >= falseCount;
        }

        #endregion
    }

    /// <summary>
    /// Contains the transform animation curves of the node.
    /// </summary>
    class _NodeTemplateTransforms
    {
        #region lifecycle

        public _NodeTemplateTransforms(Schema2.Node srcNode)
        {
            if (!srcNode.LocalTransform.TryDecompose(out TRANSFORM lxform))
            {
                lxform = Matrix4x4.Identity;
            }

            _Scale = new AnimatableProperty<Vector3>(lxform.Scale);
            _Rotation = new AnimatableProperty<Quaternion>(lxform.Rotation);
            _Translation = new AnimatableProperty<Vector3>(lxform.Translation);
        }

        #endregion

        #region data

        private readonly AnimatableProperty<Vector3> _Scale;
        private readonly AnimatableProperty<Quaternion> _Rotation;
        private readonly AnimatableProperty<Vector3> _Translation;

        #endregion

        #region properties

        public bool IsAnimated => _Scale.IsAnimated | _Rotation.IsAnimated | _Translation.IsAnimated;

        #endregion

        #region API

        public void SetCurves(Schema2.NodeCurveSamplers curves, int index, bool isolateMemory)
        {
            _Scale.SetCurve(index, curves.Scale?.CreateCurveSampler(isolateMemory));
            _Rotation.SetCurve(index, curves.Rotation?.CreateCurveSampler(isolateMemory));
            _Translation.SetCurve(index, curves.Translation?.CreateCurveSampler(isolateMemory));
        }

        public TRANSFORM GetTransform(int trackLogicalIndex, float time)
        {
            var s = _Scale?.GetValueAt(trackLogicalIndex, time);
            var r = _Rotation?.GetValueAt(trackLogicalIndex, time);
            var t = _Translation?.GetValueAt(trackLogicalIndex, time);

            return new TRANSFORM(s, r, t);
        }

        #endregion
    }
}
