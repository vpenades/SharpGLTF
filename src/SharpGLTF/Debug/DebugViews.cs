using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Debug
{    
    internal sealed class _CollectionDebugProxy<T>
    {
        // https://referencesource.microsoft.com/#mscorlib/system/collections/generic/debugview.cs,29

        public _CollectionDebugProxy(ICollection<T> collection)
        {
            _Collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        private readonly ICollection<T> _Collection;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] items = new T[_Collection.Count];
                _Collection.CopyTo(items, 0);
                return items;
            }
        }
    }

    internal sealed class _BufferDebugProxy
    {
        public _BufferDebugProxy(Schema2.BufferView value) { _Value = value; }

        public int LogicalIndex => _Value.LogicalParent.LogicalBufferViews.IndexOfReference(_Value);

        private readonly Schema2.BufferView _Value;

        public int ByteStride => _Value.ByteStride;

        public int ByteLength => _Value.Content.Count;

        public Schema2.BufferMode? DeviceBufferTarget => _Value.DeviceBufferTarget;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public Schema2.Accessor[] Accessors => _Value.FindAccessors().ToArray();
    }

    internal sealed class _AccessorDebugProxy
    {
        public _AccessorDebugProxy(Schema2.Accessor value) { _Value = value; }

        private readonly Schema2.Accessor _Value;

        public String Identity => $"Accessor[{_Value.LogicalIndex}] {_Value.Name}";

        public Schema2.BufferView Source => _Value.SourceBufferView;

        public (Schema2.ElementType, Schema2.ComponentType, bool) Format => (_Value.Dimensions, _Value.Encoding, _Value.Normalized);

        public Object[] Items
        {
            get
            {
                if (_Value.Dimensions == Schema2.ElementType.SCALAR) return _Value.AsScalarArray().Cast<Object>().ToArray();
                if (_Value.Dimensions == Schema2.ElementType.VEC2) return _Value.AsVector2Array().Cast<Object>().ToArray();
                if (_Value.Dimensions == Schema2.ElementType.VEC3) return _Value.AsVector3Array().Cast<Object>().ToArray();
                if (_Value.Dimensions == Schema2.ElementType.VEC4) return _Value.AsVector4Array().Cast<Object>().ToArray();
                if (_Value.Dimensions == Schema2.ElementType.MAT4) return _Value.AsMatrix4x4Array().Cast<Object>().ToArray();

                var itemSize = _Value.ItemByteSize;
                var byteStride = Math.Max(_Value.SourceBufferView.ByteStride, itemSize);
                var items = new ArraySegment<Byte>[_Value.Count];

                var buffer = _Value.SourceBufferView.Content.Slice(_Value.ByteOffset, _Value.Count * byteStride);

                for (int i = 0; i < items.Length; ++i )
                {
                    items[i] = buffer.Slice(i * byteStride, itemSize);
                }

                return items.Cast<Object>().ToArray();
            }
        }
    }
}
