using Phoenix.Common.Certificates;
using Phoenix.Common.IO;
using Phoenix.Common.Logging;
using Phoenix.Common.Networking.Channels;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.Networking.Registry;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using Phoenix.Common.Networking.Exceptions;

namespace Phoenix.Common.Networking.Impl
{
    public delegate bool BoolAction();
    public class NetworkClientConnection : Connection
    {
        private Logger logger;
        private static SecureRandom rnd = new SecureRandom();

        private static object _locker = new object();
        private bool _sending;

        private string _ip;
        private string? _serverID;

        private long timeSinceLastPacketSent;

        public string RemoteServerID
        {
            get
            {
                if (_serverID == null)
                    throw new InvalidOperationException("Not a secure client connection");
                return _serverID;
            }
        }

        private int _port;
        private bool _disconnected;
        private bool _connected;
        private TcpClient Client;
        private ConnectionSide _side;

        private string _address;
        private PXServerCertificate _serverCertificate;
        private PXClientsideCertificate _clientCertificate;
        private BoolAction _serverHandshake;
        private bool _secureMode;

        private CipherStream Stream;
        public DataReader Reader;
        public DataWriter Writer;
        private string addr;

        public bool SecureMode
        {
            get
            {
                return _secureMode;
            }
        }

        public void InitClient(string ip, int port, ChannelRegistry registry, PXClientsideCertificate certificate, string address)
        {
            // Assign fields
            _ip = ip;
            _port = port;
            _side = ConnectionSide.CLIENT;
            _clientCertificate = certificate;
            _address = address;
            if (certificate != null)
            {
                _secureMode = true;
                _serverID = certificate.ServerID;
            }

            // Register channels
            foreach (PacketChannel ch in registry.Channels)
            {
                RegisterChannel(ch);
            }
        }

        private string? str;
        public void InitServer(TcpClient client, ChannelRegistry registry, PXServerCertificate certificate, BoolAction handshake)
        {
            InitClient(null, 0, registry, null, null);
            Client = client;
            if (client != null)
                str = "Network Client: " + Client.Client.RemoteEndPoint;
            _side = ConnectionSide.SERVER;
            _serverCertificate = certificate;
            if (certificate != null)
                _secureMode = true;
            _serverHandshake = handshake;
        }

        public override ConnectionSide Side => _side;

