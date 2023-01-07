using Phoenix.Common.Networking.Channels;
using Phoenix.Common.Networking.Connections;
using Phoenix.Common.Networking.Packets;

namespace Phoenix.Common.Networking.Impl
{
    public class IntegratedServerConnection : ServerConnection
    {
        private bool connected = false;
        private List<IntegratedClientConnection> clients = new List<IntegratedClientConnection>();

        public void AddClient(IntegratedClientConnection client)
        {
            if (client.Side == ConnectionSide.CLIENT)
                client = client.Other;
            client.SetServer(this);
            client.Other.SetServer(this);
            clients.Add(client);
        }

        public override void Close(string reason, params string[] args)
        {
            if (!IsConnected())
                throw new InvalidOperationException("Server not running");
            foreach (IntegratedClientConnection client in clients)
                client.Close(reason, args);
            connected = false;
        }

        public override Connection[] GetClients()
        {
            return clients.Where(t => t.IsConnected()).ToArray();
        }

        public override bool IsConnected()
        {
            return connected;
        }

        public override void Open()
        {
            if (IsConnected())
                throw new InvalidOperationException("Server already running");
            connected = true;
        }

        protected override void SendPacket(int cId, int id, AbstractNetworkPacket packet, PacketChannel channel)
        {
            foreach (Connection client in clients)
                if (client.IsConnected())
                    client.GetChannel(channel).SendPacket(packet);
        }

        internal void DoCallConnectionSuccess(IntegratedClientConnection client)
        {
            CallConnectionSuccess(client);
        }

        internal void DoCallConnected(IntegratedClientConnection client, ConnectionEventArgs args)
        {
            CallConnected(client, args);
        }

        internal void DisconnectClient(IntegratedClientConnection client, string reason, string[] args)
        {
            if (!clients.Contains(client))
                client = client.Other;
            if (clients.Contains(client))
            {
                CallDisconnected(reason, args, client);
                clients.Remove(client);
            }
        }
    }
}
