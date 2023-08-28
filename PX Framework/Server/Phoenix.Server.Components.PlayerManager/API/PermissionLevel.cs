namespace Phoenix.Server.Players
{
    /// <summary>
    /// Permission level system
    /// </summary>
    public enum PermissionLevel
    {
        /// <summary>
        /// Default permission level
        /// </summary>
        DEFAULT,

        /// <summary>
        /// Trusted member permission level
        /// </summary>
        TRUSTED,

        /// <summary>
        /// Trial moderator permission level
        /// </summary>
        TRIAL_MODERATOR,

        /// <summary>
        /// Moderator permission level
        /// </summary>
        MODERATOR,

        /// <summary>
        /// Administrator permission level
        /// </summary>
        ADMINISTRATOR,

        /// <summary>
        /// Developer permission level
        /// </summary>
        DEVELOPER,

        /// <summary>
        /// Server operator permission level
        /// </summary>
        OPERATOR
    }
}
