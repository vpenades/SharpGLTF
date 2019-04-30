using System;
using System.Collections.Generic;
using System.Numerics;

using SharpGLTF.Memory;

namespace SharpGLTF.Schema2
{
    // https://github.com/KhronosGroup/glTF/issues/827#issuecomment-277537204

    [System.Diagnostics.DebuggerDisplay("Accessor[{LogicalIndex}] BufferView[{SourceBufferView.LogicalIndex}][{ByteOffset}...] => 0 => {Dimensions}x{Encoding}x{Normalized} => [{Count}]")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._AccessorDebugProxy))]
    public sealed partial class Accessor
    {
        #region debug

        internal string _DebuggerDisplay_TryIdentifyContent()
        {
            return $"{Dimensions}_{Encoding}[{_count}]";
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
            var bv = this.LogicalParent.UseBufferView(src.Data, src.Attribute.ByteStride, BufferMode.ARRAY_BUFFER);

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

        public IList<Vector4> AsColorArray()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsColorArray();

            var sparseKV = this._sparse._CreateMemoryAccessors(this);
            return MemoryAccessor.CreateColorSparseArray(memory, sparseKV.Key, sparseKV.Value);
        }

        public IList<Quaternion> AsQuaternionArray()
        {
            var memory = _GetMemoryAccessor();

            if (this._sparse == null) return memory.AsQuaternionArray();

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

        internal override void Validate(Validation.ValidationContext result)
        {
            base.Validate(result);

            if (!_bufferView.HasValue) { result.AddError(this, $"BufferView index missing"); return; }
            if (_bufferView < 0 || _bufferView >= LogicalParent.LogicalBufferViews.Count) result.AddError(this, $"BufferView index out of range");

            if (_count < _countMinimum) result.AddError(this, $"Count is out of range");
            if (_byteOffset < 0) result.AddError(this, $"ByteOffset is out of range");

            if (SourceBufferView.DeviceBufferTarget == BufferMode.ARRAY_BUFFER)
            {
                var len = Encoding.ByteLength() * Dimensions.DimCount();
                if (len > 0 && (len & 3) != 0) result.AddError(this, $"Expected length to be multiple of 4, found {len}");
            }

            if (SourceBufferView.DeviceBufferTarget == BufferMode.ELEMENT_ARRAY_BUFFER)
            {
                var len = Encoding.ByteLength() * Dimensions.DimCount();
                if (len != 1 && len != 2 && len != 4) result.AddError(this, $"Expected length to be 1, 2 or 4, found {len}");
            }
        }

        internal void ValidateBounds(Validation.ValidationContext result)
        {
            if (_min.Count == 0 && _max.Count == 0) return;

            var dimensions = this.Dimensions.DimCount();

            if (_min.Count != dimensions) { result.AddError(this, $"min bounds length mismatch; expected {dimensions} but found {_min.Count}"); return; }
            if (_max.Count != dimensions) { result.AddError(this, $"max bounds length mismatch; expected {dimensions} but found {_max.Count}"); return; }

            for (int i = 0; i < _min.Count; ++i)
            {
                if (_min[i] > _max[i]) result.AddError(this, $"min[{i}] is larger than max[{i}]");
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

                    if (!v._IsReal()) result.AddError(this, $"Item[{j}][{i}] is not a finite number: {v}");

                    var min = minimum[j];
                    var max = maximum[j];

                    if (v < min || v > max) result.AddError(this, $"Item[{j}][{i}] is out of bounds. {min} <= {v} <= {max}");
                }
            }
        }

        internal void ValidateIndices(Validation.ValidationContext result, uint vertexCount, PrimitiveType drawingType)
        {
            switch (drawingType)
            {
                case PrimitiveType.LINE_LOOP:
                case PrimitiveType.LINE_STRIP:
                    if (this.Count < 2) result.AddError(this, $"Indices count {this.Count} is less than 2");
                    break;

                case PrimitiveType.TRIANGLE_FAN:
                case PrimitiveType.TRIANGLE_STRIP:
                    if (this.Count < 3) result.AddError(this, $"Indices count {this.Count} is less than 3");
                    break;

                case PrimitiveType.LINES:
                    if (!this.Count.IsMultipleOf(2)) result.AddError(this, $"Indices count {this.Count} incompatible with Primitive.{drawingType}");
                    break;

                case PrimitiveType.TRIANGLES:
                    if (!this.Count.IsMultipleOf(3)) result.AddError(this, $"Indices count {this.Count} incompatible with Primitive.{drawingType}");
                    break;
            }

            uint restart_value = 0xff;
            if (this.Encoding == EncodingType.UNSIGNED_SHORT) restart_value = 0xffff;
            if (this.Encoding == EncodingType.UNSIGNED_INT) restart_value = 0xffffffff;

            var indices = this.AsIndicesArray();

            for (int i = 0; i < indices.Count; ++i)
            {
                var idx = indices[i];

                if (idx == restart_value) result.AddError(this, $"Index[{i}] value {idx} is invalid PRIMITIVE RESTART value");
                else if (idx >= vertexCount) result.AddError(this, $"Index[{i}] value {idx} is out of range {0}-{vertexCount}");
            }
        }

        internal void ValidatePositions(Validation.ValidationContext result)
        {
            var positions = this.AsVector3Array();

            for (int i = 0; i < positions.Count; ++i)
            {
                var pos = positions[i];

                if (!pos._IsReal()) result.AddError(this, $"POSITION[{i}] value {pos} has non finite values");
            }
        }

        internal void ValidateNormals(Validation.ValidationContext result)
        {
            var normals = this.AsVector3Array();

            for (int i = 0; i < normals.Count; ++i)
            {
                var nrm = normals[i];

                if (!nrm.IsValidNormal()) result.AddError(this, $"NORMAL[{i}] value {nrm} is invalid");
            }
        }

        internal void ValidateTangents(Validation.ValidationContext result)
        {
            var tangents = this.AsVector4Array();

            for (int i = 0; i < tangents.Count; ++i)
            {
                var tgt = tangents[i];

                if (!tgt._IsReal()) result.AddError(this, $"TANGENT[{i}] value {tgt} has non finite values");

                var len = new Vector3(tgt.X, tgt.Y, tgt.Z).Length();

                if (len < 0.99f || len > 1.01f) result.AddError(this, $"TANGENT[{i}] length {len} is not unit length");

                if (tgt.W != 1 && tgt.W != -1) result.AddError(this, $"TANGENT[{i}].W {tgt.W} has invalid value");
            }
        }

        internal void ValidateJoints(Validation.ValidationContext result, int jwset, int jointsCount)
        {
            var jj = this.AsVector4Array();

            void _CheckJoint(Validation.ValidationContext r, float v, int idx, string n)
            {
                if (!v._IsReal()) result.AddError(this, $"JOINTS_{jwset}[{idx}].{n} value {v} is not finite");
                if ((v % 1) != 0) result.AddError(this, $"JOINTS_{jwset}[{idx}].{n} value {v} should be a round value");
                if (v < 0 || v >= jointsCount) result.AddError(this, $"JOINTS_{jwset}[{idx}].{n} value {v} is out of range 0-{jointsCount}");
            }

            for (int i = 0; i < jj.Count; ++i)
            {
                var jjjj = jj[i];
                _CheckJoint(result, jjjj.X, i, "X");
                _CheckJoint(result, jjjj.Y, i, "Y");
                _CheckJoint(result, jjjj.Z, i, "Z");
                _CheckJoint(result, jjjj.W, i, "W");
            }
        }

        internal void ValidateWeights(Validation.ValidationContext result, int jwset)
        {
            var ww = this.AsVector4Array();

            void _CheckWeight(Validation.ValidationContext r, float v, int idx, string n)
            {
                if (!v._IsReal()) result.AddError(this, $"WEIGHTS_{jwset}[{idx}].{n} value {v} is not finite");
                if (v < 0 || v > 1) result.AddError(this, $"WEIGHTS_{jwset}[{idx}].{n} value {v} is out of range 0-1");
            }

            for (int i = 0; i < ww.Count; ++i)
            {
                var wwww = ww[i];
                _CheckWeight(result, wwww.X, i, "X");
                _CheckWeight(result, wwww.Y, i, "Y");
                _CheckWeight(result, wwww.Z, i, "Z");
                _CheckWeight(result, wwww.W, i, "W");

                // theoretically, the sum of all the weights should give 1, ASSUMING there's only one weight set.
                // but in practice, that seems not to be true.
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
