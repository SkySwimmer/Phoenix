namespace Phoenix.Server.SceneReplication.Data
{
    public interface SerializingObject
    {
        /// <summary>
        /// Deserializes data
        /// </summary>
        /// <param name="data">Data map</param>
        public void Deserialize(Dictionary<string, object> data);

        /// <summary>
        /// Serializes the object
        /// </summary>
        /// <param name="data">Output data map</param>
        public void Serialize(Dictionary<string, object> data);
    }
}
