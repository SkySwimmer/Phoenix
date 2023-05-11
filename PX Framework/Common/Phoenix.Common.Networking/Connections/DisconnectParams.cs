namespace Phoenix.Common.Networking.Connections
{
    /// <summary>
    /// Disconnect information
    /// </summary>
    public class DisconnectParams
    {
        private string reason;
        private string[] args;

        public DisconnectParams(string reason, params string[] args)
        {
            this.reason = reason;
            this.args = args;
        }

        /// <summary>
        /// Disconnect reason key
        /// </summary>
        public string Reason { get { return reason; } }

        /// <summary>
        /// Disconnect reason parameters
        /// </summary>
        public string[] ReasonParameters { get { return args; } }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Reason;
        }
    }
}