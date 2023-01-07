using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace Phoenix.Client.Authenticators.PhoenixAPI
{
    /// <summary>
    /// Phoenix Game Session - Contains player information needed to log in
    /// </summary>
    public class PhoenixSession
    {
        private string _accountID;
        private string _displayName;
        private string _token;

        private Dictionary<string, object> _serverResponse = new Dictionary<string, object>();

        /// <summary>
        /// Creates a full login response container
        /// </summary>
        /// <param name="accountID">Player account ID</param>
        /// <param name="displayName">Player display name</param>
        /// <param name="token">Game session token</param>
        /// <param name="serverResponse">Server response message</param>
        public PhoenixSession(string accountID, string displayName, string token, Dictionary<string, object> serverResponse)
        {
            _accountID = accountID;
            _displayName = displayName;
            _token = token;
            _serverResponse = serverResponse;
        }

        /// <summary>
        /// Creates a simple login response container
        /// </summary>
        /// <param name="accountID">Player account ID</param>
        /// <param name="displayName">Player display name</param>
        /// <param name="token">Game session token</param>
        public PhoenixSession(string accountID, string displayName, string token)
        {
            _accountID = accountID;
            _displayName = displayName;
            _token = token;
        }

        /// <summary>
        /// Creates a simple login response container <b>without token</b> (insecure-mode only)
        /// </summary>
        /// <param name="accountID">Player account ID</param>
        /// <param name="displayName">Player display name</param>
        public PhoenixSession(string accountID, string displayName)
        {
            _accountID = accountID;
            _displayName = displayName;
            _token = "undefined";
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

        /// <summary>
        /// Account ID
        /// </summary>
        public string AccountID 
        { 
            get
            {
                return _accountID;
            }
        }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        /// <summary>
        /// Game token
        /// </summary>
        public string GameSessionToken
        {
            get
            {
                return _token;
            }
        }

        /// <summary>
        /// Refreshes the token and updates the display name
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool Refresh()
        {
            if (_token.Split('.').Length != 3)
                return false;

            // Attempt to refresh token
            try
            {
                // Create url
                string url = LoginManager.API;
                if (!url.EndsWith("/"))
                    url += "/";
                url += "api/tokens/refresh";

                // Refresh token
                HttpClient cl = new HttpClient();
                cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
                string result = cl.GetAsync(url).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (result != null && result != "")
                {
                    _token = result.Trim();

                    // Pull user info

                    // Create url
                    url = LoginManager.API;
                    if (!url.EndsWith("/"))
                        url += "/";
                    url += "api/identities/pullcurrent";

                    // Download info
                    result = cl.GetAsync(url).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (result != null && result != "")
                    {
                        // Parse
                        JObject? obj = JsonConvert.DeserializeObject<JObject>(result);
                        if (obj != null)
                        {
                            JToken? propsT = obj.GetValue("properties");
                            if (propsT == null)
                                return false;
                            JObject? props = propsT.ToObject<JObject>();
                            if (props == null)
                                return false;
                            JToken? dspT = props.GetValue("displayName");
                            if (dspT == null)
                                return false;
                            JObject? dspO = dspT.ToObject<JObject>();
                            if (dspO == null)
                                return false;
                            JToken? valueT = dspO.GetValue("value");
                            if (valueT == null)
                                return false;
                            _displayName = valueT.ToObject<string>();
                            return true;
                        }
                    }
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
