using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EmaFixMaterials
{
    private const string ScenePath = "Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity";
    private const string MatDir = "Assets/VRChat_Conversion/Materials";

    [MenuItem("Ema/Fix Face Materials")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        if (avatar == null)
        {
            Debug.LogError("EmaFIX avatar not found");
            EditorApplication.Exit(1);
            return;
        }

        if (!AssetDatabase.IsValidFolder(MatDir))
        {
            var parent = Path.GetDirectoryName(MatDir).Replace("\\", "/");
            var name = Path.GetFileName(MatDir);
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets", "VRChat_Conversion");
            AssetDatabase.CreateFolder(parent, name);
        }

        int created = 0;
        foreach (var sr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mats = sr.sharedMaterials;
            var newMats = new Material[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) { newMats[i] = null; continue; }
                string path = MatDir + "/" + m.name + ".mat";
                var asset = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (asset == null)
                {
                    asset = new Material(m);
                    asset.name = m.name;
                    AssetDatabase.CreateAsset(asset, path);
                    created++;
                }
                ApplyMode(asset);
                EditorUtility.SetDirty(asset);
                newMats[i] = asset;
            }
            sr.sharedMaterials = newMats;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"EmaFIX done created={created} scene saved");
        EditorApplication.Exit(0);
    }

    private static void ApplyMode(Material m)
    {
        string n = m.name;
        if (n.Contains("Aozame") || n.Contains("Kurozame") || n.Contains("Tere") || n.Contains("Eyelid"))
            SetTransparent(m);
        else
            SetOpaque(m);
    }

    private static void SetOpaque(Material m)
    {
        m.SetOverrideTag("RenderType", "Opaque");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        m.SetInt("_ZWrite", 1);
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.SetFloat("_Mode", 0f);
        m.SetInt("_Surface", 0);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
    }

    private static void SetTransparent(Material m)
    {
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.SetFloat("_Mode", 3f);
        m.SetInt("_Surface", 1);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
