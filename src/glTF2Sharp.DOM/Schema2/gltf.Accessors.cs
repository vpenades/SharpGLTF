using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Schema2
{
    using ROOT = ModelRoot;

    // https://github.com/KhronosGroup/glTF/issues/827#issuecomment-277537204

    [System.Diagnostics.DebuggerDisplay("Accessor[{LogicalIndex}] BufferView[{Buffer.LogicalIndex}][{ByteOffset}...] => 0 => {Dimensions}x{Encoding}x{Normalized} => [{Count}]")]
    public partial class Accessor
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

        public int LogicalIndex                 => this.LogicalParent.LogicalAccessors.IndexOfReference(this);
        
        internal int _LogicalBufferViewIndex    => this._bufferView.AsValue(-1);

        public BufferView SourceBufferView      => this._bufferView.HasValue ? this.LogicalParent.LogicalBufferViews[this._bufferView.Value] : null;

        public int Count                        => this._count;

        public int ByteOffset                   => this._byteOffset.AsValue(0);

        public ElementType Dimensions           => this._type;

        public ComponentType Encoding           => this._componentType;

        public Boolean Normalized               => this._normalized.AsValue(false);

        public Boolean IsSparse                 => this._sparse != null;

        public int ItemByteSize                 => Encoding.ByteLength() * Dimensions.DimCount();        

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

        #region Data Buffer API

        internal void SetData(BufferView buffer, int byteOffset, ElementType dimensions, ComponentType encoding, Boolean normalized, int count)
        {
            Guard.NotNull(buffer, nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));

            Guard.MustBeGreaterThanOrEqualTo(byteOffset, 0, nameof(byteOffset));
            Guard.MustBeGreaterThan(count, 0, nameof(count));

            this._bufferView = buffer.LogicalIndex;
            this._byteOffset = byteOffset;
            this._count = count;

            this._type = dimensions;
            this._componentType = encoding;
            this._normalized = normalized.AsNullable(false);            

            _UpdateBounds();
        }

        public Memory.Matrix4x4Accessor CastToMatrix4x4Array()
        {
            Guard.IsTrue(this.Dimensions == ElementType.MAT4, nameof(Dimensions));

            return SourceBufferView.CreateMatrix4x4Decoder(this.ByteOffset, this._componentType, this.Normalized);
        }

        #endregion

        #region Index Buffer API

        public void SetIndexData(BufferView buffer, int byteOffset, IndexType encoding, int count)
        {
            Guard.NotNull(buffer,nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer,nameof(buffer));
            if (buffer.DeviceBufferTarget.HasValue) Guard.IsTrue(buffer.DeviceBufferTarget.Value == BufferMode.ELEMENT_ARRAY_BUFFER, nameof(buffer));

            Guard.MustBeGreaterThanOrEqualTo(byteOffset, 0, nameof(byteOffset));
            Guard.MustBeGreaterThan(count, 0, nameof(count));

            this._bufferView = buffer.LogicalIndex;
            this._byteOffset = byteOffset;
            this._count = count;

            this._type = ElementType.SCALAR;
            this._componentType = encoding.ToComponent();            
            this._normalized = null;            

            _UpdateBounds();
        }

        public UInt32[] TryGetIndices()
        {
            Guard.IsTrue(this.Dimensions == ElementType.SCALAR, nameof(Dimensions));            

            return SourceBufferView
                .CreateIndexDecoder(this.ByteOffset, this.Encoding.ToIndex())
                .ToArray();
        }

        #endregion

        #region Vertex Buffer API

        public void SetVertexData(BufferView buffer, int byteOffset, ElementType dimensions, ComponentType encoding, Boolean normalized, int count)
        {
            Guard.NotNull(buffer,nameof(buffer));
            Guard.MustShareLogicalParent(this, buffer, nameof(buffer));
            if (buffer.DeviceBufferTarget.HasValue) Guard.IsTrue(buffer.DeviceBufferTarget.Value == BufferMode.ARRAY_BUFFER, nameof(buffer));

            Guard.MustBeGreaterThanOrEqualTo(byteOffset, 0, nameof(byteOffset));
            Guard.MustBeGreaterThan(count, 0, nameof(count));

            this._bufferView = buffer.LogicalIndex;
            this._byteOffset = byteOffset;
            this._count = count;

            this._type = dimensions;
            this._componentType = encoding;
            this._normalized = normalized.AsNullable(false);

            _UpdateBounds();
        }

        public ArraySegment<Byte> TryGetVertexBytes(int vertexIdx)
        {
            if (_sparse != null) throw new InvalidOperationException("Can't be used on Acessors with Sparse Data");

            var byteSize = Encoding.ByteLength() * Dimensions.DimCount();
            var byteStride = Math.Max(byteSize, SourceBufferView.ByteStride);
            var byteOffset = vertexIdx * byteStride;

            return SourceBufferView.Data.GetSegment(this.ByteOffset + vertexIdx * byteStride, byteSize);
        }

        internal void _UpdateBounds()
        {
            var count = this._type.DimCount();

            var bounds = new double[count];
            this._min.Clear();
            this._min.AddRange(bounds);
            this._max.Clear();
            this._max.AddRange(bounds);

            this._min.Fill(double.MaxValue);
            this._max.Fill(double.MinValue);

            if (count == 1)
            {
                var accessor = SourceBufferView.CreateScalarDecoder(this.ByteOffset, this.Encoding, this.Normalized);

                var minmax = Memory.AccessorsUtils.GetBounds(accessor);

                this._min[0] = minmax.Item1;
                this._max[0] = minmax.Item2;
            }

            if (count == 2)
            {
                var accessor = SourceBufferView.CreateVector2Decoder(this.ByteOffset, this.Encoding, this.Normalized);

                for (int i = 0; i < Count; ++i)
                {
                    var v2 = accessor[i];

                    if (v2.X < this._min[0]) this._min[0] = v2.X;
                    if (v2.X > this._max[0]) this._max[0] = v2.X;
                    if (v2.Y < this._min[1]) this._min[1] = v2.Y;
                    if (v2.Y > this._max[1]) this._max[1] = v2.Y;
                }
            }

            if (count == 3)
            {
                var accessor = SourceBufferView.CreateVector3Decoder(this.ByteOffset, this.Encoding, this.Normalized);

                for (int i = 0; i < Count; ++i)
                {
                    var v3 = accessor[i];

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
                var accessor = SourceBufferView.CreateVector4Decoder(this.ByteOffset, this.Encoding, this.Normalized);

                for (int i = 0; i < Count; ++i)
                {
                    var v4 = accessor[i];

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

            if (SourceBufferView.DeviceBufferTarget == BufferMode.ARRAY_BUFFER)
            {
                var len = Encoding.ByteLength() * Dimensions.DimCount();
                if ((len & 3) != 0) exxx.Add(new ModelException(this, $"Expected length to be multiple of 4, found {len}"));
            }

            if (SourceBufferView.DeviceBufferTarget == BufferMode.ELEMENT_ARRAY_BUFFER)
            {
                var len = Encoding.ByteLength() * Dimensions.DimCount();
                if (len != 1 && len != 2 && len != 4) exxx.Add(new ModelException(this, $"Expected length to be 1, 2 or 4, found {len}"));
            }

            // validate bounds

            if (_min.Count != _max.Count) { exxx.Add(new ModelException(this, "min and max length mismatch")); return exxx; }

            for (int i = 0; i < _min.Count; ++i)
            {
                if (_min[i] > _max[i]) exxx.Add(new ModelException(this, $"min[{i}] is larger than max[{i}]"));
            }

            return exxx;
        }

        #endregion
    }   

    public partial class ModelRoot
    {
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