        public override void Open()
        {
            if (_connected)
                throw new InvalidOperationException("Connection already open");

            // Init logger
            logger = Logger.GetLogger("Client: " + GetRemoteAddress());
            bool lessSecure = false;
            if (_side == ConnectionSide.CLIENT && _ip != null)
            {
                logger.Trace("Attempting to connect to [" + _ip + "]:" + _port + "...");
                if (_ip.StartsWith("lesssecure:"))
                {
                    lessSecure = true;
                    _ip = _ip.Substring("lesssecure:".Length);
                }
                Client = new TcpClient(_ip, _port);
                str = "Network Client: " + Client.Client.RemoteEndPoint;
            }

            // Open the connection
            if (Client == null)
                throw new InvalidOperationException("Connection closed");
            addr = ((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString();
            logger.Trace("Client connected, remote address: " + addr);

            // Handshake
            try
            {
                if (_side == ConnectionSide.CLIENT && _ip != null)
                {
                    // Send hello
                    logger.Trace("Attempting Phoenix networking handshake with protocol version " + Connections.Connections.PhoenixProtocolVersion + "...");
                    byte[] hello = Encoding.UTF8.GetBytes("PHOENIX/HELLO/" + Connections.Connections.PhoenixProtocolVersion + "/");
                    byte[] helloSrv = Encoding.UTF8.GetBytes("PHOENIX/HELLO/SERVER/" + Connections.Connections.PhoenixProtocolVersion + "/");
                    logger.Debug("Sending HELLO messsage...");
                    Client.GetStream().Write(hello);
                    int i2 = 0;
                    foreach (byte b in helloSrv)
                    {
                        int i = Client.GetStream().ReadByte();
                        if (i == -1)
                        {
                            logger.Trace("Received handshake HELLO packet is invalid");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            _ip = null;
                            _port = 0;
                            throw new PhoenixConnectException("Connection failed: connection lost during HELLO", ErrorType.NONPHOENIX);
                        }
                        if (helloSrv[i2++] != i)
                        {
                            logger.Trace("Received handshake HELLO packet is invalid");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            _ip = null;
                            _port = 0;
                            throw new PhoenixConnectException("Connection failed: invalid server response during HELLO", ErrorType.NONPHOENIX);
                        }
                    }

                    // Send endpoint
                    logger.Debug("Sending connection endpoint....");
                    DataWriter wr = new DataWriter(Client.GetStream());
                    wr.WriteString(_ip);
                    wr.WriteInt(_port);
                    _ip = null;
                    _port = 0;

                    // Set mode to connect
                    logger.Debug("Sending MODE packet: CONNECT...");
                    Client.GetStream().WriteByte(1);
                }
                if ((_side == ConnectionSide.CLIENT && _clientCertificate != null) || (_side == ConnectionSide.SERVER && _serverCertificate != null))
                {
                    if (_side == ConnectionSide.CLIENT)
                    {
                        logger.Debug("Reading server certificate from remote...");

                        // Read server certificate
                        PXServerCertificatePayload certificate = PXServerCertificatePayload.FromReader(new DataReader(Client.GetStream()));
                        logger.Debug("Certificate received: " + string.Concat(certificate.Certificate.Select(t => t.ToString("x2"))) + string.Concat(certificate.Signature.Select(t => t.ToString("x2"))));

                        // Verify signature
                        try
                        {
                            logger.Trace("Verifying server certificate...");
                            logger.Debug("Verifying server certificate signature...");
                            ISigner signer = SignerUtilities.GetSigner("Sha512WithRSA");
                            signer.Init(false, _clientCertificate.PublicKey);
                            signer.BlockUpdate(certificate.Certificate, 0, certificate.Certificate.Length);
                            bool valid = signer.VerifySignature(certificate.Signature);
                            if (!valid)
                            {
                                logger.Trace("Signature verification failure!");
                                throw new PhoenixConnectException("Connection failed: invalid server certificate", ErrorType.INVALID_CERTIFICATE);
                            }

                            // Verify certificate properties
                            logger.Debug("Verifying certificate expiry...");
                            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > certificate.ExpiryTimestamp || certificate.ExpiryTimestamp != _clientCertificate.ExpiryTimestamp)
                            {
                                logger.Trace("Certificate verification failure!");
                                throw new PhoenixConnectException("Connection failed: invalid server certificate", ErrorType.INVALID_CERTIFICATE);
                            }
                            logger.Debug("Verifying certificate generation timestamp...");
                            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < (certificate.GenerationTimestamp - 120000) || certificate.GenerationTimestamp != _clientCertificate.GenerationTimestamp)
                            {
                                logger.Trace("Certificate verification failure!");
                                throw new PhoenixConnectException("Connection failed: invalid server certificate", ErrorType.INVALID_CERTIFICATE);
                            }
                            logger.Debug("Verifying current server timestamp...");
                            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < (certificate.ServerTime - 120000) || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > certificate.ServerTime + 120000)
                            {
                                logger.Trace("Certificate verification failure!");
                                throw new PhoenixConnectException("Connection failed: invalid server certificate", ErrorType.INVALID_CERTIFICATE);
                            }

                            // Check addresses
                            if (_address != "localhost" && _address == "127.0.0.1" && _address == "::1" && !lessSecure)
                            {
                                logger.Trace("Verifying address... Checking if its part of the certificate...");

                                bool found = false;
                                foreach (string address in _clientCertificate.Addresses)
                                {
                                    logger.Debug("Trying " + address);
                                    if (address == _address)
                                    {
                                        logger.Debug("Valid address found: " + address);
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    logger.Trace("Invalid server address: " + _address + ", not in certificate!");
                                    throw new PhoenixConnectException("Connection failed: invalid server certificate", ErrorType.INVALID_CERTIFICATE);
                                }
                            }
                        }
                        catch
                        {
                            _serverCertificate = null;
                            _clientCertificate = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            throw new PhoenixConnectException("Connection failed: invalid server certificate", ErrorType.INVALID_CERTIFICATE);
                        }

                        // Success
                        logger.Debug("Sending verification success...");
                        Client.GetStream().WriteByte(55);

                        // Check if the server agrees
                        logger.Debug("Verifying remote verification success...");
                        int resp = Client.GetStream().ReadByte();
                        if (resp != 55)
                        {
                            logger.Debug("Remote verification failure!");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            throw new PhoenixConnectException("Connection failed: server sent invalid response", ErrorType.NONPHOENIX);
                        }

                        // Okay lets encrypt this connection
                        // Generate AES key
                        logger.Trace("Generating AES encryption key...");
                        CipherKeyGenerator gen = GeneratorUtilities.GetKeyGenerator("AES256");
                        byte[] key = gen.GenerateKey();

                        // Generate IV
                        logger.Trace("Generating AES encryption IV...");
                        byte[] iv = new SecureRandom().GenerateSeed(16);

                        // Encrypt the key and some data to make it more random
                        long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        int rnd1 = rnd.Next();
                        int rnd2 = rnd.Next();
                        logger.Debug("Creating key exchange payload...");
                        logger.Debug("    " + time);
                        logger.Debug("    [REDACTED]");
                        logger.Debug("    [REDACTED]");
                        logger.Debug("    " + certificate.ServerTime);
                        logger.Debug("    " + rnd1);
                        logger.Debug("    " + rnd2);
                        MemoryStream strm = new MemoryStream();
                        DataWriter wr = new DataWriter(strm);
                        wr.WriteLong(time);
                        wr.WriteBytes(key);
                        wr.WriteBytes(iv);
                        wr.WriteLong(certificate.ServerTime);
                        wr.WriteInt(rnd1);
                        wr.WriteInt(rnd2);
                        byte[] payload = strm.ToArray();
                        strm.Close();

                        // Encrypt it
                        logger.Debug("Encrypting payload...");
                        IAsymmetricBlockCipher cipher = new Pkcs1Encoding(new RsaEngine());
                        cipher.Init(true, _clientCertificate.PublicKey);
                        byte[] enc = cipher.ProcessBlock(payload, 0, payload.Length);

                        // Send to server and wait for response
                        logger.Trace("Sending encryption request...");
                        Client.GetStream().Write(enc);
                        logger.Trace("Verifying encryption response...");
                        resp = Client.GetStream().ReadByte();
                        if (resp != 243)
                        {
                            logger.Trace("Encryption failure!");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            throw new PhoenixConnectException("Connection failed: server rejected key", ErrorType.ENCRYPTION_KEY_REJECTED);
                        }

                        // Assign parameters
                        logger.Debug("Building AES ciphers...");
                        KeyParameter aesKey = new KeyParameter(key);
                        ParametersWithIV ivParams = new ParametersWithIV(aesKey, iv);
                        IBufferedCipher cipherEncrypt = CipherUtilities.GetCipher("AES/CFB8/NoPadding");
                        IBufferedCipher cipherDecrypt = CipherUtilities.GetCipher("AES/CFB8/NoPadding");
                        cipherEncrypt.Init(true, ivParams);
                        cipherDecrypt.Init(false, ivParams);
                        logger.Debug("Building AES cryptostream...");
                        Stream = new CipherStream(Client.GetStream(), cipherDecrypt, cipherEncrypt);
                        logger.Debug("Assigning output writer...");
                        Writer = new DataWriter(Stream);
                        logger.Debug("Assigning input reader...");
                        Reader = new DataReader(Stream);

                        // Send test
                        logger.Trace("Sending encryption test message...");
                        byte[] testMessage = new byte[127];
                        rnd.NextBytes(testMessage);
                        Writer.WriteBytes(testMessage);
                        Writer.WriteBytes(testMessage);
                        logger.Debug("Test message sent: " + string.Concat(testMessage.Select(t => t.ToString("x2"))) + string.Concat(testMessage.Select(t => t.ToString("x2"))));

                        // Read test
                        logger.Trace("Verifying response message...");
                        byte[] t1 = Reader.ReadBytes();
                        byte[] t2 = Reader.ReadBytes();
                        if (t1.Length != t2.Length)
                        {
                            logger.Trace("Encryption failure!");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            throw new PhoenixConnectException("Connection failed: corrupted post-handshake", ErrorType.ENCRYPTION_FAILURE);
                        }
                        for (int i = 0; i < t1.Length; i++)
                            if (t1[i] != t2[i])
                            {
                                logger.Trace("Encryption failure!");
                                _serverCertificate = null;
                                _clientCertificate = null;
                                Client.GetStream().WriteByte(0);
                                Client.Close();
                                throw new PhoenixConnectException("Connection failed: corrupted post-handshake", ErrorType.ENCRYPTION_FAILURE);
                            }

                        // Final handshake     
                        logger.Trace("Attempting program handshake...");
                        if (!AttemptCustomHandshake(this, Writer, Reader) || Reader.ReadRawByte() != 102)
                        {
                            logger.Trace("Handshake failure!");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            Client.Close();
                            throw new PhoenixConnectException("Connection failed: program handshake failed", ErrorType.PROGRAM_HANDSHAKE_FAILURE);
                        }

                        // Log
                        logger.Trace("Connected to server: " + GetRemoteAddress());
                    }
                    else
                    {
                        // Send certificate
                        logger.Trace("Sending server certificate...");
                        DataWriter output = new DataWriter(Client.GetStream());
                        _serverCertificate.WriteCertificateTo(output);

                        // Check response
                        logger.Debug("Awaiting client response...");
                        Client.GetStream().WriteByte(55);
                        int resp = Client.GetStream().ReadByte();
                        if (resp != 55)
                        {
                            logger.Trace("Client rejected server certificate");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            throw new PhoenixConnectException("Connection failed: client rejected certificate", ErrorType.REJECTED_CERTIFICATE);
                        }

                        // Read AES key
                        logger.Trace("Reading AES encryption key...");
                        logger.Debug("Reading encryption request payload...");
                        byte[] enc = new byte[256];
                        Client.GetStream().Read(enc, 0, enc.Length);

                        // Decrypt
                        logger.Debug("Decrypting encryption request payload...");
                        IAsymmetricBlockCipher cipher = new Pkcs1Encoding(new RsaEngine());
                        cipher.Init(false, _serverCertificate.PrivateKey);
                        byte[] payload = cipher.ProcessBlock(enc, 0, enc.Length);
                        logger.Debug("Received encryption request: " + string.Concat(payload.Select(t => t.ToString("x2"))));
                        logger.Debug("Processing encryption request payload...");
                        MemoryStream strm = new MemoryStream(payload);
                        DataReader rd = new DataReader(strm);
                        long cTime = rd.ReadLong();
                        logger.Debug("  Client time: " + cTime);
                        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > cTime + 60000 || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < cTime - 60000)
                        {
                            logger.Trace("Client sent invalid encryption payload");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            throw new PhoenixConnectException("Connection failed: client sent invalid response", ErrorType.ENCRYPTION_FAILURE);
                        }
                        byte[] key = rd.ReadBytes();
                        logger.Debug("  Key: [REDACTED]");
                        byte[] iv = rd.ReadBytes();
                        logger.Debug("  IV: [REDACTED]");
                        long lServerTime = rd.ReadLong();
                        logger.Debug("  Last server time: " + lServerTime);
                        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > lServerTime + 60000)
                        {
                            logger.Trace("Client sent invalid encryption payload");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            throw new PhoenixConnectException("Connection failed: client sent invalid response", ErrorType.ENCRYPTION_FAILURE);
                        }
                        logger.Debug("  Random: " + rd.ReadInt());
                        logger.Debug("  Random: " + rd.ReadInt());
                        strm.Close();

                        // Success, send a success message
                        logger.Debug("Sending success...");
                        Client.GetStream().WriteByte(243);

                        // Assign parameters
                        logger.Trace("Encrypting connection...");
                        logger.Debug("Building AES ciphers...");
                        KeyParameter aesKey = new KeyParameter(key);
                        ParametersWithIV ivParams = new ParametersWithIV(aesKey, iv);
                        IBufferedCipher cipherEncrypt = CipherUtilities.GetCipher("AES/CFB8/NoPadding");
                        IBufferedCipher cipherDecrypt = CipherUtilities.GetCipher("AES/CFB8/NoPadding");
                        cipherEncrypt.Init(true, ivParams);
                        cipherDecrypt.Init(false, ivParams);
                        logger.Debug("Building AES cryptostream...");
                        Stream = new CipherStream(Client.GetStream(), cipherDecrypt, cipherEncrypt);
                        logger.Debug("Assigning output writer...");
                        Writer = new DataWriter(Stream);
                        logger.Debug("Assigning input reader...");
                        Reader = new DataReader(Stream);

                        // Send test
                        logger.Trace("Sending encryption test message...");
                        byte[] testMessage = new byte[127];
                        rnd.NextBytes(testMessage);
                        Writer.WriteBytes(testMessage);
                        Writer.WriteBytes(testMessage);
                        logger.Debug("Test message sent: " + string.Concat(testMessage.Select(t => t.ToString("x2"))) + string.Concat(testMessage.Select(t => t.ToString("x2"))));

                        // Read test
                        logger.Trace("Verifying response message...");
                        byte[] t1 = Reader.ReadBytes();
                        byte[] t2 = Reader.ReadBytes();
                        if (t1.Length != t2.Length)
                        {
                            logger.Trace("Encryption failure!");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            _serverHandshake = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            throw new PhoenixConnectException("Connection failed: corrupted post-handshake", ErrorType.ENCRYPTION_FAILURE);
                        }
                        for (int i = 0; i < t1.Length; i++)
                            if (t1[i] != t2[i])
                            {
                                logger.Trace("Encryption failure!");
                                _serverCertificate = null;
                                _clientCertificate = null;
                                _serverHandshake = null;
                                Client.GetStream().WriteByte(0);
                                Client.Close();
                                throw new PhoenixConnectException("Connection failed: corrupted post-handshake", ErrorType.ENCRYPTION_FAILURE);
                            }

                        // Attempt final handshake
                        logger.Trace("Attempting program handshake...");
                        if (!AttemptCustomHandshake(this, Writer, Reader) || !_serverHandshake())
                        {
                            logger.Trace("Handshake failure!");
                            _serverCertificate = null;
                            _clientCertificate = null;
                            _serverHandshake = null;
                            Client.GetStream().WriteByte(0);
                            Client.Close();
                            throw new PhoenixConnectException("Connection failed: program handshake failed", ErrorType.PROGRAM_HANDSHAKE_FAILURE);
                        }

                        // Final success
                        Writer.WriteRawByte(102);

                        // Log connection
                        logger.Trace("Client connected: " + GetRemoteAddress());
                    }
                }
                else
                {
                    logger.Debug("Assigning input reader...");
                    Reader = new DataReader(Client.GetStream());
                    logger.Debug("Assigning output writer...");
                    Writer = new DataWriter(Client.GetStream());

                    // Server handshake
                    logger.Trace("Attempting program handshake...");
                    if (_side == ConnectionSide.SERVER && !_serverHandshake())
                    {
                        logger.Trace("Handshake failure!");
                        _serverCertificate = null;
                        _clientCertificate = null;
                        _serverHandshake = null;
                        Client.GetStream().WriteByte(1);
                        Client.Close();
                        throw new PhoenixConnectException("Connection failed: program handshake failed", ErrorType.PROGRAM_HANDSHAKE_FAILURE);
                    }
                    else
                    {
                        // Final handshake
                        if (_side == ConnectionSide.CLIENT && !AttemptCustomHandshake(this, Writer, Reader))
                            Client.GetStream().WriteByte(1);
                        else
                            Client.GetStream().WriteByte(0);
                    }

                    // Check handshake
                    if (Client.GetStream().ReadByte() != 0)
                    {
                        logger.Trace("Handshake failure!");
                        Client.Close();
                        throw new PhoenixConnectException("Connection failed: program handshake failed", ErrorType.PROGRAM_HANDSHAKE_FAILURE);
                    }

                    // Log connection
                    if (_side == ConnectionSide.SERVER)
                    {
                        logger.Trace("Client connected: " + GetRemoteAddress());
                    }
                    else
                    {
                        logger.Trace("Connected to server: " + GetRemoteAddress());
                    }
                }
            }
            catch
            {
                try
                {
                    Client.Close();
                }
                catch { }
                throw;
            }

