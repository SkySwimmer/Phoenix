namespace Phoenix.Server.Configuration
{
    /// <summary>
    /// Configuration entry
    /// </summary>
    public abstract class AbstractConfigurationEntry<T>
    {
        /// <summary>
        /// Entry key
        /// </summary>
        public abstract string Key { get; }

        /// <summary>
        /// Entry value
        /// </summary>
        public abstract T Value { get; set; }
    }
}
