using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Channels;
using Common.Packets;
using Phoenix.Common.Events;
using Phoenix.Common.Tasks;
using Phoenix.Server;
using Phoenix.Server.Events;
using Phoenix.Server.SceneReplication;

namespace Common.Components.Server
{
    /// <summary>
    /// Example server component
    /// </summary>
    public class ExampleServerComponent : ServerComponent
    {
        public override string ID => "example-component";

        protected override string ConfigurationKey => "example";

        protected override void Define()
        {
            // Add a required dependency for the task manager
            DependsOn("task-manager");

            // Dependency management:
            //
            //
            // Components can depend on other components.
            // There are multiple dependency rules, for instance:
            // 
            // Required dependencies: load the specified component BEFORE this component, crash if missing
            // DependsOn("another-component");
            //
            // Optional dependencies: load the specified component BEFORE this component if present
            // OptDependsOn("another-component");
            //
            // Load-before: makes sure the specified component loads AFTER this component, if the target is present
            // LoadBefore("another-component");
            //
            // Conflict: specifies components incompatible with this component
            // ConflictsWith("another-component");
        }

        #region Component loading

        public override void PreInit()
        {
            // Called on pre-init, called during server initialization
        }

        public override void Init()
        {
            // Called on init, when the server is starting
        }

        public override void StartServer()
        {
            // Called when the server finished starting


            #region Asset management

            // Phoenix has a custom asset manager you can use to read server assets
            // They MUST be below two gigabytes as the asset encryption system is at the moment in a buggy stage
            // Phoenix Unity bindings does not support >2gb asset files either, however most assets typically are below 2gb in size so you should be fine

            // You can use the asset manager like this:
            string exampleMessage = AssetManager.GetAssetString("example.txt");

            // The above will read Client/Assets/Resources/PhoenixAssets/example.txt during debug
            // Outside of debug mode, it will read from an encrypted asset container
            // The server assets are linked to the client for easy management

            // You can change where assets are read from by editing project.json

            GetLogger().Info(exampleMessage); // Log it to the console

            #endregion
        }

        public override void StopServer()
        {
            // Called when the server is stopping
        }

        public override void Tick()
        {
            // Called each server tick
        }

        #endregion

        #region Event handlers

        // Event handlers
        // Phoenix supplies a plugin-like event system
        // You subscribe to events with [EventListener], the first argument on your method specifies what event to subscribe to
        //
        // Note that the event bus uses reflection, it is not too great when called often as it will slow down
        // Hence why server ticks have no event as they need to perform as quickly as possible


        // Example:
        // Subscribe to player joining

