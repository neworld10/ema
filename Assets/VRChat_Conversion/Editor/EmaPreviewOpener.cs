using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EmaPreviewOpener
{
    const string ScenePath = "Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity";

    [MenuItem("Ema/Preview/Start")]
    static void StartPreview()
    {
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Ema/Preview/Stop")]
    static void StopPreview()
    {
        EditorApplication.isPlaying = false;
    }
}