using Phoenix.Common.IO;

namespace Phoenix.Server
{
    /// <summary>
    /// Closeable data reader
    /// </summary>
    public class CloseableDataReader : DataReader
    {
        public CloseableDataReader(Stream input) : base(input)
        {
        }

        public void Close()
        {
            GetStream().Close();
        }
    }
}
