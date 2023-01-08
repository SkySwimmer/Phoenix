using Newtonsoft.Json;
using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    public class Transform
    {
        public Transform() { }
        public Transform(Vector3 position, Vector3 scale, Vector3 rotation)
        {
            Position = position;
            Scale = scale;
            Rotation = rotation;
        }

        public Vector3 Position = new Vector3();
        public Vector3 Scale = new Vector3();
        public Vector3 Rotation = new Vector3();
    }
    public class Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3()
        {
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    /// <summary>
    /// Object Replication Packet
    /// </summary>
    public class ReplicateObjectPacket : AbstractNetworkPacket
    {
        public bool HasTransformChanges = false;
        public bool HasNameChanges = false;
        public bool HasActiveStatusChanges = false;
        public bool HasDataChanges = false;

        public bool IsInitial = false;

        public string? Name;
        public Transform? Transform = new Transform();
        public bool Active;
        public List<string> RemovedData = new List<string>();
        public Dictionary<string, object?> Data = new Dictionary<string, object?>();

        public string ObjectPath = "";
        public string ScenePath = "";
        public string Room = "";

        public override bool Synchronized => true;

        public override AbstractNetworkPacket Instantiate()
        {
            return new ReplicateObjectPacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
            Room = reader.ReadString();
            IsInitial = reader.ReadBoolean();

            ObjectPath = reader.ReadString();
            HasTransformChanges = reader.ReadBoolean();
            HasNameChanges = reader.ReadBoolean();
            HasActiveStatusChanges = reader.ReadBoolean();
            HasDataChanges = reader.ReadBoolean();

            if (HasTransformChanges)
                Transform = new Transform(new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()), new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()), new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()));
            if (HasNameChanges)
                Name = reader.ReadString();
            if (HasActiveStatusChanges)
                Active = reader.ReadBoolean();
            if (HasDataChanges)
            {
                Data = JsonConvert.DeserializeObject<Dictionary<string, object>>(reader.ReadString());
                int l = reader.ReadInt();
                for (int i = 0; i < l; i++)
                    RemovedData.Add(reader.ReadString());
            }
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteString(Room);
            writer.WriteBoolean(IsInitial);

            writer.WriteString(ObjectPath);
            writer.WriteBoolean(HasTransformChanges);
            writer.WriteBoolean(HasNameChanges);
            writer.WriteBoolean(HasActiveStatusChanges);
            writer.WriteBoolean(HasDataChanges);

            if (HasTransformChanges)
            {
                writer.WriteFloat(Transform.Position.X);
                writer.WriteFloat(Transform.Position.Y);
                writer.WriteFloat(Transform.Position.Z);
                writer.WriteFloat(Transform.Scale.X);
                writer.WriteFloat(Transform.Scale.Y);
                writer.WriteFloat(Transform.Scale.Z);
                writer.WriteFloat(Transform.Rotation.X);
                writer.WriteFloat(Transform.Rotation.Y);
                writer.WriteFloat(Transform.Rotation.Z);
            }
            if (HasNameChanges)
                writer.WriteString(Name);
            if (HasActiveStatusChanges)
                writer.WriteBoolean(Active);
            if (HasDataChanges)
            {
                writer.WriteString(JsonConvert.SerializeObject(Data));
                writer.WriteInt(RemovedData.Count);
                foreach (string key in RemovedData)
                    writer.WriteString(key);
            }
        }
    }
}
