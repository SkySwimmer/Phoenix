using Newtonsoft.Json;
using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Data;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Component message packet
    /// </summary>
    public class ComponentMessagePacket : AbstractNetworkPacket
    {
        public string ScenePath = "";
        public string Room = "";

        public string ObjectID = "";
        public int MessengerComponentIndex = 0;

        public bool HasDebugHeaders;
        public Dictionary<string, int> DebugComponentMessageRegistry = new Dictionary<string, int>();
        public string DebugRemoteComponentTypeName = "";

        public int MessageID;
        public Dictionary<string, object?> MessagePayload = new Dictionary<string, object?>();

        public override AbstractNetworkPacket Instantiate()
        {
            return new ComponentMessagePacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
            Room = reader.ReadString();

            ObjectID = reader.ReadString();
            MessengerComponentIndex = reader.ReadInt();

            HasDebugHeaders = reader.ReadBoolean();
            if (HasDebugHeaders)
            {
                DebugRemoteComponentTypeName = reader.ReadString();
                int l = reader.ReadInt();
                for (int i = 0; i < l; i++)
                    DebugComponentMessageRegistry[reader.ReadString()] = reader.ReadInt();
            }

            MessageID = reader.ReadInt();
            DecodeMap(MessagePayload, reader);
        }

        private void DecodeMap(Dictionary<string, object?> messagePayload, DataReader reader)
        {
            int length = reader.ReadInt();
            for (int i = 0; i < length; i++)
            {
                // Read key
                string key = reader.ReadString();

                // Read value
                messagePayload[key] = ReadObject(reader);
            }
        }

        private object? ReadObject(DataReader reader)
        {
            // Read type
            byte type = reader.ReadRawByte();
            switch (type)
            {
                case 0:
                    {
                        // Null
                        return null;
                    }
                case 1:
                    {
                        // String
                        return reader.ReadString();
                    }
                case 2:
                    {
                        // Byte
                        return reader.ReadRawByte();
                    }
                case 3:
                    {
                        // Integer
                        return reader.ReadInt();
                    }
                case 4:
                    {
                        // Short
                        return reader.ReadShort();
                    }
                case 5:
                    {
                        // Long
                        return reader.ReadLong();
                    }
                case 6:
                    {
                        // Float
                        return reader.ReadFloat();
                    }
                case 7:
                    {
                        // Double
                        return reader.ReadDouble();
                    }
                case 8:
                    {
                        // Boolean
                        return reader.ReadBoolean();
                    }
                case 9:
                    {
                        // Byte array
                        return reader.ReadBytes();
                    }
                case 10:
                    {
                        // String/object map
                        Dictionary<string, object?> map = new Dictionary<string, object?>();
                        DecodeMap(map, reader);
                        return map;
                    }
                case 11:
                    {
                        // Array
                        int length = reader.ReadInt();
                        byte arrayType = reader.ReadRawByte();
                        Array arr;
                        switch (arrayType)
                        {
                            case 1:
                                {
                                    // String
                                    arr = new string[length];
                                    break;
                                }
                            case 3:
                                {
                                    // Integer
                                    arr = new int[length];
                                    break;
                                }
                            case 4:
                                {
                                    // Short
                                    arr = new short[length];
                                    break;
                                }
                            case 5:
                                {
                                    // Long
                                    arr = new long[length];
                                    break;
                                }
                            case 6:
                                {
                                    // Float
                                    arr = new float[length];
                                    break;
                                }
                            case 7:
                                {
                                    // Double
                                    arr = new double[length];
                                    break;
                                }
                            case 8:
                                {
                                    // Boolean
                                    arr = new bool[length];
                                    break;
                                }
                            default:
                                {
                                    // Default
                                    arr = new object[length];
                                    break;
                                }
                        }

                        // Read payload
                        for (int i = 0; i < length; i++)
                        {
                            object? ent = ReadObject(reader);
                            arr.SetValue(ent, i);
                        }

                        // Return
                        return arr;
                    }
                default:
                    {
                        // Json-encoded
                        return JsonConvert.DeserializeObject(reader.ReadString());
                    }
            }
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteString(Room);

            writer.WriteString(ObjectID);
            writer.WriteInt(MessengerComponentIndex);

            writer.WriteBoolean(HasDebugHeaders);
            if (HasDebugHeaders)
            {
                writer.WriteString(DebugRemoteComponentTypeName);
                writer.WriteInt(DebugComponentMessageRegistry.Count);
                foreach (string id in DebugComponentMessageRegistry.Keys)
                {
                    writer.WriteString(id);
                    writer.WriteInt(DebugComponentMessageRegistry[id]);
                }
            }

            writer.WriteInt(MessageID);
            EncodeMap(MessagePayload, writer);
        }

        private void WriteObject(DataWriter writer, object? obj)
        {
            if (obj == null)
            {
                writer.WriteRawByte(0);
            }
            else
            {
                if (obj is string)
                {
                    writer.WriteRawByte(1);
                    writer.WriteString((string)obj);
                }
                else if (obj is byte)
                {
                    writer.WriteRawByte(2);
                    writer.WriteRawByte((byte)obj);
                }
                else if (obj is int)
                {
                    writer.WriteRawByte(3);
                    writer.WriteInt((int)obj);
                }
                else if (obj is short)
                {
                    writer.WriteRawByte(4);
                    writer.WriteShort((short)obj);
                }
                else if (obj is long)
                {
                    writer.WriteRawByte(5);
                    writer.WriteLong((long)obj);
                }
                else if (obj is float)
                {
                    writer.WriteRawByte(6);
                    writer.WriteFloat((float)obj);
                }
                else if (obj is double)
                {
                    writer.WriteRawByte(7);
                    writer.WriteDouble((double)obj);
                }
                else if (obj is bool)
                {
                    writer.WriteRawByte(8);
                    writer.WriteBoolean((bool)obj);
                }
                else if (obj is byte[])
                {
                    writer.WriteRawByte(9);
                    writer.WriteBytes((byte[])obj);
                }
                else if (obj is Dictionary<string, object?> || obj is Dictionary<string, object>)
                {
                    // String/object map
                    writer.WriteRawByte(10);
                    EncodeMap((Dictionary<string, object?>)obj, writer);
                }
                else if (obj is SerializingObject)
                {
                    // Serializing object
                    Dictionary<string, object?> mp = new Dictionary<string, object?>();
                    ((SerializingObject)obj).Serialize(mp);
                    WriteObject(writer, mp);
                }
                else if (obj is Array && (obj is string[] || obj is int[] || obj is short[] || obj is long[] || obj is float[] || obj is double[] || obj is bool[] || obj is object[]))
                {
                    // Array
                    writer.WriteRawByte(11);
                    Array arr = (Array)obj;

                    // Write headers
                    writer.WriteInt(arr.Length);

                    // Find type
                    if (obj is string[])
                        writer.WriteRawByte(1);
                    else if (obj is int[])
                        writer.WriteRawByte(3);
                    else if (obj is short[])
                        writer.WriteRawByte(4);
                    else if (obj is long[])
                        writer.WriteRawByte(5);
                    else if (obj is float[])
                        writer.WriteRawByte(6);
                    else if (obj is double[])
                        writer.WriteRawByte(7);
                    else if (obj is bool[])
                        writer.WriteRawByte(8);
                    else
                        writer.WriteRawByte(12);

                    // Write entries
                    for (int i = 0; i < arr.Length; i++)
                        WriteObject(writer, arr.GetValue(i));
                }
                else
                {
                    // Json-encode
                    writer.WriteRawByte(12);
                    writer.WriteString(JsonConvert.SerializeObject(obj));
                }
            }
        }
        private void EncodeMap(Dictionary<string, object?> messagePayload, DataWriter writer)
        {
            writer.WriteInt(messagePayload.Count);
            foreach (string key in messagePayload.Keys)
            {
                writer.WriteString(key);
                WriteObject(writer, messagePayload[key]);
            }
        }

    }
}
