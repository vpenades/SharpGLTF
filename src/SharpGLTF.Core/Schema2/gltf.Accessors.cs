using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SharpGLTF.Memory;
using SharpGLTF.Validation;

using VALIDATIONCTX = SharpGLTF.Validation.ValidationContext;

namespace SharpGLTF.Schema2
{
    // https://github.com/KhronosGroup/glTF/issues/827#issuecomment-277537204

    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._AccessorDebugProxy))]
    public sealed partial class Accessor
    {
        #region debug

        internal string _GetDebuggerDisplay()
        {
            return Debug.DebuggerDisplay.ToReportLong(this);
        }

        #endregion

        #region lifecycle

        internal Accessor()
        {
            _min = new List<double>();
            _max = new List<double>();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Accessor"/> at <see cref="ModelRoot.LogicalAccessors"/>
        /// </summary>
        public int LogicalIndex                 => this.LogicalParent.LogicalAccessors.IndexOfReference(this);

        internal int _SourceBufferViewIndex => this._bufferView.AsValue(-1);

        /// <summary>
        /// Gets the <see cref="BufferView"/> buffer that contains the items as an encoded byte array.
        /// </summary>
        public BufferView SourceBufferView      => this._bufferView.HasValue ? this.LogicalParent.LogicalBufferViews[this._bufferView.Value] : null;

        /// <summary>
        /// Gets the number of items.
        /// </summary>
        public int Count                        => this._count;

        /// <summary>
        /// Gets the starting byte offset within <see cref="SourceBufferView"/>.
        /// </summary>
        public int ByteOffset                   => this._byteOffset.AsValue(0);

        /// <summary>
        /// Gets the number of bytes, starting at <see cref="ByteOffset"/> use by this <see cref="Accessor"/>
        /// </summary>
        public int ByteLength                   => SourceBufferView.GetAccessorByteLength(Format, Count);

        /// <summary>
        /// Gets the <see cref="DimensionType"/> of an item.
        /// </summary>
        public DimensionType Dimensions           => this._type;

        /// <summary>
        /// Gets the <see cref="EncodingType"/> of an item.
        /// </summary>
        public EncodingType Encoding           => this._componentType;

        /// <summary>
        /// Gets a value indicating whether the items values are normalized.
        /// </summary>
        public Boolean Normalized               => this._normalized.AsValue(false);

        /// <summary>
        /// Gets a value indicating whether this <see cref="Accessor"/> has a sparse structure.
        /// </summary>
        public Boolean IsSparse                 => this._sparse != null;

        public AttributeFormat Format => new AttributeFormat(_type, _componentType, this._normalized.AsValue(false));

        /// <summary>
        /// Gets the number of bytes required to encode a single item in <see cref="SourceBufferView"/>
        /// Given the current <see cref="Dimensions"/> and <see cref="Encoding"/> states.
        /// </summary>
        [Obsolete("Use Format.ByteSize instead")]
        public int ElementByteSize                 => Encoding.ByteLength() * Dimensions.DimCount();

        #endregion

        #region API

        internal MemoryAccessor _GetMemoryAccessor(string name = null)
        {
            var view = SourceBufferView;
            var info = new MemoryAccessInfo(name, ByteOffset, Count, view.ByteStride, Format);
            return new MemoryAccessor(view.Content, info);
        }

        internal KeyValuePair<IntegerArray, MemoryAccessor>? _GetSparseMemoryAccessor()
        {
            return this._sparse == null
                ?
                (KeyValuePair<IntegerArray, MemoryAccessor>?)null
                :
                this._sparse._CreateMemoryAccessors(this);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatItems(_sparse);
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

            var array = new MultiArray(this.SourceBufferView.Content, this.ByteOffset, this.Count, this.SourceBufferView.ByteStride, dimensions, this.Encoding, false);

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

            this._type = dimensions;
            this._componentType = encoding;
            this._normalized = normalized.AsNullable(_normalizedDefault);

            UpdateBounds();
        }

        public Matrix4x4Array AsMatrix4x4Array()
        {
            return _GetMemoryAccessor().AsMatrix4x4Array();
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

        public IntegerArray AsIndicesArray()
        {
            Guard.IsFalse(this.IsSparse, nameof(IsSparse));
            Guard.IsTrue(this.Dimensions == DimensionType.SCALAR, nameof(Dimensions));

            return new IntegerArray(SourceBufferView.Content, this.ByteOffset, this._count, this.Encoding.ToIndex());
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

        public IList<Single> AsScalarArray()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsScalarArray();

            var sparseKV = this._sparse._CreateMemoryAccessors(this);
            return MemoryAccessor.CreateScalarSparseArray(memory, sparseKV.Key, sparseKV.Value);
        }

        public IList<Vector2> AsVector2Array()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsVector2Array();

            var sparseKV = this._sparse._CreateMemoryAccessors(this);
            return MemoryAccessor.CreateVector2SparseArray(memory, sparseKV.Key, sparseKV.Value);
        }

        public IList<Vector3> AsVector3Array()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsVector3Array();

            var sparseKV = this._sparse._CreateMemoryAccessors(this);
            return MemoryAccessor.CreateVector3SparseArray(memory, sparseKV.Key, sparseKV.Value);
        }

        public IList<Vector4> AsVector4Array()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsVector4Array();

            var sparseKV = this._sparse._CreateMemoryAccessors(this);
            return MemoryAccessor.CreateVector4SparseArray(memory, sparseKV.Key, sparseKV.Value);
        }

        public IList<Vector4> AsColorArray(Single defaultW = 1)
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsColorArray(defaultW);

            var sparseKV = this._sparse._CreateMemoryAccessors(this);
            return MemoryAccessor.CreateColorSparseArray(memory, sparseKV.Key, sparseKV.Value, defaultW);
        }

        public IList<Quaternion> AsQuaternionArray()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsQuaternionArray();

            throw new NotImplementedException();
        }

        public IList<Single[]> AsMultiArray(int dimensions)
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsMultiArray(dimensions);

            throw new NotImplementedException();
        }

        public ArraySegment<Byte> TryGetVertexBytes(int vertexIdx)
        {
            if (_sparse != null) throw new InvalidOperationException("Can't be used on Acessors with Sparse Data");

            var itemByteSz = Encoding.ByteLength() * Dimensions.DimCount();
            var byteStride = Math.Max(itemByteSz, SourceBufferView.ByteStride);
            var byteOffset = vertexIdx * byteStride;

            return SourceBufferView.Content.Slice(this.ByteOffset + (vertexIdx * byteStride), itemByteSz);
        }

        #endregion

        #region Validation

        protected override void OnValidateReferences(VALIDATIONCTX validate)
        {
            base.OnValidateReferences(validate);

            validate
                .IsDefined(nameof(_bufferView), _bufferView)
                .NonNegative(nameof(_byteOffset), _byteOffset)
                .IsGreaterOrEqual(nameof(_count), _count, _countMinimum)
                .IsNullOrIndex(nameof(_bufferView), _bufferView, this.LogicalParent.LogicalBufferViews);
        }

        protected override void OnValidateContent(VALIDATIONCTX validate)
        {
            base.OnValidateContent(validate);

            BufferView.VerifyAccess(validate, this.SourceBufferView, this.ByteOffset, this.Format, this.Count);

            validate.That(() => MemoryAccessor.VerifyAccessorBounds(_GetMemoryAccessor(), _min, _max));

            // at this point we don't know which kind of data we're accessing, so it's up to the components
            // using this accessor to validate the data.
        }

        internal void ValidateIndices(VALIDATIONCTX validate, uint vertexCount, PrimitiveType drawingType)
        {
            validate = validate.GetContext(this);

            SourceBufferView.ValidateBufferUsageGPU(validate, BufferMode.ELEMENT_ARRAY_BUFFER);
            validate.IsAnyOf("Format", Format, (DimensionType.SCALAR, EncodingType.UNSIGNED_BYTE), (DimensionType.SCALAR, EncodingType.UNSIGNED_SHORT), (DimensionType.SCALAR, EncodingType.UNSIGNED_INT));

            validate.AreEqual(nameof(SourceBufferView.ByteStride), SourceBufferView.ByteStride, 0); // "bufferView.byteStride must not be defined for indices accessor.";

            validate.That(() => MemoryAccessor.VerifyVertexIndices(_GetMemoryAccessor(), vertexCount));
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
            if (attributes.TryGetValue("JOINTS_1", out Accessor joints1)) joints0._ValidateJoints(validate, "JOINTS_1", skinsMaxJointCount);

            attributes.TryGetValue("WEIGHTS_0", out Accessor weights0);
            attributes.TryGetValue("WEIGHTS_1", out Accessor weights1);
            _ValidateWeights(validate, weights0, weights1);
        }

        private void _ValidatePositions(VALIDATIONCTX validate)
        {
            validate = validate.GetContext(this);

            SourceBufferView.ValidateBufferUsageGPU(validate, BufferMode.ARRAY_BUFFER);

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

            SourceBufferView.ValidateBufferUsageGPU(validate, BufferMode.ARRAY_BUFFER);

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

            SourceBufferView.ValidateBufferUsageGPU(validate, BufferMode.ARRAY_BUFFER);

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

            SourceBufferView.ValidateBufferUsageGPU(validate, BufferMode.ARRAY_BUFFER);

            validate
                .IsAnyOf(nameof(Format), Format, (DimensionType.VEC4, EncodingType.UNSIGNED_BYTE), (DimensionType.VEC4, EncodingType.UNSIGNED_SHORT), DimensionType.VEC4)
                .AreJoints(attributeName, this.AsVector4Array(), skinsMaxJointCount);
        }

        private static void _ValidateWeights(VALIDATIONCTX validate, Accessor weights0, Accessor weights1)
        {
            weights0?._ValidateWeights(validate);
            weights1?._ValidateWeights(validate);

            var memory0 = weights0?._GetMemoryAccessor("WEIGHTS_0");
            var memory1 = weights1?._GetMemoryAccessor("WEIGHTS_1");

            validate.That(() => MemoryAccessor.VerifyWeightsSum(memory0, memory1));
        }

        private void _ValidateWeights(VALIDATIONCTX validate)
        {
            validate = validate.GetContext(this);

            SourceBufferView.ValidateBufferUsageGPU(validate, BufferMode.ARRAY_BUFFER);

            validate.IsAnyOf(nameof(Format), Format, (DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, true), (DimensionType.VEC4, EncodingType.UNSIGNED_SHORT, true), DimensionType.VEC4);
        }

        internal void ValidateMatrices(VALIDATIONCTX validate, bool mustDecompose = true, bool mustInvert = true)
        {
            validate = validate.GetContext(this);

            SourceBufferView.ValidateBufferUsagePlainData(validate);

            validate.IsAnyOf(nameof(Format), Format, (DimensionType.MAT4, EncodingType.BYTE, true), (DimensionType.MAT4, EncodingType.SHORT, true), DimensionType.MAT4);

            var matrices = this.AsMatrix4x4Array();

            for (int i = 0; i < matrices.Count; ++i)
            {
                validate.IsNullOrMatrix("Matrices", matrices[i], mustDecompose, mustInvert);
            }
        }

        internal void ValidateAnimationInput(VALIDATIONCTX validate)
        {
            SourceBufferView.ValidateBufferUsagePlainData(validate);

            validate.IsAnyOf(nameof(Dimensions), Dimensions, DimensionType.SCALAR);
        }

        internal void ValidateAnimationOutput(VALIDATIONCTX validate)
        {
            SourceBufferView.ValidateBufferUsagePlainData(validate);

            validate.IsAnyOf(nameof(Dimensions), Dimensions, DimensionType.SCALAR, DimensionType.VEC3, DimensionType.VEC4);
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
