using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;
using VRC.SDK3.Avatars.Components;

public class EmaPrepare : EditorWindow
{
    private const string SCENE_PATH = "Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity";
    private const string AVATAR_NAME = "SakurabaEma_ByPOWER_VRChat";
    private const string THUMBNAIL_PATH = "Assets/VRChat_Conversion/SakurabaEma_VRChat_preview.png";

    [MenuItem("Ema/Prepare For Upload")]
    public static void Prepare()
    {
        Scene scene = EditorSceneManager.OpenScene(SCENE_PATH);
        if (!scene.isLoaded)
        {
            Debug.LogError("Failed to load scene: " + SCENE_PATH);
            EditorApplication.Exit(1);
            return;
        }

        GameObject avatar = GameObject.Find(AVATAR_NAME);
        if (avatar == null)
        {
            var descriptors = Object.FindObjectsOfType<VRCAvatarDescriptor>();
            if (descriptors.Length > 0)
                avatar = descriptors[0].gameObject;
        }
        if (avatar == null)
        {
            Debug.LogError("Avatar not found in scene");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("EMAPREP avatar: " + avatar.name);

        var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
        if (descriptor == null)
        {
            Debug.LogError("No VRCAvatarDescriptor on avatar");
            EditorApplication.Exit(1);
            return;
        }

        var pm = avatar.GetComponent<PipelineManager>();
        if (pm == null)
        {
            pm = avatar.AddComponent<PipelineManager>();
            Debug.Log("EMAPREP added PipelineManager");
        }

        if (descriptor.ViewPosition == Vector3.zero)
        {
            Transform head = FindTransformRecursive(avatar.transform, "Head");
            if (head != null)
            {
                descriptor.ViewPosition = head.localPosition;
                Debug.Log("EMAPREP set ViewPosition=" + head.localPosition);
            }
        }

        var animator = avatar.GetComponent<Animator>();
        Debug.Log("EMAPREP animator=" + (animator != null ? animator.isHuman.ToString() : "missing"));

        EditorUtility.SetDirty(descriptor);
        EditorUtility.SetDirty(pm);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("EMAPREP DONE - ready for upload. Thumbnail: " + THUMBNAIL_PATH);
        EditorApplication.Exit(0);
    }

    private static Transform FindTransformRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindTransformRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
