using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using SharpGLTF.Memory;

using VALIDATIONCTX = SharpGLTF.Validation.ValidationContext;

namespace SharpGLTF.Schema2
{
    // https://github.com/KhronosGroup/glTF/issues/827#issuecomment-277537204

    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Diagnostics._AccessorDebugProxy))]
    public sealed partial class Accessor
    {
        #region debug

        internal string _GetDebuggerDisplay()
        {
            return Diagnostics.DebuggerDisplay.ToReportLong(this);
        }

        #endregion

        #region lifecycle

        internal Accessor()
        {
            _min = new List<double>();
            _max = new List<double>();

            // this is required because when ByteOffset in the schema is defined, even if it's zero, it triggers requiring a BufferView
            _byteOffset = null;
            _normalized = null;
        }

        #endregion

        #region data

        /// <summary>
        /// This must be null, or always in sync with <see cref="_type"/>
        /// </summary>
        private DimensionType? _CachedType;

        #endregion

        #region properties

        internal int _SourceBufferViewIndex => this._bufferView.AsValue(-1);

        /// <summary>
        /// Gets the number of bytes, starting at <see cref="ByteOffset"/> use by this <see cref="Accessor"/>
        /// </summary>
        public int ByteLength => BufferView.GetAccessorByteLength(Format, Count, SourceBufferView);

        /// <summary>
        /// Gets the <see cref="BufferView"/> buffer that contains the items as an encoded byte array.
        /// </summary>
        public BufferView SourceBufferView => this._bufferView.HasValue ? this.LogicalParent.LogicalBufferViews[this._bufferView.Value] : null;

        /// <summary>
        /// Gets the number of items.
        /// </summary>
        public int Count => this._count;

        /// <summary>
        /// Gets the starting byte offset within the byte data.
        /// </summary>
        public int ByteOffset => this._byteOffset.AsValue(0);        

        /// <summary>
        /// Gets the <see cref="DimensionType"/> of an item.
        /// </summary>
        public DimensionType Dimensions => _GetDimensions();

        /// <summary>
        /// Gets the <see cref="EncodingType"/> of an item.
        /// </summary>
        public EncodingType Encoding => this._componentType;

        /// <summary>
        /// Gets a value indicating whether the items values are normalized.
        /// </summary>
        public Boolean Normalized => this._normalized.AsValue(false);

        /// <summary>
        /// Gets a value indicating whether this <see cref="Accessor"/> has a sparse structure.
        /// </summary>
        public Boolean IsSparse => this._sparse != null;

        public AttributeFormat Format => new AttributeFormat(this.Dimensions, _componentType, this._normalized.AsValue(false));

        /// <summary>
        /// Gets the bounds of this accessor.
        /// </summary>
        /// <remarks>
        /// Bounds may not be available or up to date, call <see cref="UpdateBounds"/> for update.
        /// </remarks>
        public (IReadOnlyList<double> Min, IReadOnlyList<double> Max) Bounds
        {
            get
            {
                IReadOnlyList<double> min = _min;
                min ??= Array.Empty<double>();

                IReadOnlyList<double> max = _max;
                max ??= Array.Empty<double>();

                return (min, max);
            }
        }

        #endregion

        #region API

        private DimensionType _GetDimensions()
        {
            if (_CachedType.HasValue)
            {
                #if DEBUG
                var parsedType = Enum.TryParse<DimensionType>(this._type, out var rr) ? rr : DimensionType.CUSTOM;
                System.Diagnostics.Debug.Assert(_CachedType.Value == parsedType);
                #endif

                return _CachedType.Value;
            }

            _CachedType = Enum.TryParse<DimensionType>(this._type, out var r) ? r : DimensionType.CUSTOM;

            return _CachedType.Value;
        }        

        internal bool _TryGetMemoryAccessor(out MemoryAccessor mem)
        {
            if (!TryGetBufferView(out var view)) { mem = default; return false; }
            var info = new MemoryAccessInfo(null, ByteOffset, Count, view.ByteStride, Format);
            mem = new MemoryAccessor(view.Content, info);
            return true;
        }

        internal bool _TryGetMemoryAccessor(string name, out MemoryAccessor mem)
        {
            if (!TryGetBufferView(out var view)) { mem = default; return false; }
            var info = new MemoryAccessInfo(name, ByteOffset, Count, view.ByteStride, Format);
            mem = new MemoryAccessor(view.Content, info);
            return true;
        }

        public bool TryGetBufferView(out BufferView bv)
        {
            if (_bufferView.HasValue)
            {
                bv = this.LogicalParent.LogicalBufferViews[this._bufferView.Value];
                return true;
            }

            bv = null;
            return false;
        }

        public void UpdateBounds()
        {
            this._min.Clear();
            this._max.Clear();

            if (this.Count == 0) return;

            // With the current limitations of the serializer, we can only handle floating point values.
            if (this.Encoding != EncodingType.FLOAT) return;

            // https://github.com/KhronosGroup/glTF-Validator/issues/79

            var dimensions = this.Dimensions.DimCount();

            for (int i = 0; i < dimensions; ++i)
            {
                this._min.Add(double.MaxValue);
                this._max.Add(double.MinValue);
            }

            if (this.TryGetBufferView(out var view))
            {
                var array = new MultiArray(view.Content, this.ByteOffset, this.Count, view.ByteStride, dimensions, this.Encoding, false);

                var current = new float[dimensions];

                for (int i = 0; i < array.Count; ++i)
                {
                    array.CopyItemTo(i, current);

                    for (int j = 0; j < current.Length; ++j)
                    {
                        this._min[j] = Math.Min(this._min[j], current[j]);
                        this._max[j] = Math.Max(this._max[j], current[j]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < this._min.Count; ++i) this._min[i] = 0;
                for (int i = 0; i < this._max.Count; ++i) this._max[i] = 0;
            }
        }

        #endregion

        #region Data Buffer API

        /// <summary>
        /// Associates this <see cref="Accessor"/> with a <see cref="BufferView"/>
        /// </summary>
        /// <param name="buffer">The <see cref="BufferView"/> source.</param>
        /// <param name="bufferByteOffset">The start byte offset within <paramref name="buffer"/>.</param>
        /// <param name="itemCount">The number of items in the accessor.</param>
        /// <param name="dimensions">The <see cref="DimensionType"/> item type.</param>
        /// <param name="encoding">The <see cref="EncodingType"/> item encoding.</param>
        /// <param name="normalized">The item normalization mode.</param>
        public void SetData(BufferView buffer, int bufferByteOffset, int itemCount, DimensionType dimensions, EncodingType encoding, Boolean normalized)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));
            Guard.MustBeGreaterThanOrEqualTo(bufferByteOffset, _byteOffsetMinimum, nameof(bufferByteOffset));
            Guard.MustBeGreaterThanOrEqualTo(itemCount, _countMinimum, nameof(itemCount));

            this._bufferView = buffer.LogicalIndex;
            this._byteOffset = bufferByteOffset.AsNullable(_byteOffsetDefault, _byteOffsetMinimum, int.MaxValue);
            this._count = itemCount;

            this._CachedType = dimensions;
            this._type = Enum.GetName(typeof(DimensionType), dimensions);

            this._componentType = encoding;
            this._normalized = normalized.AsNullable(_normalizedDefault);

            UpdateBounds();
        }

        public IAccessorArray<Matrix3x2> AsMatrix2x2Array()
        {
            return _TryGetMemoryAccessor(out var mem)
                ? mem.AsMatrix2x2Array()
                : new ZeroAccessorArray<Matrix3x2>(this._count);
        }

        public IAccessorArray<Matrix4x4> AsMatrix3x3Array()
        {
            return _TryGetMemoryAccessor(out var mem)
                ? mem.AsMatrix3x3Array()
                : new ZeroAccessorArray<Matrix4x4>(this._count);
        }

        public IAccessorArray<Matrix4x4> AsMatrix4x3Array()
        {
            /*
            const int dimsize = 4 * 3;

            var view = SourceBufferView;
            var stride = Math.Max(dimsize * this.Encoding.ByteLength(), view.ByteStride);
            var content = view.Content.Slice(this.ByteOffset, Count * stride);

            return new Matrix4x3Array(content, stride, this.Encoding, this.Normalized);
            */

            return _TryGetMemoryAccessor(out var mem)
                ? mem.AsMatrix4x3Array()
                : new ZeroAccessorArray<Matrix4x4>(this._count);
        }

        public IAccessorArray<Matrix4x4> AsMatrix4x4Array()
        {
            return _TryGetMemoryAccessor(out var mem)
                ? mem.AsMatrix4x4Array()
                : new ZeroAccessorArray<Matrix4x4>(this._count);
        }

        [Obsolete("Use AsMatrix4x4Array instead", true)]
        internal IReadOnlyList<Matrix4x4> AsMatrix4x4ReadOnlyList()
        {
            return _TryGetMemoryAccessor(out var mem)
                ? mem.AsMatrix4x4Array()
                : new ZeroAccessorArray<Matrix4x4>(this._count);
        }

        public IAccessorArray<Quaternion> AsQuaternionArray()
        {
            if (_TryGetMemoryAccessor(out var memory))
            {
                if (this._sparse == null) return memory.AsQuaternionArray();

                throw new NotImplementedException();
            }
            else
            {
                if (this._sparse == null) return new ZeroAccessorArray<Quaternion>(this._count);

                throw new NotImplementedException();
            }
        }

        public IAccessorArray<Single[]> AsMultiArray(int dimensions)
        {
            if (_TryGetMemoryAccessor(out var memory))
            {

                if (this._sparse == null) return memory.AsMultiArray(dimensions);

                throw new NotImplementedException();
            }
            else
            {
                if (this._sparse == null) return new ZeroAccessorArray<Single[]>(this._count);

                throw new NotImplementedException();
            }
        }

        #endregion

        #region Index Buffer API

        public void SetIndexData(MemoryAccessor src)
        {
            Guard.NotNull(src, nameof(src));

            var bv = this.LogicalParent.UseBufferView(src.Data, src.Attribute.ByteStride, BufferMode.ELEMENT_ARRAY_BUFFER);
            SetIndexData(bv, src.Attribute.ByteOffset, src.Attribute.ItemsCount, src.Attribute.Encoding.ToIndex());
        }

        /// <summary>
        /// Associates this <see cref="Accessor"/> with a <see cref="BufferView"/>
        /// </summary>
        /// <param name="buffer">The <see cref="BufferView"/> source.</param>
        /// <param name="bufferByteOffset">The start byte offset within <paramref name="buffer"/>.</param>
        /// <param name="itemCount">The number of items in the accessor.</param>
        /// <param name="encoding">The <see cref="IndexEncodingType"/> item encoding.</param>
        public void SetIndexData(BufferView buffer, int bufferByteOffset, int itemCount, IndexEncodingType encoding)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));
            Guard.IsFalse(buffer.IsVertexBuffer, nameof(buffer));

            SetData(buffer, bufferByteOffset, itemCount, DimensionType.SCALAR, encoding.ToComponent(), false);
        }

        public IAccessorArray<UInt32> AsIndicesArray()
        {
            Guard.IsFalse(this.IsSparse, nameof(IsSparse));
            Guard.IsTrue(this.Dimensions == DimensionType.SCALAR, nameof(Dimensions));

            return _TryGetMemoryAccessor(out var mem)
                ? new IntegerArray(SourceBufferView.Content, this.ByteOffset, this._count, this.Encoding.ToIndex())
                : new ZeroAccessorArray<UInt32>(this._count);
        }

        #endregion

        #region Vertex Buffer API

        public void SetVertexData(MemoryAccessor src)
        {
            Guard.NotNull(src, nameof(src));

            var bv = this.LogicalParent.UseBufferView(src.Data, src.Attribute.StepByteLength, BufferMode.ARRAY_BUFFER);

            SetVertexData(bv, src.Attribute.ByteOffset, src.Attribute.ItemsCount, src.Attribute.Dimensions, src.Attribute.Encoding, src.Attribute.Normalized);
        }

        /// <summary>
        /// Associates this <see cref="Accessor"/> with a <see cref="BufferView"/>
        /// </summary>
        /// <param name="buffer">The <see cref="BufferView"/> source.</param>
        /// <param name="bufferByteOffset">The start byte offset within <paramref name="buffer"/>.</param>
        /// <param name="itemCount">The number of items in the accessor.</param>
        /// <param name="dimensions">The <see cref="DimensionType"/> item type.</param>
        /// <param name="encoding">The <see cref="EncodingType"/> item encoding.</param>
        /// <param name="normalized">The item normalization mode.</param>
        public void SetVertexData(BufferView buffer, int bufferByteOffset, int itemCount, DimensionType dimensions = DimensionType.VEC3, EncodingType encoding = EncodingType.FLOAT, Boolean normalized = false)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));
            Guard.MustBePositiveAndMultipleOf(dimensions.DimCount() * encoding.ByteLength(), 4, nameof(encoding));
            Guard.IsFalse(buffer.IsIndexBuffer, nameof(buffer));

            SetData(buffer, bufferByteOffset, itemCount, dimensions, encoding, normalized);
        }

        internal IAccessorArray<T> AsVertexArray<T>()
        {
            if (typeof(T) == typeof(float)) return (IAccessorArray<T>)AsScalarArray();
            if (typeof(T) == typeof(Vector2)) return (IAccessorArray<T>)AsVector2Array();
            if (typeof(T) == typeof(Vector3)) return (IAccessorArray<T>)AsVector3Array();
            if (typeof(T) == typeof(Vector4)) return (IAccessorArray<T>)AsVector4Array();

            throw new NotImplementedException();
        }

        public IAccessorArray<Single> AsScalarArray()
        {
            if (_TryGetMemoryAccessor(out var memory))
            {
                if (this._sparse == null) return memory.AsScalarArray();

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateScalarSparseArray(memory, sparseKV.Key, sparseKV.Value);
            }
            else
            {
                if (this._sparse == null) return new ZeroAccessorArray<Single>(this._count);

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateScalarSparseArray(this._count, sparseKV.Key, sparseKV.Value);
            }            
        }

        public IAccessorArray<Vector2> AsVector2Array()
        {
            if (_TryGetMemoryAccessor(out var memory))
            {
                if (this._sparse == null) return memory.AsVector2Array();

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateVector2SparseArray(memory, sparseKV.Key, sparseKV.Value);
            }
            else
            {
                if (this._sparse == null) return new ZeroAccessorArray<Vector2>(this._count);

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateVector2SparseArray(this._count, sparseKV.Key, sparseKV.Value);
            }
        }

        public IAccessorArray<Vector3> AsVector3Array()
        {
            if (_TryGetMemoryAccessor(out var memory))
            {

                if (this._sparse == null) return memory.AsVector3Array();

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateVector3SparseArray(memory, sparseKV.Key, sparseKV.Value);
            }
            else
            {
                if (this._sparse == null) return new ZeroAccessorArray<Vector3>(this._count);

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateVector3SparseArray(this._count, sparseKV.Key, sparseKV.Value);
            }
        }

        public IAccessorArray<Vector4> AsVector4Array()
        {
            if (_TryGetMemoryAccessor(out var memory))
            {
                if (this._sparse == null) return memory.AsVector4Array();

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateVector4SparseArray(memory, sparseKV.Key, sparseKV.Value);
            }
            else
            {
                if (this._sparse == null) return new ZeroAccessorArray<Vector4>(this._count);

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateVector4SparseArray(this._count, sparseKV.Key, sparseKV.Value);
            }
        }

        public IAccessorArray<Vector4> AsColorArray(Single defaultW = 1)
        {
            if (_TryGetMemoryAccessor(out var memory))
            {

                if (this._sparse == null) return memory.AsColorArray(defaultW);

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateColorSparseArray(memory, sparseKV.Key, sparseKV.Value, defaultW);
            }
            else
            {
                if (this._sparse == null) return new ZeroAccessorArray<Vector4>(this._count);

                var sparseKV = this._sparse._CreateMemoryAccessors(this);
                return MemoryAccessor.CreateColorSparseArray(this._count, sparseKV.Key, sparseKV.Value);
            }
        }
        
        public ArraySegment<Byte> TryGetVertexBytes(int vertexIdx)
        {
            if (_sparse != null) throw new InvalidOperationException("Can't be used on Acessors with Sparse Data");

            if (!this.TryGetBufferView(out var bufferView)) return default;

            var itemByteSz = Encoding.ByteLength() * Dimensions.DimCount();
            var byteStride = Math.Max(itemByteSz, bufferView.ByteStride);
            var byteOffset = vertexIdx * byteStride;

            return bufferView.Content.Slice(this.ByteOffset + (vertexIdx * byteStride), itemByteSz);
        }

        #endregion

        #region Validation

        protected override void OnValidateReferences(VALIDATIONCTX validate)
        {
            base.OnValidateReferences(validate);            

            if (_byteOffset.HasValue)
            {
                validate
                    .IsDefined(nameof(_bufferView), _bufferView)
                    .IsNullOrIndex(nameof(_bufferView), _bufferView, this.LogicalParent.LogicalBufferViews);                    
            }            
        }

        protected override void OnValidateContent(VALIDATIONCTX validate)
        {
            base.OnValidateContent(validate);

            validate.IsGreaterOrEqual(nameof(_count), _count, _countMinimum);

            if (_byteOffset.HasValue)
            {
                validate.NonNegative(nameof(_byteOffset), _byteOffset);
            }

            // if Accessor.Type uses a custom dimension,
            // we cannot check the rest of the accessor.
            if (this.Dimensions == DimensionType.CUSTOM) return;

            if (TryGetBufferView(out var bv))
            {
                BufferView.VerifyAccess(validate, bv, this.ByteOffset, this.Format, this.Count);
            }            

            if (_TryGetMemoryAccessor(out var mem))
            {
                validate.That(() => MemoryAccessor.VerifyAccessorBounds(mem, _min, _max));                
            }

            if (this._normalized == true)
            {
                bool isNormalizable = false;
                isNormalizable |= Encoding == EncodingType.BYTE;
                isNormalizable |= Encoding == EncodingType.UNSIGNED_BYTE;
                isNormalizable |= Encoding == EncodingType.SHORT;
                isNormalizable |= Encoding == EncodingType.UNSIGNED_SHORT;
                validate.That(isNormalizable, "Normalized", "Only Byte and Short can be normalized");
            }

            // at this point we don't know which kind of data we're accessing, so it's up to the components
            // using this accessor to validate the data.
        }

        internal void ValidateIndices(VALIDATIONCTX validate, uint vertexCount, PrimitiveType drawingType)
        {
            validate = validate.GetContext(this);

            validate.IsAnyOf("Format", Format, (DimensionType.SCALAR, EncodingType.UNSIGNED_BYTE), (DimensionType.SCALAR, EncodingType.UNSIGNED_SHORT), (DimensionType.SCALAR, EncodingType.UNSIGNED_INT));

            if (TryGetBufferView(out var bufferView))
            {
                bufferView.ValidateBufferUsageGPU(validate, BufferMode.ELEMENT_ARRAY_BUFFER);

                validate.AreEqual(nameof(bufferView.ByteStride), bufferView.ByteStride, 0); // "bufferView.byteStride must not be defined for indices accessor.";
            }

            if (_TryGetMemoryAccessor(out var mem))
            {
                validate.That(() => MemoryAccessor.VerifyVertexIndices(mem, vertexCount));
            }            
        }

        internal static void ValidateVertexAttributes(VALIDATIONCTX validate, IReadOnlyDictionary<string, Accessor> attributes, int skinsMaxJointCount)
        {
            if (validate.TryFix)
            {
                foreach (var kvp in attributes.Where(item => item.Key != "POSITION"))
                {
                    // remove unnecessary bounds
                    kvp.Value._min.Clear();
                    kvp.Value._max.Clear();
                }
            }

            if (attributes.TryGetValue("POSITION", out Accessor positions)) positions._ValidatePositions(validate);

            if (attributes.TryGetValue("NORMAL", out Accessor normals)) normals._ValidateNormals(validate);
            if (attributes.TryGetValue("TANGENT", out Accessor tangents)) tangents._ValidateTangents(validate);

            if (attributes.TryGetValue("JOINTS_0", out Accessor joints0)) joints0._ValidateJoints(validate, "JOINTS_0", skinsMaxJointCount);
            if (attributes.TryGetValue("JOINTS_1", out Accessor joints1)) joints1._ValidateJoints(validate, "JOINTS_1", skinsMaxJointCount);

            attributes.TryGetValue("WEIGHTS_0", out Accessor weights0);
            attributes.TryGetValue("WEIGHTS_1", out Accessor weights1);
            _ValidateWeights(validate, weights0, weights1);
        }

        private void _ValidatePositions(VALIDATIONCTX validate)
        {
            validate = validate.GetContext(this);

            if (TryGetBufferView(out var bufferView)) bufferView.ValidateBufferUsageGPU(validate, BufferMode.ARRAY_BUFFER);

            if (!this.LogicalParent.MeshQuantizationAllowed)
            {
                validate.IsAnyOf(nameof(Format), Format, DimensionType.VEC3);
            }
            else
            {
                validate.IsAnyOf(nameof(Dimensions), Dimensions, DimensionType.VEC3);
            }

            validate.ArePositions("POSITION", this.AsVector3Array());
        }

        private void _ValidateNormals(VALIDATIONCTX validate)
        {
            validate = validate.GetContext(this);

            if (TryGetBufferView(out var bufferView)) bufferView.ValidateBufferUsageGPU(validate, BufferMode.ARRAY_BUFFER);

            if (!this.LogicalParent.MeshQuantizationAllowed)
            {
                validate.IsAnyOf(nameof(Format), Format, DimensionType.VEC3);
            }
            else
            {
                validate.IsAnyOf(nameof(Dimensions), Dimensions, DimensionType.VEC3);
            }

            if (validate.TryFix) this.AsVector3Array().SanitizeNormals();

            validate.AreNormals("NORMAL", this.AsVector3Array());
        }

        private void _ValidateTangents(VALIDATIONCTX validate)
        {
            validate = validate.GetContext(this);

            if (TryGetBufferView(out var bufferView)) bufferView.ValidateBufferUsageGPU(validate, BufferMode.ARRAY_BUFFER);

            if (!this.LogicalParent.MeshQuantizationAllowed)
            {
                validate.IsAnyOf(nameof(Format), Format, DimensionType.VEC3, DimensionType.VEC4);
            }
            else
            {
                validate.IsAnyOf(nameof(Dimensions), Dimensions, DimensionType.VEC3, DimensionType.VEC4);
            }

            if (validate.TryFix)
            {
                if (Dimensions == DimensionType.VEC3) this.AsVector3Array().SanitizeNormals();
                if (Dimensions == DimensionType.VEC4) this.AsVector4Array().SanitizeTangents();
            }

            if (Dimensions == DimensionType.VEC3) validate.AreNormals("TANGENT", this.AsVector3Array());
            if (Dimensions == DimensionType.VEC4) validate.AreTangents("TANGENT", this.AsVector4Array());
        }

        private void _ValidateJoints(VALIDATIONCTX validate, string attributeName, int skinsMaxJointCount)
        {
            validate = validate.GetContext(this);

            if (TryGetBufferView(out var bufferView)) bufferView.ValidateBufferUsageGPU(validate, BufferMode.ARRAY_BUFFER);

            validate
                .IsAnyOf(nameof(Format), Format, (DimensionType.VEC4, EncodingType.UNSIGNED_BYTE), (DimensionType.VEC4, EncodingType.UNSIGNED_SHORT), DimensionType.VEC4)
                .AreJoints(attributeName, this.AsVector4Array(), skinsMaxJointCount);
        }

        private static void _ValidateWeights(VALIDATIONCTX validate, Accessor weights0, Accessor weights1)
        {
            weights0?._ValidateWeights(validate);
            weights1?._ValidateWeights(validate);

            var memory0 = (weights0?._TryGetMemoryAccessor("WEIGHTS_0", out var mem0) ?? false) ? mem0 : null;
            var memory1 = (weights1?._TryGetMemoryAccessor("WEIGHTS_1", out var mem1) ?? false) ? mem1 : null;

            validate.That(() => MemoryAccessor.VerifyWeightsSum(memory0, memory1));
        }

        private void _ValidateWeights(VALIDATIONCTX validate)
        {
            validate = validate.GetContext(this);

            if (TryGetBufferView(out var bufferView)) bufferView.ValidateBufferUsageGPU(validate, BufferMode.ARRAY_BUFFER);

            validate.IsAnyOf(nameof(Format), Format, (DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, true), (DimensionType.VEC4, EncodingType.UNSIGNED_SHORT, true), DimensionType.VEC4);
        }

        internal void ValidateMatrices4x3(VALIDATIONCTX validate, bool mustInvert = true, bool mustDecompose = true)
        {
            validate = validate.GetContext(this);

            if (TryGetBufferView(out var bv))
            {
                bv.ValidateBufferUsagePlainData(validate);
            }            

            validate.IsAnyOf(nameof(Format), Format, (DimensionType.MAT4, EncodingType.BYTE, true), (DimensionType.MAT4, EncodingType.SHORT, true), DimensionType.MAT4);

            IReadOnlyList<Matrix4x4> matrices = this.AsMatrix4x4Array();

            for (int i = 0; i < matrices.Count; ++i)
            {
                validate.IsNullOrMatrix4x3("Matrices", matrices[i], mustInvert, mustDecompose);
            }
        }

        internal void ValidateAnimationInput(VALIDATIONCTX validate)
        {
            if (TryGetBufferView(out var bv))
            {
                bv.ValidateBufferUsagePlainData(validate, false); // as per glTF specification, animation accessors must not have ByteStride
            }

            validate.IsAnyOf(nameof(Dimensions), Dimensions, DimensionType.SCALAR);
        }

        internal void ValidateAnimationOutput(VALIDATIONCTX validate)
        {
            if (TryGetBufferView(out var bv))
            {
                bv.ValidateBufferUsagePlainData(validate, false); // as per glTF specification, animation accessors must not have ByteStride
            }

            validate.IsAnyOf(nameof(Dimensions), Dimensions, DimensionType.SCALAR, DimensionType.VEC2, DimensionType.VEC3, DimensionType.VEC4);
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Accessor"/> instance
        /// and adds it to <see cref="ModelRoot.LogicalAccessors"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Accessor"/> instance.</returns>
        public Accessor CreateAccessor(string name = null)
        {
            var accessor = new Accessor
            {
                Name = name
            };

            _accessors.Add(accessor);

            return accessor;
        }
    }
}
