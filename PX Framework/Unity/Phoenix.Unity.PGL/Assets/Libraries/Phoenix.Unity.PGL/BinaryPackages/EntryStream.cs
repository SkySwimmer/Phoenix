using System;
using System.IO;

namespace Phoenix.Unity.PGL.Internal.Packages
{
    public class EntryStream : Stream
    {
        private long cPos;
        private long _pos;
        private long _max;
        private Stream _delegate;

        public EntryStream(long pos, long max, Stream target)
        {
            _pos = pos;
            cPos = _pos;
            _max = max;
            _delegate = target;
            _delegate.Position = pos;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _max - _pos;

        public override long Position { get => cPos - _pos; set => Seek(value, SeekOrigin.Begin); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesToRead = count;
            if (bytesToRead + cPos >= _max)
                bytesToRead = (int)(_max - cPos);
            int read = _delegate.Read(buffer, offset, bytesToRead);
            cPos += read;
            return read;
        }

        public override int ReadByte()
        {
            if (cPos >= _max)
                return -1;
            int b = _delegate.ReadByte();
            cPos++;
            return b;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // Move cursor
            switch (origin)
            {
                case SeekOrigin.Begin:
                    cPos = _pos + offset;
                    _delegate.Seek(_pos + Position, SeekOrigin.Begin);
                    break;
                case SeekOrigin.Current:
                    cPos = _pos + Position + offset;
                    _delegate.Seek(_pos + Position, SeekOrigin.Begin);
                    break;
                case SeekOrigin.End:
                    cPos = _pos + Length + offset;
                    _delegate.Seek(_pos + Position, SeekOrigin.Begin);
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            base.Close();
            _delegate.Close();
        }
    }
}
