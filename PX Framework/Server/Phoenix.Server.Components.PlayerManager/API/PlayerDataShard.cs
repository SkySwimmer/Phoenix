namespace Phoenix.Server.Players
{
    /// <summary>
    /// Player Data Shard - Sub-container for player data
    /// </summary>
    public abstract class PlayerDataShard
    {
        /// <summary>
        /// Checks if a data entry is present
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if present, false otherwise</returns>
        public abstract bool HasEntry(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsShard(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsString(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsBool(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsInteger(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsLong(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsShort(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsFloat(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsDouble(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsByte(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsChar(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsEnum(string key)
        {
            return IsInteger(key);
        }

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsByteArray(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsCharArray(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsStringArray(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsBoolArray(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsIntegerArray(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsLongArray(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsShortArray(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsFloatArray(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool IsDoubleArray(string key);

        /// <summary>
        /// Checks the type of a data entry
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsEnumArray(string key)
        {
            return IsIntegerArray(key);
        }

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract PlayerDataShard GetShard(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract string GetString(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract bool GetBool(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract int GetInteger(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract long GetLong(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract short GetShort(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract float GetFloat(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract double GetDouble(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract char GetChar(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract byte GetByte(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract string[] GetStringArray(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract bool[] GetBoolArray(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract int[] GetIntegerArray(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract long[] GetLongArray(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract short[] GetShortArray(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract float[] GetFloatArray(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract double[] GetDoubleArray(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract char[] GetCharArray(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public abstract byte[] GetByteArray(string key);

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public T GetEnum<T>(string key) where T : Enum
        {
            int i = GetInteger(key);
            string e = typeof(T).GetEnumNames()[i];
            return (T)Enum.Parse(typeof(T), e);
        }

        /// <summary>
        /// Retrieves data entries (throws exception if type is invalid)
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>Data entry value as requested type</returns>
        public T[] GetEnumArray<T>(string key) where T : Enum
        {
            int[] arr = GetIntegerArray(key);
            T[] res = new T[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                string e = typeof(T).GetEnumNames()[arr[i]];
                res[i] = (T)Enum.Parse(typeof(T), e);
            }
            return res;
        }

        /// <summary>
        /// Deletes data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        public abstract void Delete(string key);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, string value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, bool value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, int value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, long value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, short value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, float value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, double value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, char value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, byte value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, string[] value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, bool[] value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, int[] value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, long[] value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, short[] value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, float[] value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, double[] value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, char[] value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public abstract void Set(string key, byte[] value);

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public void Set(string key, Enum value)
        {
            string[] enumEntries = value.GetType().GetEnumNames();

            int i = 0;
            foreach (string str in enumEntries)
            {
                if (str == value.ToString())
                    break;
                i++;
            }
            Set(key, i);
        }

        /// <summary>
        /// Assigns data entries
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <param name="value">Entry value</param>
        public void Set(string key, Enum[] value)
        {
            int[] res = new int[value.Length];
            for (int ind = 0; ind < res.Length; ind++)
            {
                string[] enumEntries = value[ind].GetType().GetEnumNames();

                int i = 0;
                foreach (string str in enumEntries)
                {
                    if (str == value[ind].ToString())
                        break;
                    i++;
                }
                res[ind] = i;
            }
            Set(key, res);
        }

        /// <summary>
        /// Creates data shards
        /// </summary>
        /// <param name="key">Entry key</param>
        /// <returns>PlayerDataShard instance</returns>
        public abstract PlayerDataShard CreateShard(string key);
    }
}
