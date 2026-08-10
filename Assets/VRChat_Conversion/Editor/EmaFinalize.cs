using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone;

public class EmaFinalize : EditorWindow
{
    private const string SCENE_PATH = "Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity";
    private const string AVATAR_NAME = "SakurabaEma_ByPOWER_VRChat";

    [MenuItem("Ema/Finalize Scene")]
    public static void FinalizeScene()
    {
        Scene scene = SceneManager.OpenScene(SCENE_PATH);
        if (scene.isLoaded == false)
        {
            Debug.LogError("Failed to load scene: " + SCENE_PATH);
            return;
        }

        GameObject avatar = GameObject.Find(AVATAR_NAME);
        if (avatar == null)
        {
            // If not found by name, try finding by component
            var descriptors = Object.FindObjectsOfType<VRCAvatarDescriptor>();
            if (descriptors.Length > 0)
            {
                avatar = descriptors[0].gameObject;
                Debug.Log($"Found avatar by descriptor: {avatar.name}");
            }
            else
            {
                Debug.LogError($"Avatar '{AVATAR_NAME}' not found in scene!");
                return;
            }
        }

        var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
        if (descriptor == null)
        {
            Debug.LogError("VRCAvatarDescriptor not found on avatar!");
            return;
        }

        // 1. Set ViewPosition
        Transform head = FindTransformRecursive(avatar.transform, "Head");
        if (head != null)
        {
            descriptor.viewPosition = head.localPosition;
            Debug.Log($"ViewPosition set to {head.localPosition}");
        }
        else
        {
            Debug.LogWarning("Head transform not found. ViewPosition not set.");
        }

        // 2. Add PhysBones
        AddPhysBone(avatar.transform, "Hair_Root");
        AddPhysBone(avatar.transform, "Skirt_Root");
        AddPhysBone(avatar.transform, "ChestRb_Root");

        EditorUtility.SetDirty(descriptor);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Ema Finalization Complete and Scene Saved.");
    }

    private static void AddPhysBone(Transform root, string boneName)
    {
        Transform target = FindTransformRecursive(root, boneName);
        if (target != null)
        {
            if (target.GetComponent<VRCPhysBone>() == null)
            {
                target.AddComponent<VRCPhysBone>();
                Debug.Log($"Added VRCPhysBone to {boneName}");
            }
            else
            {
                Debug.Log($"{boneName} already has VRCPhysBone.");
            }
        }
        else
        {
            Debug.LogWarning($"Bone '{boneName}' not found.");
        }
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
