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
    public bool SmoothReplication = false;
    public float SmoothReplicationTime = 1f;
    public LeanTweenType SmoothReplicationPositionEaseStyle = LeanTweenType.linear;
    public LeanTweenType SmoothReplicationRotationEaseStyle = LeanTweenType.linear;
    public LeanTweenType SmoothReplicationScaleEaseStyle = LeanTweenType.linear;

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
        if (packet.IsInitial)
        {
            if (packet.HasNameChanges)
                gameObject.name = packet.Name;
            if (packet.HasActiveStatusChanges)
                gameObject.SetActive(packet.Active);
            if (packet.HasTransformChanges)
            {
                if (gameObject.transform.localPosition != packet.Transform.Position.ToUnityVector3())
                    gameObject.transform.localPosition = packet.Transform.Position.ToUnityVector3();
                if (gameObject.transform.localEulerAngles != packet.Transform.Rotation.ToUnityVector3())
                    gameObject.transform.localEulerAngles = packet.Transform.Rotation.ToUnityVector3();
                if (gameObject.transform.localScale != packet.Transform.Scale.ToUnityVector3())
                    gameObject.transform.localScale = packet.Transform.Scale.ToUnityVector3();
            }
            if (packet.HasDataChanges)
                DeserializeFrom(packet.Data);
            return;
        }
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

    private bool _tPosR;
    private bool _tRotR;
    private bool _tScR;
    private int _posLtId = -1;
    private int _rotLtId = -1;
    private int _scLtId = -1;

    /// <summary>
    /// Changes the object's position
    /// </summary>
    /// <param name="newPosition">New position</param>
    public virtual void ReplicatePosition(UnityEngine.Vector3 newPosition)
    {
        if (SmoothReplication)
        {
            if (_tPosR)
                LeanTween.cancel(gameObject, _posLtId);
            LTDescr tween = LeanTween.moveLocal(gameObject, newPosition, SmoothReplicationTime).setEase(SmoothReplicationPositionEaseStyle);
            tween.setOnComplete(() =>
            {
                _tPosR = false;
            });
            _posLtId = tween.uniqueId;
            _tPosR = true;
        }
        else
            gameObject.transform.localPosition = newPosition;
    }

    /// <summary>
    /// Changes the object's rotation
    /// </summary>
    /// <param name="newRotation">New rotation</param>
    public virtual void ReplicateRotation(UnityEngine.Vector3 newRotation)
    {
        if (SmoothReplication)
        {
            if (_tRotR)
                LeanTween.cancel(gameObject, _rotLtId);
            LTDescr tween = LeanTween.rotateLocal(gameObject, newRotation, SmoothReplicationTime).setEase(SmoothReplicationRotationEaseStyle);
            tween.setOnComplete(() =>
            {
                _tRotR = false;
            });
            _rotLtId = tween.uniqueId;
            _tRotR = true;
        }
        else
            gameObject.transform.localEulerAngles = newRotation;
    }


    /// <summary>
    /// Changes the object's scale
    /// </summary>
    /// <param name="newScale">New scale</param>
    public virtual void ReplicateScale(UnityEngine.Vector3 newScale)
    {
        if (SmoothReplication)
        {
            if (_tScR)
                LeanTween.cancel(gameObject, _scLtId);
            LTDescr tween = LeanTween.scale(gameObject, newScale, SmoothReplicationTime).setEase(SmoothReplicationScaleEaseStyle);
            tween.setOnComplete(() =>
            {
                _tScR = false;
            });
            _scLtId = tween.uniqueId;
            _tScR = true;
        }
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