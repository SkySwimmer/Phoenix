namespace Phoenix.Common.Networking.Internal
{
    public class LoopbackStream : Stream
    {
        private bool writing = false;
        private bool closed = false;

        private List<byte> buffer = new List<byte>();
        private byte[] Buffer
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return buffer.ToArray();
                    }
                    catch { }
                }
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Close()
        {
            closed = true;
            buffer.Clear();
        }

        public override void Flush()
        {
        }

        private bool reading = false;

        public override int ReadByte()
        {
            if (closed)
                return -1;
            while (reading)
                Thread.Sleep(10);
            reading = true;
            while (Buffer.Length == 0)
            {
                if (closed)
                {
                    reading = false;
                    return -1;
                }
                Thread.Sleep(10);
            }
            byte b = Buffer[0];
            buffer.RemoveAt(0);
            reading = false;
            return b;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            for (int i = 0; i < count; i++)
            {
                int b = ReadByte();
                if (b == -1)
                    break;
                buffer[offset + i] = (byte)b;
                read++;
            }
            return read;
        }

        public override void WriteByte(byte value)
        {
            if (closed)
                throw new IOException("Closed");
            while (writing)
            {
                if (closed)
                    throw new IOException("Closed");
            }
            writing = true;
            while (Buffer.Length >= int.MaxValue / 10)
                Thread.Sleep(10); // Buffer too large
            buffer.Add(value);
            writing = false;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                WriteByte(buffer[offset + i]);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

    }
}
