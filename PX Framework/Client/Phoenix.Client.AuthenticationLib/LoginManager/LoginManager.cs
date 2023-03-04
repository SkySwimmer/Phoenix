using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Phoenix.Common;
using System.Net.Http;
using System.Text;

namespace Phoenix.Client.Authenticators.PhoenixAPI
{
    /// <summary>
    /// Handler for session refresh failures
    /// </summary>
    public delegate void SessionRefreshFailureHandler();

    /// <summary>
    /// Handler for when logins are deferred
    /// </summary>
    /// <param name="deferMessage">Server response message</param>
    public delegate void LoginDeferredHandler(LoginDeferredMessage deferMessage);

    /// <summary>
    /// Handler for when logins fail
    /// </summary>
    /// <param name="failureMessage">Server response message</param>
    public delegate void LoginFailureHandler(LoginFailureMessage failureMessage);

    /// <summary>
    /// Handler for successful logins
    /// </summary>
    /// <param name="session">Session instance</param>
    public delegate void LoginSuccessHandler(PhoenixSession session);

    /// <summary>
    /// Phoenix Login Manager
    /// </summary>
    public static class LoginManager
    {
        private static PhoenixSession? _session;

        /// <summary>
        /// Called when the login manager fails to refresh session data and logs out
        /// </summary>
        public static event SessionRefreshFailureHandler? OnSessionRefreshFailure;

        /// <summary>
        /// Defines the API used to log into the game
        /// </summary>
        public static string? API = null;

        /// <summary>
        /// Defines the token used to call the authentication API, null makes it use the token from the Game descriptor
        /// </summary>
        public static string? LoginToken = null;

        /// <summary>
        /// Checks if a user is presently logged in
        /// </summary>
        public static bool IsLoggedIn
        {
            get
            {
                return _session != null;
            }
        }

        /// <summary>
        /// Retrieves the current session
        /// </summary>
        public static PhoenixSession Session
        {
            get
            {
                if (_session == null)
                    throw new InvalidOperationException("Not logged in");
                return _session;
            }
        }

        /// <summary>
        /// Logs out and clears the session
        /// </summary>
        public static void Logout()
        {
            _session = null;
        }

        /// <summary>
        /// Logs into the authentication API to retrieve login information
        /// </summary>
        /// <param name="loginPayload">Login payload message</param>
        /// <param name="onFailure">Login failure handler</param>
        /// <param name="onDefer">Login deferred handler</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool Login(Dictionary<string, object> loginPayload, LoginFailureHandler onFailure, LoginDeferredHandler onDefer, LoginSuccessHandler onSuccess)
        {
            if (IsLoggedIn)
                Logout();

            try
            {
                bool retry = false;
                while (true) {
                    retry = false;
                    // Build URL
                    string? tkn = LoginToken;
                    if (tkn == null)
                        tkn = Game.SessionToken;
                    string url;
                    if (API == null)
                        url = PhoenixEnvironment.DefaultAPIServer;
                    else
                        url = API;
                    if (!url.EndsWith("/"))
                        url += "/";
                    url += "auth/authenticate";

                    // Contact phoenix
                    HttpClient cl = new HttpClient();
                    string payload = JsonConvert.SerializeObject(loginPayload);
                    cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + tkn);
                    string result = cl.PostAsync(url, new StringContent(payload)).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    Dictionary<string, object>? response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
                    if (response == null || !response.ContainsKey("status"))
                    {
                        // Handle unparseable response
                        if (response != null && response.ContainsKey("error"))
                            onFailure(new LoginFailureMessage(response, response["error"].ToString(), response.ContainsKey("errorMessage") ? response["errorMessage"].ToString() : "Missing error message, server error"));
                        else
                            throw new Exception("Invalid response data");
                    }
                    else
                    {
                        // Handle response
                        switch (response["status"])
                        {
                            case "success":
                                {
                                    string acc = response["accountID"].ToString();
                                    string dsp = response["displayName"].ToString();
                                    string ses = response["sessionToken"].ToString();
                                    PhoenixSession sesData = new PhoenixSession(acc, dsp, ses, response);
                                    _session = sesData;
                                    onSuccess(_session);

                                    // Start refresh
                                    Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() =>
                                    {
                                        while (IsLoggedIn)
                                        {
                                            try
                                            {
                                                if (Session != sesData)
                                                    break;

                                                // Parse token
                                                string[] parts = tkn.Split('.');
                                                string payloadJson = Encoding.UTF8.GetString(Base64Url.Decode(parts[1]));
                                                JObject payload = JsonConvert.DeserializeObject<JObject>(payloadJson);
                                                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (15 * 60) >= payload.GetValue("exp").ToObject<long>())
                                                {
                                                    if (!sesData.Refresh())
                                                    {
                                                        try
                                                        {
                                                            OnSessionRefreshFailure?.Invoke();
                                                        }
                                                        finally
                                                        {
                                                            Logout();
                                                        }
                                                        break;
                                                    }
                                                }

                                                Thread.Sleep(1000);
                                            }
                                            catch
                                            {
                                                if (IsLoggedIn && Session == sesData)
                                                    try
                                                    {
                                                        OnSessionRefreshFailure?.Invoke();
                                                    }
                                                    finally
                                                    {
                                                        Logout();
                                                    }
                                                break;
                                            }
                                        }
                                    });

                                    return true;
                                }
                            case "deferred":
                                {
                                    onDefer(new LoginDeferredMessage(response, response["dataRequestKey"].ToString(), req => {
                                        retry = true;
                                        loginPayload = req;
                                    }));
                                    break;
                                }
                            case "failure":
                                {
                                    onFailure(new LoginFailureMessage(response, response["error"].ToString(), response["errorMessage"].ToString()));
                                    return false;
                                }
                            default:
                                throw new Exception("Invalid response data");
                        }
                    }
                    if (!retry)
                    {
                        onFailure(new LoginFailureMessage(new Dictionary<string, object>()
                        {
                            ["error"] = "deferred",
                            ["errorMessage"] = "Login was deferred and not handled"
                        }, "deferred", "Login was deferred and not handled"));
                        return false;
                    }
                }
            }
            catch (IOException)
            {
                onFailure(new LoginFailureMessage(new Dictionary<string, object>()
                {
                    ["error"] = "connect_error",
                    ["errorMessage"] = "Could not connect to the authentication API"
                }, "connect_error", "Could not connect to the authentication API"));
                return false;
            }
            catch (Exception)
            {
                onFailure(new LoginFailureMessage(new Dictionary<string, object>()
                {
                    ["error"] = "processor_error",
                    ["errorMessage"] = "Failed to process login request or response, unknown error"
                }, "processor_error", "Failed to process login request or response, unknown error"));
                return false;
            }

            // Failed
            return false;
        }

        private static class Base64Url
        {
            public static string Encode(byte[] arg)
            {
                if (arg == null)
                {
                    throw new ArgumentNullException("arg");
                }

                var s = Convert.ToBase64String(arg);
                return s
                    .Replace("=", "")
                    .Replace("/", "_")
                    .Replace("+", "-");
            }

            public static string ToBase64(string arg)
            {
                if (arg == null)
                {
                    throw new ArgumentNullException("arg");
                }

                var s = arg
                        .PadRight(arg.Length + (4 - arg.Length % 4) % 4, '=')
                        .Replace("_", "/")
                        .Replace("-", "+");

                return s;
            }

            public static byte[] Decode(string arg)
            {
                return Convert.FromBase64String(ToBase64(arg));
            }
        }

    }
}