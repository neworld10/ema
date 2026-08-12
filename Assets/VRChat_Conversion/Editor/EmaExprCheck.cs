using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

public static class EmaExprCheck
{
    [MenuItem("Ema/Check Expressions")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity");
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        if (avatar == null) { Debug.LogError("EMAXP avatar not found"); EditorApplication.Exit(1); return; }

        var desc = avatar.GetComponent<VRCAvatarDescriptor>();
        if (desc == null) { Debug.LogError("EMAXP no VRCAvatarDescriptor"); EditorApplication.Exit(1); return; }

        Debug.Log($"EMAXP desc present={desc != null}");

        // blend shapes
        var face = avatar.transform.Find("SakurabaEma_Face");
        SkinnedMeshRenderer faceSR = face != null ? face.GetComponent<SkinnedMeshRenderer>() : null;
        if (faceSR == null)
        {
            foreach (var sr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (sr.sharedMesh != null && sr.sharedMesh.blendShapeCount > 0) faceSR = sr;
        }
        if (faceSR != null && faceSR.sharedMesh != null)
        {
            var mesh = faceSR.sharedMesh;
            Debug.Log($"EMAXP blendshape count={mesh.blendShapeCount}");
            for (int i = 0; i < mesh.blendShapeCount && i < 200; i++)
                Debug.Log($"EMAXP BS[{i}] {mesh.GetBlendShapeName(i)}");
        }
        else Debug.Log("EMAXP no blendshape renderer found");

        // expression parameters / menu
        var ep = desc.expressionParameters;
        Debug.Log($"EMAXP expressionParameters set={ep != null} count={(ep != null && ep.parameters != null ? ep.parameters.Length : -1)}");
        if (ep != null && ep.parameters != null)
            foreach (var p in ep.parameters)
                Debug.Log($"EMAXP PARAM {p.name} type={p.valueType} saved={p.saved} default={p.defaultValue}");

        var menu = desc.expressionsMenu;
        Debug.Log($"EMAXP expressionsMenu set={menu != null} controls={(menu != null && menu.controls != null ? menu.controls.Count : -1)}");
        if (menu != null && menu.controls != null)
            foreach (var c in menu.controls)
                Debug.Log($"EMAXP MENU {c.name} type={c.type}");

        EditorApplication.Exit(0);
    }
}
