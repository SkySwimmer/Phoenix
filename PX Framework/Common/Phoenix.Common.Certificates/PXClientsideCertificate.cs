using Phoenix.Common.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using System.Text;
using System.Net.Http;

namespace Phoenix.Common.Certificates
{
    public class PXClientsideCertificate
    {
        private string _gameID;
        private string _serverID;
        private string[] _addresses;
        private long _certGenTime;
        private long _certExpiryTime;
        private AsymmetricKeyParameter _publicKey;

        // Internal
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private PXClientsideCertificate() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// Downloads a certificate from a Phoenix server
        /// </summary>
        /// <param name="phoenixServer">Phoenix Server URL</param>
        /// <param name="gameID">Game ID</param>
        /// <param name="serverID">Server ID</param>
        /// <returns>PXClientsideCertificate instance</returns>
        public static PXClientsideCertificate Download(string phoenixServer, string gameID, string serverID)
        {
            HttpClient client = new HttpClient();
            Stream strm = client.GetStreamAsync(phoenixServer + "/servers/certificate/" + gameID + "/" + serverID).GetAwaiter().GetResult();
            DataReader rd = new DataReader(strm);
            PXClientsideCertificate cert = PXClientsideCertificate.FromReader(rd);
            strm.Close();
            return cert;
        }

        /// <summary>
        /// Creates a PXClientsideCertificate instance from a data reader
        /// </summary>
        /// <param name="reader">Input reader</param>
        /// <returns>PXClientsideCertificate instance</returns>
        public static PXClientsideCertificate FromReader(DataReader reader)
        {
            // Create object
            PXClientsideCertificate cert = new PXClientsideCertificate();

            // Parse
            cert._gameID = reader.ReadString();
            cert._serverID = reader.ReadString();
            cert._addresses = new string[reader.ReadInt()];
            for (int i = 0; i < cert._addresses.Length; i++)
            {
                cert._addresses[i] = reader.ReadString();
            }
            cert._certGenTime = reader.ReadLong();
            cert._certExpiryTime = reader.ReadLong();
            string pem = Encoding.UTF8.GetString(reader.ReadAllBytes());
            StringReader r = new StringReader(pem);
            PemReader rd = new PemReader(r);
            cert._publicKey = (AsymmetricKeyParameter)rd.ReadObject();
            r.Dispose();

            // Return certificate
            return cert;
        }

        /// <summary>
        /// Creates a PXClientsideCertificate instance from a set of properties
        /// </summary>
        /// <returns>PXClientsideCertificate instance</returns>
        public static PXClientsideCertificate FromProperties(string gameID, string serverID, string[] addresses, long generationTime, long expiry, AsymmetricKeyParameter publicKey)
        {
            PXClientsideCertificate cert = new PXClientsideCertificate();
            cert._gameID = gameID;
            cert._serverID = serverID;
            cert._addresses = addresses;
            cert._certGenTime = generationTime;
            cert._certExpiryTime = expiry;
            cert._publicKey = publicKey;
            return cert;
        }

        /// <summary>
        /// Retrieves the game ID
        /// </summary>
        public string GameID
        {
            get
            {
                return _gameID;
            }
        }

        /// <summary>
        /// Retrieves the server ID
        /// </summary>
        public string ServerID
        {
            get
            {
                return _serverID;
            }
        }

        /// <summary>
        /// Retrieves the certificate generation timestamp (miliseconds)
        /// </summary>
        public long GenerationTimestamp
        {
            get
            {
                return _certGenTime;
            }
        }

        /// <summary>
        /// Retrieves the certificate expiry timestamp (miliseconds)
        /// </summary>
        public long ExpiryTimestamp
        {
            get
            {
                return _certExpiryTime;
            }
        }

        /// <summary>
        /// Retrieves an array of valid server addresses
        /// </summary>
        public string[] Addresses
        {
            get
            {
                return _addresses;
            }
        }

        /// <summary>
        /// Retrieves the public key
        /// </summary>
        public AsymmetricKeyParameter PublicKey
        {
            get
            {
                return _publicKey;
            }
        }
    }
}