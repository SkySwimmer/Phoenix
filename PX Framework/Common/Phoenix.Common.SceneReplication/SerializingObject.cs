using System.Reflection;

namespace Phoenix.Common.SceneReplication.Data
{
    /// <summary>
    /// Helper class for interacting with SerializingObject instances
    /// </summary>
    public static class SerializingObjects
    {
        /// <summary>
        /// Serializes objects
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="inst">Object to serialize</param>
        /// <returns>Dictionary of the serialized object</returns>
        public static Dictionary<string, object?> SerializeObject<T>(T inst) where T : SerializingObject
        {
            Dictionary<string, object?> mp = new Dictionary<string, object?>();
            inst.Serialize(mp);
            return mp;
        }

        /// <summary>
        /// Deserializes objects (requires a parameterless constructor, reflective)
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="payload">Object payload</param>
        /// <returns>Deserialized object instance</returns>
        public static T DeserializeObject<T>(Dictionary<string, object?> payload) where T : SerializingObject
        {
            Type t = typeof(T);
            ConstructorInfo? constr = t.GetConstructor(new Type[0]);
            if (constr == null)
                throw new ArgumentException("No parameterless constructors");
            T inst = (T)constr.Invoke(new object[0]);
            inst.Deserialize(payload);
            return inst;
        }
    }

    /// <summary>
    /// Serializing object interface (use SerializingObjects for quick access)
    /// </summary>
    public interface SerializingObject
    {
        /// <summary>
        /// Deserializes data (use SerializingObjects for easy access to sub-objects, you can put dictionaries as values for sub-objects)
        /// </summary>
        /// <param name="data">Data map</param>
        public void Deserialize(Dictionary<string, object?> data);

        /// <summary>
        /// Serializes the object
        /// </summary>
        /// <param name="data">Output data map</param>
        public void Serialize(Dictionary<string, object?> data);
    }
}
