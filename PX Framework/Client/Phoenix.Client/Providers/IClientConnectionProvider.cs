using Phoenix.Common.Networking.Connections;

namespace Phoenix.Client.Providers
{
    /// <summary>
    /// Client connection provider
    /// </summary>
    public interface IClientConnectionProvider
    {
        /// <summary>
        /// Provides the client connection
        /// </summary>
        /// <returns>Connection instance</returns>
        public Connection Provide();

        /// <summary>
        /// Called to start the client
        /// </summary>
        public void StartGameClient();

        /// <summary>
        /// Called to stop the client
        /// </summary>
        public void StopGameClient();

        /// <summary>
        /// Called to provide connection info
        /// </summary>
        /// <returns>ConnectionInfo instance</returns>
        public ConnectionInfo ProvideInfo();

        /// <summary>
        /// Simple container for a IP and Port
        /// </summary>
        public class ConnectionInfo
        {
            private string ip;
            private int port;

            public ConnectionInfo(string ip, int port)
            {
                this.ip = ip;
                this.port = port;
            }

            /// <summary>
            /// Retrieves the IP the client used to connect to the server
            /// </summary>
            public string ServerAddress
            {
                get
                {
                    return ip;
                }
            }

            /// <summary>
            /// Retrieves the port the client used to connect with the server
            /// </summary>
            public int Port
            {
                get
                {
                    return port;
                }
            }
        }
    }
}
