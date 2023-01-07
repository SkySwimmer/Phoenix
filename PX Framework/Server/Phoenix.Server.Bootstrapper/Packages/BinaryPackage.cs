﻿namespace Phoenix.Server.Bootstrapper.Packages
{
    /// <summary>
    /// Binary package tool - Seek-capable archives
    /// </summary>
    public class BinaryPackage
    {
        private string _name;
        private StreamProvider _provider;
        private Stream Stream;

        private Dictionary<string, BinaryPackageEntry> Entries = new Dictionary<string, BinaryPackageEntry>();

        /// <summary>
        /// Used to create streams for reading entries
        /// </summary>
        public delegate Stream StreamProvider();

        /// <summary>
        /// Loads a binary package
        /// </summary>
        /// <param name="stream">Binary package stream</param>
        /// <param name="name">File name</param>
        public BinaryPackage(Stream stream, string name, StreamProvider provider)
        {
            _provider = provider;
            _name = name;
            Stream = stream;
            if (!stream.CanSeek)
                throw new ArgumentException("stream: requires a seek-capable stream");

            // Parse
            DataReader reader = new DataReader(stream);
            int length = reader.ReadInt();
            for (int i = 0; i < length; i++ )
            {
                BinaryPackageEntry entry = new BinaryPackageEntry(reader.ReadString(), reader.ReadLong(), reader.ReadLong());
                Entries[entry.Key] = entry;
            }
        }

        /// <summary>
        /// Retrieves the package file name
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Retrieves all entries in the package
        /// </summary>
        /// <returns></returns>
        public BinaryPackageEntry[] GetEntries()
        {
            return Entries.Values.ToArray();
        }

        public BinaryPackageEntry[] GetEntriesIn(string key)
        {
            string k = key;
            while (k.Contains("//"))
                k = k.Replace("//", "/");
            while (k.EndsWith("/"))
                k = k.Remove(k.LastIndexOf("/"));
            while (k.StartsWith("/"))
                k = k.Substring(1);
            return Entries.Values.Where(t =>
            {
                if (t.Key.StartsWith(k + "/"))
                {
                    if (!t.Key.Substring((k + "/").Length).Contains("/"))
                        return true;
                }
                return false;
            }).ToArray();
        }

        public BinaryPackageEntry? GetEntry(string key)
        {
            string k = key;
            while (k.Contains("//"))
                k = k.Replace("//", "/");
            while (k.EndsWith("/"))
                k = k.Remove(k.LastIndexOf("/"));
            while (k.StartsWith("/"))
                k = k.Substring(1);
            if (!Entries.ContainsKey(k))
                return null;
            else
                return Entries[k];
        }
        
        public EntryStream GetStream(BinaryPackageEntry entry)
        {
            return new EntryStream(entry.Start, entry.End, _provider());
        }

        public void Close()
        {
            Stream.Close();
            Entries.Clear();
        }
    }
}
