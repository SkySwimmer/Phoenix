namespace Phoenix.Server.Components.ServerlistPublisher
{
    /// <summary>
    /// Details block of the server list
    /// </summary>
    public class ServerListDetails
    {
        private Dictionary<string, string> _data = new Dictionary<string, string>();
        private bool changed = false;

        /// <summary>
        /// Retrieves server detail entries
        /// </summary>
        /// <param name="key">Detail key</param>
        /// <returns>Detail value or null</returns>
        public string? GetOrNull(string key)
        {
            if (_data.ContainsKey(key))
                return _data[key];
            return null;
        }

        /// <summary>
        /// Retrieves server detail entries
        /// </summary>
        /// <param name="key">Detail key</param>
        /// <returns>Detail value</returns>
        public string Get(string key)
        {
            if (_data.ContainsKey(key))
                return _data[key];
            throw new ArgumentException("Key not present");
        }

        /// <summary>
        /// Assigns a detail entry
        /// </summary>
        /// <param name="key">Detail key</param>
        /// <param name="value">Detail value</param>
        public void Set(string key, string value)
        {
            if (Has(key) && Get(key) == value)
                return;
            _data[key] = value;
            changed = true;
        }

        /// <summary>
        /// Checks if a detail entry is present
        /// </summary>
        /// <param name="key">Detail key</param>
        /// <returns>True if present, false otherwise</returns>
        public bool Has(string key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
        /// Checks if the server list details have changed
        /// </summary>
        public bool HasChanged
        {
            get
            {
                return changed;
            }
        }

        /// <summary>
        /// Compiles the details and marks the data as up-to-date
        /// </summary>
        /// <returns>Dictionary instance</returns>
        public Dictionary<string, string> Compile(bool markUpToDate = true)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            foreach (string key in Keys)
                data[key] = Get(key);
            if (markUpToDate)
                changed = false;
            return data;
        }

        /// <summary>
        /// Retrieves all detail keys
        /// </summary>
        public string[] Keys
        {
            get
            {
                return _data.Keys.ToArray();
            }
        }
    }
}
