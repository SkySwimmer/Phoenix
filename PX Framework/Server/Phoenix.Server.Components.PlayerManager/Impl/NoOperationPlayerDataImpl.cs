using Phoenix.Common.Logging;
using Phoenix.Server.Players;

namespace Phoenix.Server.Components.PlayerManager.Impl
{
    public class NoOperationPlayerDataProvider : PlayerDataProvider
    {
        public bool HasPlayerData(string id)
        {
            return false;
        }

        public bool CanUseAsFallback()
        {
            return true;
        }

        public void DeletePlayerData(string id)
        {
        }

        public int GetCurrentMajorDataVersion()
        {
            return 0;
        }

        public int GetCurrentMinorDataVersion()
        {
            return 0;
        }

        public PlayerDataContainer Provide(string id)
        {
            return new NoOperationPlayerDataImpl(id, 0, 0);
        }
    }

    public class NoOperationPlayerDataImpl : PlayerDataContainer
    {
        /// <summary>
        /// Defines if the warning about running in no-operation data storage mode should be displayed
        /// </summary>
        public static bool IgnoreNoOperationDataStorage = false;
        private static bool warned = false;

        private string playerID;
        private int minorDataVersion;
        private int majorDataVersion;

        public NoOperationPlayerDataImpl(string playerID, int majorDataVersion, int minorDataVersion)
        {
            this.playerID = playerID;
            this.majorDataVersion = majorDataVersion;
            this.minorDataVersion = minorDataVersion;
            if (!IgnoreNoOperationDataStorage)
            {
                if (!warned)
                {
                    Logger.GetLogger("player-data-manager").Trace("");
                    Logger.GetLogger("player-data-manager").Trace("WARNING! Player Data Manager is running in no-operation mode! Please add a player data provider!");
                    Logger.GetLogger("player-data-manager").Trace("You can disable this warning by setting NoOperationPlayerDataImpl.IgnoreNoOperationDataStorage to true");
                    Logger.GetLogger("player-data-manager").Trace("You can add data providers by implementing a PlayerDataProvider or adding one from a library.");
                    Logger.GetLogger("player-data-manager").Trace("Providers are added by calling the PlayerDataService.AddProvider method.");
                    Logger.GetLogger("player-data-manager").Trace("");
                    warned = true;
                }
            }
        }

        public override string PlayerID => playerID;

        public override int DataMajorVersion
        {
            get
            {
                return majorDataVersion;
            }

            set
            {
                minorDataVersion = value;
            }
        }

        public override int DataMinorVersion
        {
            get
            {
                return minorDataVersion;
            }

            set
            {
                minorDataVersion = value;
            }
        }

        public override void Save()
        {
        }

        private Dictionary<string, object> entries = new Dictionary<string, object>();
        public override void Delete(string key)
        {
            if (entries.ContainsKey(key))
                entries.Remove(key);
        }

