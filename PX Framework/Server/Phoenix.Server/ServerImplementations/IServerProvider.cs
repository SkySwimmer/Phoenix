using Phoenix.Common.Networking.Connections;

namespace Phoenix.Server.ServerImplementations
{
    /// <summary>
    /// Interface for custom server implementations
    /// </summary>
    public interface IServerProvider
    {
        /// <summary>
        /// Called to create a server object
        /// </summary>
        public ServerConnection ProvideServer();

        /// <summary>
        /// Called to start the server
        /// </summary>
        public void StartGameServer();

        /// <summary>
        /// Called to stop the server
        /// </summary>
        public void StopGameServer();
    }
}
