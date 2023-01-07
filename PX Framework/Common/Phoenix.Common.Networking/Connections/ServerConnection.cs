using Phoenix.Common.IO;
using Phoenix.Common.Networking.Channels;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.Networking.Connections
{
    /// <summary>
    /// Handshake event object
    /// </summary>
    public class HandshakeEventArgs : EventArgs
    {
        private DataWriter _out;
        private DataReader _in;
        private bool _failed;

        public HandshakeEventArgs(DataWriter @out, DataReader @in)
        {
            _out = @out;
            _in = @in;
        }

        /// <summary>
        /// Client input reader
        /// </summary>
        public DataReader ClientInput
        {
            get
            {
                return _in;
            }
        }

        /// <summary>
        /// Client output writer
        /// </summary>
        public DataWriter ClientOutput
        {
            get
            {
                return _out;
            }
        }

        /// <summary>
        /// Checks if the handshake has failed
        /// </summary>
        public bool HasFailed()
        {
            return _failed;
        }

        /// <summary>
        /// Call this to fail the handshake
        /// </summary>
        public void FailHandshake()
        {
            _failed = true;
        }
    }

    /// <summary>
    /// Abstract server connection type
    /// </summary>
    public abstract class ServerConnection : Connection
    {
        public override ConnectionSide Side => ConnectionSide.SERVER;

        public override string GetRemoteAddress()
        {
            throw new NotImplementedException();
        }

        protected override void SendPacket(int cId, int id, AbstractNetworkPacket packet, PacketChannel channel)
        {
            // Broadcast
            Connection[] clients = GetClients();
            foreach (Connection conn in clients)
            {
                if (conn != null && conn.IsConnected())
                    try
                    {
                        conn.GetChannel(channel).SendPacket(packet);
                    }
                    catch { }
            }
        }

        /// <summary>
        /// Retrieves all clients connected to this server
        /// </summary>
        /// <returns>Array of client connections</returns>
        public abstract Connection[] GetClients();

    }
}
