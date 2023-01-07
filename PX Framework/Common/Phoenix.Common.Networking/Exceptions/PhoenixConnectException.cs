namespace Phoenix.Common.Networking.Exceptions
{
    /// <summary>
    /// Phoenix Connection Exception
    /// </summary>
    public class PhoenixConnectException : IOException
    {
        private ErrorType error;

        public PhoenixConnectException(string message, ErrorType type) : base(message)
        {
            error = type;
        }

        public PhoenixConnectException(string message, ErrorType type, Exception source) : base(message, source)
        {
            error = type;
        }

        /// <summary>
        /// Retrieves the error type
        /// </summary>
        public ErrorType ErrorType
        {
            get
            {
                return error;
            }
        }
    }

    public enum ErrorType
    { 
        /// <summary>
        /// The client connected to a non-phoenix server or a outdated protocol
        /// </summary>
        NONPHOENIX,

        /// <summary>
        /// Invalid server certificate (client-side error)
        /// </summary>
        INVALID_CERTIFICATE,

        /// <summary>
        /// Client rejected server certificate (server-side error)
        /// </summary>
        REJECTED_CERTIFICATE,

        /// <summary>
        /// Encryption key was rejected by the server (client-side error)
        /// </summary>
        ENCRYPTION_KEY_REJECTED,

        /// <summary>
        /// An encrypted connection could not be established
        /// </summary>
        ENCRYPTION_FAILURE,

        /// <summary>
        /// Program-specific handshake failure
        /// </summary>
        PROGRAM_HANDSHAKE_FAILURE
    }
}
