using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameObjectUtils
{
    /// <summary>
    /// Retrieves child game objects
    /// </summary>
    /// <param name="parent">Parent object</param>
    /// <param name="name">Child object name</param>
    /// <returns>GameObject instance or null</returns>
    public static GameObject GetChild(this GameObject parent, string name)
    {
        if (name.Contains("/"))
        {
            string pth = name.Remove(name.IndexOf("/"));
            string ch = name.Substring(name.IndexOf("/") + 1);
            foreach (GameObject obj in GetChildren(parent))
            {
                if (obj.name == pth)
                {
                    GameObject t = obj.GetChild(ch);
                    if (t != null)
                        return t;
                }
            }
            return null;
        }
        Transform tr = parent.transform;
        Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in trs)
        {
            if (t.name == name && t.parent == tr.gameObject.transform)
            {
                return t.gameObject;
            }
        }
        return null;
    }

    /// <summary>
    /// Retrieves all child game objects
    /// </summary>
    /// <param name="parent">Parent object</param>
    /// <returns>Array of GameObject instances</returns>
    public static GameObject[] GetChildren(this GameObject parent)
    {
        Transform tr = parent.transform;
        List<GameObject> children = new List<GameObject>();
        Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform trCh in trs)
        {
            if (trCh.parent == tr.gameObject.transform)
                children.Add(trCh.gameObject);
        }
        return children.ToArray();
    }

    /// <summary>
    /// Retrieves game objects in specific scenes
    /// </summary>
    /// <param name="scene">Scene instance</param>
    /// <param name="name">Object name</param>
    /// <returns>GameObject instance or null</returns>
    public static GameObject GetObjectInScene(Scene scene, string name)
    {
        if (name.Contains("/"))
        {
            string pth = name.Remove(name.IndexOf("/"));
            string ch = name.Substring(name.IndexOf("/") + 1);
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                if (obj.name == pth)
                {
                    GameObject t = obj.GetChild(ch);
                    if (t != null)
                        return t;
                }
            }
            return null;
        }
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if (obj.name == name)
                return obj;
        }
        return null;
    }
}
