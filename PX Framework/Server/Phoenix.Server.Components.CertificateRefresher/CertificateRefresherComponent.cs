using Newtonsoft.Json;
using Phoenix.Common;
using Phoenix.Common.Certificates;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Tasks;
using Phoenix.Server.Configuration;
using System.Net.Http;
using System.Text;

namespace Phoenix.Server.Components
{
    public class CertificateRefresherComponent : ServerComponent
    {
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
        public override string ID => "certificate-refresher";

        protected override string ConfigurationKey => "server";

        protected override void Define()
        {
            DependsOn("task-manager");
        }

        public override void StartServer()
        {
            // Set up refresh
            if (Server.ServerConnection is NetworkServerConnection)
            {
                // Verify certificate & token validity
                bool secureMode = Configuration.GetBool("secure-mode");
                if (secureMode)
                {
                    // Read server token
                    string? token = Configuration.GetString("token");
                    if (!secureMode)
                        token = "disabled";
                    else if (token == null || token == "undefined" || token.Split('.').Length != 3)
                    {
                        Configuration.SetString("token", "undefined");
                        secureMode = false;
                        token = "undefined";
                    }

                    // Check token validity
                    string serverID = "";
                    long tokenIssueTime = -1;
                    if (secureMode)
                    {
                        // Decode the payload
                        string[] jwt = token.Split('.');
                        string payloadEncoded = jwt[1];
                        try
                        {
                            string payload = Encoding.UTF8.GetString(Base64Url.Decode(payloadEncoded));
                            Dictionary<string, object>? data = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
                            if (data == null || !data.ContainsKey("exp"))
                                throw new ArgumentException();

                            // Check expiry
                            long exp = long.Parse(data["exp"].ToString());
                            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > exp)
                                throw new ArgumentException();
                            serverID = data["sub"].ToString();
                            tokenIssueTime = long.Parse(data["iat"].ToString());
                            if (data["cgi"].ToString() != Game.GameID)
                                throw new ArgumentException();

                            AbstractConfigurationSegment? certificate = Configuration.GetSegment("certificate");
                            if (certificate != null && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() <= (certificate.GetLong("expiry") - (7 * 24 * 60 * 60 * 1000)) && tokenIssueTime == certificate.GetLong("tokenIssueTime") && Enumerable.SequenceEqual(Configuration.GetStringArray("addresses"), certificate.GetStringArray("addressesInternal")))
                            {
                                // Valid

                                // Set up a refresh task
                                ScheduledTask task = null;
                                task = Server.ServiceManager.GetService<TaskManager>().Interval(() => {
                                    // Check if a refresh is needed
                                    if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > (certificate.GetLong("expiry") - (7 * 24 * 60 * 60 * 1000)) || tokenIssueTime != certificate.GetLong("tokenIssueTime") || !Enumerable.SequenceEqual(Configuration.GetStringArray("addresses"), certificate.GetStringArray("addressesInternal")))
                                    {
                                        // Log
                                        GetLogger().Info("Refreshing server certificate...");

                                        // Attempt to refresh certificate
                                        try
                                        {
                                            HttpClient cl = new HttpClient();
                                            string payload = JsonConvert.SerializeObject(new Dictionary<string, object>() { ["addresses"] = Configuration.GetStringArray("addresses", new string[0]) });
                                            cl.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                                            string result = cl.PostAsync(Configuration.GetString("phoenix-api-server") + "/refreshserver", new StringContent(payload)).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                                            RefreshResponse? response = JsonConvert.DeserializeObject<RefreshResponse>(result);
                                            if (response == null)
                                                throw new IOException();

                                            // Update token
                                            token = response.token;
                                            Configuration.SetString("token", token);
                                            string[] jwt = token.Split('.');
                                            string payloadEncoded = jwt[1];
                                            try
                                            {
                                                payload = Encoding.UTF8.GetString(Base64Url.Decode(payloadEncoded));
                                                Dictionary<string, object>? data = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);
                                                if (data == null || !data.ContainsKey("exp"))
                                                    throw new ArgumentException();

                                                // Check expiry
                                                long exp = long.Parse(data["exp"].ToString());
                                                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > exp)
                                                    throw new ArgumentException();
                                                tokenIssueTime = long.Parse(data["iat"].ToString());
                                            }
                                            catch
                                            {
                                            }

                                            // Set ID
                                            serverID = response.identity;

                                            // Update certificate
                                            certificate.SetLong("expiry", response.certificate.expiry);
                                            certificate.SetLong("lastUpdate", response.certificate.lastUpdate);
                                            certificate.SetStringArray("addressesInternal", response.certificate.addresses);
                                            Configuration.SetStringArray("addresses", response.certificate.addresses);
                                            certificate.SetString("publicKey", response.certificate.publicKey);
                                            certificate.SetString("privateKey", response.certificate.privateKey);
                                            certificate.SetLong("tokenIssueTime", tokenIssueTime);

                                            // Create the certificate object
                                            RefreshResponse resp = new RefreshResponse();
                                            resp.identity = serverID;
                                            resp.certificate = new CertificateObject();
                                            resp.certificate.addresses = response.certificate.addresses;
                                            resp.certificate.expiry = response.certificate.expiry;
                                            resp.certificate.lastUpdate = response.certificate.lastUpdate;
                                            resp.certificate.privateKey = response.certificate.privateKey;
                                            resp.certificate.publicKey = response.certificate.publicKey;
                                            resp.token = token;
                                            PXServerCertificate srvCertificate = PXServerCertificate.FromJson(Game.GameID, JsonConvert.SerializeObject(resp));

                                            // Swap certificate
                                            NetworkServerConnection conn = (NetworkServerConnection)Server.ServerConnection;
                                            conn.UpdateCertificate(srvCertificate);
                                            GetLogger().Info("Certificate refreshed successfully.");
                                        }
                                        catch
                                        {
                                            GetLogger().Warn("Failed to refresh the server certificate!");
                                            GetLogger().Warn("Players will likely be unable to connect to the server soon!");
                                            GetLogger().Warn("Please check the internet connection, the automatic refresh system will no longer run til the next server restart.");
                                            Server.ServiceManager.GetService<TaskManager>().Cancel(task);
                                        }
                                    }
                                }, 60 * 60 * 100);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
        
        public class RefreshResponse
        {
            public string identity;
            public CertificateObject certificate;
            public string token;
        }

        public class CertificateObject
        {
            public long lastUpdate;
            public long expiry;
            public string[] addresses;
            public string publicKey;
            public string privateKey;
        }
    }
}