using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Schema2
{
    using ROOT = ModelRoot;

    // https://github.com/KhronosGroup/glTF/issues/827#issuecomment-277537204

    [System.Diagnostics.DebuggerDisplay("Accessor[{LogicalIndex}] BufferView[{Buffer.LogicalIndex}][{ByteOffset}...] => {Dimension}_{Packing}[{_count}]")]
    public partial class Accessor
    {
        #region debug

        internal string _DebuggerDisplay_TryIdentifyContent()
        {
            return $"{Element}_{Component}[{_count}]";
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

        public int LogicalIndex => this.LogicalParent._LogicalAccessors.IndexOfReference(this);

        internal int _LogicalBufferViewIndex => this._bufferView ?? -1; // todo: clarify behaviour when BufferView is not define.

        public BufferView Buffer => this._bufferView.HasValue ? this.LogicalParent.LogicalBufferViews[this._bufferView.Value] : null;

        public int Count => this._count;

        public int ByteOffset => _byteOffset ?? 0;        

        public ElementType Element => this._type;        

        public ComponentType Component => this._componentType;

        public Boolean Normalized => this._normalized ?? false;        

        public int ItemByteSize => Component.ByteLength() * Element.Length();

        public bool IsSparse => _sparse != null;

        public BoundingBox3? LocalBounds3
        {
            get
            {
                if (this._min.Count != 3) return null;
                if (this._max.Count != 3) return null;

                return new BoundingBox3(this._min, this._max);
            }
        }

        #endregion

        #region API

        internal void SetDataBuffer(BufferView buffer, int byteOffset, ComponentType ct, ElementType et, int count)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));            

            Guard.MustBeGreaterThanOrEqualTo(byteOffset, 0, nameof(byteOffset));
            Guard.MustBeGreaterThan(count, 0, nameof(count));

            this._bufferView = buffer.LogicalIndex;
            this._componentType = ct;
            this._normalized = false;
            this._type = et;
            this._byteOffset = byteOffset;
            this._count = count;

            _UpdateBounds(this._type.Length());
        }

        internal void SetIndexBuffer(BufferView buffer, IndexType type, int byteOffset, int count)
        {
            Guard.NotNull(buffer,nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer,nameof(buffer));
            Guard.MustBeGreaterThanOrEqualTo(byteOffset, 0, nameof(byteOffset));
            Guard.MustBeGreaterThan(count, 0, nameof(count));

            this._bufferView = buffer.LogicalIndex;
            this._componentType = type.ToComponent();
            this._type = ElementType.SCALAR;
            this._normalized = null;
            this._byteOffset = byteOffset;
            this._count = count;

            _UpdateBounds(1);
        }
        
        internal void SetVertexBuffer(BufferView buffer, ComponentType ctype, ElementType etype, bool? normalized, int byteOffset, int count)
        {
            Guard.NotNull(buffer,nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));

            Guard.MustBeGreaterThanOrEqualTo(byteOffset, 0, nameof(byteOffset));
            Guard.MustBeGreaterThan(count, 0, nameof(count));

            this._bufferView = buffer.LogicalIndex;
            this._componentType = ctype;
            this._normalized = normalized;
            this._type = etype;            
            this._byteOffset = byteOffset;
            this._count = count;

            _UpdateBounds(this._type.Length());
        }

        public ArraySegment<Byte> GetVertexBytes(int vertexIdx)
        {
            if (_sparse != null) throw new InvalidOperationException("Can't be used on Acessors with Sparse Data");

            var byteSize = Component.ByteLength() * Element.Length();
            var byteStride = Math.Max(byteSize, Buffer.ByteStride);
            var byteOffset = vertexIdx * byteStride;

            return Buffer.Data.GetSegment(this.ByteOffset + vertexIdx * byteStride, byteSize);
        }

        public IReadOnlyList<ArraySegment<Byte>> GetVertices()
        {
            throw new NotImplementedException();

            // if sparse exist, create a dictionary with the indices
        }

        /*
        public Byte[] GetColumnBytes()
        {
            var dstStride = Component.ByteLength() * Element.Length();
            var srcStride = Math.Max(dstStride, Buffer.ByteStride);                

            var srcData = Buffer.Data.GetSegment(this.ByteOffset, srcStride * Count);
            var dstData = new Byte[dstStride * Count];

            for(int i=0; i < Count; ++i)
            {
                srcData.CopyTo(i * srcStride, dstData, i * dstStride, dstStride);
            }

            if (this._sparse != null)
            {
                this._sparse.CopyTo(this, dstData, dstStride);
            }

            return dstData;
        }*/

        public Int32[] TryGetIndices()
        {
            if (this._type != ElementType.SCALAR) throw new InvalidOperationException();

            var decoder = Buffer.CreateIndexDecoder(this.Component.ToIndex(), this.ByteOffset);

            return _TryGetColumn<int>(decoder);            
        }

        
        public T[] TryGetAttribute<T>() where T:struct
        {
            switch(this._type)
            {
                case ElementType.SCALAR:
                    {
                        var decoder = Buffer.CreateScalarDecoder(this._componentType, this.ByteOffset);
                        return (T[])(Array)_TryGetColumn(decoder);
                    }

                case ElementType.VEC2:
                    {
                        var decoder = Buffer.CreateVector2Decoder(this._componentType, this.Normalized, this.ByteOffset);
                        return (T[])(Array)_TryGetColumn(decoder);
                    }

                case ElementType.VEC3:
                    {
                        var decoder = Buffer.CreateVector3Decoder(this._componentType, this.Normalized, this.ByteOffset);

                        // this is a special case because although Tangets
                        // are expected to be Vector4, sometimes are found as Vector3
                        // Bug found in AnimatedMorphCube

                        if (typeof(T) == typeof(Vector3)) return (T[])(Array)_TryGetColumn(decoder);
                        if (typeof(T) == typeof(Vector4)) return (T[])(Array)_TryGetColumn(decoder).Select(item => new Vector4(item, 0)).ToArray();

                        throw new NotImplementedException();
                    }

                case ElementType.VEC4:
                    {
                        var decoder = Buffer.CreateVector4Decoder(this._componentType, this.Normalized, this.ByteOffset);
                        return (T[])(Array)_TryGetColumn(decoder);
                    }

                case ElementType.MAT4:
                    {
                        var decoder = Buffer.CreateMatrix4x4Decoder(this._componentType, this.Normalized, this.ByteOffset);
                        return (T[])(Array)_TryGetColumn(decoder);
                    }

                default: throw new NotImplementedException();
            }
        }

        
        private T[] _TryGetColumn<T>(Func<int, T> decoder) where T:struct
        {
            var dstBuff = new T[_count];

            for (int i = 0; i < dstBuff.Length; ++i)
            {
                dstBuff[i] = decoder.Invoke(i);
            }
            
            if (this._sparse != null)
            {
                this._sparse.CopyTo(this, dstBuff);
            }

            return dstBuff;
        }

        internal void _UpdateBounds(int count)
        {
            var bounds = new double[count];
            this._min.Clear();
            this._min.AddRange(bounds);
            this._max.Clear();
            this._max.AddRange(bounds);

            this._min.Fill(double.MaxValue);
            this._max.Fill(double.MinValue);

            if (count == 1)
            {
                var decoder = Buffer.CreateScalarDecoder(this.Component, this.ByteOffset);

                for (int i = 0; i < Count; ++i)
                {
                    var scalar = decoder.Invoke(i);
                    
                    if (scalar < this._min[0]) this._min[0] = scalar;
                    if (scalar > this._max[0]) this._max[0] = scalar;                    
                }
            }

            if (count == 2)
            {
                var decoder = Buffer.CreateVector2Decoder(this.Component, this.Normalized, this.ByteOffset);

                for (int i = 0; i < Count; ++i)
                {
                    var v2 = decoder.Invoke(i);

                    if (v2.X < this._min[0]) this._min[0] = v2.X;
                    if (v2.X > this._max[0]) this._max[0] = v2.X;
                    if (v2.Y < this._min[1]) this._min[1] = v2.Y;
                    if (v2.Y > this._max[1]) this._max[1] = v2.Y;
                }
            }

            if (count == 3)
            {
                var decoder = Buffer.CreateVector3Decoder(this.Component, this.Normalized, this.ByteOffset);

                for (int i = 0; i < Count; ++i)
                {
                    var v3 = decoder.Invoke(i);

                    if (v3.X < this._min[0]) this._min[0] = v3.X;
                    if (v3.X > this._max[0]) this._max[0] = v3.X;
                    if (v3.Y < this._min[1]) this._min[1] = v3.Y;
                    if (v3.Y > this._max[1]) this._max[1] = v3.Y;
                    if (v3.Z < this._min[2]) this._min[2] = v3.Z;
                    if (v3.Z > this._max[2]) this._max[2] = v3.Z;
                }
            }

            if (count == 4)
            {
                var decoder = Buffer.CreateVector4Decoder(this.Component, this.Normalized, this.ByteOffset);

                for (int i = 0; i < Count; ++i)
                {
                    var v4 = decoder.Invoke(i);

                    if (v4.X < this._min[0]) this._min[0] = v4.X;
                    if (v4.X > this._max[0]) this._max[0] = v4.X;
                    if (v4.Y < this._min[1]) this._min[1] = v4.Y;
                    if (v4.Y > this._max[1]) this._max[1] = v4.Y;
                    if (v4.Z < this._min[2]) this._min[2] = v4.Z;
                    if (v4.Z > this._max[2]) this._max[2] = v4.Z;
                    if (v4.W < this._min[3]) this._min[3] = v4.W;
                    if (v4.W > this._max[3]) this._max[3] = v4.W;
                }
            }

            if (count > 4)
            {
                this._min.Clear();
                this._max.Clear();
            }
        }

        public override IEnumerable<Exception> Validate()
        {
            var exxx = base.Validate().ToList();

            if (!_bufferView.HasValue) { exxx.Add(new ModelException(this, $"BufferView index missing")); return exxx; }
            if (_bufferView < 0 || _bufferView >= LogicalParent.LogicalBufferViews.Count) exxx.Add(new ModelException(this, $"BufferView index out of range"));

            if (_count < 0) exxx.Add(new ModelException(this, $"Count is out of range"));
            if (_byteOffset < 0) exxx.Add(new ModelException(this, $"ByteOffset is out of range"));

            if (Buffer.DeviceBufferTarget == BufferMode.ARRAY_BUFFER)
            {
                var len = Component.ByteLength() * Element.Length();
                if ((len & 3) != 0) exxx.Add(new ModelException(this, $"Expected length to be multiple of 4, found {len}"));
            }

            if (Buffer.DeviceBufferTarget == BufferMode.ELEMENT_ARRAY_BUFFER)
            {
                var len = Component.ByteLength() * Element.Length();
                if (len != 1 && len != 2 && len != 4) exxx.Add(new ModelException(this, $"Expected length to be 1, 2 or 4, found {len}"));
            }

            // validate bounds

            if (_min.Count != _max.Count) { exxx.Add(new ModelException(this, "min and max length mismatch")); return exxx; }

            for(int i=0; i < _min.Count; ++i)
            {
                if (_min[i] > _max[i]) exxx.Add(new ModelException(this, $"min[{i}] is larger than max[{i}]"));
            }

            return exxx;
        }

        #endregion
    }

    public partial class AccessorSparse
    {
        
        public void CopyTo(Accessor srcAccessor, Byte[] dstBuffer, int dstStride)
        {
            if (this._count == 0) return;

            var idxDecoder = this._indices.GetDecoder(srcAccessor.LogicalParent);
            var valCopier = this._values.CopyTo(srcAccessor.LogicalParent, srcAccessor.Element, srcAccessor.Component);

            for (int i = 0; i < this._count; ++i)
            {
                var key = idxDecoder.Invoke(i);

                valCopier(i, dstBuffer, dstStride, key);                
            }
        }

        public void CopyTo<T>(Accessor srcAccessor, T[] dstBuffer) where T:struct
        {
            if (this._count == 0) return;

            var idxDecoder = this._indices.GetDecoder(srcAccessor.LogicalParent);
            var valDecoder = this._values.GetDecoder<T>(srcAccessor.LogicalParent, srcAccessor.Element, srcAccessor.Component, srcAccessor.Normalized);

            for (int i = 0; i < this._count; ++i)
            {
                var key = idxDecoder.Invoke(i);
                var val = valDecoder.Invoke(i);
                dstBuffer[key] = val;
            }
        }
    }

    public partial class AccessorSparseIndices
    {
        
        public Func<int, int> GetDecoder(ROOT root)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];

            return srcBuffer.CreateIndexDecoder(this._componentType, this._byteOffset ?? 0);
        }

        public IReadOnlyList<int> GetIndices(ROOT root, int count)
        {
            var srcDecoder = GetDecoder(root);

            var indices = new int[count];

            for(int i=0; i < indices.Length; ++i)
            {
                indices[i] = srcDecoder(i);
            }

            return indices;
        }
    }

    public partial class AccessorSparseValues
    {
        
        public Func<int, T> GetDecoder<T>(ROOT root, ElementType et, ComponentType ct, bool normalized) where T:struct
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];

            var decoder = srcBuffer.CreateValueDecoder(et, ct, normalized, this._byteOffset ?? 0);

            return idx => (T)decoder(idx);
        }

        public Action<int,IList<Byte>,int, int> CopyTo(ROOT root, ElementType et, ComponentType ct)
        {
            var srcBuffer = root.LogicalBufferViews[this._bufferView];

            var itemLen = et.Length() * ct.ByteLength();
            var srcStride = Math.Max(srcBuffer.ByteStride, itemLen);

            return (srcIdx, dstBuff, dstStride, dstIdx) =>
            {                
                srcBuffer.Data.CopyTo(srcStride * srcIdx, dstBuff, dstStride * dstIdx, itemLen);
            };
            
        }
    }

    /*
    public partial class ModelRoot
    {
        internal Accessor _CreateVertexAccessor(BufferView buffer, Runtime.AttributeInfo desc, int extraByteOffset, int count)
        {
            var accessor = new Accessor();

            _accessors.Add(accessor);

            accessor.SetVertexBuffer(buffer, desc, extraByteOffset, count);

            return accessor;
        }

        internal Accessor _CreateIndexAccessor(BufferView buffer, Runtime.AttributeInfo desc, int extraByteOffset, int count)
        {
            if (desc.Dimension != Runtime.Encoding.DimensionType.Scalar) throw new ArgumentException(nameof(desc));

            var accessor = new Accessor();

            _accessors.Add(accessor);

            accessor.SetIndexBuffer(buffer, desc.Packed.ToSchema2().ToIndex(), extraByteOffset, count);

            return accessor;
        }

        internal Accessor _CreateDataAccessor(Byte[] data, Runtime.Encoding.DimensionType dtype, int count)
        {
            var buffer = new Buffer(data);
            _buffers.Add(buffer);

            var bufferView = CreateBufferView(buffer, data.Length, null, null, null);

            var accessor = new Accessor();

            _accessors.Add(accessor);

            accessor.SetDataBuffer(bufferView, 0, ComponentType.FLOAT, dtype.ToSchema2(), count);

            return accessor;
        }
    }*/

   
}
