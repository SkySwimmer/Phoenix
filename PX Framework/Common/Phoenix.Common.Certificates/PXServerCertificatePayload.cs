using Phoenix.Common.IO;

namespace Phoenix.Common.Certificates
{
    public class PXServerCertificatePayload
    {
        private string _gameID;
        private string _serverID;
        private long _certGenTime;
        private long _certExpiryTime;
        private long _serverTime;
        private byte[] _cert;
        private byte[] _sig;

        // Internal
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private PXServerCertificatePayload() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// Creates a PXServerCertificatePayload instance from a data reader
        /// </summary>
        /// <param name="reader">Input reader</param>
        /// <returns>PXServerCertificatePayload instance</returns>
        public static PXServerCertificatePayload FromReader(DataReader reader)
        {
            // Create object
            PXServerCertificatePayload cert = new PXServerCertificatePayload();
            cert._cert = reader.ReadBytes();
            cert._sig = reader.ReadBytes();

            // Parse
            MemoryStream certD = new MemoryStream(cert._cert);
            DataReader rd = new DataReader(certD);
            cert._gameID = rd.ReadString();
            cert._serverID = rd.ReadString();
            cert._serverTime = rd.ReadLong();
            cert._certGenTime = rd.ReadLong();
            cert._certExpiryTime = rd.ReadLong();
            certD.Close();

            // Return certificate
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
        /// Retrieves the server timestamp (miliseconds)
        /// </summary>
        public long ServerTime
        {
            get
            {
                return _serverTime;
            }
        }

        /// <summary>
        /// Retrieves the signature of the certificate
        /// </summary>
        public byte[] Signature
        {
            get
            {
                return _sig;
            }
        }

        /// <summary>
        /// Retrieves the encoded certificate (without signature)
        /// </summary>
        public byte[] Certificate
        {
            get
            {
                return _cert;
            }
        }

    }
}
