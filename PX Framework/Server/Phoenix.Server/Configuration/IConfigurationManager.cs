namespace Phoenix.Server.Configuration
{
    /// <summary>
    /// Configuration management interface
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Retrieves confiurations by name
        /// </summary>
        /// <param name="name">Configuration name</param>
        /// <returns>AbstractConfigurationSegment instance</returns>
        public AbstractConfigurationSegment GetConfiguration(string name);
    }
}
