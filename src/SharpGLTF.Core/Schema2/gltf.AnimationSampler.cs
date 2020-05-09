using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Animations;
using SharpGLTF.Collections;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;

namespace SharpGLTF.Schema2
{
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
        IEnumerable<(Single Key, (T TangentIn, T Value, T TangentOut))> GetCubicKeys();

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

    sealed partial class AnimationSampler :
        IChildOf<Animation>,
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

        private static (Single[] Keys, TValue[] Values) _Split<TValue>(IReadOnlyDictionary<Single, TValue> keyframes)
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

        private static (Single[] Keys, TValue[] Values) _Split<TValue>(IReadOnlyDictionary<Single, (TValue TangentIn, TValue Value, TValue TangentOut)> keyframes)
        {
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
            var (keys, values) = _Split(keyframes);
            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, Quaternion> keyframes)
        {
            var (keys, values) = _Split(keyframes);
            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, SparseWeight8> keyframes, int expandedCount)
        {
            var (keys, values) = _Split(keyframes);
            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values, expandedCount).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, (Vector3 TangentIn, Vector3 Value, Vector3 TangentOut)> keyframes)
        {
            var (keys, values) = _Split(keyframes);

            // this might not be true for a looped animation, where first and last might be the same
            values[0] = Vector3.Zero;
            values[values.Length - 1] = Vector3.Zero;

            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, (Quaternion TangentIn, Quaternion Value, Quaternion TangentOut)> keyframes)
        {
            var (keys, values) = _Split(keyframes);

            // this might not be true for a looped animation, where first and last might be the same
            values[0] = default;
            values[values.Length - 1] = default;

            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values).LogicalIndex;
        }

        internal void SetKeys(IReadOnlyDictionary<Single, (SparseWeight8 TangentIn, SparseWeight8 Value, SparseWeight8 TangentOut)> keyframes, int expandedCount)
        {
            var (keys, values) = _Split(keyframes);

            // this might not be true for a looped animation, where first and last might be the same
            values[0] = default;
            values[values.Length - 1] = default;

            _input = this._CreateInputAccessor(keys).LogicalIndex;
            _output = this._CreateOutputAccessor(values, expandedCount).LogicalIndex;
        }

        IEnumerable<(Single, Vector3)> IAnimationSampler<Vector3>.GetLinearKeys()
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsVector3Array();

            return keys.Zip(frames, (key, val) => (key, val));
        }

        IEnumerable<(Single, Quaternion)> IAnimationSampler<Quaternion>.GetLinearKeys()
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsQuaternionArray();

            return keys.Zip(frames, (key, val) => (key, val));
        }

        IEnumerable<(Single, SparseWeight8)> IAnimationSampler<SparseWeight8>.GetLinearKeys()
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / this.Input.Count;

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsMultiArray(dimensions);

            return keys.Zip(frames, (key, val) => (key, SparseWeight8.Create(val)));
        }

        IEnumerable<(Single, Single[])> IAnimationSampler<Single[]>.GetLinearKeys()
        {
            Guard.IsFalse(this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / this.Input.Count;

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsMultiArray(dimensions);

            return keys.Zip(frames, (key, val) => (key, val));
        }

        IEnumerable<(Single, (Vector3, Vector3, Vector3))> IAnimationSampler<Vector3>.GetCubicKeys()
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByTangentValueTangent(this.Output.AsVector3Array());

            return keys.Zip(frames, (key, val) => (key, val));
        }

        IEnumerable<(Single, (Quaternion, Quaternion, Quaternion))> IAnimationSampler<Quaternion>.GetCubicKeys()
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByTangentValueTangent(this.Output.AsQuaternionArray());

            return keys.Zip(frames, (key, val) => (key, val));
        }

        IEnumerable<(Single, (Single[], Single[], Single[]))> IAnimationSampler<Single[]>.GetCubicKeys()
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / (this.Input.Count * 3);

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByTangentValueTangent(this.Output.AsMultiArray(dimensions));

            return keys.Zip(frames, (key, val) => (key, val));
        }

        IEnumerable<(Single, (SparseWeight8, SparseWeight8, SparseWeight8))> IAnimationSampler<SparseWeight8>.GetCubicKeys()
        {
            Guard.IsFalse(this.InterpolationMode != AnimationInterpolationMode.CUBICSPLINE, nameof(InterpolationMode));

            var dimensions = this.Output.Count / (this.Input.Count * 3);

            var keys = this.Input.AsScalarArray();
            var frames = _GroupByTangentValueTangent(this.Output.AsMultiArray(dimensions));

            return keys.Zip(frames, (key, val) => (key, SparseWeight8.AsTuple(val.TangentIn, val.Value, val.TangentOut)) );
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

        ICurveSampler<SparseWeight8> IAnimationSampler<SparseWeight8>.CreateCurveSampler(bool isolateMemory)
        {
            var xsampler = this as IAnimationSampler<SparseWeight8>;

            switch (this.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP: return xsampler.GetLinearKeys().CreateSampler(false, isolateMemory);
                case AnimationInterpolationMode.LINEAR: return xsampler.GetLinearKeys().CreateSampler(true, isolateMemory);
                case AnimationInterpolationMode.CUBICSPLINE: return xsampler.GetCubicKeys().CreateSampler(isolateMemory);
            }

            throw new NotImplementedException();
        }

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

        #endregion

        #region validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate
                .IsNullOrIndex("Input", _input, this.LogicalParent.LogicalParent.LogicalAccessors)
                .IsNullOrIndex("Output", _output, this.LogicalParent.LogicalParent.LogicalAccessors);
        }

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
