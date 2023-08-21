using Newtonsoft.Json;
using Phoenix.Client.Authenticators.PhoenixAPI;
using Phoenix.Client.Providers;
using Phoenix.Common;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Impl;
using System.Net.Http;
using System.Text;

namespace Phoenix.Client.Authenticators
{
    /// <summary>
    /// Phoenix Authenticator - Authenticate logins with the basic Phoenix authentication API
    /// </summary>
    public class PhoenixAuthenticator : AuthenticationComponent
    {
        public override string ID => "phoenix-authenticator";

        // Authentication response holder
        private class AuthResponse
        {
            public string? secret;
        }

        private PhoenixSession auth;
        private string? api = null;

        /// <summary>
        /// Creates the authenticator
        /// </summary>
        /// <param name="authData">Authentication data</param>
        /// <param name="api">API server</param>
        public PhoenixAuthenticator(PhoenixSession authData, string api = null)
        {
            auth = authData;
            this.api = api;
        }

        protected override void Define()
        {
        }

        public override void Init()
        {
            // Bind event
            Client.OnLateHandshake += clientLateHandshakeHandler;
        }

        public override void DeInit()
        {
            // Unbind event
            Client.OnLateHandshake -= clientLateHandshakeHandler;
        }

        private bool IsSecure(Connection connection)
        {
            // Check client type
            if (connection is NetworkClientConnection)
            {
                // Verify secure-mode
                NetworkClientConnection client = (NetworkClientConnection)connection;
                if (client.SecureMode)
                    return true;
            }
            return false;
        }

        private void clientLateHandshakeHandler(Connection connection, ConnectionEventArgs args)
        {
            byte[] magic = Encoding.UTF8.GetBytes("PHOENIXAUTHSTART");
            args.ClientOutput.WriteRawBytes(magic);
            for (int i = 0; i < magic.Length; i++)
            {
                if (magic[i] != args.ClientInput.ReadRawByte())
                {
                    // Log debug warning
                    GetLogger().Error("WARNING! Failed to authenticate due to the first bit of network traffic not being a Phoenix authentication packet.");
                    GetLogger().Error("Please make sure the order of loading for components subscribed to the late handshake event is the same on both client and server.");

                    // Disconnect
                    ThrowAuthenticationFailure(null);
                    connection.Close();
                    return;
                }
            }

            // Check secure mode
            if (IsSecure(connection))
            {
                // Secure-mode

                try
                {
                    // Build URL
                    GetLogger().Info("Attempting to authenticate with Phoenix...");
                    string url;
                    if (api == null)
                        url = PhoenixEnvironment.DefaultAPIServer;
                    else
                        url = api;
                    if (!url.EndsWith("/"))
                        url += "/";
                    url += "auth/joinserver";

                    // Get server ID
                    NetworkClientConnection client = (NetworkClientConnection)connection;
                    string serverID = client.RemoteServerID;

                    try
                    {
                        // Contact login API
                        GetLogger().Trace("Contacting API: " + url + ", server ID: " + serverID + ", requesting join secret...");
                        HttpClient cl = new HttpClient();
                        string payload = JsonConvert.SerializeObject(new Dictionary<string, object>() { ["serverID"] = serverID });
                        cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + auth.GameSessionToken);
                        string result = cl.PostAsync(url, new StringContent(payload)).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        // Handle response
                        AuthResponse? response = JsonConvert.DeserializeObject<AuthResponse>(result);
                        if (response == null || response.secret == null)
                        {
                            GetLogger().Trace("Received response: " + result);
                            GetLogger().Trace("Authentication failure!");
                            throw new IOException("Server rejected login token");
                        }
                        GetLogger().Trace("Received response: " + result.Replace(response.secret, "[REDACTED]"));
                        GetLogger().Trace("Sending to server...");
                        args.ClientOutput.WriteString(response.secret);
                    }
                    catch (Exception e)
                    {
                        GetLogger().Error("Failed to authenticate server login!", e);
                        ThrowAuthenticationFailure(null);
                        client.Close();
                        return;
                    }
                    if (!args.ClientInput.ReadBoolean())
                    {  
                        // Read disconnect reason
                        DisconnectParams? disconnectParams = null;
                        if (args.ClientInput.ReadBoolean())
                        {
                            string reason = args.ClientInput.ReadString();
                            string[] parameters = new string[args.ClientInput.ReadInt()];
                            for (int i = 0; i < parameters.Length; i++)
                                parameters[i] = args.ClientInput.ReadString();
                            disconnectParams = new DisconnectParams(reason, parameters);
                        }
                        
                        // Error
                        GetLogger().Warn("Authentication failure! Server rejected join request!");
                        ThrowAuthenticationFailure(disconnectParams);
                    }
                    else
                    {
                        GetLogger().Info("Authentication success! Joined as " + auth.DisplayName + "!");
                    }
                }
                catch
                {
                    ThrowAuthenticationFailure(null);
                }
            }
            else
            {
                // Insecure-mode
                GetLogger().Info("Sending account ID and display name to server...");
                args.ClientOutput.WriteString(auth.AccountID);
                args.ClientOutput.WriteString(auth.DisplayName);
                try
                {
                    if (!args.ClientInput.ReadBoolean())
                    {                        
                        // Read disconnect reason
                        DisconnectParams? disconnectParams = null;
                        if (args.ClientInput.ReadBoolean())
                        {
                            string reason = args.ClientInput.ReadString();
                            string[] parameters = new string[args.ClientInput.ReadInt()];
                            for (int i = 0; i < parameters.Length; i++)
                                parameters[i] = args.ClientInput.ReadString();
                            disconnectParams = new DisconnectParams(reason, parameters);
                        }

                        // Error
                        GetLogger().Warn("Authentication failure! Server rejected join request!");
                        ThrowAuthenticationFailure(disconnectParams);
                    }
                    else
                    {
                        GetLogger().Warn("Authentication success! Joined as " + auth.DisplayName + "!");
                    }
                }
                catch
                {
                    GetLogger().Warn("Authentication failure! Server rejected join request!");
                    ThrowAuthenticationFailure(null);
                }
            }
        }
    }
}
