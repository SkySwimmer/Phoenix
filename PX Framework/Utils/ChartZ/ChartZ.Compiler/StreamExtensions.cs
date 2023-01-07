using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartZ.Compiler
{
    internal static class StreamExtensions
    {
        public static void Write(this Stream strm, byte[] data)
        {
            strm.Write(data, 0, data.Length);
        }
    }
}
