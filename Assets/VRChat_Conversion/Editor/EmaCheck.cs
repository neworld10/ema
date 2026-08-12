using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EmaCheck
{
    static void Log(string m) => Debug.Log("EMACHK " + m);

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
            var t = o as Transform;
            if (t != null) return t.name;
            var uo = o as UnityEngine.Object;
            if (uo != null) return uo.name;
            return o.ToString();
        }
        catch (Exception e) { return "ERR " + e.Message; }
    }

    [MenuItem("Ema/Full Check")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity");
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        if (avatar == null) { Log("avatar not found"); EditorApplication.Exit(1); return; }

        var descType = FindType("VRCAvatarDescriptor");
        var desc = avatar.GetComponent(descType);
        if (desc == null) { Log("NO VRCAvatarDescriptor"); EditorApplication.Exit(1); return; }
        Log("desc present");

        // ---- 1. Blend shapes (Ey_ / Ex_ morphs) ----
        SkinnedMeshRenderer faceSR = null;
        foreach (var sr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (sr.sharedMesh != null && sr.sharedMesh.blendShapeCount > 0)
            {
                faceSR = sr;
                break;
            }
        if (faceSR != null)
        {
            var mesh = faceSR.sharedMesh;
            Log($"blendshapes on {faceSR.name}: count={mesh.blendShapeCount}");
            int ey = 0, ex = 0;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                var n = mesh.GetBlendShapeName(i);
                if (n.StartsWith("Ey_") || n.StartsWith("Ey" + "_")) ey++;
                if (n.StartsWith("Ex_")) ex++;
            }
            Log($"Ey_ morphs={ey} Ex_ morphs={ex}");
            for (int i = 0; i < mesh.blendShapeCount && i < 40; i++)
                Log($"  BS[{i}] {mesh.GetBlendShapeName(i)}");
        }
        else Log("no blendshape renderer");

        // ---- 2. Expression parameters / menu ----
        var ep = GetField(desc, "expressionParameters");
        if (ep != null)
        {
            var ps = GetField(ep, "parameters");
            var arr = ps as Array;
            Log($"expressionParameters set, params={(arr != null ? arr.Length : -1)}");
            if (arr != null)
                foreach (var p in arr)
                    Log($"  PARAM {GetField(p, "name")} type={GetField(p, "valueType")} saved={GetField(p, "saved")}");
        }
        else Log("expressionParameters = null");

        var menu = GetField(desc, "expressionsMenu");
        if (menu != null)
        {
            var ctrls = GetField(menu, "controls") as System.Collections.ICollection;
            Log("expressionsMenu set, controls=" + (ctrls != null ? ctrls.Count.ToString() : "-1"));
            if (ctrls != null)
                foreach (var c in ctrls)
                    Log($"  MENU {GetField(c, "name")} type={GetField(c, "type")}");
        }
        else Log("expressionsMenu = null");

        // ---- 3. Eye look ----
        try
        {
            var eyl = GetField(desc, "customEyeLookSettings");
            if (eyl != null)
            {
                var leftEye = GetField(eyl, "leftEye");
                var rightEye = GetField(eyl, "rightEye");
                Log("customEyeLookSettings present, leftEye=" + SafeName(leftEye) + " rightEye=" + SafeName(rightEye));
            }
            else Log("customEyeLookSettings = null");
        }
        catch (Exception e) { Log("eye look err: " + e.Message); }

        // ---- 4. Skirt ----
        foreach (var sr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (sr.name.ToLowerInvariant().Contains("skirt"))
            {
                for (int i = 0; i < sr.sharedMaterials.Length; i++)
                {
                    var m = sr.sharedMaterials[i];
                    Log("SKIRT " + sr.name + " mat[" + i + "] " + (m != null ? m.name + " shader=" + m.shader.name + " queue=" + m.renderQueue : "null"));
                }
            }

        // ---- 5. All shaders in use ----
        Log("--- all renderers ---");
        foreach (var sr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            foreach (var m in sr.sharedMaterials)
                if (m != null)
                    Log($"REN {sr.name} | {m.name} | {m.shader.name} | q={m.renderQueue} | mode={m.GetFloat("_Mode")}");

        // ---- 6. VRC builder validation via reflection ----
        Log("--- VRC SDK builder ---");
        var ctrlType = FindType("VRCSdkControlPanel");
        if (ctrlType == null) { Log("VRCSdkControlPanel type not found"); EditorApplication.Exit(0); return; }
        var mTryGet = ctrlType.GetMethod("TryGetBuilder", BindingFlags.Public | BindingFlags.Static);
        if (mTryGet == null) { Log("TryGetBuilder not found"); EditorApplication.Exit(0); return; }
        Log("TryGetBuilder found: " + mTryGet);

        // build generic args
        // find IVRCSdkAvatarBuilderApi type
        Type builderApi = null;
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in a.GetTypes())
            {
                if (t.IsInterface && (t.Name.Contains("IVRCSdkAvatarBuilderApi") || t.Name == "IVRCSdkAvatarBuilderApi"))
                {
                    builderApi = t;
                    break;
                }
            }
            if (builderApi != null) break;
        }
        Log("builderApi type=" + (builderApi != null ? builderApi.FullName : "not found"));
        if (builderApi == null) { EditorApplication.Exit(0); return; }

        var generic = mTryGet.MakeGenericMethod(builderApi);
        var args = new object[] { null };
        var ok = (bool)generic.Invoke(null, args);
        var builder = args[0];
        Log("TryGetBuilder=" + ok + " builder=" + (builder != null ? builder.GetType().FullName : "null"));
        if (builder == null) { EditorApplication.Exit(0); return; }

        foreach (var m in builder.GetType().GetMethods())
        {
            if (m.Name.Contains("SDKError") || m.Name.Contains("Valid") || m.Name.Contains("GetSDKError") || m.Name.Contains("RefreshSDK"))
                Log("API " + m.ReturnType.Name + " " + m.Name + "(" + string.Join(",", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name)) + ")");
        }

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
