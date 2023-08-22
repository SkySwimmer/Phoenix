using Newtonsoft.Json;
using Phoenix.Common.IO;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.SceneReplication.Packets
{
    /// <summary>
    /// Spawn Prefab Packet - Called when a prefab is spawned
    /// </summary>
    public class SpawnPrefabPacket : AbstractNetworkPacket
    {
        public string PrefabPath = "";
        public string ObjectID = "";
        public string? ParentObjectID = null;
        public string ScenePath = "";
        public string Room = "";

        public bool Active;
        public Transform? Transform = new Transform();
        public Dictionary<string, object?> Data = new Dictionary<string, object?>();
        
        public override bool Synchronized => true;

        public override AbstractNetworkPacket Instantiate()
        {
            return new SpawnPrefabPacket();
        }

        public override void Parse(DataReader reader)
        {
            ScenePath = reader.ReadString();
            Room = reader.ReadString();

            ObjectID = reader.ReadString();
            PrefabPath = reader.ReadString();

            if (reader.ReadBoolean())
                ParentObjectID = reader.ReadString();

            Transform = new Transform(new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()), new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()), new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()));
            Active = reader.ReadBoolean();
            Data = JsonConvert.DeserializeObject<Dictionary<string, object>>(reader.ReadString());
        }

        public override void Write(DataWriter writer)
        {
            writer.WriteString(ScenePath);
            writer.WriteString(Room);

            writer.WriteString(ObjectID);
            writer.WriteString(PrefabPath);

            writer.WriteBoolean(ParentObjectID != null);
            if (ParentObjectID != null)
                writer.WriteString(ParentObjectID);

            writer.WriteFloat(Transform.Position.X);
            writer.WriteFloat(Transform.Position.Y);
            writer.WriteFloat(Transform.Position.Z);
            writer.WriteFloat(Transform.Scale.X);
            writer.WriteFloat(Transform.Scale.Y);
            writer.WriteFloat(Transform.Scale.Z);
            writer.WriteFloat(Transform.Rotation.X);
            writer.WriteFloat(Transform.Rotation.Y);
            writer.WriteFloat(Transform.Rotation.Z);

            writer.WriteBoolean(Active);
            writer.WriteString(JsonConvert.SerializeObject(Data));
        }
    }
}
