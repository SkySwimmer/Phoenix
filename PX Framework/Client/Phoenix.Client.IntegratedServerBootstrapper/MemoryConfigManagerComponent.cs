using Phoenix.Server;
using Phoenix.Server.Configuration;
using YamlDotNet.Serialization;

namespace Phoenix.Client.IntegratedServerBootstrapper
{
    /// <summary>
    /// Simple config manager component that works from memory and doesnt save to disk
    /// </summary>
    public class MemoryConfigManagerComponent : ServerComponent, IConfigurationManager
    {
        private Dictionary<string, MemoryConfigSegment> configurations = new Dictionary<string, MemoryConfigSegment>();

        public override string ID => "memory-config-manager";
        public override string[] Aliases => new string[] { "config-manager" };

        protected override string ConfigurationKey => throw new NotImplementedException();

        public AbstractConfigurationSegment GetConfiguration(string name)
        {
            while (true)
            {
                try
                {
                    if (!configurations.ContainsKey(name))
                        configurations[name] = new MemoryConfigSegment(name + ".", Server);
                    break;
                }
                catch { }
            }
            return configurations[name];
        }

        protected override void Define()
        {
            ConflictsWith("config-manager");
        }


        private class MemoryConfigSegment : AbstractConfigurationSegment
        {
            private string prefix;
            private GameServer server;
            private Dictionary<string, object> objects;
            private Dictionary<string, object> root;
            private Deserializer deserializer = new Deserializer();

            public MemoryConfigSegment(string prefix, GameServer server)
            {
                this.prefix = prefix;
                this.server = server;

                objects = new Dictionary<string, object>();
                root = new Dictionary<string, object>();
            }

            public MemoryConfigSegment(string prefix, GameServer server, Dictionary<string, object> objects, Dictionary<string, object> root)
            {
                this.prefix = prefix;
                this.server = server;

                this.objects = objects;
                this.root = root;
            }

            public override AbstractConfigurationEntry<T> CreateEntry<T>(string key)
            {
                if (key.Contains("."))
                {
                    // Recurse
                    string prefix = key.Remove(key.IndexOf("."));
                    key = key.Substring(key.IndexOf(".") + 1);
                    if (prefix == "")
                        throw new ArgumentException("Invalid path");
                    AbstractConfigurationSegment? seg = GetSegment(prefix);
                    if (seg == null)
                        seg = CreateSegment(prefix);
                    return seg.CreateEntry<T>(key);
                }
                if (HasEntry(key))
                    throw new ArgumentException("Duplicate entry");
                objects[key] = null;
                
                // Return entry
                return GetEntry<T>(key);
            }

            public override AbstractConfigurationEntry<T> GetEntry<T>(string key)
            {
                if (key.Contains("."))
                {
                    // Recurse
                    string prefix = key.Remove(key.IndexOf("."));
                    key = key.Substring(key.IndexOf(".") + 1);
                    if (prefix == "")
                        throw new ArgumentException("Invalid path");
                    AbstractConfigurationSegment? seg = GetSegment(prefix);
                    if (seg == null)
                        seg = CreateSegment(prefix);
                    return seg.GetEntry<T>(key);
                }
                if (!HasEntry(key))
                    throw new ArgumentException("No such entry");
                if (!IsRightType<T>(key))
                    throw new ArgumentException("Entry type does not match");
                return new MemoryConfigEntry<T>(this, key);
            }

            public override AbstractConfigurationSegment CreateSegment(string key)
            {
                if (key.Contains("."))
                {
                    // Recurse
                    string prefix = key.Remove(key.IndexOf("."));
                    key = key.Substring(key.IndexOf(".") + 1);
                    if (prefix == "")
                        throw new ArgumentException("Invalid path");
                    AbstractConfigurationSegment? seg = GetSegment(prefix);
                    if (seg == null)
                        seg = CreateSegment(prefix);
                    return seg.CreateSegment(key);
                }
                if (HasEntry(key))
                    throw new ArgumentException("Duplicate entry");

                // Create segment
                objects[key] = new Dictionary<string, object>();

                // Return
                return new MemoryConfigSegment(prefix + key + ".", server, (Dictionary<string, object>)objects[key], root);
            }

            public override AbstractConfigurationSegment? GetSegment(string key, AbstractConfigurationSegment? def = null)
            {
                if (key.Contains("."))
                {
                    // Recurse
                    string prefix = key.Remove(key.IndexOf("."));
                    key = key.Substring(key.IndexOf(".") + 1);
                    if (prefix == "")
                        throw new ArgumentException("Invalid path");
                    AbstractConfigurationSegment? seg = GetSegment(prefix);
                    if (seg == null)
                        seg = CreateSegment(prefix);
                    return seg.GetSegment(key);
                }
                if (!HasEntry(key))
                    return def;
                if (!IsRightType<AbstractConfigurationSegment>(key))
                    throw new ArgumentException("Entry type does not match");
                return new MemoryConfigSegment(prefix + key + ".", server, (Dictionary<string, object>)objects[key], root);
            }

            public override bool HasEntry(string key)
            {
                if (key.Contains("."))
                {
                    // Recurse
                    string prefix = key.Remove(key.IndexOf("."));
                    key = key.Substring(key.IndexOf(".") + 1);
                    if (prefix == "")
                        throw new ArgumentException("Invalid path");
                    AbstractConfigurationSegment? seg = GetSegment(prefix);
                    if (seg == null)
                        seg = CreateSegment(prefix);
                    return seg.HasEntry(key);
                }
                return objects.ContainsKey(key);
            }

            public override bool IsRightType<T>(string key)
            {
                if (key.Contains("."))
                {
                    // Recurse
                    string prefix = key.Remove(key.IndexOf("."));
                    key = key.Substring(key.IndexOf(".") + 1);
                    if (prefix == "")
                        throw new ArgumentException("Invalid path");
                    AbstractConfigurationSegment? seg = GetSegment(prefix);
                    if (seg == null)
                        seg = CreateSegment(prefix);
                    return seg.IsRightType<T>(key);
                }
                if (!HasEntry(key))
                    throw new ArgumentException("Entry not found");
                if (objects[key] == null)
                    return true;
                if (objects[key] is T)
                    return true;
                if (typeof(T).IsAssignableFrom(typeof(AbstractConfigurationSegment)))
                {
                    return objects[key] is Dictionary<object, object>;
                }
                return objects[key] is T || objects[key].GetType().FullName.StartsWith("System.Collections.Generic.List");
            }

            private class MemoryConfigEntry<T> : AbstractConfigurationEntry<T>
            {
                private MemoryConfigSegment segment;
                private string key;

                public MemoryConfigEntry(MemoryConfigSegment segment, string key)
                {
                    this.segment = segment;
                    this.key = key;
                }

                public override string Key => key;

                public override T Value
                {
                    get
                    {
                        // Server overrides
                        if (segment.server.ConfigurationOverrides.ContainsKey(segment.prefix + key))
                            return segment.deserializer.Deserialize<T>(segment.server.ConfigurationOverrides[segment.prefix + key]);

                        // Config
#pragma warning disable CS8603 // Possible null reference return.
                        object v = segment.objects[key];
                        if (v == null)
                            return default(T);
                        if (v is T)
                            return (T)v;
                        return default(T);
#pragma warning enable CS8603 // Possible null reference return.
                    }

                    set
                    {
                        // Assign
                        T v = value;
                        if (v == null)
                            segment.objects.Remove(key);
                        else
                            segment.objects[key] = v;
                    }
                }
            }
        }
    }
}
