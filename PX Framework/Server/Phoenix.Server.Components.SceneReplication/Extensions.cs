global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Threading;
global using global::System.Threading.Tasks;

internal static class StreamExtensions
{
    public static void Write(this Stream strm, byte[] data)
    {
        strm.Write(data, 0, data.Length);
    }
    public static void Read(this Stream strm, byte[] data)
    {
        strm.Read(data, 0, data.Length);
    }
}


internal static class DictionaryExtensions
{
    public static T2 GetValueOrDefault<T1, T2>(this Dictionary<T1, T2> self, T1 key, T2 def)
    {
        if (!self.ContainsKey(key))
            return def;
        return self[key];
    }
    public static T2 GetValueOrDefault<T1, T2>(this Dictionary<T1, T2> self, T1 key)
    {
        if (!self.ContainsKey(key))
            return default(T2);
        return self[key];
    }
}


