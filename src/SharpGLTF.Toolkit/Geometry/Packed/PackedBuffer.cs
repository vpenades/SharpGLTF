using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpGLTF.Memory;

using BYTES = System.ArraySegment<byte>;

namespace SharpGLTF.Geometry
{
    class PackedBuffer
    {
        private readonly List<MemoryAccessor> _Accessors = new List<MemoryAccessor>();

        protected int? ByteStride
        {
            get
            {
                if (_Accessors.Count == 0) return null;

                return _Accessors[0].Attribute.StepByteLength;
            }
        }

        public void AddAccessors(params MemoryAccessor[] accessors)
        {
            foreach (var a in accessors)
            {
                if (a == null) continue;

                // ensure that all accessors have the same byte stride
                if (this.ByteStride.HasValue)
                {
                    var astride = a.Attribute.StepByteLength;
                    Guard.IsTrue(this.ByteStride.Value == astride, nameof(accessors));
                }

                _Accessors.Add(a);
            }
        }

        public void MergeBuffers()
        {
            if (_Accessors.Count == 0) return;

            var srcBuffers = _Accessors
                .Select(item => item.Data)
                .Distinct()
                .OrderByDescending(item => item.Count)
                .ToList();

            var array = new Byte[srcBuffers.Sum(item => item.Count)];

            int offset = 0;

            var dstOffsets = new Dictionary<BYTES, int>();

            foreach (var src in srcBuffers)
            {
                // find src in array

                dstOffsets[src] = offset;

                src.CopyTo(0, array, offset, src.Count);
                offset += src.Count;
            }

            var dstBuffer = new BYTES(array);

            foreach (var a in _Accessors)
            {
                offset = dstOffsets[a.Data];

                var attribute = a.Attribute;
                attribute.ByteOffset += offset;

                a.Update(dstBuffer, attribute);
            }
        }
    }
}
