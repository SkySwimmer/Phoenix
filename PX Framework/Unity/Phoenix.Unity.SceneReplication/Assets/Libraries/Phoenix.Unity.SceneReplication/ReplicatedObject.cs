using System.Collections;
using System.Collections.Generic;
using Phoenix.Client.SceneReplicatorLib.Binding;
using Phoenix.Common.SceneReplication.Packets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Phoenix Replication API Component
/// </summary>
public abstract class ReplicatedObject : MonoBehaviour, IReplicatingSceneObject
{
    public bool SmoothReplicationMovement = false;
    public float SmoothReplicationMovmentTime = 1f;
    public LeanTweenType SmoothReplicationMovementPositionEaseStyle = LeanTweenType.linear;
    public LeanTweenType SmoothReplicationMovementRotationEaseStyle = LeanTweenType.linear;
    public LeanTweenType SmoothReplicationMovementScaleEaseStyle = LeanTweenType.linear;

    /// <summary>
    /// Serializes the object
    /// </summary>
    /// <param name="replicationMap">Map holding replication properties</param>
    public abstract void SerializeInto(Dictionary<string, object> replicationMap);

    /// <summary>
    /// De-serializes or updates the object
    /// </summary>
    /// <param name="replicationMap">Map holding replication properties (note that replication is done in split parts, only changed keys are present)</param>
    public abstract void DeserializeFrom(Dictionary<string, object> replicationMap);

    /// <summary>
    /// Destroys the object
    /// </summary>
    public virtual void Destroy()
    {
        // Destroy object
        GameObject.Destroy(gameObject);
    }

    /// <summary>
    /// Handles replication
    /// </summary>
    /// <param name="packet">Replication dataframe packet</param>
    public void Replicate(ReplicateObjectPacket packet)
    {
        if (packet.HasNameChanges)
            ReplicateName(packet.Name);
        if (packet.HasActiveStatusChanges)
            ReplicateActiveStatus(packet.Active);
        if (packet.HasTransformChanges)
            ReplicateTransform(packet.Transform);
        if (packet.HasDataChanges)
            DeserializeFrom(packet.Data);
    }

    /// <summary>
    /// Activates/deactivates the object
    /// </summary>
    /// <param name="status">True to activate, false to deactivate</param>
    public virtual void ReplicateActiveStatus(bool status)
    {
        gameObject.SetActive(status);
    }

    /// <summary>
    /// Changes the object's name
    /// </summary>
    /// <param name="newName">New object name</param>
    public virtual void ReplicateName(string newName)
    {
        gameObject.name = newName;
    }

    /// <summary>
    /// Changes the object's transform
    /// </summary>
    /// <param name="transform">New transform</param>
    public virtual void ReplicateTransform(Phoenix.Common.SceneReplication.Packets.Transform transform)
    {
        if (gameObject.transform.localPosition != transform.Position.ToUnityVector3())
            ReplicatePosition(transform.Position.ToUnityVector3());
        if (gameObject.transform.localEulerAngles != transform.Rotation.ToUnityVector3())
            ReplicateRotation(transform.Rotation.ToUnityVector3());
        if (gameObject.transform.localScale != transform.Scale.ToUnityVector3())
            ReplicateScale(transform.Scale.ToUnityVector3());
    }

    /// <summary>
    /// Changes the object's position
    /// </summary>
    /// <param name="newPosition">New position</param>
    public virtual void ReplicatePosition(UnityEngine.Vector3 newPosition)
    {
        if (SmoothReplicationMovement)
            LeanTween.moveLocal(gameObject, newPosition, SmoothReplicationMovmentTime).setEase(SmoothReplicationMovementPositionEaseStyle);
        else
            gameObject.transform.localPosition = newPosition;
    }

    /// <summary>
    /// Changes the object's rotation
    /// </summary>
    /// <param name="newRotation">New rotation</param>
    public virtual void ReplicateRotation(UnityEngine.Vector3 newRotation)
    {
        if (SmoothReplicationMovement)
            LeanTween.rotateLocal(gameObject, newRotation, SmoothReplicationMovmentTime).setEase(SmoothReplicationMovementRotationEaseStyle);
        else
            gameObject.transform.localEulerAngles = newRotation;
    }


    /// <summary>
    /// Changes the object's scale
    /// </summary>
    /// <param name="newScale">New scale</param>
    public virtual void ReplicateScale(UnityEngine.Vector3 newScale)
    {
        if (SmoothReplicationMovement)
            LeanTween.scale(gameObject, newScale, SmoothReplicationMovmentTime).setEase(SmoothReplicationMovementScaleEaseStyle);
        else
            gameObject.transform.localScale = newScale;
    }

    /// <summary>
    /// Changes the parent object
    /// </summary>
    /// <param name="newParentPath">New parent path</param>
    public virtual void Reparent(string newParentPath)
    {
        // Save old transform
        UnityEngine.Vector3 oldPos = new UnityEngine.Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, gameObject.transform.localPosition.z);
        UnityEngine.Vector3 oldScale = new UnityEngine.Vector3(gameObject.transform.localScale.x, gameObject.transform.localScale.y, gameObject.transform.localScale.z);
        UnityEngine.Vector3 oldRot = new UnityEngine.Vector3(gameObject.transform.localEulerAngles.x, gameObject.transform.localEulerAngles.y, gameObject.transform.localEulerAngles.z);

        // Change parent
        if (newParentPath == null)
            gameObject.transform.parent = null;
        else
        {
            GameObject newParent = GameObjectUtils.GetObjectInScene(gameObject.scene, newParentPath);
            gameObject.transform.parent = newParent.transform;
        }

        // Restore position to match Phoenix behaviour
        gameObject.transform.localPosition = oldPos;
        gameObject.transform.localScale = oldScale;
        gameObject.transform.localEulerAngles = oldRot;
    }

    /// <summary>
    /// Moves the object to another scene
    /// </summary>
    /// <param name="newScene">New scene path</param>
    public virtual void ChangeScene(string newScene)
    {
        // Move to another scene
        if (newScene == null)
            GameObject.Destroy(gameObject);
        else
        {
            try
            {
                Scene target = SceneManager.GetSceneByPath(newScene);
                SceneManager.MoveGameObjectToScene(gameObject, target);
            }
            catch
            {
            }
        }
    }
}
