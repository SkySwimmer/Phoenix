using UnityEngine;
using Phoenix.Client.SceneReplicatorLib.Messages;
using System.Collections.Generic;
using Phoenix.Common.SceneReplication.Messages;

/// <summary>
/// Wrapper around the MonoBehaviour class and the IComponentMessageReceiver interface to easily create networked components
/// </summary>
public abstract class NetworkedBehaviour : MonoBehaviour, IComponentMessageReceiver
{
    public Dictionary<string, ComponentMessenger> Messengers { get; set; }

    public virtual void HandleMessage(IComponentMessage message, ComponentMessenger messenger)
    {
    }

    public virtual void SetupMessenger(ComponentMessenger messenger)
    {
        JoinRoom(messenger.Room);
    }

    public virtual void JoinRoom(string room) { }
}