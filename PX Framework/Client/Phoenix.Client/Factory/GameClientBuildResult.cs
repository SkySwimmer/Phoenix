using Phoenix.Common.Networking.Connections;

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
                return FailureCode == GameClientBuildFailureCode.NONE;
            }
        }

        /// <summary>
        /// Retrieves the disconnect reason parameters, returns null if not present
        /// </summary>
        public DisconnectParams? DisconnectReason
        {
            get
            {
                if (Client == null)
                {
                    if (!IsSuccess)
                    {
                        // Verify failure code
                        switch (FailureCode)
                        {
                            case GameClientBuildFailureCode.NONE:
                                return null;
                            case GameClientBuildFailureCode.CANCELLED:
                                return new DisconnectParams("connect.error.cancelled", new string[0]);
                            case GameClientBuildFailureCode.AUTO_INIT_FAILURE:
                                return new DisconnectParams("connect.error.initfailure", new string[0]);
                            case GameClientBuildFailureCode.CONNECTION_TEST_FAILED:
                                return new DisconnectParams("connect.error.connectfailure.unreachable", new string[0]);
                            case GameClientBuildFailureCode.AUTO_CONNECT_FAILURE:
                                return new DisconnectParams("connect.error.connectfailure.inernalerror", new string[] { "connect.error.unknown" });
                            case GameClientBuildFailureCode.INSECURE_MODE_SERVER:
                                return new DisconnectParams("connect.error.connectfailure.insecure", new string[0]);
                            case GameClientBuildFailureCode.MISSING_CHANNEL_REGISTRY:
                                return new DisconnectParams("connect.error.connectfailure.inernalerror", new string[] { "connect.error.missing.channelregistry" });
                            case GameClientBuildFailureCode.MISSING_CONNECTION_PROVIDER:
                                return new DisconnectParams("connect.error.connectfailure.inernalerror", new string[] { "connect.error.missing.connectionprovider" });
                        }
                    }
                    return null;
                }
                return Client.DisconnectReason;
            }
        }
    }
}