            // Finish handshake
            logger.Trace("Cleaning up...");
            _serverCertificate = null;
            _clientCertificate = null;
            _serverHandshake = null;

            // Mark connection as open
            logger.Trace("Calling connection event...");
            _connected = true;
            CallConnected(new ConnectionEventArgs(Writer, Reader));
            logger.Debug("Checking connection...");
            if (!IsConnected())
            {
                logger.Trace("Disconnected from remote");
                _connected = false;
                return;
            }
            logger.Debug("Sending post-handshake completion...");
            Writer.WriteRawByte(102);
            logger.Trace("Verifying post-handshake...");
            try
            {
                byte b = Reader.ReadRawByte();
                if (b != 102)
                {
                    // Error
                    throw new Exception();
                }
            }
            catch
            {
                // Connection ended
                logger.Trace("Connection closed by remote from post-handshake");
                if (_connected)
                    try
                    {
                        Close("connection.lost");
                    }
                    catch
                    {
                        if (!_connected)
                            DisconnectInternal("connection.lost", new string[0]);
                    }
                else
                    DisconnectInternal("connection.lost", new string[0]);
                return;
            }

            logger.Trace("Starting packet handlers...");
            Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() =>
            {
                // Input
                while (Client != null)
                {
                    try
                    {
                        // Read packet
                        int cId = Reader.ReadInt();
                        int pId = Reader.ReadInt();

                        // Get channel
                        PacketChannel? channel = GetChannel(cId);
                        if (channel != null)
                        {
                            // Find packet
                            AbstractNetworkPacket? def = channel.GetPacketDefinition(pId);
                            if (def != null && !def.LengthPrefixed)
                            {
                                // Handle unprefixed packet
                                if (_connected)
                                    if (!HandlePacket(cId, pId, Reader))
                                    {
                                        // Unhandled packet
                                        // Log if in debug
                                        if (Game.DebugMode)
                                            logger.Error("Unhandled packet: " + def.GetType().Name + ", channel type name: " + channel.GetType().Name);
                                    }
                                continue;
                            }
                            else if (def != null && def.Synchronized)
                            {
                                byte[] packetData = Reader.ReadBytes();
                                MemoryStream strmD = new MemoryStream(packetData);
                                DataReader rdD = new DataReader(strmD);
                                if (_connected)
                                {
                                    try
                                    {
                                        if (!HandlePacket(cId, pId, rdD))
                                        {
                                            // Unhandled packet
                                            // Log if in debug
                                            if (Game.DebugMode)
                                                logger.Error("Unhandled packet: " + def.GetType().Name + ": [" + string.Concat(packetData.Select(x => x.ToString("x2"))) + "], channel type name: " + channel.GetType().Name);
                                        }
                                    }
                                    catch { }
                                }
                                continue;
                            }
                        }

                        byte[] packet = Reader.ReadBytes();
                        MemoryStream strm = new MemoryStream(packet);
                        DataReader rd = new DataReader(strm);

                        // Handle
                        Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() =>
                        {
                            if (cId == -1)
                            {
                                // System
                                if (pId == 0)
                                {
                                    // Disconnect
                                    string reason = rd.ReadString();
                                    int l = rd.ReadInt();
                                    string[] args = new string[l];
                                    for (int i = 0; i < l; i++)
                                    {
                                        args[i] = rd.ReadString();
                                    }
                                    try
                                    {
                                        Close(reason, args);
                                    }
                                    catch
                                    {
                                        DisconnectInternal(reason, args);
                                    }
                                }
                                else if (pId == 1) 
                                {
                                    // Ping
                                }
                            }
                            else
                            {
                                // Handle
                                if (_connected)
                                {
                                    try
                                    {
                                        if (!HandlePacket(cId, pId, rd))
                                        {
                                            // Unhandled packet
                                            // Log if in debug
                                            if (Game.DebugMode)
                                            {
                                                PacketChannel? ch = GetChannel(cId);
                                                if (ch == null)
                                                    logger.Error("Unhandled packet: " + cId + ":" + pId + ": [" + string.Concat(packet.Select(x => x.ToString("x2"))) + "]");
                                                else
                                                {
                                                    AbstractNetworkPacket? pkt = ch.GetPacketDefinition(pId);
                                                    if (pkt != null)
                                                        logger.Error("Unhandled packet: " + pkt.GetType().Name + ": [" + string.Concat(packet.Select(x => x.ToString("x2"))) + "], channel type name: " + channel.GetType().Name);
                                                    else
                                                        logger.Error("Unhandled packet: " + cId + ":" + pId + ": [" + string.Concat(packet.Select(x => x.ToString("x2"))) + "], channel type name: " + channel.GetType().Name);
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        });
                    }
                    catch
                    {
                        if (_connected)
                            try
                            {
                                Close("connection.lost");
                            }
                            catch
                            {
                                if (!_connected)
                                    DisconnectInternal("connection.lost", new string[0]);
                            }
                        else
                            DisconnectInternal("connection.lost", new string[0]);
                    }
                }
            });

            // Start pinger
            timeSinceLastPacketSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Phoenix.Common.AsyncTasks.AsyncTaskManager.RunAsync(() =>
            {
                while (IsConnected())
                {
                    if ((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timeSinceLastPacketSent) > 20000)
                    {
                        // Send ping
                        try
                        {
                            // Send disconnect packet
                            MemoryStream strm = new MemoryStream();
                            DataWriter writer = new DataWriter(strm);
                            writer.WriteLong(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                            byte[] packet = strm.ToArray();
                            strm.Close();

                            // Send ping
                            lock (_locker)
                            {
                                // Backup in case the lock somehow fails
                                // THIS FUCKING HAPPENED
                                while (_sending) ;
                                _sending = true;

                                try
                                {
                                    Writer.WriteInt(-1);
                                    Writer.WriteInt(1);
                                    Writer.WriteBytes(packet);
                                }
                                finally
                                {
                                    // Unlock
                                    _sending = false;
                                }
                            }
                            timeSinceLastPacketSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        catch
                        {

                        }
                    }
                    Thread.Sleep(100);
                }
            });

            // Call success
            logger.Trace("Calling connection success...");
            CallConnectionSuccess();
            logger.Trace("Connection successfully established!");
        }

        public override void Close(string reason, params string[] args)
        {
            // Check if connected
            if (!_connected)
                throw new InvalidOperationException("Not connected");

            if (!_disconnected)
            {
                _disconnected = true;
                string ip = GetRemoteAddress();
                _connected = false;
                CallDisconnected(reason, args);
                if (_side == ConnectionSide.SERVER)
                    logger.Trace("Client disconnected: " + ip);
                else
                    logger.Trace("Disconnected from " + ip);
            }

            try
            {
                // Send disconnect packet
                MemoryStream strm = new MemoryStream();
                DataWriter writer = new DataWriter(strm);
                writer.WriteString(reason);
                writer.WriteInt(args.Length);
                foreach (string arg in args)
                    writer.WriteString(arg);
                byte[] packet = strm.ToArray();
                strm.Close();

                // Send disconnect
                lock (_locker)
                {
                    // Backup in case the lock somehow fails
                    // THIS FUCKING HAPPENED
                    while (_sending) ;
                    _sending = true;

                    try
                    {
                        Writer.WriteInt(-1);
                        Writer.WriteInt(0);
                        Writer.WriteBytes(packet);
                    }
                    finally
                    {
                        // Unlock
                        _sending = false;
                    }
                }
                timeSinceLastPacketSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            catch { }

            // Actually disconnect
            DisconnectInternal(reason, args);
        }

        private void DisconnectInternal(string reason, string[] args)
        {
            string ip = GetRemoteAddress();
            _connected = false;
            try
            {
                if (Client != null)
                    Client.Close();
            }
            catch
            {
            }
            Client = null;
            if (!_disconnected)
            {
                CallDisconnected(reason, args);
                if (_side == ConnectionSide.SERVER)
                    logger.Trace("Client disconnected: " + ip);
                else
                    logger.Trace("Disconnected from " + ip);
            }
            _disconnected = true;
        }

        protected override void SendPacket(int cId, int id, AbstractNetworkPacket packet, PacketChannel channel)
        {
            // Check if connected
            if (!_connected)
                return;

            // Create packet
            if (packet.LengthPrefixed)
            {
                MemoryStream strm = new MemoryStream();
                DataWriter writer = new DataWriter(strm);
                packet.Write(writer);
                byte[] packetData = strm.ToArray();
                strm.Close();

                // Send
                if (_connected)
                {
                    lock (_locker)
                    {
                        // Backup in case the lock somehow fails
                        while (_sending) ;
                        _sending = true;

                        try
                        {
                            if (!_connected)
                                return;
                            Writer.WriteInt(cId);
                            Writer.WriteInt(id);
                            Writer.WriteBytes(packetData);
                        }
                        catch
                        {
                            // Error occured, likely connection loss
                            if (_connected)
                                DisconnectInternal("connection.lost", new string[0]);
                        }
                        finally
                        {
                            // Unlock
                            _sending = false;
                        }
                    }
                }
            }
            else
            {
                // Send
                if (_connected)
                {
                    lock (_locker)
                    {
                        // Backup in case the lock somehow fails
                        while (_sending) ;
                        _sending = true;

                        try
                        {
                            if (!_connected)
                                return;
                            Writer.WriteInt(cId);
                            Writer.WriteInt(id);
                            packet.Write(Writer);
                        }
                        catch
                        {
                            // Error occured, likely connection loss
                            if (_connected)
                                DisconnectInternal("connection.lost", new string[0]);
                        }
                        finally
                        {
                            // Unlock
                            _sending = false;
                        }
                    }
                }
            }
            timeSinceLastPacketSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public override bool IsConnected()
        {
            return _connected;
        }

        public override string ToString()
        {
            if (str != null)
                return str;
            if (IsConnected() || Client != null)
            {
                str = "Network Client: " + Client.Client.RemoteEndPoint;
                return str;
            }
            else
                return "Network Client";
        }

        public override string GetRemoteAddress()
        {
            return addr;
        }
    }
}