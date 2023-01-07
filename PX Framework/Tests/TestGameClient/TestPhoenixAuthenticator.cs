using Newtonsoft.Json;
using Phoenix.Client.Events;
using Phoenix.Client.Providers;
using Phoenix.Common.Events;
using Phoenix.Common.Networking.Connections;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace TestGameClient
{
    public class TestPhoenixAuthenticator : AuthenticationComponent
    {
        public class AuthResponse
        {
            public string secret;
        }

        private string gameToken;

        public TestPhoenixAuthenticator(string gameToken)
        {
            this.gameToken = gameToken;
        }

        public override string ID => "phoenix-authenticator";

        protected override void Define()
        {
        }

        public override void Init()
        {
            Client.OnLateHandshake += clientLateHandshakeHandler;
        }

        public override void DeInit()
        {
            Client.OnLateHandshake -= clientLateHandshakeHandler;
        }

        private void clientLateHandshakeHandler(Connection connection, ConnectionEventArgs args)
        {
            // Contact Phoenix to log into the server
            HttpClient cl = new HttpClient();
            string payload = JsonConvert.SerializeObject(new Dictionary<string, object>() { ["serverID"] = Connections.DownloadServerID("localhost", 16719) });
            cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + gameToken);
            string result = cl.PostAsync("https://aerialworks.ddns.net/api/auth/joinserver", new StringContent(payload)).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
            AuthResponse? response = JsonConvert.DeserializeObject<AuthResponse>(result);
            if (response == null || response.secret == null)
                response = new AuthResponse()
                {
                    secret = ""
                };
            args.ClientOutput.WriteString(response.secret);
            args.ClientInput.ReadBoolean();
        }
    }

    public class TestInsecureModeAuthenticator : AuthenticationComponent
    {
        public class AuthResponse
        {
            public string secret;
        }

        private string accountID;
        private string displayName;

        public TestInsecureModeAuthenticator(string accountID, string displayName)
        {
            this.accountID = accountID;
            this.displayName = displayName;
        }

        public override string ID => "phoenix-authenticator";

        protected override void Define()
        {
        }

        public override void Init()
        {
            Client.OnLateHandshake += clientLateHandshakeHandler;
        }

        public override void DeInit()
        {
            Client.OnLateHandshake -= clientLateHandshakeHandler;
        }

        private void clientLateHandshakeHandler(Connection connection, ConnectionEventArgs args)
        {
            args.ClientOutput.WriteString(accountID);
            args.ClientOutput.WriteString(displayName);
            args.ClientInput.ReadBoolean();
        }
    }
}