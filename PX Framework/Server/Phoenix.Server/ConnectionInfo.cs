namespace Phoenix.Server
{
    /// <summary>
    /// Simple container for a IP and Port, used by Phoenix for holding information on how the client connected (like what hostname or ip and port they used to reach the server, useful for wildcard domains)
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
        /// Retrieves the IP the client used to connect to the server (untranslated, this is what the client had in the IP box or received from the server list)
        /// </summary>
        public string ServerAddress
        {
            get
            {
                return ip;
            }
        }

        /// <summary>
        /// Retrieves the port the client used to connect with the server (unmapped, straight from the server IP/port box or server list)
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
