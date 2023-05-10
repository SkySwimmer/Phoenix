namespace Phoenix.Client.Factory
{
    /// <summary>
    /// Client build result
    /// </summary>
    public class GameClientBuildResult
    {
        /// <summary>
        /// Game client that was created, may be null in case the build failed
        /// </summary>
        public GameClient? Client;

        /// <summary>
        /// Build failure code
        /// </summary>
        public GameClientBuildFailureCode FailureCode = GameClientBuildFailureCode.NONE;

        /// <summary>
        /// Checks if the client was successfully made
        /// </summary>
        public bool IsSuccess
        {
            get
            {
                return FailureCode != GameClientBuildFailureCode.NONE;
            }
        }
    }
}
