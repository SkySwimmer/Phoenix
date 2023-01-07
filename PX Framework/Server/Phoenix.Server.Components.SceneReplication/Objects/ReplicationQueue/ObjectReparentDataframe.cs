﻿namespace Phoenix.Server.Components.SceneReplication.Objects.ReplicationQueue
{
    public class ObjectReparentDataframe : ReplicationDataframe
    {
        public string? OldParentPath;
        public string? NewParentPath;

        public override ReplicationCommandType Type => ReplicationCommandType.REPARENT;
    }
}