        [EventListener]
        public void PlayerJoin(PlayerJoinEvent ev)
        {
            // Do something


            #region Task scheduling

            // Lets show how task scheduling works
            // The task scheduler is a very powerful part of phoenix
            // It allows to schedule tasks on server ticks, client-side it will allow for running eg. unity code from packet threads

            // There are quite some types of tick tasks, we recommend you to experiment
            // Oneshot schedules a task for the next server tick

            // First, retrieve the task manager (requires the component task-manager to be present) from the service manager
            TaskManager manager = ServiceManager.GetService<TaskManager>();

            // Next, schedule a task for the next tick
            manager.Oneshot(() =>
            {
                // Called on the next server tick
                // Note that server ticks may not take longer than 500ms
                // So please don't use intensive code in this

                // Lets log something
                GetLogger().Info("Hello World from tick task! This was called when " + ev.Player.DisplayName + " joined the game.");
            });

            #endregion

            #region Networking example

            // Now, you might want to communicate with the client after a player joins
            // This is where the packet channel system comes in
            // 
            // Phoenix uses a channel-based networking system
            // Each packet is defined in a channel, eg. scene replication has a channel, so would chat, and player movement
            //
            // Lets send our first example packet to the client
            // For this, we retrieve the example channel from the client connection object
            ExampleChannel channel = ev.Player.Client.GetChannel<ExampleChannel>();

            // Now, lets send a hello world packet
            channel.SendPacket(new ServersentCommonHelloWorldPacket()
            {
                Message1 = "Hello",
                Message2 = "World!"
            });

            // You might notice something
            // Outside of integrated clients, packet sending and receiving is asynchronous
            // Unless you use SendPacketAndWaitForResponse, the server will not wait for the client to send a response back
            //
            // On the client side, you should see 'Hello World!' in the log
            // However, the server already continued with other code by then
            // You will need to set up response packets in case you dont want that
            //
            // Note that ServersentCommonHelloWorldPacket is handled by a common handler, present on both client and server
            // If the client sends this back, we will say 'Hello World!' in the log too
            //
            // View SharedGameServer and ExampleChannel for details


            #region Response packets

            // Now, lets talk about packets with requests and responses
            // Phoenix does allow for replying with a packet, you can send a request and wait for a response:

            // Build the packet
            ExampleRequestPacket exampleRequest = new ExampleRequestPacket();
            exampleRequest.RequestString = "hello_world";

            // Send it and wait for a response of the packet type ExampleResponsePacket
            // Maximum wait of 5 seconds
            ExampleResponsePacket? response = channel.SendPacketAndWaitForResponse<ExampleResponsePacket>(exampleRequest);
            if (response != null)
            {
                // We got a response

                // Lets log it
                GetLogger().Info(response.ResponseMessage);


                // There is one thing thats important to remember with requests and responses
                // Packet processing is asynchronous, therefor response packets might not be sent in the same order as the requests
                // If you send multiple requests over a channel without waiting for responses, the client might send the wrong response first
                //
                // When using this feature, remember to only send one request at a time, and have a specific channel for request packets
                // Or use a response packet type that is only sent in response to a specific request, for instance ExampleResponsePacket is only sent
                // in response to ExampleRequestPacket, not at any other time.
                //
                //
                //
                // Avoid asynchronous use, if you REALLY need to handle it asynchronously, use a response key system:
                // Response keys would be defined from the request, for instance, you create a request ID on the server, and send it in the request packet.
                // You need to store that request ID somewhere in memory
                //
                // On your client, make sure to include the request ID in the response packet.
                // When your server receives the response packet, make sure it checks which key was used and what code to execute.
                // It might receive a different response intended for a different request before the one you are waiting for.
                //
                // If this happens, make sure your server can call the intended response handling code, by, for instance, keeping the handler attached to the request ID.
                // You can use eg. a dictionary for this, when you receive a response, call the action stored in the dictionary that is attached to your request ID.
                // Make sure not to iterate over the dictionary, dictionaries are NOT thread-safe
            }

            #endregion


            #endregion

            #region Scene Replication

            // The biggest part of Phoenix is its scene replication system
            // It is quite complex so we recommend to just experiment with it
            // Tho lets make the client load a scene
            //
            // Retrieve the replication controller
            SceneReplicator? replicator = ev.Player.GetObject<SceneReplicator>(); // Retrieves it from the player object container
            if (replicator != null)
            {
                // Load the sample scene
                Scene sc = replicator.LoadScene("Scenes/SampleScene", "DEFAULT", SceneLoadMethod.SINGLE); // Loads and subscribes the client to the sample scene in room DEFAULT
                // Short version: replicator.LoadScene("Scenes/SampleScene");

                // The client may still be loading the scene but we can already interact with it
                // You can check if a scene is subscribed (and fully loaded) by calling 'replicator.IsSubscribedToScene("<scene/path>")'
                // Note that you need to have the PRISM scene maps in the server assets for this to work

                // Example use of scene replication:
                sc.GetObject("Main Camera").Transform.Position.Y += 10; // Move the camera up as an example

                // When the client is done loading it will perform initial replicaiton, note that the camera will be moved up for each player that joins
                // Scene replication is not client-specific, its for everyone subscribed to the same scene in the same room

                // Please do note that the PRISM scene maps included in the example assets are not in sync with unity
                // Scene replication will misbehave until you set it up client-side
                
                // You can export the scene maps in unity via the tools provided by Phoenix
                // They will automatically be loaded into the dedicated server as it is configured to read from the client assets folder as long as you dump them with the Phoenix-provided tools
                
                // The samples are not set up on the unity side, you need to do that manually
                // For this sample to work, add a SimpleReplicatedObject component to the main camera, and after that, dump the scene maps and update them here
                // Then the sample will work
                // TODO: change this
            }

            #endregion
        }

        [EventListener]
        public void PlayerLeave(PlayerLeaveEvent ev)
        {
            // Player left
        } 

        #endregion
    }
}
