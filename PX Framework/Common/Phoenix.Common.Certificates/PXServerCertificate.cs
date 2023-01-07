using Phoenix.Common.IO;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System.Text;

namespace Phoenix.Common.Certificates
{
    /// <summary>
    /// Phoenix Server Certificate
    /// </summary>
    public class PXServerCertificate
    {
        private static SecureRandom rnd = new SecureRandom();

        private string _gameID;
        private string _serverID;
        private long _certGenTime;
        private long _certExpiryTime;
        private AsymmetricKeyParameter _privateKey;

        // Internal
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private PXServerCertificate() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// Creates a PXServerCertificate instance from certificate properties
        /// </summary>
        /// <returns>PXServerCertificate instance</returns>
        public static PXServerCertificate FromCertificateProperties(string gameID, string serverID, long generationTime, long expiryTime, AsymmetricKeyParameter privateKey)
        {
            PXServerCertificate cert = new PXServerCertificate();
            cert._gameID = gameID;
            cert._serverID = serverID;
            cert._certGenTime = generationTime;
            cert._certExpiryTime = expiryTime;
            cert._privateKey = privateKey;
            return cert;
        }

        /// <summary>
        /// Creates a PXServerCertificate instance from a data reader
        /// </summary>
        /// <param name="reader">Reader to read from</param>
        /// <returns>PXServerCertificate instance</returns>
        public static PXServerCertificate FromReader(DataReader reader)
        {
            string gameID = reader.ReadString();
            string serverID = reader.ReadString();
            long generationTime = reader.ReadLong();
            long expiryTime = reader.ReadLong();
            StringReader r = new StringReader(Encoding.UTF8.GetString(reader.ReadAllBytes()));
            PemReader pem = new PemReader(r);
            PXServerCertificate cert = FromCertificateProperties(gameID, serverID, generationTime, expiryTime, (AsymmetricKeyParameter)pem.ReadObject());
            r.Dispose();
            return cert;
        }

        /// <summary>
        /// Creates a PXServerCertificate instance from a PX API JSON-encoded server identity object
        /// </summary>
        /// <param name="gameId">Game ID</param>
        /// <param name="json">JSON-encoded server identity</param>
        /// <returns>PXServerCertificate instance</returns>
        public static PXServerCertificate FromJson(string gameId, string json)
        {
            JsonEncodedServerIdentity? identity = JsonConvert.DeserializeObject<JsonEncodedServerIdentity>(json);
            if (identity == null || identity.certificate == null || identity.certificate.expiry == 0
                    || identity.certificate.lastUpdate == 0 || identity.certificate.publicKey == null
                    || identity.identity == null || identity.token == null)
                throw new ArgumentException("Invalid json");
            StringReader r = new StringReader(identity.certificate.privateKey);
            PemReader pem = new PemReader(r);
            PXServerCertificate cert = FromCertificateProperties(gameId, identity.identity, identity.certificate.lastUpdate
                    , identity.certificate.expiry, (AsymmetricKeyParameter)pem.ReadObject());
            r.Dispose();
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
        /// Retrieves the certificate private key
        /// </summary>
        public AsymmetricKeyParameter PrivateKey
        {
            get
            {
                return _privateKey;
            }
        }

        /// <summary>
        /// Writes the certificate to a client
        /// </summary>
        /// <param name="writer">Output writer</param>
        public void WriteCertificateTo(DataWriter writer)
        {
            MemoryStream strm = new MemoryStream();
            DataWriter wr = new DataWriter(strm);
            wr.WriteString(GameID);
            wr.WriteString(ServerID);
            wr.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            wr.WriteLong(GenerationTimestamp);
            wr.WriteLong(ExpiryTimestamp);
            wr.WriteInt(rnd.Next());
            byte[] data = strm.ToArray();
            strm.Close();

            // Sign
            ISigner signer = SignerUtilities.GetSigner("Sha512WithRSA");
            signer.Init(true, _privateKey);
            signer.BlockUpdate(data, 0, data.Length);
            byte[] sig = signer.GenerateSignature();

            // Write
            writer.WriteBytes(data);
            writer.WriteBytes(sig);
        }
    }
}
