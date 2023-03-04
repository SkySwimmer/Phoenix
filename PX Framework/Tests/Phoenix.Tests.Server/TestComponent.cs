using Phoenix.Server;
using ChartZ.Engine;
using Phoenix.Server.Components;
using Phoenix.Server.Configuration;
using Phoenix.Common.Events;
using Phoenix.Server.Events;
using System;
using System.IO;
using System.Collections.Generic;
using Phoenix.Server.SceneReplication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Phoenix.Common.Tasks;

namespace Phoenix.Tests.Server
{
    public class TestComponent : ServerComponent
    {
        public override string ID => "test";

        protected override string ConfigurationKey => ID;

        protected override void Define()
        {
        }

        public override void Init()
        {
            if (Server.IsComponentLoaded("server-list-publisher"))
            {
                AbstractConfigurationSegment conf = Server.GetConfiguration("server");
                if (!conf.HasEntry("name"))
                    conf.SetString("name", "Server #" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString("X"));
                Server.GetComponent<ServerListPublisherComponent>().Details.Set("name", conf.GetString("name"));
            }
        }

        public override void StartServer()
        {
            // Config test
            string? str = Configuration.GetString("hello");
            Configuration.SetString("hello", "world");
            str = Configuration.GetString("hello");
            if (str != "world")
                throw new Exception("Config issue");

            // Asset test
            string s = AssetManager.GetAssetString("test.txt");
            if (s != "eee")
                throw new Exception("Asset corrupted");

            // Chart test
            ChartChain chain = ChartChain.Create();
            Stream chart = AssetManager.GetAssetStream("test.ccf");
            chain.Load(chart);
            chain.GlobalMemory[chain.TagsReverse["player_standing"]] = -25;
            chain.RegisterCommand(new TestChartDialogueCommand());
            chain.EntrySegment?.Run();

            // Test delayed tasks
            ServiceManager.GetService<TaskManager>().AfterSecs(() =>
            {
                GetLogger().Info("test");
            }, 5);

            // Test replication loading
            Scene sc = Scene.FromJson("Scenes/TitleScreen", "TitleScreen", AssetManager.GetAssetString("SceneReplication/Scenes/TitleScreen.prsm"));
            SceneObject prefab = SceneObject.FromJson(AssetManager.GetAssetString("SceneReplication/TestReplicationPrefab.prpm"));
            SceneObject ipBox = sc.GetObject("LoadCanvas/Panel/ServerPanel/ConnectPanel/IPBox");
            prefab.Parent = ipBox;
            prefab.Destroy();
            ipBox.SpawnPrefab("TestReplicationPrefab").AddComponent<TestObjectComponent>();
            ipBox = ipBox;

            // Test interaction with the scene manager
            SceneManager manager = ServiceManager.GetService<SceneManager>();
            sc = manager.GetScene("Scenes/WorldScene");;
            SceneObject cam = sc.GetObject("Main Camera");
            cam.Unlock();
            cam.Transform.Position.X += 100;
            cam.ReplicationData.Set("Tester", "test");
            cam.ReplicationData.Set("Tester 2", "123");
            cam.ReplicationData.Set("Tester 3", "456");
            sc.SpawnPrefab("TestReplicationPrefab").Parent = cam;
        }

        [EventListener]
        public void LeavePlayer(PlayerLeaveEvent ev)
        {
            TestPlayerCharacterContainer? cont = ev.Player.GetObject<TestPlayerCharacterContainer>();
            if (cont != null)
                cont.Character.Destroy();
        }

        [EventListener]
        public void JoinPlayer(PlayerJoinEvent ev)
        {
            Scene? sc = ev.Player.GetObject<SceneReplicator>()?.LoadScene("Scenes/WorldScene");
            if (sc != null)
            {
                ServiceManager.GetService<TaskManager>().Oneshot(() =>
                {
                    SceneObject obj = sc.SpawnPrefab("TestReplicationPrefab");
                    obj.Name = "Player-" + ev.Player.PlayerID;
                    ev.Player.AddObject(new TestPlayerCharacterContainer()
                    {
                        Character = obj
                    });
                    obj.OwningConnection = ev.Player.Client;
                });
            }
        }

        [EventListener]
        public void RequestListUpdate(ServerListUpdateEvent ev)
        {
            ev.DetailBlock.Set("time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        }
    }
}
