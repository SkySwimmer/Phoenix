namespace Phoenix.Client.Authenticators.PhoenixAPI
{
    /// <summary>
    /// Phoenix login failure message
    /// </summary>
    public class LoginFailureMessage
    {
        private Dictionary<string, object> _serverResponse ;
        private string _error;
        private string _errorMessage;

        public LoginFailureMessage(Dictionary<string, object> serverResponse, string error, string errorMessage)
        {
            _serverResponse = serverResponse;
            _errorMessage = errorMessage;
            _error = error;
        }

        /// <summary>
        /// Retrieves the error code
        /// </summary>
        public string Error
        {
            get
            {
                return _error;
            }
        }

        /// <summary>
        /// Retrieves the human-readable error message
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
        }

        /// <summary>
        /// Retrieves the raw response message
        /// </summary>
        public Dictionary<string, object> RawResponseMessage
        {
            get
            {
                return _serverResponse;
            }
        }
    }
}
