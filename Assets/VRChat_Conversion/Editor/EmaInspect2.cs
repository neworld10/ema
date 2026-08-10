using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EmaInspect2
{
    [MenuItem("Ema/Inspect Scene Materials")]
    public static void Inspect()
    {
        EditorSceneManager.OpenScene("Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity");
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        if (avatar == null)
        {
            Debug.LogError("EmaINSP avatar not found");
            EditorApplication.Exit(1);
            return;
        }
        foreach (var sr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Debug.Log($"EmaINSP --- {sr.gameObject.name} mats={sr.sharedMaterials.Length}");
            for (int i = 0; i < sr.sharedMaterials.Length; i++)
            {
                var m = sr.sharedMaterials[i];
                if (m == null)
                {
                    Debug.Log($"EmaINSP   [{i}] null");
                    continue;
                }
                float mode = m.HasProperty("_Mode") ? m.GetFloat("_Mode") : -1f;
                float queue = m.renderQueue;
                string tags = m.GetTag("RenderType", true);
                string mainTex = m.mainTexture != null ? m.mainTexture.name : "none";
                bool smooth = m.HasProperty("_Smoothness");
                Debug.Log($"EmaINSP   [{i}] {m.name} shader={m.shader.name} mode={mode} queue={queue} renderType={tags} tex={mainTex}");
            }
        }
        EditorApplication.Exit(0);
    }
}
