using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EmaCheck2
{
    static void Log(string m) => Debug.Log("EMA2 " + m);

    static object GetField(object o, string name)
    {
        var f = o.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return f != null ? f.GetValue(o) : null;
    }

    static string SafeName(object o)
    {
        try
        {
            if (o == null) return "null";
            var t = o as Transform; if (t != null) return t.name;
            var uo = o as UnityEngine.Object; if (uo != null) return uo.name;
            return o.ToString();
        }
        catch (Exception e) { return "ERR " + e.Message; }
    }

    [MenuItem("Ema/Check2")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity");
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        if (avatar == null) { Log("avatar not found"); EditorApplication.Exit(1); return; }

        // Animator
        var anim = avatar.GetComponent<Animator>();
        Log("Animator present=" + (anim != null) + (anim != null ? " controller=" + SafeName(anim.runtimeAnimatorController) + " avatar=" + (anim.avatar != null ? anim.avatar.name : "null") : ""));

        var descType = FindType("VRCAvatarDescriptor");
        var desc = avatar.GetComponent(descType);

        // Playable layers
        var pl = GetField(desc, "baseAnimationLayers");
        if (pl != null)
        {
            var arr = pl as Array;
            Log("baseAnimationLayers count=" + (arr != null ? arr.Length : -1));
            if (arr != null)
                foreach (var l in arr)
                {
                    var type = GetField(l, "type");
                    var isDefault = GetField(l, "isDefault");
                    var ac = GetField(l, "animatorController");
                    Log("  layer type=" + type + " isDefault=" + isDefault + " controller=" + SafeName(ac));
                }
        }

        // Lip sync
        var lipSync = GetField(desc, "lipSync");
        var lipSyncMode = GetField(desc, "lipSyncMode");
        Log("lipSync=" + lipSync + " lipSyncMode=" + lipSyncMode);

        // bones: find eye / skirt / hair / chest bones
        Log("--- bones (name contains) ---");
        foreach (var t in avatar.GetComponentsInChildren<Transform>(true))
        {
            var n = t.name;
            if (n.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Skirt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Chest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0)
                Log("  bone " + t.name);
        }

        // Mesh submeshes for the body (which submesh is skirt?)
        var bodySR = avatar.transform.Find("SakurabaEma_Body").GetComponent<SkinnedMeshRenderer>();
        if (bodySR != null)
        {
            var mesh = bodySR.sharedMesh;
            Log("Body mesh submeshes=" + mesh.subMeshCount + " bones=" + mesh.bindposes.Length);
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var topo = mesh.GetTopology(i);
                uint idxcount = mesh.GetIndexCount(i);
                int tris = topo == MeshTopology.Triangles ? (int)(idxcount / 3) : (int)idxcount;
                var m = i < bodySR.sharedMaterials.Length ? bodySR.sharedMaterials[i] : null;
                Log("  submesh[" + i + "] tris=" + tris + " mat=" + (m != null ? m.name : "null"));
            }
            // which bones deform the skirt submesh
            var bind = mesh.bindposes;
            var boneNames = bodySR.bones;
            Log("  bones count=" + (boneNames != null ? boneNames.Length : -1));
        }
        else Log("Body renderer not found");

        EditorApplication.Exit(0);
    }

    static Type FindType(string name)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in a.GetTypes())
            {
                if (t.Name == name || t.FullName == name) return t;
            }
        }
        return null;
    }
}
