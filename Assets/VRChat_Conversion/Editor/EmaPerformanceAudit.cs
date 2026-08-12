#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation.Performance.Stats;

public static class EmaPerformanceAudit
{
    const string ScenePath = "Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity";
    const string ReportPath = "Assets/VRChat_Conversion/PERFORMANCE_AUDIT.txt";

    [MenuItem("Tools/Sakuraba Ema/Audit Performance")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        if (avatar == null) throw new InvalidOperationException("Avatar root not found");
        var desc = avatar.GetComponent<VRCAvatarDescriptor>();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("SakurabaEma VRChat performance audit");
        sb.AppendLine("===================================");
        sb.AppendLine("Triangles are Unity imported triangle counts; mobile limits are SDK 3.7.6 Quest limits.");
        WriteStats(sb, avatar, false, "PC");
        WriteStats(sb, avatar, true, "Mobile");
        sb.AppendLine();
        sb.AppendLine("Renderer detail");
        foreach (var smr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = smr.sharedMesh;
            int tris = 0;
            if (mesh != null)
                for (int i = 0; i < mesh.subMeshCount; i++) tris += (int)mesh.GetIndexCount(i) / 3;
            sb.AppendLine(string.Format("{0}: vertices={1}, triangles={2}, materials={3}, bones={4}, blendShapes={5}, readWrite={6}",
                smr.name, mesh != null ? mesh.vertexCount : 0, tris, smr.sharedMaterials.Length,
                smr.bones != null ? smr.bones.Length : 0, mesh != null ? mesh.blendShapeCount : 0,
                mesh != null && mesh.isReadable));
        }
        var pbs = avatar.GetComponentsInChildren<VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone>(true);
        foreach (var pb in pbs)
            sb.AppendLine(string.Format("PhysBone {0}: transforms={1}, colliders={2}", pb.name,
                pb.GetComponentsInChildren<Transform>(true).Length, pb.colliders != null ? pb.colliders.Count : 0));
        File.WriteAllText(ReportPath, sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log(sb.ToString());
        EditorApplication.Exit(0);
    }

    static void WriteStats(System.Text.StringBuilder sb, GameObject avatar, bool mobile, string label)
    {
        var stats = new AvatarPerformanceStats(mobile);
        try
        {
            AvatarPerformance.CalculatePerformanceStats(avatar.name, avatar, stats, mobile);
        }
        catch (NullReferenceException)
        {
            // In Unity batchmode this SDK build can calculate the counters but
            // fail while loading its Resources rating table. Keep the counters.
        }
        var pb = stats.physBone.GetValueOrDefault();
        sb.AppendLine();
        sb.AppendLine(label + " raw stats (SDK rating calculation unavailable in headless audit)");
        sb.AppendLine(string.Format("  triangles={0}, skinnedMeshes={1}, meshes={2}, materials={3}, animators={4}, bones={5}, texturesMB={6}",
            stats.polyCount, stats.skinnedMeshCount, stats.meshCount, stats.materialCount, stats.animatorCount, stats.boneCount, stats.textureMegabytes));
        sb.AppendLine(string.Format("  physBones={0}/{1}/{2}/{3}, contacts={4}, constraints={5}/{6}",
            pb.componentCount, pb.transformCount, pb.colliderCount, pb.collisionCheckCount,
            stats.contactCount, stats.constraintsCount, stats.constraintDepth));
        if (!mobile)
        {
            string overall = "Good";
            if (stats.skinnedMeshCount > 8 || stats.materialCount > 16 || stats.boneCount > 400 ||
                pb.transformCount > 256 || pb.collisionCheckCount > 512 || stats.polyCount > 70000)
                overall = "Very Poor";
            else if (stats.skinnedMeshCount > 2 || stats.materialCount > 8 || stats.boneCount > 256 ||
                     pb.transformCount > 128 || pb.collisionCheckCount > 128)
                overall = "Poor (no Very Poor overperformance condition)";
            sb.AppendLine("  PC target result=" + overall);
        }
        // The SDK's rating tables are loaded by the interactive Builder. The
        // raw counters above are sufficient for a deterministic headless audit.
    }
}
#endif
