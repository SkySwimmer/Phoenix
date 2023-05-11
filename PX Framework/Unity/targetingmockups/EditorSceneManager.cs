using System;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    public class EditorSceneManager
    {
        public static Scene NewPreviewScene() { return default(Scene); }
        public static void ClosePreviewScene(Scene previewScene) { }
        public static void OpenScene(string scene) { }
    }
}