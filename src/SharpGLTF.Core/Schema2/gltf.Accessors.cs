using System;
using System.Collections.Generic;
using System.Numerics;

using SharpGLTF.Memory;

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

        internal int _LogicalBufferViewIndex    => this._bufferView.AsValue(-1);

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

        /// <summary>
        /// Gets the number of bytes required to encode a single item in <see cref="SourceBufferView"/>
        /// Given the current <see cref="Dimensions"/> and <see cref="Encoding"/> states.
        /// </summary>
        public int ItemByteSize                 => Encoding.ByteLength() * Dimensions.DimCount();

        #endregion

        #region API

        internal MemoryAccessor _GetMemoryAccessor()
        {
            var view = SourceBufferView;
            var info = new MemoryAccessInfo(null, ByteOffset, Count, view.ByteStride, Dimensions, Encoding, Normalized);
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

            if (buffer.DeviceBufferTarget.HasValue) Guard.IsTrue(buffer.DeviceBufferTarget.Value == BufferMode.ELEMENT_ARRAY_BUFFER, nameof(buffer));

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

            var bv = this.LogicalParent.UseBufferView(src.Data, src.Attribute.PaddedByteLength, BufferMode.ARRAY_BUFFER);

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

            if (buffer.DeviceBufferTarget.HasValue) Guard.IsTrue(buffer.DeviceBufferTarget.Value == BufferMode.ARRAY_BUFFER, nameof(buffer));

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

        #region API

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

        #region Validation

        protected override void OnValidateReferences(Validation.ValidationContext result)
        {
            base.OnValidateReferences(result);

            result.CheckIsDefined("BufferView", _bufferView);
            result.CheckReferenceIndex("BufferView", _bufferView, this.LogicalParent.LogicalBufferViews);
        }

        /// <see href="https://github.com/KhronosGroup/glTF-Validator/blob/master/lib/src/base/accessor.dart"/>
        /// <seealso href="https://github.com/KhronosGroup/glTF-Validator/blob/master/lib/src/data_access/validate_accessors.dart"/>
        protected override void OnValidate(Validation.ValidationContext result)
        {
            base.OnValidate(result);

            // if (_count < _countMinimum) result.AddError(this, $"Count is out of range");
            // if (_byteOffset < 0) result.AddError(this, $"ByteOffset is out of range");

            if (this.Normalized)
            {
                if (this._componentType != EncodingType.UNSIGNED_BYTE && this._componentType != EncodingType.UNSIGNED_SHORT)
                {
                    result.AddDataError(Validation.ErrorCodes.ACCESSOR_NORMALIZED_INVALID);
                }
            }

            if (SourceBufferView.DeviceBufferTarget == BufferMode.ARRAY_BUFFER)
            {
                var len = Encoding.ByteLength() * Dimensions.DimCount();
                result.CheckMultipleOf("Encoding", len, 4);
            }

            if (SourceBufferView.DeviceBufferTarget == BufferMode.ELEMENT_ARRAY_BUFFER)
            {
                if (this.Normalized) result.AddDataError("Normalized", Validation.ErrorCodes.ACCESSOR_NORMALIZED_INVALID);
                if (this.Dimensions != DimensionType.SCALAR) result.AddDataError("Dimensions", Validation.ErrorCodes.VALUE_MULTIPLE_OF, Encoding.ByteLength() * Dimensions.DimCount(), Encoding.ByteLength());
                if (this.Encoding != EncodingType.UNSIGNED_BYTE && this.Encoding != EncodingType.UNSIGNED_INT && this.Encoding != EncodingType.UNSIGNED_SHORT) result.AddDataError("Encoding", Validation.ErrorCodes.VALUE_MULTIPLE_OF, Encoding.ByteLength() * Dimensions.DimCount(), Encoding.ByteLength());

                // if this.Encoding != EncodingType.UNSIGNED_BYTE > warning, no longer valid.
            }
        }

        internal void ValidateBounds(Validation.ValidationContext result)
        {
            result = result.GetContext(this);

            if (_min.Count == 0 && _max.Count == 0) return;

            var dimensions = this.Dimensions.DimCount();

            // if (_min.Count != dimensions) { result.AddError($"min bounds length mismatch; expected {dimensions} but found {_min.Count}"); return; }
            // if (_max.Count != dimensions) { result.AddError($"max bounds length mismatch; expected {dimensions} but found {_max.Count}"); return; }

            for (int i = 0; i < _min.Count; ++i)
            {
                // if (_min[i] > _max[i]) result.AddError(this, $"min[{i}] is larger than max[{i}]");
            }

            if (this.Encoding != EncodingType.FLOAT) return;

            var current = new float[dimensions];
            var minimum = this._min.ConvertAll(item => (float)item);
            var maximum = this._max.ConvertAll(item => (float)item);

            var array = new MultiArray(this.SourceBufferView.Content, this.ByteOffset, this.Count, this.SourceBufferView.ByteStride, dimensions, this.Encoding, false);

            for (int i = 0; i < array.Count; ++i)
            {
                array.CopyItemTo(i, current);

                for (int j = 0; j < current.Length; ++j)
                {
                    var v = current[j];

                    // if (!v._IsFinite()) result.AddError(this, $"Item[{j}][{i}] is not a finite number: {v}");

                    var min = minimum[j];
                    var max = maximum[j];

                    // if (v < min || v > max) result.AddError(this, $"Item[{j}][{i}] is out of bounds. {min} <= {v} <= {max}");
                }
            }
        }

        internal void ValidateIndices(Validation.ValidationContext result, uint vertexCount, PrimitiveType drawingType)
        {
            result = result.GetContext(this);

            SourceBufferView.ValidateBufferUsage(result, BufferMode.ELEMENT_ARRAY_BUFFER);

            if (this.Normalized) result.AddDataError(Validation.ErrorCodes.MESH_PRIMITIVE_INDICES_ACCESSOR_INVALID_FORMAT, this.Normalized, false);

            if (Encoding != EncodingType.UNSIGNED_BYTE &&
                Encoding != EncodingType.UNSIGNED_SHORT &&
                Encoding != EncodingType.UNSIGNED_INT) result.AddDataError(Validation.ErrorCodes.MESH_PRIMITIVE_INDICES_ACCESSOR_INVALID_FORMAT, this.Encoding, EncodingType.UNSIGNED_BYTE, EncodingType.UNSIGNED_SHORT, EncodingType.UNSIGNED_INT);

            if (Dimensions != DimensionType.SCALAR) result.AddDataError(Validation.ErrorCodes.MESH_PRIMITIVE_INDICES_ACCESSOR_INVALID_FORMAT, this.Dimensions, DimensionType.SCALAR);

            uint restart_value = 0xff;
            if (this.Encoding == EncodingType.UNSIGNED_SHORT) restart_value = 0xffff;
            if (this.Encoding == EncodingType.UNSIGNED_INT) restart_value = 0xffffffff;

            var indices = this.AsIndicesArray();

            for (int i = 0; i < indices.Count; ++i)
            {
                result.CheckVertexIndex(i, indices[i], vertexCount, restart_value);
            }
        }

        internal void ValidatePositions(Validation.ValidationContext result)
        {
            result = result.GetContext(this);

            SourceBufferView.ValidateBufferUsage(result, BufferMode.ARRAY_BUFFER);

            var positions = this.AsVector3Array();

            for (int i = 0; i < positions.Count; ++i)
            {
                var pos = positions[i];
                result.CheckDataIsFinite(i, pos);
            }
        }

        internal void ValidateNormals(Validation.ValidationContext result)
        {
            result = result.GetContext(this);

            SourceBufferView.ValidateBufferUsage(result, BufferMode.ARRAY_BUFFER);

            var normals = this.AsVector3Array();

            for (int i = 0; i < normals.Count; ++i)
            {
                var nrm = normals[i];
                result.CheckDataIsFinite(i, nrm);
                result.CheckDataIsUnitLength(i, nrm);
            }
        }

        internal void ValidateTangents(Validation.ValidationContext result)
        {
            result = result.GetContext(this);

            SourceBufferView.ValidateBufferUsage(result, BufferMode.ARRAY_BUFFER);

            var tangents = this.AsVector4Array();

            for (int i = 0; i < tangents.Count; ++i)
            {
                var tgt = tangents[i];

                result.CheckDataIsFinite(i, tgt);
                result.CheckDataIsUnitLength(i, new Vector3(tgt.X, tgt.Y, tgt.Z));
                result.CheckDataIsValidSign(i, tgt.W);
            }
        }

        internal void ValidateJoints(Validation.ValidationContext result, int jwset, int jointsCount)
        {
            result = result.GetContext(this);

            SourceBufferView.ValidateBufferUsage(result, BufferMode.ARRAY_BUFFER);

            var joints = this.AsVector4Array();

            for (int i = 0; i < joints.Count; ++i)
            {
                var jjjj = joints[i];
                result.CheckDataIsFinite(i, jjjj);
                result.CheckDataIsInRange(i, jjjj, 0, jointsCount-1);
            }
        }

        internal void ValidateWeights(Validation.ValidationContext result, int jwset)
        {
            result = result.GetContext(this);

            SourceBufferView.ValidateBufferUsage(result, BufferMode.ARRAY_BUFFER);

            var weights = this.AsVector4Array();

            for (int i = 0; i < weights.Count; ++i)
            {
                var wwww = weights[i];
                result.CheckDataIsFinite(i, wwww);
                result.CheckDataIsInRange(i, wwww, 0, 1);

                // theoretically, the sum of all the weights should give 1, ASSUMING there's only one weight set.
                // but in practice, that seems not to be true.
            }
        }

        internal void ValidateMatrices(Validation.ValidationContext result)
        {
            result = result.GetContext(this);

            // if (SourceBufferView.DeviceBufferTarget != null)  

            var matrices = this.AsMatrix4x4Array();

            for (int i = 0; i < matrices.Count; ++i)
            {
                var m = matrices[i];

                if (Matrix4x4.Invert(m, out Matrix4x4 r)) continue;
                result.AddDataError(Validation.ErrorCodes.ACCESSOR_INDECOMPOSABLE_MATRIX, i);
            }
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
