using Newtonsoft.Json;

namespace Phoenix.Server.SceneReplication.Data
{
    /// <summary>
    /// Scene replication data map
    /// </summary>
    public class ReplicationDataMap
    {
        internal Dictionary<string, object?> data = new Dictionary<string, object?>();

        public ReplicationDataMap ReadOnlyCopy()
        {
            return new ReplicationDataMap(data, true);
        }

        bool readOnly = false;
        public ReplicationDataMap() { }
        public ReplicationDataMap(Dictionary<string, object?> data, bool readOnly = false)
        {
            this.data = data;
            this.readOnly = readOnly;
        }

        public delegate void DataChangedEvent(string key, object? newValue);
        public event DataChangedEvent? OnChange;
        public delegate void DataEntryRemovedEvent(string key);
        public event DataEntryRemovedEvent? OnRemove;

        /// <summary>
        /// Retrieves all keys in the data map
        /// </summary>
        public string[] Keys
        {
            get
            {
                while (true)
                {
                    try
                    {
                        return data.Keys.ToArray();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Checks if a entry is present in the data map
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Has(string key)
        {
            while (true)
            {
                try
                {
                    return data.ContainsKey(key);
                }
                catch {}
            }
        }

        /// <summary>
        /// Removes entries
        /// </summary>
        /// <param name="key">Entry to remove</param>
        public void Remove(string key)
        {
            if (Has(key))
            {
                while (true)
                {
                    try
                    {
                        data.Remove(key);
                        break;
                    }
                    catch { }
                }
                OnRemove?.Invoke(key);
            }
        }

        /// <summary>
        /// Assigns entry values
        /// </summary>
        /// <typeparam name="T">Entry type</typeparam>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public void Set<T>(string key, T value)
        {
            if (readOnly)
                throw new ArgumentException("Cannot change properties of a read-only object");
            if (value is SerializingObject)
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                ((SerializingObject)value).Serialize(data);
                Set(key, data);
                return;
            }
            if (value is ReplicationDataMap)
            {
                Set(key, ((ReplicationDataMap)(object)value).data); // Wtf? C# said it cannot convert value to ReplicationDataMap, but its just another object so wtf, so had to cast like this
                return;
            }

            // Set data
            while (true)
            {
                try
                {
                    data[key] = value;
                    break;
                }
                catch { }
            }
            OnChange?.Invoke(key, value);
        }

        /// <summary>
        /// Retrieves entries
        /// </summary>
        /// <typeparam name="T">Entry type</typeparam>
        /// <param name="key">Entry key</param>
        /// <returns>Entry value</returns>
        public T Get<T>(string key)
        {
            return GetOrDefault<T>(key, default(T));
        }

        /// <summary>
        /// Retrieves entries
        /// </summary>
        /// <typeparam name="T">Entry type</typeparam>
        /// <param name="key">Entry key</param>
        /// <param name="def">Default value</param>
        /// <returns>Entry value</returns>
        public T GetOrDefault<T>(string key, T def)
        {
            if (Has(key))
            {
                object val;
                while (true)
                {
                    try
                    {
                        if (!data.ContainsKey(key))
                            return def;
                        val = data[key];
                        break;
                    }
                    catch { }
                }
                if (typeof(SerializingObject).IsAssignableFrom(typeof(T)))
                {
                    SerializingObject baseObj = (SerializingObject)typeof(T).GetConstructor(new Type[0]).Invoke(new object[0]);
                    baseObj.Deserialize((Dictionary<string, object>)val);
                    return (T)baseObj;
                }
                else
                    return (T)val;
            }
            return def;
        }
    }
}
