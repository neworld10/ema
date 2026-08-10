#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// bone verification pass


/// <summary>
/// Ensures the SakurabaEma FBX imports as a valid Humanoid avatar, then verifies
/// meshes, blend shapes and material textures. Run headless:
/// Unity -batchmode -projectPath ... -executeMethod EmaImportVerifier.Verify -quit
/// </summary>
public static class EmaImportVerifier
{
    private const string FbxPath = "Assets/VRChat_Conversion/SakurabaEma_ByPOWER_VRChat.fbx";
    private const string ReportPath = "Assets/VRChat_Conversion/VALIDATION_REPORT_IMPORT.txt";

    [InitializeOnLoadMethod]
    private static void AutoVerify()
    {
        EditorApplication.delayCall += () =>
        {
            var fbxStamp = File.GetLastWriteTimeUtc(FbxPath);
            var reportStamp = File.Exists(ReportPath) ? File.GetLastWriteTimeUtc(ReportPath) : DateTime.MinValue;
            if (fbxStamp > reportStamp.AddMinutes(1))
                Verify();
        };
    }

    [MenuItem("Tools/Sakuraba Ema/Verify Import")]
    public static void Verify()
    {
        var sb = new StringBuilder();
        sb.AppendLine("SakurabaEma FBX import verification");
        sb.AppendLine("====================================");
        var ok = true;

        var importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            sb.AppendLine("rig: set Humanoid + reimported");
        }
        AssetDatabase.ImportAsset(FbxPath, ImportAssetOptions.ForceUpdate);

        var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(FbxPath);
        if (avatar != null)
        {
            var spurious = new List<HumanBone>();
            foreach (var hb in avatar.humanDescription.human)
            {
                if (hb.humanName == "Jaw" && hb.boneName != "Jaw")
                    spurious.Add(hb);
            }
            if (spurious.Count > 0)
            {
                var kept = new List<HumanBone>();
                foreach (var hb in avatar.humanDescription.human)
                    kept.Add(hb);
                kept.RemoveAll(hb => spurious.Contains(hb));
                var hd = importer.humanDescription;
                hd.human = kept.ToArray();
                importer.humanDescription = hd;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(FbxPath, ImportAssetOptions.ForceUpdate);
                avatar = AssetDatabase.LoadAssetAtPath<Avatar>(FbxPath);
                sb.AppendLine("rig: removed " + spurious.Count + " spurious Jaw mapping(s)");
            }
        }

        sb.AppendLine("animationType: " + importer.animationType);
        sb.AppendLine("avatar found: " + (avatar != null));
        sb.AppendLine("avatar valid: " + (avatar != null && avatar.isValid));
        sb.AppendLine("avatar humanoid: " + (avatar != null && avatar.isHuman));
        if (avatar != null)
        {
            var mapped = avatar.humanDescription.human;
            sb.AppendLine("mapped human bones: " + mapped.Length);
            foreach (var hb in mapped)
                sb.AppendLine("  " + hb.humanName + " <- " + hb.boneName);
        }
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            sb.AppendLine("IMPORT_ERROR avatar invalid");
            ok = false;
        }

        var root = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (root == null)
        {
            sb.AppendLine("IMPORT_ERROR root prefab not found");
            ok = false;
        }
        else
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null && avatar != null)
                sb.AppendLine("animator avatar: " + (animator.avatar == avatar ? "linked" : "NOT LINKED"));

            var bones = new List<string>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                bones.Add(t.name);
            sb.AppendLine("transforms: " + bones.Count);
            sb.AppendLine("has Hips: " + bones.Contains("Hips"));
            sb.AppendLine("has Head: " + bones.Contains("Head"));
            sb.AppendLine("has LeftEye: " + bones.Contains("LeftEye"));
            sb.AppendLine("has LeftHand: " + bones.Contains("LeftHand"));

            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in renderers)
            {
                var mesh = r.sharedMesh;
                var blend = new List<string>();
                if (mesh != null)
                {
                    for (var i = 0; i < mesh.blendShapeCount; i++)
                        blend.Add(mesh.GetBlendShapeName(i));
                }
                sb.AppendLine("SMR " + r.gameObject.name + " mesh=" + (mesh != null ? mesh.name : "null")
                              + " vertices=" + (mesh != null ? mesh.vertexCount : 0)
                              + " blendShapes=" + blend.Count
                              + " bones=" + (r.bones != null ? r.bones.Length : 0)
                              + " rootBone=" + (r.rootBone != null ? r.rootBone.name : "none"));
                if (mesh != null)
                {
                    var bw = mesh.boneWeights;
                    var weighted = 0;
                    foreach (var b in bw)
                        if (b.weight0 + b.weight1 + b.weight2 + b.weight3 > 0.0001f)
                            weighted++;
                    sb.AppendLine("  weightedVerts=" + weighted + "/" + bw.Length);
                }
                foreach (var m in r.sharedMaterials)
                {
                    var main = m != null ? m.mainTexture : null;
                    sb.AppendLine("    mat " + (m != null ? m.name : "null")
                                  + " shader=" + (m != null && m.shader != null ? m.shader.name : "null")
                                  + " mainTex=" + (main != null ? main.name : "NONE"));
                    if (m != null && main == null)
                        sb.AppendLine("      WARN material has no main texture");
                }
            }
            sb.AppendLine("SMR count: " + renderers.Length);
            if (renderers.Length != 3)
            {
                sb.AppendLine("IMPORT_ERROR expected 3 SkinnedMeshRenderers, got " + renderers.Length);
                ok = false;
            }
        }

        var reportPath = ReportPath;
        File.WriteAllText(reportPath, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log("EMAVERIFY " + (ok ? "PASS" : "FAIL"));
        Debug.Log(sb.ToString());

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Sakuraba Ema", "검증 " + (ok ? "통과" : "실패") + ", 자세한 내용은 VALIDATION_REPORT_IMPORT.txt", "확인");
    }
}
#endif
