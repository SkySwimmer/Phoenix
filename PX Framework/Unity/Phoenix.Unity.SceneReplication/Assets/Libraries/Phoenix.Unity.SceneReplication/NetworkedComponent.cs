using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// More complex version of the NetworkedBehaviour instance, this type allows you to specify the server-side component during scene dump
/// </summary>
public abstract class NetworkedComponent : NetworkedBehaviour
{
    /// <summary>
    /// Defines the server component type
    /// </summary>
    public abstract Type ServerComponentType { get; }

    /// <summary>
    /// Server component properties, used to assign properties that are passed on server-side scene load
    /// </summary>
    public Dictionary<string, object> ServerComponentProperties = new Dictionary<string, object>();
}
