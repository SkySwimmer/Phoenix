namespace Phoenix.Client.ServerList
{
    /// <summary>
    /// Server list filter type
    /// </summary>
    public enum ServerListFilterType
    {
        /// <summary>
        /// Default filter type (uses a loose-like filtering system)
        /// </summary>
        DEFUALT,

        /// <summary>
        /// Exact values only
        /// </summary>
        STRICT,

        /// <summary>
        /// Checks if the value contains the filter string
        /// </summary>
        LOOSE,

        /// <summary>
        /// Only if the entry does not have the filter string value
        /// </summary>
        REVERSE_STRICT,

        /// <summary>
        /// Checks if the value does not contains the filter string
        /// </summary>
        REVERSE_LOOSE
    }
}
