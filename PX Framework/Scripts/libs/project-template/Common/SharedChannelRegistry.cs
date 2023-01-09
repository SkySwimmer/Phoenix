using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Channels;
using Phoenix.Common.Networking.Registry;
using Phoenix.Common.SceneReplication;

namespace Common
{
    /// <summary>
    /// Shared channel registry stuff
    /// </summary>
    public static class SharedChannelRegistry
    {
        public static ChannelRegistry Create()
        {
            // Build the channel registry
            // This registry object MUST match on both sides, both client and server MUST have an identical registry
            // Otherwise network packets will be corrupted
            //
            // Networking is demonstrated in the example server component
            //
            // Lets make our registry
            ChannelRegistry registry = new ChannelRegistry();

            // Add the scene replication channel
            registry.Register(new SceneReplicationChannel());

            // Add one of our own channels
            registry.Register(new ExampleChannel());

            return registry;
        }
    }
}
