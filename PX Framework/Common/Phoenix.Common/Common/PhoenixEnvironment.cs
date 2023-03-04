namespace Phoenix.Common
{
    /// <summary>
    /// Phoenix Environment Settings
    /// </summary>
    public static class PhoenixEnvironment
    {
        private static string _api = "https://aerialworks.ddns.net/api/";

        /// <summary>
        /// Default API server
        /// </summary>
        public static string DefaultAPIServer
        {
            get
            {
                return _api;
            }
            set
            {
                _api = value;
                if (!_api.EndsWith("/"))
                    _api += "/";
            }
        }
    }
}
