namespace Phoenix.Common.Certificates
{
    class JsonPrivateCertificate
    {
        public long lastUpdate;
        public long expiry;
        public string publicKey;
        public string privateKey;
        public string[] addresses;
    }

    class JsonEncodedServerIdentity
    {
        public string identity;
        public JsonPrivateCertificate certificate;
        public string token;
    }
}
