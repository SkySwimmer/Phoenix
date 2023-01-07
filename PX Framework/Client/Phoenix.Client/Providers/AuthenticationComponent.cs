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
        protected void ThrowAuthenticationFailure()
        {
            Client.FailAuth();
        }
    }
}
