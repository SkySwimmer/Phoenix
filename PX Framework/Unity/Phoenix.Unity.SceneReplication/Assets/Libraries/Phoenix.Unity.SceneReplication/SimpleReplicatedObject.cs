using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Basic replicated object that only replicates its transform
/// </summary>
public class SimpleReplicatedObject : ReplicatedObject
{
    public override void SerializeInto(Dictionary<string, object> replicationMap)
    {
    }

    public override void DeserializeFrom(Dictionary<string, object> replicationMap)
    {
    }
}
