using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Collections;
using SharpGLTF.Transforms;
using SharpGLTF.Animations;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Animation[{LogicalIndex}] {Name}")]
    public sealed partial class Animation
    {
        #region lifecycle

        internal Animation()
        {
            _channels = new ChildrenCollection<AnimationChannel, Animation>(this);
            _samplers = new ChildrenCollection<AnimationSampler, Animation>(this);
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Animation"/> at <see cref="ModelRoot.LogicalAnimations"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalAnimations.IndexOfReference(this);

        internal IReadOnlyList<AnimationSampler> _Samplers => _samplers;

        internal IReadOnlyList<AnimationChannel> _Channels => _channels;

        public float Duration => _samplers.Select(item => item.Duration).Max();

        #endregion

        #region API

        /// <inheritdoc />
        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_samplers).Concat(_channels);
        }

        private AnimationSampler _CreateSampler(AnimationInterpolationMode interpolation)
        {
            var sampler = new AnimationSampler(interpolation);

            _samplers.Add(sampler);

            return sampler;
        }

        /// <remarks>
        /// There can only be one <see cref="AnimationChannel"/> for every node and path
        /// </remarks>
        private AnimationChannel _UseChannel(Node node, PropertyPath path)
        {
            Guard.MustShareLogicalParent(this, node, nameof(node));

            var channel = _channels.FirstOrDefault(item => item.TargetNode == node && item.TargetNodePath == path);
            if (channel != null) return channel;

            channel = new AnimationChannel(node, path);

            _channels.Add(channel);

            return channel;
        }

        public void CreateScaleChannel(Node node, IReadOnlyDictionary<Single, Vector3> keyframes, bool linear = true)
        {
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.scale)
                .SetSampler(sampler);
        }

        public void CreateScaleChannel(Node node, IReadOnlyDictionary<Single, (Vector3, Vector3, Vector3)> keyframes)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.scale)
                .SetSampler(sampler);
        }

        public void CreateRotationChannel(Node node, IReadOnlyDictionary<Single, Quaternion> keyframes, bool linear = true)
        {
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.rotation)
                .SetSampler(sampler);
        }

        public void CreateRotationChannel(Node node, IReadOnlyDictionary<Single, (Quaternion, Quaternion, Quaternion)> keyframes)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.rotation)
                .SetSampler(sampler);
        }

        public void CreateTranslationChannel(Node node, IReadOnlyDictionary<Single, Vector3> keyframes, bool linear = true)
        {
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.translation)
                .SetSampler(sampler);
        }

        public void CreateTranslationChannel(Node node, IReadOnlyDictionary<Single, (Vector3, Vector3, Vector3)> keyframes)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.translation)
                .SetSampler(sampler);
        }

        public void CreateMorphChannel(Node node, IReadOnlyDictionary<Single, SparseWeight8> keyframes, int morphCount, bool linear = true)
        {
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes, morphCount);

            this._UseChannel(node, PropertyPath.weights)
                .SetSampler(sampler);
        }

        public void CreateMorphChannel(Node node, IReadOnlyDictionary<Single, (SparseWeight8, SparseWeight8, SparseWeight8)> keyframes, int morphCount)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetKeys(keyframes, morphCount);

            this._UseChannel(node, PropertyPath.weights)
                .SetSampler(sampler);
        }

        private AnimationChannel FindChannel(Node node, PropertyPath path)
        {
            return _channels.FirstOrDefault(item => item.TargetNode == node && item.TargetNodePath == path);
        }

        public IAnimationSampler<Vector3> FindScaleSampler(Node node) { return FindChannel(node, PropertyPath.scale)?.Sampler; }

        public IAnimationSampler<Quaternion> FindRotationSampler(Node node) { return FindChannel(node, PropertyPath.rotation)?.Sampler; }

        public IAnimationSampler<Vector3> FindTranslationSampler(Node node) { return FindChannel(node, PropertyPath.translation)?.Sampler; }

        public IAnimationSampler<Single[]> FindMorphSampler(Node node) { return FindChannel(node, PropertyPath.weights)?.Sampler; }

        public IAnimationSampler<SparseWeight8> FindSparseMorphSampler(Node node) { return FindChannel(node, PropertyPath.weights)?.Sampler; }

        public AffineTransform GetLocalTransform(Node node, float time)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));

            var xform = node.LocalTransform;

            var sfunc = FindScaleSampler(node)?.CreateCurveSampler();
            var rfunc = FindRotationSampler(node)?.CreateCurveSampler();
            var tfunc = FindTranslationSampler(node)?.CreateCurveSampler();

            if (sfunc != null) xform.Scale = sfunc.GetPoint(time);
            if (rfunc != null) xform.Rotation = rfunc.GetPoint(time);
            if (tfunc != null) xform.Translation = tfunc.GetPoint(time);

            return xform;
        }

        public IReadOnlyList<float> GetMorphWeights(Node node, float time)
        {
            Guard.NotNull(node, nameof(node));

            var morphWeights = node.MorphWeights;
            if (morphWeights == null || morphWeights.Count == 0) return morphWeights;

            Guard.MustShareLogicalParent(this, node, nameof(node));

            var mfunc = FindMorphSampler(node)?.CreateCurveSampler();
            if (mfunc == null) return morphWeights;

            return mfunc.GetPoint(time);
        }

        public SparseWeight8 GetSparseMorphWeights(Node node, float time)
        {
            Guard.NotNull(node, nameof(node));

            var morphWeights = node.MorphWeights;
            if (morphWeights == null || morphWeights.Count == 0) return default;

            Guard.MustShareLogicalParent(this, node, nameof(node));

            var mfunc = FindSparseMorphSampler(node)?.CreateCurveSampler();
            if (mfunc == null) return default;

            return mfunc.GetPoint(time);
        }

        #endregion
    }

    sealed partial class AnimationChannelTarget
    {
        #region lifecycle

        internal AnimationChannelTarget() { }

        internal AnimationChannelTarget(Node targetNode, PropertyPath targetPath)
        {
            _node = targetNode.LogicalIndex;
            _path = targetPath;
        }

        #endregion

        #region data

        internal int? _NodeId => this._node;

        internal PropertyPath _NodePath => this._path;

        #endregion
    }

    sealed partial class AnimationChannel : IChildOf<Animation>
    {
        #region lifecycle

        internal AnimationChannel() { }

        internal AnimationChannel(Node targetNode, PropertyPath targetPath)
        {
            _target = new AnimationChannelTarget(targetNode, targetPath);
            _sampler = -1;
        }

        internal void SetSampler(AnimationSampler sampler)
        {
            Guard.NotNull(sampler, nameof(sampler));
            Guard.IsTrue(this.LogicalParent == sampler.LogicalParent, nameof(sampler));

            _sampler = sampler.LogicalIndex;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the <see cref="Animation"/> instance that owns this object.
        /// </summary>
        public Animation LogicalParent { get; private set; }

        void IChildOf<Animation>._SetLogicalParent(Animation parent) { LogicalParent = parent; }

        /// <summary>
        /// Gets the <see cref="AnimationSampler"/> instance used by this <see cref="AnimationChannel"/>.
        /// </summary>
        public AnimationSampler Sampler => this.LogicalParent._Samplers[this._sampler];

        public Node TargetNode
        {
            get
            {
                var idx = this._target?._NodeId ?? -1;
                if (idx < 0) return null;
                return this.LogicalParent.LogicalParent.LogicalNodes[idx];
            }
        }

        public PropertyPath TargetNodePath => this._target?._NodePath ?? PropertyPath.translation;

        #endregion
    }

    sealed partial class AnimationSampler : IChildOf<Animation>,
        IAnimationSampler<Vector3>,
        IAnimationSampler<Quaternion>,
        IAnimationSampler<SparseWeight8>,
        IAnimationSampler<Single[]>
    {
        #region lifecycle

        internal AnimationSampler() { }

        internal AnimationSampler(AnimationInterpolationMode interpolation)
        {
            _interpolation = interpolation.AsNullable(_interpolationDefault);
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the <see cref="Animation"/> instance that owns this <see cref="AnimationSampler"/> instance.
        /// </summary>
        public Animation LogicalParent { get; private set; }

        /// <summary>
        /// Gets the zero-based index of this <see cref="AnimationSampler"/> at <see cref="Animation._samplers"/>.
        /// </summary>
        public int LogicalIndex => LogicalParent._Samplers.IndexOfReference(this);

        void IChildOf<Animation>._SetLogicalParent(Animation parent) { LogicalParent = parent; }

        public AnimationInterpolationMode InterpolationMode
        {
            get => _interpolation.AsValue(_interpolationDefault);
            set => _interpolation = value.AsNullable(_interpolationDefault);
        }

        public Accessor Input => this.LogicalParent.LogicalParent.LogicalAccessors[this._input];

        public Accessor Output => this.LogicalParent.LogicalParent.LogicalAccessors[this._output];

        public float Duration { get { var keys = Input.AsScalarArray(); return keys.Count == 0 ? 0 : keys[keys.Count - 1]; } }

        #endregion

        #region API

        private Accessor _CreateInputAccessor(IReadOnlyList<Single> input)
        {
            var root = LogicalParent.LogicalParent;

            var buffer = root.UseBufferView(new Byte[input.Count * 4]);
            var accessor = root.CreateAccessor("Animation.Input");

            accessor.SetData(buffer, 0, input.Count, DimensionType.SCALAR, EncodingType.FLOAT, false);

            Memory.EncodedArrayUtils._CopyTo(input, accessor.AsScalarArray());

            accessor.UpdateBounds();

            return accessor;
        }

        private Accessor _CreateOutputAccessor(IReadOnlyList<Vector3> output)
        {
            var root = LogicalParent.LogicalParent;

            var buffer = root.UseBufferView(new Byte[output.Count * 4 * 3]);

            System.Diagnostics.Debug.Assert(buffer.ByteStride == 0);

            var accessor = root.CreateAccessor("Animation.Output");

            accessor.SetData(buffer, 0, output.Count, DimensionType.VEC3, EncodingType.FLOAT, false);

            Memory.EncodedArrayUtils._CopyTo(output, accessor.AsVector3Array());

            accessor.UpdateBounds();

            return accessor;
        }

        private Accessor _CreateOutputAccessor(IReadOnlyList<Quaternion> output)
        {
            var root = LogicalParent.LogicalParent;

            var buffer = root.UseBufferView(new Byte[output.Count * 4 * 4]);
            var accessor = root.CreateAccessor("Animation.Output");

            accessor.SetData(buffer, 0, output.Count, DimensionType.VEC4, EncodingType.FLOAT, false);

            Memory.EncodedArrayUtils._CopyTo(output, accessor.AsQuaternionArray());

            accessor.UpdateBounds();

            return accessor;
        }

        private Accessor _CreateOutputAccessor(IReadOnlyList<SparseWeight8> output, int expandedCount)
        {
            var root = LogicalParent.LogicalParent;

            var buffer = root.UseBufferView(new Byte[output.Count * 4 * expandedCount]);
            var accessor = root.CreateAccessor("Animation.Output");

            accessor.SetData(buffer, 0, output.Count * expandedCount, DimensionType.SCALAR, EncodingType.FLOAT, false);

            var dst = accessor.AsScalarArray();

            for (int i = 0; i < output.Count; ++i)
            {
                var src = output[i];

                for (int j = 0; j < expandedCount; ++j)
                {
                    dst[(i * expandedCount) + j] = src[j];
                }
            }

            accessor.UpdateBounds();

            return accessor;
        }

        private static (Single[], TValue[]) _Split<TValue>(IReadOnlyDictionary<Single, TValue> keyframes)
        {
            var sorted = keyframes
                .OrderBy(item => item.Key)
                .ToList();

            var keys = new Single[sorted.Count];
            var vals = new TValue[sorted.Count];

            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i] = sorted[i].Key;
                vals[i] = sorted[i].Value;
            }

            return (keys, vals);
        }

        private static (Single[], TValue[]) _Split<TValue>(IReadOnlyDictionary<Single, (TValue, TValue, TValue)> keyframes)
        {
            var sorted = keyframes
                .OrderBy(item => item.Key)
                .ToList();

            var keys = new Single[sorted.Count];
            var vals = new TValue[sorted.Count * 3];

            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i] = sorted[i].Key;
                vals[(i * 3) + 0] = sorted[i].Value.Item1;
                vals[(i * 3) + 1] = sorted[i].Value.Item2;
                vals[(i * 3) + 2] = sorted[i].Value.Item3;
            }

            return (keys, vals);
        }

        internal void SetKeys(IReadOnlyDictionary<Single, Vector3> keyframes)
        {
            var kv = _Split(keyframes);
            _input = this._CreateInputAccessor(kv.Item1).LogicalIndex;
            _output = this._CreateOutputAccessor(kv.Item2).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, Quaternion> keyframes)
        {
            var kv = _Split(keyframes);
            _input = this._CreateInputAccessor(kv.Item1).LogicalIndex;
            _output = this._CreateOutputAccessor(kv.Item2).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, SparseWeight8> keyframes, int expandedCount)
        {
            var kv = _Split(keyframes);
            _input = this._CreateInputAccessor(kv.Item1).LogicalIndex;
            _output = this._CreateOutputAccessor(kv.Item2, expandedCount).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, (Vector3, Vector3, Vector3)> keyframes)
        {
            var kv = _Split(keyframes);

            kv.Item2[0] = Vector3.Zero;
            kv.Item2[kv.Item2.Length - 1] = Vector3.Zero;

            _input = this._CreateInputAccessor(kv.Item1).LogicalIndex;
            _output = this._CreateOutputAccessor(kv.Item2).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, (Quaternion, Quaternion, Quaternion)> keyframes)
        {
            var kv = _Split(keyframes);

            kv.Item2[0] = default(Quaternion);
            kv.Item2[kv.Item2.Length - 1] = default(Quaternion);

            _input = this._CreateInputAccessor(kv.Item1).LogicalIndex;
            _output = this._CreateOutputAccessor(kv.Item2).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, (SparseWeight8, SparseWeight8, SparseWeight8)> keyframes, int expandedCount)
        {
            var kv = _Split(keyframes);

            kv.Item2[0] = default(SparseWeight8);
            kv.Item2[kv.Item2.Length - 1] = default(SparseWeight8);

            _input = this._CreateInputAccessor(kv.Item1).LogicalIndex;
            _output = this._CreateOutputAccessor(kv.Item2, expandedCount).LogicalIndex;
        }

        IEnumerable<(Single, Vector3)> IAnimationSampler<Vector3>.GetLinearKeys(bool isolateMemory)
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsVector3Array();

            return keys
                .Zip(frames, (key, val) => (key, val))
                .Isolate(isolateMemory);
        }

        IEnumerable<(Single, Quaternion)> IAnimationSampler<Quaternion>.GetLinearKeys(bool isolateMemory)
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsQuaternionArray();

            return keys
                .Zip(frames, (key, val) => (key, val))
                .Isolate(isolateMemory);
        }

        IEnumerable<(Single, SparseWeight8)> IAnimationSampler<SparseWeight8>.GetLinearKeys(bool isolateMemory)
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / this.Input.Count;

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsMultiArray(dimensions);

            return keys
                .Zip(frames, (key, val) => (key, SparseWeight8.Create(val)))
                .Isolate(isolateMemory);
        }

        IEnumerable<(Single, Single[])> IAnimationSampler<Single[]>.GetLinearKeys(bool isolateMemory)
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / this.Input.Count;

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsMultiArray(dimensions);

            return keys
                .Zip(frames, (key, val) => (key, val))
                .Isolate(isolateMemory);
        }

        IEnumerable<(Single, (Vector3, Vector3, Vector3))> IAnimationSampler<Vector3>.GetCubicKeys(bool isolateMemory)
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByThree(this.Output.AsVector3Array());

            return keys
                .Zip(frames, (key, val) => (key, val))
                .Isolate(isolateMemory);
        }

        IEnumerable<(Single, (Quaternion, Quaternion, Quaternion))> IAnimationSampler<Quaternion>.GetCubicKeys(bool isolateMemory)
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByThree(this.Output.AsQuaternionArray());

            return keys
                .Zip(frames, (key, val) => (key, val))
                .Isolate(isolateMemory);
        }

        IEnumerable<(Single, (Single[], Single[], Single[]))> IAnimationSampler<Single[]>.GetCubicKeys(bool isolateMemory)
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / (this.Input.Count * 3);

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByThree(this.Output.AsMultiArray(dimensions));

            return keys
                .Zip(frames, (key, val) => (key, val))
                .Isolate(isolateMemory);
        }

        IEnumerable<(Single, (SparseWeight8, SparseWeight8, SparseWeight8))> IAnimationSampler<SparseWeight8>.GetCubicKeys(bool isolateMemory)
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / (this.Input.Count * 3);

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByThree(this.Output.AsMultiArray(dimensions));

            return keys
                .Zip(frames, (key, val) => (key, (SparseWeight8.Create(val.Item1), SparseWeight8.Create(val.Item2), SparseWeight8.Create(val.Item3))))
                .Isolate(isolateMemory);
        }

        private static IEnumerable<(T, T, T)> _GroupByThree<T>(IEnumerable<T> collection)
        {
            using (var ptr = collection.GetEnumerator())
            {
                while (true)
                {
                    if (!ptr.MoveNext()) break;
                    var a = ptr.Current;
                    if (!ptr.MoveNext()) break;
                    var b = ptr.Current;
                    if (!ptr.MoveNext()) break;
                    var c = ptr.Current;

                    yield return (a, b, c);
                }
            }
        }

        ICurveSampler<Vector3> IAnimationSampler<Vector3>.CreateCurveSampler(bool isolateMemory)
        {
            var xsampler = this as IAnimationSampler<Vector3>;

            switch (this.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP: return xsampler.GetLinearKeys(isolateMemory).CreateSampler(false);
                case AnimationInterpolationMode.LINEAR: return xsampler.GetLinearKeys(isolateMemory).CreateSampler();
                case AnimationInterpolationMode.CUBICSPLINE: return xsampler.GetCubicKeys(isolateMemory).CreateSampler();
            }

            throw new NotImplementedException();
        }

        ICurveSampler<Quaternion> IAnimationSampler<Quaternion>.CreateCurveSampler(bool isolateMemory)
        {
            var xsampler = this as IAnimationSampler<Quaternion>;

            switch (this.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP: return xsampler.GetLinearKeys(isolateMemory).CreateSampler(false);
                case AnimationInterpolationMode.LINEAR: return xsampler.GetLinearKeys(isolateMemory).CreateSampler();
                case AnimationInterpolationMode.CUBICSPLINE: return xsampler.GetCubicKeys(isolateMemory).CreateSampler();
            }

            throw new NotImplementedException();
        }

        ICurveSampler<Single[]> IAnimationSampler<Single[]>.CreateCurveSampler(bool isolateMemory)
        {
            var xsampler = this as IAnimationSampler<Single[]>;

            switch (this.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP: return xsampler.GetLinearKeys(isolateMemory).CreateSampler(false);
                case AnimationInterpolationMode.LINEAR: return xsampler.GetLinearKeys(isolateMemory).CreateSampler();
                case AnimationInterpolationMode.CUBICSPLINE: return xsampler.GetCubicKeys(isolateMemory).CreateSampler();
            }

            throw new NotImplementedException();
        }

        ICurveSampler<SparseWeight8> IAnimationSampler<SparseWeight8>.CreateCurveSampler(bool isolateMemory)
        {
            var xsampler = this as IAnimationSampler<SparseWeight8>;

            switch (this.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP: return xsampler.GetLinearKeys(isolateMemory).CreateSampler(false);
                case AnimationInterpolationMode.LINEAR: return xsampler.GetLinearKeys(isolateMemory).CreateSampler();
                case AnimationInterpolationMode.CUBICSPLINE: return xsampler.GetCubicKeys(isolateMemory).CreateSampler();
            }

            throw new NotImplementedException();
        }

        #endregion
    }

    public interface IAnimationSampler<T>
    {
        AnimationInterpolationMode InterpolationMode { get; }

        IEnumerable<(Single, T)> GetLinearKeys(bool isolateMemory = false);

        IEnumerable<(Single, (T, T, T))> GetCubicKeys(bool isolateMemory = false);

        ICurveSampler<T> CreateCurveSampler(bool isolateMemory = false);
    }

    public sealed partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Animation"/> instance and adds it to <see cref="ModelRoot.LogicalAnimations"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Animation"/> instance.</returns>
        public Animation CreateAnimation(string name = null)
        {
            var anim = new Animation
            {
                Name = name
            };

            _animations.Add(anim);

            return anim;
        }
    }
}