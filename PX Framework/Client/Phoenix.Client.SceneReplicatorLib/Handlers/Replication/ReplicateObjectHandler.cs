﻿using Phoenix.Client.Components;
using Phoenix.Client.SceneReplicatorLib.Binding;
using Phoenix.Common.Networking.Packets;
using Phoenix.Common.SceneReplication.Packets;

namespace Phoenix.Client.SceneReplicatorLib.Handlers.Replication
{
    public class ReplicateObjectHandler : PacketHandler<ReplicateObjectPacket>
    {
        protected override PacketHandler<ReplicateObjectPacket> CreateInstance()
        {
            return new ReplicateObjectHandler();
        }

        protected override bool Handle(ReplicateObjectPacket packet)
        {
            GameClient? client = GetChannel().Connection.GetObject<GameClient>();
            if (client != null)
            {
                SceneReplicationComponent comp = client.GetComponent<SceneReplicationComponent>();
                if (comp.IsSubscribedToScene(packet.ScenePath) && comp.IsSubscribedToRoom(packet.Room) && comp.Bindings != null)
                {
                    comp.Bindings.RunOnNextFrameUpdate(() =>
                    {
                        IReplicatingSceneObject? obj = comp.Bindings.GetObjectInScene(packet.Room, packet.ScenePath, packet.ObjectPath);
                        if (obj != null)
                            obj.Replicate(packet);
                    });
                }
            }
            return true;
        }
    }
}
