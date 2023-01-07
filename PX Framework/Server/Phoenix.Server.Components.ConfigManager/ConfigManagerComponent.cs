using Phoenix.Server.Configuration;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Phoenix.Server.Components
{
    /// <summary>
    /// Configuration manager component
    /// </summary>
    public class ConfigManagerComponent : ServerComponent, IConfigurationManager
    {
        private Dictionary<string, YamlConfigSegment> configurations = new Dictionary<string, YamlConfigSegment>();

        public override string ID => "config-manager";

        protected override string ConfigurationKey => throw new NotImplementedException();

        public AbstractConfigurationSegment GetConfiguration(string name)
        {
            Directory.CreateDirectory("Config/" + Server.DataPrefix);
            while (true)
            {
                try
                {
                    if (!configurations.ContainsKey(name))
                        configurations[name] = new YamlConfigSegment("Config/" + Server.DataPrefix + "/" + name + ".yml", name + ".", Server);
                    break;
                }
                catch { }
            }
            return configurations[name];
        }

        protected override void Define()
        {
        }

        private class YamlConfigSegment : AbstractConfigurationSegment
        {
            private string filePath;
            private string prefix;
            private Serializer serializer = new Serializer();
            private Deserializer deserializer = new Deserializer();
            private Dictionary<object, object> objects;
            private Dictionary<object, object> root;
            private GameServer server;

            public YamlConfigSegment(string filePath, string prefix, GameServer server)
            {
                this.server = server;
                this.filePath = filePath;
                this.prefix = prefix;

                // Load
                if (!File.Exists(filePath))
                {
                    // Create
                    File.WriteAllText(filePath, "");
                }

                // Deserialize
                objects = deserializer.Deserialize<Dictionary<object, object>>(File.ReadAllText(filePath));
                if (objects == null)
                    objects = new Dictionary<object, object>();
                root = objects;
            }

            public YamlConfigSegment(string filePath, string prefix, GameServer server, Dictionary<object, object> objects, Dictionary<object, object> root)
            {
                this.root = root;
                this.objects = objects;
                this.server = server;
                this.filePath = filePath;
                this.prefix = prefix;
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

                // Save
                File.WriteAllText(filePath, serializer.Serialize(root));

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
                return new YamlEntry<T>(this, key);
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
                objects[key] = new Dictionary<object, object>();

                // Save
                File.WriteAllText(filePath, serializer.Serialize(root));

                return GetSegment(key);
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
                return new YamlConfigSegment(filePath, prefix + key + ".", server, (Dictionary<object, object>)objects[key], root);
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
                if (typeof(T).IsAssignableFrom(typeof(AbstractConfigurationSegment)))
                {
                    return objects[key] is Dictionary<object, object>;
                }
                return objects[key] is string || objects[key] is T || objects[key].GetType().FullName.StartsWith("System.Collections.Generic.List");
            }

            private class YamlEntry<T> : AbstractConfigurationEntry<T>
            {
                private YamlConfigSegment segment;
                private string key;

                public YamlEntry(YamlConfigSegment segment, string key)
                {
                    this.segment = segment;
                    this.key = key;
                }

                public override string Key =>key;

                public override T Value
                {
                    get
                    {
                        // Server overrides
                        if (segment.server.ConfigurationOverrides.ContainsKey(segment.prefix + key))
                            return segment.deserializer.Deserialize<T>(segment.server.ConfigurationOverrides[segment.prefix + key]);

                        // Config
                        object v = segment.objects[key];
                        if (v == null)
#pragma warning disable CS8603 // Possible null reference return.
                            return default(T);
#pragma warning restore CS8603 // Possible null reference return.
                        if (v.GetType().IsPrimitive || v is string) {
#pragma warning disable CS8604 // Possible null reference argument.
                            if (typeof(T).FullName == typeof(string).FullName)
                                return (T)v;
                            return segment.deserializer.Deserialize<T>(v.ToString());
#pragma warning restore CS8604 // Possible null reference argument.
                        }
                        else
                        {
                            try
                            {
                                return (T)v;
                            }
                            catch
                            {
                                if (v is List<Object> && ((List<Object>)v).Count == 0)
                                {
                                    return (T)typeof(T).GetConstructor(new Type[0]).Invoke(new object[0]);
                                }
                                else if (v is List<Object>) {
                                    Type type = Type.GetType(typeof(T).GenericTypeArguments[0].FullName);
                                    object res = (T)typeof(T).GetConstructor(new Type[0]).Invoke(new object[0]);
                                    foreach (Object obj in (List<Object>)v)
                                    {
                                        res.GetType().GetMethod("Add").Invoke(res, new object[] { Convert.ChangeType(obj, type) });
                                    }
                                    return (T)res;
                                }
                                return default(T);
                            }
                        }
                    }

                    set
                    {
                        // Assign
                        T v = value;
                        if (v == null)
                            segment.objects.Remove(key);
                        else
                            segment.objects[key] = v.GetType().IsPrimitive ? v.ToString() : v;

                        // Save
                        File.WriteAllText(segment.filePath, segment.serializer.Serialize(segment.root));
                    }
                }
            }
        }
    }
}
