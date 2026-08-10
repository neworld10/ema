using UnityEditor;
using UnityEngine;

public static class EmaInspect
{
    [MenuItem("Ema/Inspect Face Materials")]
    public static void Inspect()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRChat_Conversion/SakurabaEma_ByPOWER_VRChat.fbx");
        var sr = FindFace(prefab);
        if (sr == null)
        {
            Debug.LogError("EmaINSP face renderer not found");
            return;
        }
        var mesh = sr.sharedMesh;
        var mats = sr.sharedMaterials;
        Debug.Log("EmaINSP face mesh submesh count=" + mesh.subMeshCount + " materials=" + mats.Length);
        for (int m = 0; m < mats.Length; m++)
        {
            var sub = mesh.GetSubMesh(m);
            int triCount = sub.indexCount / 3;
            string texName = mats[m]?.mainTexture?.name ?? "none";
            // compute UV bbox of the submesh
            var indices = mesh.GetIndices(m);
            var uvs = mesh.uv;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            var hash = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (!hash.Add(indices[i])) continue;
                Vector2 uv = uvs[indices[i]];
                min = Vector2.Min(min, uv);
                max = Vector2.Max(max, uv);
            }
            Debug.Log($"EmaINSP mat[{m}] {mats[m]?.name} shader={mats[m]?.shader?.name} tex={texName} tris={triCount} uvbbox=({min.x:F2},{min.y:F2})-({max.x:F2},{max.y:F2})");
        }
        EditorApplication.Exit(0);
    }

    private static SkinnedMeshRenderer FindFace(GameObject root)
    {
        foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (r.gameObject.name == "SakurabaEma_Face" || (r.sharedMesh != null && r.sharedMesh.name == "SakurabaEma_Face"))
                return r;
        return null;
    }
}
