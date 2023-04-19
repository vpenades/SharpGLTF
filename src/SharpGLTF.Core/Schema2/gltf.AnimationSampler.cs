using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Animations;
using SharpGLTF.Collections;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;

using ROLIST = System.Collections.Generic.IReadOnlyList<float>;
using SEGMENT = System.ArraySegment<float>;
using SPARSE8 = SharpGLTF.Transforms.SparseWeight8;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Represents an interface to a curve made of time-value points.
    /// </summary>
    /// <typeparam name="T">The curve point value type.</typeparam>
    public interface IAnimationSampler<T>
    {
        /// <summary>
        /// Gets a value indicating the interpolation being used.
        /// If the value is <see cref="AnimationInterpolationMode.STEP"/> or <see cref="AnimationInterpolationMode.LINEAR"/>, <see cref="GetLinearKeys"/> should be used.
        /// If the value is <see cref="AnimationInterpolationMode.CUBICSPLINE"/>, <see cref="GetCubicKeys"/> should be used.
        /// </summary>
        AnimationInterpolationMode InterpolationMode { get; }

        /// <summary>
        /// Gets the linear animation entries for <see cref="AnimationInterpolationMode.STEP"/> and <see cref="AnimationInterpolationMode.LINEAR"/> modes.
        /// </summary>
        /// <returns>A sequence of Time-Value keys.</returns>
        IEnumerable<(Single Key, T Value)> GetLinearKeys();

        /// <summary>
        /// Gets the cubic animation entries fot <see cref="AnimationInterpolationMode.CUBICSPLINE"/> mode.
        /// </summary>
        /// <returns>A sequence of Time-(TangentIn,Value,TangentOut) keys.</returns>
        IEnumerable<(Single Key, (T TangentIn, T Value, T TangentOut) Value)> GetCubicKeys();

        /// <summary>
        /// Creates an interpolation sampler that can be used to query the value of the curve at any time.
        /// </summary>
        /// <param name="isolateMemory">
        /// If true, this call will do an internal copy of the curve data,
        /// so it will not reference the source date in the original document.
        /// </param>
        /// <returns>An object that can be used to sample the curve at any time.</returns>
        /// <remarks>
        /// When <paramref name="isolateMemory"/> is true, it also arranges the data so it's much
        /// faster to query.
        /// </remarks>
        ICurveSampler<T> CreateCurveSampler(bool isolateMemory = false);
    }

    /// <remarks>
    /// This class can be casted to these interfaces:
    /// <list type="table">
    /// <item><see cref="IAnimationSampler{T}"/> with T: <see cref="Vector3"/> for Scale and Translation keys.</item>
    /// <item><see cref="IAnimationSampler{T}"/> with T: <see cref="Quaternion"/> for Rotation keys.</item>
    /// <item><see cref="IAnimationSampler{T}"/> with T: <see cref="SPARSE8"/> for Morph targets (limited to 8 weights).</item>
    /// <item><see cref="IAnimationSampler{T}"/> with T: <see cref="Single"/>[] for Morph targets (unlimited weights).</item>
    /// </list>
    /// </remarks>
    sealed partial class AnimationSampler :
        IChildOfList<Animation>,
        IAnimationSampler<Vector3>,
        IAnimationSampler<Quaternion>,
        IAnimationSampler<SPARSE8>,
        IAnimationSampler<SEGMENT>,
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
        public int LogicalIndex { get; private set; } = -1;

        void IChildOfList<Animation>.SetLogicalParent(Animation parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        /// <inheritdoc/>
        public AnimationInterpolationMode InterpolationMode
        {
            get => _interpolation.AsValue(_interpolationDefault);
            set => _interpolation = value.AsNullable(_interpolationDefault);
        }

        public Accessor Input => this.LogicalParent.LogicalParent.LogicalAccessors[this._input];

        public Accessor Output => this.LogicalParent.LogicalParent.LogicalAccessors[this._output];

        public float Duration
        {
            get
            {
                var keys = Input.AsScalarArray();
                return keys.Count == 0 ? 0 : keys[keys.Count - 1];
            }
        }

        #endregion

        #region API

        private Accessor _CreateInputAccessor(IReadOnlyList<Single> input)
        {
            Guard.NotNull(input, nameof(input));
            Guard.MustBeGreaterThan(input.Count, 0, nameof(input.Count));

            var root = LogicalParent.LogicalParent;

            var buffer = root.CreateBufferView(input.Count * 4);
            var accessor = root.CreateAccessor("Animation.Input");

            accessor.SetData(buffer, 0, input.Count, DimensionType.SCALAR, EncodingType.FLOAT, false);

            Memory.EncodedArrayUtils._CopyTo(input, accessor.AsScalarArray());

            accessor.UpdateBounds();

            return accessor;
        }

        private Accessor _CreateOutputAccessor(IReadOnlyList<Vector3> output)
        {
            Guard.NotNull(output, nameof(output));
            Guard.MustBeGreaterThan(output.Count, 0, nameof(output.Count));

            var root = LogicalParent.LogicalParent;

            var buffer = root.CreateBufferView(output.Count * 4 * 3);

            System.Diagnostics.Debug.Assert(buffer.ByteStride == 0);

            var accessor = root.CreateAccessor("Animation.Output");

            accessor.SetData(buffer, 0, output.Count, DimensionType.VEC3, EncodingType.FLOAT, false);

            Memory.EncodedArrayUtils._CopyTo(output, accessor.AsVector3Array());

            accessor.UpdateBounds();

            return accessor;
        }

        private Accessor _CreateOutputAccessor(IReadOnlyList<Quaternion> output)
        {
            Guard.NotNull(output, nameof(output));
            Guard.MustBeGreaterThan(output.Count, 0, nameof(output.Count));

            var root = LogicalParent.LogicalParent;

            var buffer = root.CreateBufferView(output.Count * 4 * 4);
            var accessor = root.CreateAccessor("Animation.Output");

            accessor.SetData(buffer, 0, output.Count, DimensionType.VEC4, EncodingType.FLOAT, false);

            Memory.EncodedArrayUtils._CopyTo(output, accessor.AsQuaternionArray());

            accessor.UpdateBounds();

            return accessor;
        }

        private Accessor _CreateOutputAccessor(IReadOnlyList<SPARSE8> output, int itemsStride)
        {
            return _CreateOutputAccessor(output.Count, itemsStride, (y, x) => output[y][x]);
        }

        private Accessor _CreateOutputAccessor<T>(IReadOnlyList<T> output, int itemsStride)
            where T : ROLIST
        {
            System.Diagnostics.Debug.Assert(output.All(item => item.Count == itemsStride));

            float eval(int y, int x)
            {
                var row = output[y];
                return x < row.Count ? row[x] : 0;
            }

            return _CreateOutputAccessor(output.Count, itemsStride, eval);
        }

        private Accessor _CreateOutputAccessor(int itemCount, int itemsStride, Func<int, int, float> output)
        {
            Guard.NotNull(output, nameof(output));
            Guard.MustBeGreaterThan(itemCount, 0, nameof(itemCount));
            Guard.MustBeGreaterThan(itemsStride, 0, nameof(itemsStride));

            var root = LogicalParent.LogicalParent;

            var buffer = root.CreateBufferView(itemCount * 4 * itemsStride);
            var accessor = root.CreateAccessor("Animation.Output");

            accessor.SetData(buffer, 0, itemCount * itemsStride, DimensionType.SCALAR, EncodingType.FLOAT, false);

            var dst = accessor.AsScalarArray();

            for (int y = 0; y < itemCount; ++y)
            {
                for (int x = 0; x < itemsStride; ++x)
                {
                    dst[(y * itemsStride) + x] = output(y, x);
                }
            }

            accessor.UpdateBounds();

            return accessor;
        }

        private static (Single[] Keys, TValue[] Values) _Split<TValue>(IReadOnlyDictionary<Single, TValue> keyframes)
        {
            Guard.NotNull(keyframes, nameof(keyframes));

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

        private static (Single[] Keys, TValue[] Values) _Split<TValue>(IReadOnlyDictionary<Single, (TValue TangentIn, TValue Value, TValue TangentOut)> keyframes)
        {
            Guard.NotNull(keyframes, nameof(keyframes));

            var sorted = keyframes
                .OrderBy(item => item.Key)
                .ToList();

            var keys = new Single[sorted.Count];
            var vals = new TValue[sorted.Count * 3];

            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i] = sorted[i].Key;
                vals[(i * 3) + 0] = sorted[i].Value.TangentIn;
                vals[(i * 3) + 1] = sorted[i].Value.Value;
                vals[(i * 3) + 2] = sorted[i].Value.TangentOut;
            }

            return (keys, vals);
        }

        internal void SetKeys(IReadOnlyDictionary<Single, Vector3> keyframes)
        {
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var (keys, values) = _Split(keyframes);
            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, Quaternion> keyframes)
        {
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var (keys, values) = _Split(keyframes);
            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values).LogicalIndex;
        }

        internal void SetKeys<TWeights>(IReadOnlyDictionary<Single, TWeights> keyframes, int itemsStride)
            where TWeights : ROLIST
        {
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));
            GuardAll.MustBeEqualTo(keyframes.Values.Select(item => item.Count), itemsStride, nameof(keyframes));

            var (keys, values) = _Split(keyframes);
            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values, itemsStride).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, SPARSE8> keyframes, int itemsStride)
        {
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var (keys, values) = _Split(keyframes);
            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values, itemsStride).LogicalIndex;
        }

        internal void SetCubicKeys(IReadOnlyDictionary<Single, (Vector3 TangentIn, Vector3 Value, Vector3 TangentOut)> keyframes)
        {
            Guard.NotNull(keyframes, nameof(keyframes));
            Guard.MustBeGreaterThan(keyframes.Count, 0, nameof(keyframes.Count));

            // splits the dictionary into separated input/output collections, also, the output will be flattened to plain Vector3 values.
            var (keys, values) = _Split(keyframes);
            System.Diagnostics.Debug.Assert(keys.Length * 3 == values.Length, "keys and values must have 1 to 3 ratio");

            // fix for first incoming tangent and last outgoing tangent
            // this might not be true for a looped animation, where first and last might be the same
            values[0] = Vector3.Zero;
            values[values.Length - 1] = Vector3.Zero;

            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values).LogicalIndex;
        }

        internal void SetCubicKeys(IReadOnlyDictionary<Single, (Quaternion TangentIn, Quaternion Value, Quaternion TangentOut)> keyframes)
        {
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            // splits the dictionary into separated input/output collections, also, the output will be flattened to plain Vector3 values.
            var (keys, values) = _Split(keyframes);
            System.Diagnostics.Debug.Assert(keys.Length * 3 == values.Length, "keys and values must have 1 to 3 ratio");

            // fix for first incoming tangent and last outgoing tangent
            // this might not be true for a looped animation, where first and last might be the same
            values[0] = default;
            values[values.Length - 1] = default;

            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values).LogicalIndex;
        }

        internal void SetCubicKeys<TWeights>(IReadOnlyDictionary<Single, (TWeights TangentIn, TWeights Value, TWeights TangentOut)> keyframes, int expandedCount)
            where TWeights : ROLIST
        {
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));
            Guard.MustBeGreaterThan(expandedCount, 0, nameof(expandedCount));

            // splits the dictionary into separated input/output collections, also, the output will be flattened to plain Vector3 values.
            var (keys, values) = _Split(keyframes);
            System.Diagnostics.Debug.Assert(keys.Length * 3 == values.Length, "keys and values must have 1 to 3 ratio");

            // fix for first incoming tangent and last outgoing tangent
            // this might not be true for a looped animation, where first and last might be the same

            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values, expandedCount).LogicalIndex;
        }

        internal void SetCubicKeys(IReadOnlyDictionary<Single, (SPARSE8 TangentIn, SPARSE8 Value, SPARSE8 TangentOut)> keyframes, int expandedCount)
        {
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));
            Guard.MustBeGreaterThan(expandedCount, 0, nameof(expandedCount));

            // splits the dictionary into separated input/output collections, also, the output will be flattened to plain Vector3 values.
            var (keys, values) = _Split(keyframes);
            System.Diagnostics.Debug.Assert(keys.Length * 3 == values.Length, "keys and values must have 1 to 3 ratio");

            // fix for first incoming tangent and last outgoing tangent
            // this might not be true for a looped animation, where first and last might be the same
            values[0] = default;
            values[values.Length - 1] = default;

            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values, expandedCount).LogicalIndex;
        }

        /// <inheritdoc/>
        IEnumerable<(Single, Vector3)> IAnimationSampler<Vector3>.GetLinearKeys()
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsVector3Array();

            return keys.Zip(frames, (key, val) => (key, val));
        }

        /// <inheritdoc/>
        IEnumerable<(Single, Quaternion)> IAnimationSampler<Quaternion>.GetLinearKeys()
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsQuaternionArray();

            return keys.Zip(frames, (key, val) => (key, val));
        }

        /// <inheritdoc/>
        IEnumerable<(Single, SPARSE8)> IAnimationSampler<SPARSE8>.GetLinearKeys()
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / this.Input.Count;

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsMultiArray(dimensions);

            return keys.Zip(frames, (key, val) => (key, SparseWeight8.Create(val)));
        }

        /// <inheritdoc/>
        IEnumerable<(Single, SEGMENT)> IAnimationSampler<SEGMENT>.GetLinearKeys()
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / this.Input.Count;

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsMultiArray(dimensions);

            return keys.Zip(frames, (key, val) => (key, new ArraySegment<float>(val)));
        }

        /// <inheritdoc/>
        IEnumerable<(Single, Single[])> IAnimationSampler<Single[]>.GetLinearKeys()
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / this.Input.Count;

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsMultiArray(dimensions);

            return keys.Zip(frames, (key, val) => (key, val));
        }

        /// <inheritdoc/>
        IEnumerable<(Single, (Vector3, Vector3, Vector3))> IAnimationSampler<Vector3>.GetCubicKeys()
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByTangentValueTangent(this.Output.AsVector3Array());

            return keys.Zip(frames, (key, val) => (key, val));
        }

        /// <inheritdoc/>
        IEnumerable<(Single, (Quaternion, Quaternion, Quaternion))> IAnimationSampler<Quaternion>.GetCubicKeys()
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByTangentValueTangent(this.Output.AsQuaternionArray());

            return keys.Zip(frames, (key, val) => (key, val));
        }

        /// <inheritdoc/>
        IEnumerable<(Single, (Single[], Single[], Single[]))> IAnimationSampler<Single[]>.GetCubicKeys()
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / (this.Input.Count * 3);

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByTangentValueTangent(this.Output.AsMultiArray(dimensions));

            return keys.Zip(frames, (key, val) => (key, val));
        }

        IEnumerable<(Single, (SEGMENT, SEGMENT, SEGMENT))> IAnimationSampler<SEGMENT>.GetCubicKeys()
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / (this.Input.Count * 3);

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByTangentValueTangent(this.Output.AsMultiArray(dimensions));

            return keys.Zip(frames, (key, val) => (key, (new SEGMENT(val.TangentIn), new SEGMENT(val.Value), new SEGMENT(val.TangentOut))));
        }

        /// <inheritdoc/>
        IEnumerable<(Single, (SPARSE8, SPARSE8, SPARSE8))> IAnimationSampler<SPARSE8>.GetCubicKeys()
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / (this.Input.Count * 3);

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByTangentValueTangent(this.Output.AsMultiArray(dimensions));

            return keys.Zip(frames, (key, val) => (key, SPARSE8.AsTuple(val.TangentIn, val.Value, val.TangentOut)) );
        }

        /// <inheritdoc/>
        ICurveSampler<Vector3> IAnimationSampler<Vector3>.CreateCurveSampler(bool isolateMemory)
        {
            var xsampler = this as IAnimationSampler<Vector3>;

            switch (this.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP: return xsampler.GetLinearKeys().CreateSampler(false, isolateMemory);
                case AnimationInterpolationMode.LINEAR: return xsampler.GetLinearKeys().CreateSampler(true, isolateMemory);
                case AnimationInterpolationMode.CUBICSPLINE: return xsampler.GetCubicKeys().CreateSampler(isolateMemory);
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        ICurveSampler<Quaternion> IAnimationSampler<Quaternion>.CreateCurveSampler(bool isolateMemory)
        {
            var xsampler = this as IAnimationSampler<Quaternion>;

            switch (this.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP: return xsampler.GetLinearKeys().CreateSampler(false, isolateMemory);
                case AnimationInterpolationMode.LINEAR: return xsampler.GetLinearKeys().CreateSampler(true, isolateMemory);
                case AnimationInterpolationMode.CUBICSPLINE: return xsampler.GetCubicKeys().CreateSampler(isolateMemory);
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        ICurveSampler<SPARSE8> IAnimationSampler<SPARSE8>.CreateCurveSampler(bool isolateMemory)
        {
            var xsampler = this as IAnimationSampler<SPARSE8>;

            switch (this.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP: return xsampler.GetLinearKeys().CreateSampler(false, isolateMemory);
                case AnimationInterpolationMode.LINEAR: return xsampler.GetLinearKeys().CreateSampler(true, isolateMemory);
                case AnimationInterpolationMode.CUBICSPLINE: return xsampler.GetCubicKeys().CreateSampler(isolateMemory);
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        ICurveSampler<Single[]> IAnimationSampler<Single[]>.CreateCurveSampler(bool isolateMemory)
        {
            var xsampler = this as IAnimationSampler<Single[]>;

            switch (this.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP: return xsampler.GetLinearKeys().CreateSampler(false, isolateMemory);
                case AnimationInterpolationMode.LINEAR: return xsampler.GetLinearKeys().CreateSampler(true, isolateMemory);
                case AnimationInterpolationMode.CUBICSPLINE: return xsampler.GetCubicKeys().CreateSampler(isolateMemory);
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        ICurveSampler<SEGMENT> IAnimationSampler<SEGMENT>.CreateCurveSampler(bool isolateMemory)
        {
            var xsampler = this as IAnimationSampler<SEGMENT>;

            switch (this.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP: return xsampler.GetLinearKeys().CreateSampler(false, isolateMemory);
                case AnimationInterpolationMode.LINEAR: return xsampler.GetLinearKeys().CreateSampler(true, isolateMemory);
                case AnimationInterpolationMode.CUBICSPLINE: return xsampler.GetCubicKeys().CreateSampler(isolateMemory);
            }

            throw new NotImplementedException();
        }

        private static IEnumerable<(T TangentIn, T Value, T TangentOut)> _GroupByTangentValueTangent<T>(IEnumerable<T> collection)
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

        #endregion

        #region validation

        /// <inheritdoc/>
        protected override void OnValidateReferences(ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate
                .IsNullOrIndex("Input", _input, this.LogicalParent.LogicalParent.LogicalAccessors)
                .IsNullOrIndex("Output", _output, this.LogicalParent.LogicalParent.LogicalAccessors);
        }

        /// <inheritdoc/>
        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);

            if (Output.Dimensions != DimensionType.SCALAR)
            {
                var outMult = InterpolationMode == AnimationInterpolationMode.CUBICSPLINE ? 3 : 1;

                validate.AreEqual("Output", Output.Count, Input.Count * outMult);
            }

            Input.ValidateAnimationInput(validate);
            Output.ValidateAnimationOutput(validate);
        }

        #endregion
    }
}