        public override bool GetBool(string key)
        {
            if (entries.ContainsKey(key))
                return (bool)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override bool[] GetBoolArray(string key)
        {
            if (entries.ContainsKey(key))
                return (bool[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override byte GetByte(string key)
        {
            if (entries.ContainsKey(key))
                return (byte)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override byte[] GetByteArray(string key)
        {
            if (entries.ContainsKey(key))
                return (byte[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override char GetChar(string key)
        {
            if (entries.ContainsKey(key))
                return (char)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override char[] GetCharArray(string key)
        {
            if (entries.ContainsKey(key))
                return (char[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override double GetDouble(string key)
        {
            if (entries.ContainsKey(key))
                return (double)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override double[] GetDoubleArray(string key)
        {
            if (entries.ContainsKey(key))
                return (double[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override float GetFloat(string key)
        {
            if (entries.ContainsKey(key))
                return (float)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override float[] GetFloatArray(string key)
        {
            if (entries.ContainsKey(key))
                return (float[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override int GetInteger(string key)
        {
            if (entries.ContainsKey(key))
                return (int)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override int[] GetIntegerArray(string key)
        {
            if (entries.ContainsKey(key))
                return (int[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override long GetLong(string key)
        {
            if (entries.ContainsKey(key))
                return (long)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override long[] GetLongArray(string key)
        {
            if (entries.ContainsKey(key))
                return (long[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override PlayerDataShard GetShard(string key)
        {
            if (entries.ContainsKey(key))
                return (PlayerDataShard)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override short GetShort(string key)
        {
            if (entries.ContainsKey(key))
                return (short)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override short[] GetShortArray(string key)
        {
            if (entries.ContainsKey(key))
                return (short[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override string GetString(string key)
        {
            if (entries.ContainsKey(key))
                return (string)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override string[] GetStringArray(string key)
        {
            if (entries.ContainsKey(key))
                return (string[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override bool HasEntry(string key)
        {
            return entries.ContainsKey(key);
        }

        public override bool IsBool(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is bool;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsBoolArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is bool[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsByte(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is byte;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsByteArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is byte[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsChar(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is char;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsCharArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is char[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsDouble(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is double;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsDoubleArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is double[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsFloat(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is float;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsFloatArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is float[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsInteger(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is int;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsIntegerArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is int[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsLong(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is long;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsLongArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is long[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsShard(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is PlayerDataShard;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsShort(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is short;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsShortArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is short[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsString(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is string;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsStringArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is string[];
            throw new ArgumentException("Entry not found");
        }

        public override PlayerDataShard CreateShard(string key)
        {
            NoOperationPlayerDataShardImpl shard = new NoOperationPlayerDataShardImpl();
            entries[key] = shard;
            return shard;
        }

        public override void Set(string key, string value)
        {
            entries[key] = value;
        }

        public override void Set(string key, bool value)
        {
            entries[key] = value;
        }

        public override void Set(string key, int value)
        {
            entries[key] = value;
        }

        public override void Set(string key, long value)
        {
            entries[key] = value;
        }

        public override void Set(string key, short value)
        {
            entries[key] = value;
        }

        public override void Set(string key, float value)
        {
            entries[key] = value;
        }

        public override void Set(string key, double value)
        {
            entries[key] = value;
        }

        public override void Set(string key, char value)
        {
            entries[key] = value;
        }

        public override void Set(string key, byte value)
        {
            entries[key] = value;
        }

        public override void Set(string key, string[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, bool[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, int[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, long[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, short[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, float[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, double[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, char[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, byte[] value)
        {
            entries[key] = value;
        }
    }

    public class NoOperationPlayerDataShardImpl : PlayerDataShard
    {
        private Dictionary<string, object> entries = new Dictionary<string, object>();
        public override void Delete(string key)
        {
            if (entries.ContainsKey(key))
                entries.Remove(key);
        }

        public override bool GetBool(string key)
        {
            if (entries.ContainsKey(key))
                return (bool)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override bool[] GetBoolArray(string key)
        {
            if (entries.ContainsKey(key))
                return (bool[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override byte GetByte(string key)
        {
            if (entries.ContainsKey(key))
                return (byte)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override byte[] GetByteArray(string key)
        {
            if (entries.ContainsKey(key))
                return (byte[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override char GetChar(string key)
        {
            if (entries.ContainsKey(key))
                return (char)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override char[] GetCharArray(string key)
        {
            if (entries.ContainsKey(key))
                return (char[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override double GetDouble(string key)
        {
            if (entries.ContainsKey(key))
                return (double)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override double[] GetDoubleArray(string key)
        {
            if (entries.ContainsKey(key))
                return (double[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override float GetFloat(string key)
        {
            if (entries.ContainsKey(key))
                return (float)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override float[] GetFloatArray(string key)
        {
            if (entries.ContainsKey(key))
                return (float[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override int GetInteger(string key)
        {
            if (entries.ContainsKey(key))
                return (int)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override int[] GetIntegerArray(string key)
        {
            if (entries.ContainsKey(key))
                return (int[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override long GetLong(string key)
        {
            if (entries.ContainsKey(key))
                return (long)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override long[] GetLongArray(string key)
        {
            if (entries.ContainsKey(key))
                return (long[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override PlayerDataShard GetShard(string key)
        {
            if (entries.ContainsKey(key))
                return (PlayerDataShard)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override short GetShort(string key)
        {
            if (entries.ContainsKey(key))
                return (short)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override short[] GetShortArray(string key)
        {
            if (entries.ContainsKey(key))
                return (short[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override string GetString(string key)
        {
            if (entries.ContainsKey(key))
                return (string)entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override string[] GetStringArray(string key)
        {
            if (entries.ContainsKey(key))
                return (string[])entries[key];
            throw new ArgumentException("Entry not found");
        }

        public override bool HasEntry(string key)
        {
            return entries.ContainsKey(key);
        }

        public override bool IsBool(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is bool;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsBoolArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is bool[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsByte(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is byte;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsByteArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is byte[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsChar(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is char;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsCharArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is char[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsDouble(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is double;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsDoubleArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is double[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsFloat(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is float;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsFloatArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is float[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsInteger(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is int;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsIntegerArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is int[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsLong(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is long;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsLongArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is long[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsShard(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is PlayerDataShard;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsShort(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is short;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsShortArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is short[];
            throw new ArgumentException("Entry not found");
        }

        public override bool IsString(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is string;
            throw new ArgumentException("Entry not found");
        }

        public override bool IsStringArray(string key)
        {
            if (entries.ContainsKey(key))
                return entries[key] is string[];
            throw new ArgumentException("Entry not found");
        }

        public override PlayerDataShard CreateShard(string key)
        {
            NoOperationPlayerDataShardImpl shard = new NoOperationPlayerDataShardImpl();
            entries[key] = shard;
            return shard;
        }

        public override void Set(string key, string value)
        {
            entries[key] = value;
        }

        public override void Set(string key, bool value)
        {
            entries[key] = value;
        }

        public override void Set(string key, int value)
        {
            entries[key] = value;
        }

        public override void Set(string key, long value)
        {
            entries[key] = value;
        }

        public override void Set(string key, short value)
        {
            entries[key] = value;
        }

        public override void Set(string key, float value)
        {
            entries[key] = value;
        }

        public override void Set(string key, double value)
        {
            entries[key] = value;
        }

        public override void Set(string key, char value)
        {
            entries[key] = value;
        }

        public override void Set(string key, byte value)
        {
            entries[key] = value;
        }

        public override void Set(string key, string[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, bool[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, int[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, long[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, short[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, float[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, double[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, char[] value)
        {
            entries[key] = value;
        }

        public override void Set(string key, byte[] value)
        {
            entries[key] = value;
        }
    }
}
