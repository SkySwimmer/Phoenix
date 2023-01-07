namespace Phoenix.Client.Factory
{
    /// <summary>
    /// Client build failure code
    /// </summary>
    public enum GameClientBuildFailureCode
    {
        NONE,
        
        CANCELLED,
        
        INSECURE_MODE_SERVER,
        
        CONNECTION_TEST_FAILED,
        
        MISSING_CHANNEL_REGISTRY,
        MISSING_CONNECTION_PROVIDER,

        AUTO_INIT_FAILURE,
        AUTO_CONNECT_FAILURE
    }
}
