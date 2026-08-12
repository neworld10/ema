using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

public static class EmaFixBuild
{
    static void L(string m) => Debug.Log("EMAFIX " + m);

    public static void Run()
    {
        string scenePath = "Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity";
        EditorSceneManager.OpenScene(scenePath);

        foreach (var model in new[] { "Assets/VRChat_Conversion/SakurabaEma_ByPOWER_VRChat.fbx", "Assets/VRChat_Conversion/SakurabaEma_ByPOWER_VRChat.blend" })
        {
            var mi = AssetImporter.GetAtPath(model) as ModelImporter;
            if (mi == null) { L("NO IMPORTER " + model); continue; }
            mi.isReadable = true;
            mi.importBlendShapeNormals = ModelImporterNormals.Calculate;
            var so = new SerializedObject(mi);
            var sp = so.FindProperty("legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes");
            if (sp != null) { sp.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); L("LEGACY SET " + model); }
            else L("LEGACY NOT FOUND " + model);
            AssetDatabase.ImportAsset(model, ImportAssetOptions.ForceUpdate);
            L("MODEL FIXED " + model);
        }

        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) continue;
            ti.streamingMipmaps = true;
            var ef = ti.mipmapFilter.GetType();
            if (!Enum.IsDefined(ef, "KaiserFilter"))
            {
                foreach (var name in Enum.GetNames(ef)) L("MIPFILTER ENUM " + name);
            }
            try { ti.mipmapFilter = (UnityEditor.TextureImporterMipFilter)Enum.Parse(ef, "KaiserFilter"); }
            catch { L("KAISER PARSE FAIL " + p); }
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        }
        L("TEXTURES DONE");

        var arm = GameObject.Find("SakurabaEma_VRChat_Armature");
        if (arm == null) { L("NO ARMATURE"); EditorApplication.Exit(1); return; }

        ReorderArm(arm, "LeftUpperArm", "LeftLowerArm");
        ReorderArm(arm, "RightUpperArm", "RightLowerArm");
        ReorderArm(arm, "LeftLowerArm", "LeftHand");
        ReorderArm(arm, "RightLowerArm", "RightHand");
        L("BONE ORDER FIXED");

        foreach (var side in new[] { "Left", "Right" })
        {
            var ua = FindDeep(arm.transform, side + "UpperArm");
            var la = FindDeep(arm.transform, side + "LowerArm");
            L("CHECK " + ua.name + " firstChild=" + (ua.GetChild(0).name) + " | " + la.name + " firstChild=" + (la.GetChild(0).name));
        }

        FixPhysics(arm);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        L("SCENE SAVED");

        var mi2 = AssetImporter.GetAtPath("Assets/VRChat_Conversion/SakurabaEma_ByPOWER_VRChat.fbx") as ModelImporter;
        var so2 = new SerializedObject(mi2);
        var sp2 = so2.FindProperty("legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes");
        var mesh2 = AssetDatabase.LoadAllAssetsAtPath("Assets/VRChat_Conversion/SakurabaEma_ByPOWER_VRChat.fbx").OfType<Mesh>().FirstOrDefault();
        L("VERIFY fbx readable=" + (mesh2 != null && mesh2.isReadable) + " blendShapeNormals=" + mi2.importBlendShapeNormals + " legacy=" + (sp2 != null ? sp2.boolValue : "?"));
        EditorApplication.Exit(0);
    }

    static void ReorderArm(GameObject arm, string parentName, string childName)
    {
        var p = FindDeep(arm.transform, parentName);
        var c = FindDeep(arm.transform, childName);
        if (p == null || c == null) { L("MISSING " + parentName + "/" + childName); return; }
        c.SetSiblingIndex(0);
    }

    static Transform FindDeep(Transform t, string name)
    {
        foreach (Transform c in t)
        {
            if (c.name == name) return c;
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    static void FixPhysics(GameObject arm)
    {
        var physbones = arm.GetComponentsInChildren<VRCPhysBone>(true);
        var colliders = arm.GetComponentsInChildren<VRCPhysBoneCollider>(true)
            .ToDictionary(c => c.transform.name, c => (VRCPhysBoneColliderBase)c);

        foreach (var pb in physbones)
        {
            string rootName = pb.rootTransform != null ? pb.rootTransform.name : pb.name;
            List<string> toAssign = new();
            if (rootName.Contains("Hair")) toAssign.AddRange(new[] { "PB_Collider_Head", "PB_Collider_Chest" });
            // Four skirt colliders keep waist and upper-leg collision while
            // staying below the PC PhysBone collision-check budget.
            else if (rootName.Contains("Skirt")) toAssign.AddRange(new[] { "PB_Collider_Hips", "PB_Collider_Chest", "PB_Collider_LLeg", "PB_Collider_RLeg" });
            else if (rootName.Contains("ChestRb")) toAssign.Add("PB_Collider_Chest");

            pb.colliders.Clear();
            foreach (var cn in toAssign)
                if (colliders.TryGetValue(cn, out var col)) pb.colliders.Add(col);

            var bones = pb.GetComponentsInChildren<Transform>(true).Length;
            L("PHYSBONE " + rootName + " bones=" + bones + " colliders=" + pb.colliders.Count + " -> estChecks=" + (bones * pb.colliders.Count));
        }
    }
}
