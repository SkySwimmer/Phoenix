namespace Phoenix.Client.Authenticators.PhoenixAPI
{
    /// <summary>
    /// Phoenix login deferred message
    /// </summary>
    public class LoginDeferredMessage
    {
        /// <summary>
        /// Handler for the Retry method
        /// </summary>
        /// <param name="request">New login request</param>
        public delegate void RetryCallbackHandler(Dictionary<string, object> request);

        private RetryCallbackHandler _retryCallbackHandler;
        private Dictionary<string, object> _serverResponse;
        private string _dataRequestKey;

        public LoginDeferredMessage(Dictionary<string, object> serverResponse, string dataRequestKey, RetryCallbackHandler retryCallbackHandler)
        {
            _retryCallbackHandler = retryCallbackHandler;
            _serverResponse = serverResponse;
            _dataRequestKey = dataRequestKey;
        }

        /// <summary>
        /// Retries the request with additional data
        /// </summary>
        /// <param name="request">New request message (recommended to send the old message with the missing data added to it)</param>
        public void Retry(Dictionary<string, object> request)
        {
            _retryCallbackHandler(request);
        }

        /// <summary>
        /// Retrieves the ID of what the server requires to be added to the login payload
        /// </summary>
        public string DataRequestKey
        {
            get
            {
                return _dataRequestKey;
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
