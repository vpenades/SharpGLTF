using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGLTF.Schema2.LoadAndSave
{
    /// <summary>
    /// Utility stream used to simulate streams that don't support <see cref="CanSeek"/> nor <see cref="Length"/>
    /// </summary>
    internal class ReadOnlyTestStream : System.IO.Stream
    {
        public ReadOnlyTestStream(Byte[] data)
        {
            _Data = data;
        }

        private readonly Byte[] _Data;
        private int _Position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_Position >= _Data.Length) return 0;

            if (count > 1) count /= 2; // simulate partial reads
            
            var bytesLeft = _Data.Length - _Position;

            count = Math.Min(count, bytesLeft);

            var dst = buffer.AsSpan().Slice(offset, count);
            var src = _Data.AsSpan().Slice(_Position, count);

            src.CopyTo(dst);

            _Position += count;

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
