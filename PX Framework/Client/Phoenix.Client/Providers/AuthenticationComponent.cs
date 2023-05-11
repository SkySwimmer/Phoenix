using Phoenix.Common.Networking.Connections;

namespace Phoenix.Client.Providers
{
    /// <summary>
    /// Authentication component
    /// </summary>
    public abstract class AuthenticationComponent : ClientComponent
    {
        /// <summary>
        /// Fails the connection attempt with an authentication error
        /// </summary>
        /// <param name="disconnectParams">Disconnect reason</param>
        protected void ThrowAuthenticationFailure(DisconnectParams? disconnectParams)
        {
            Client.FailAuth(disconnectParams);
        }
    }
}
