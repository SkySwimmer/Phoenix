using Phoenix.Common.SceneReplication.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class PhoenixReplicatationUtils
{
    public static UnityEngine.Vector3 ToUnityVector3(this Vector3 vec)
    {
        return new UnityEngine.Vector3(vec.X, vec.Y, vec.Z);
    }
}