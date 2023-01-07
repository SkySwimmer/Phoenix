namespace Phoenix.Server.Configuration
{
    /// <summary>
    /// Abstract configuration segment
    /// </summary>
    public abstract class AbstractConfigurationSegment
    {
        /// <summary>
        /// Retrieves configuration entries
        /// </summary>
        /// <typeparam name="T">Entry type</typeparam>
        /// <param name="key">Entry key</param>
        /// <returns>Configuration entry</returns>
        public abstract AbstractConfigurationEntry<T> GetEntry<T>(string key);

        /// <summary>
        /// Creates configuration entries
        /// </summary>
        /// <typeparam name="T">Entry type</typeparam>
        /// <param name="key">Entry key</param>
        /// <returns>New configuration entry</returns>
        public abstract AbstractConfigurationEntry<T> CreateEntry<T>(string key);

        /// <summary>
        /// Checks if a key is present in the configuration
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if present, false otherwise</returns>
        public abstract bool HasEntry(string key);

        /// <summary>
        /// Checks if a entry is of the right type
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <typeparam name="T">Entry type</typeparam>
        /// <returns>True if the type matches, false otherwise</returns>
        public abstract bool IsRightType<T>(string key);

        /// <summary>
        /// Retrieves entries by key, creates if not present
        /// </summary>
        /// <typeparam name="T">Entry type</typeparam>
        /// <param name="key">Entry key</param>
        /// <returns>Configuration entry</returns>
        public AbstractConfigurationEntry<T> GetOrCreateEntry<T>(string key)
        {
            if (HasEntry(key) && !IsRightType<T>(key))
                throw new ArgumentException("Entry conflict, entry with same key exists but of a different type");
            if (HasEntry(key))
                return GetEntry<T>(key);
            else
                return CreateEntry<T>(key);
        }

        /// <summary>
        /// Creates configuration segments
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>AbstractConfigurationSegment instance</returns>
        public abstract AbstractConfigurationSegment CreateSegment(string key);

        /// <summary>
        /// Retrieves configuration segments
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>AbstractConfigurationSegment instance or null</returns>
        public abstract AbstractConfigurationSegment? GetSegment(string key, AbstractConfigurationSegment? def = null);

        /// <summary>
        /// Retrieves a string entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>String value or null</returns>
        public string? GetString(string key, string? def = null)
        {
            if (HasEntry(key) && IsRightType<string>(key))
                return GetEntry<string>(key).Value;
            else
                return def;
        }

        /// <summary>
        /// Assigns a string entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetString(string key, string value)
        {
            GetOrCreateEntry<string>(key).Value = value;
            return this;
        }

        /// <summary>
        /// Retrieves a bool entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Bool value or false</returns>
        public bool GetBool(string key, bool def = false)
        {
            if (HasEntry(key) && IsRightType<bool>(key))
                return GetEntry<bool>(key).Value;
            else
                return def;
        }

        /// <summary>
        /// Assigns a bool entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetBool(string key, bool value)
        {
            GetOrCreateEntry<bool>(key).Value = value;
            return this;
        }

        /// <summary>
        /// Retrieves a int entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Integer value or -1</returns>
        public int GetInteger(string key, int def = -1)
        {
            if (HasEntry(key) && IsRightType<int>(key))
                return GetEntry<int>(key).Value;
            else
                return def;
        }

        /// <summary>
        /// Assigns a int entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetInteger(string key, int value)
        {
            GetOrCreateEntry<int>(key).Value = value;
            return this;
        }

        /// <summary>
        /// Retrieves a float entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Float value or -1</returns>
        public float GetFloat(string key, float def = -1)
        {
            if (HasEntry(key) && IsRightType<float>(key))
                return GetEntry<float>(key).Value;
            else
                return def;
        }

        /// <summary>
        /// Assigns a float entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetFloat(string key, float value)
        {
            GetOrCreateEntry<float>(key).Value = value;
            return this;
        }

        /// <summary>
        /// Retrieves a double entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Double value or -1</returns>
        public double GetDouble(string key, double def = -1)
        {
            if (HasEntry(key) && IsRightType<double>(key))
                return GetEntry<double>(key).Value;
            else
                return def;
        }

        /// <summary>
        /// Assigns a double entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetDouble(string key, double value)
        {
            GetOrCreateEntry<double>(key).Value = value;
            return this;
        }

        /// <summary>
        /// Retrieves a long entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Long value or -1</returns>
        public long GetLong(string key, long def = -1)
        {
            if (HasEntry(key) && IsRightType<long>(key))
                return GetEntry<long>(key).Value;
            else
                return def;
        }

        /// <summary>
        /// Assigns a long entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetLong(string key, long value)
        {
            GetOrCreateEntry<long>(key).Value = value;
            return this;
        }

        /// <summary>
        /// Retrieves a short entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Short value or -1</returns>
        public short GetShort(string key, short def = -1)
        {
            if (HasEntry(key) && IsRightType<short>(key))
                return GetEntry<short>(key).Value;
            else
                return def;
        }

        /// <summary>
        /// Assigns a short entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetShort(string key, short value)
        {
            GetOrCreateEntry<short>(key).Value = value;
            return this;
        }

        /// <summary>
        /// Retrieves a string array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>String array</returns>
        public string[] GetStringArray(string key, string[]? def = null)
        {
            if (HasEntry(key) && IsRightType<List<string>>(key))
                return GetEntry<List<string>>(key).Value.ToArray();
            else
                return (def == null ? new string[0] : def);
        }

        /// <summary>
        /// Assigns a string array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetStringArray(string key, string[] value)
        {
            GetOrCreateEntry<List<string>>(key).Value = new List<string>(value);
            return this;
        }

        /// <summary>
        /// Retrieves a integer array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Integer array</returns>
        public int[] GetIntegerArray(string key, int[]? def = null)
        {
            if (HasEntry(key) && IsRightType<List<int>>(key))
                return GetEntry<List<int>>(key).Value.ToArray();
            else
                return (def == null ? new int[0] : def);
        }

        /// <summary>
        /// Assigns a integer array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetIntegerArray(string key, int[] value)
        {
            GetOrCreateEntry<List<int>>(key).Value = new List<int>(value);
            return this;
        }

        /// <summary>
        /// Retrieves a float array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Float array</returns>
        public float[] GetFloatArray(string key, float[]? def = null)
        {
            if (HasEntry(key) && IsRightType<List<float>>(key))
                return GetEntry<List<float>>(key).Value.ToArray();
            else
                return (def == null ? new float[0] : def);
        }

        /// <summary>
        /// Assigns a float array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetFloatArray(string key, float[] value)
        {
            GetOrCreateEntry<List<float>>(key).Value = new List<float>(value);
            return this;
        }

        /// <summary>
        /// Retrieves a long array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Long array</returns>
        public long[] GetLongArray(string key, long[]? def = null)
        {
            if (HasEntry(key) && IsRightType<List<long>>(key))
                return GetEntry<List<long>>(key).Value.ToArray();
            else
                return (def == null ? new long[0] : def);
        }

        /// <summary>
        /// Assigns a long array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetLongArray(string key, long[] value)
        {
            GetOrCreateEntry<List<long>>(key).Value = new List<long>(value);
            return this;
        }

        /// <summary>
        /// Retrieves a double array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Double array</returns>
        public double[] GetDoubleArray(string key, double[]? def = null)
        {
            if (HasEntry(key) && IsRightType<List<double>>(key))
                return GetEntry<List<double>>(key).Value.ToArray();
            else
                return (def == null ? new double[0] : def);
        }

        /// <summary>
        /// Assigns a double array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetDoubleArray(string key, double[] value)
        {
            GetOrCreateEntry<List<double>>(key).Value = new List<double>(value);
            return this;
        }

        /// <summary>
        /// Retrieves a short array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="def">Default return value</param>
        /// <returns>Short array</returns>
        public short[] GetShortArray(string key, short[]? def = null)
        {
            if (HasEntry(key) && IsRightType<List<short>>(key))
                return GetEntry<List<short>>(key).Value.ToArray();
            else
                return (def == null ? new short[0] : def);
        }

        /// <summary>
        /// Assigns a short array entry
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">New value</param>
        public AbstractConfigurationSegment SetShortArray(string key, short[] value)
        {
            GetOrCreateEntry<List<short>>(key).Value = new List<short>(value);
            return this;
        }

    }
}
