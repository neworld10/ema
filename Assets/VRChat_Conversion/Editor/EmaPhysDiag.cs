using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Animations;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.Dynamics;

public static class EmaPhysDiag
{
    static void L(string m) => Debug.Log("EMAPDIAG " + m);

    static string PathOf(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }

    static int CountTransforms(Transform t)
    {
        int n = 1;
        for (int i = 0; i < t.childCount; i++) n += CountTransforms(t.GetChild(i));
        return n;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity");
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        if (avatar == null) { L("AVATAR NOT FOUND"); EditorApplication.Exit(0); return; }

        foreach (var pb in avatar.GetComponentsInChildren<VRCPhysBone>(true))
        {
            var root = pb.rootTransform != null ? pb.rootTransform : pb.transform;
            L("PB on=" + PathOf(pb.transform) +
              " enabled=" + pb.enabled +
              " root=" + root.name +
              " rootTransformSet=" + (pb.rootTransform != null) +
              " transformsUnderRoot=" + CountTransforms(root) +
              " multiChild=" + pb.multiChildType +
              " isAnimated=" + pb.isAnimated +
              " integration=" + pb.integrationType +
              " pull=" + pb.pull + " spring=" + pb.spring + " stiff=" + pb.stiffness +
              " immobile=" + pb.immobile + " grav=" + pb.gravity +
              " radius=" + pb.radius +
              " colliders=" + (pb.colliders != null ? pb.colliders.Count : -1));
            if (pb.colliders != null)
                foreach (var c in pb.colliders)
                    L("   PB.collider=" + (c == null ? "NULL" : PathOf(c.transform) + " shape=" + c.shapeType + " r=" + c.radius + " h=" + c.height));
        }

        foreach (var c in avatar.GetComponentsInChildren<VRCPhysBoneCollider>(true))
        {
            L("COL " + PathOf(c.transform) + " shape=" + c.shapeType +
              " r=" + c.radius.ToString("F3") + " h=" + c.height.ToString("F3") +
              " pos=" + c.position.ToString("F3") + " rotEuler=" + c.rotation.eulerAngles.ToString("F1") +
              " enabled=" + c.enabled);
        }

        var animator = avatar.GetComponentInChildren<Animator>();
        L("animator=" + (animator != null ? animator.name : "NULL") +
          " controller=" + (animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL"));

        var clips = new Dictionary<string, AnimationClip>();
        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (var c in animator.runtimeAnimatorController.animationClips)
                clips[c.name] = c;
        foreach (var kv in clips)
        {
            var bindings = AnimationUtility.GetCurveBindings(kv.Value);
            int skt = 0, hair = 0, body = 0, face = 0;
            var sktPaths = new HashSet<string>();
            foreach (var b in bindings)
            {
                if (b.path.Contains("Skt_") || b.path.Contains("Skirt")) { skt++; sktPaths.Add(b.path); }
                else if (b.path.Contains("BaHair") || b.path.Contains("Hair")) hair++;
                else if (b.path.Contains("Face") || b.path.Contains("Eye")) face++;
                else body++;
            }
            L("CLIP " + kv.Key + " len=" + kv.Value.length.ToString("F2") +
              " curves=" + bindings.Length + " skirtCurves=" + skt + " hairCurves=" + hair +
              " faceCurves=" + face + " otherCurves=" + body +
              " skirtPaths=" + string.Join(",", sktPaths));
        }

        foreach (var smr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            int sktBones = 0, hairBones = 0;
            var sktNames = new List<string>();
            if (smr.bones != null)
                foreach (var b in smr.bones)
                {
                    if (b == null) continue;
                    if (b.name.StartsWith("Skt_") || b.name.Contains("Skirt")) { sktBones++; if (sktNames.Count < 4) sktNames.Add(b.name); }
                    else if (b.name.Contains("Hair") || b.name.StartsWith("BaHair")) hairBones++;
                }
            L("SMR " + smr.name + " bones=" + (smr.bones != null ? smr.bones.Length : -1) +
              " sktBones=" + sktBones + " hairBones=" + hairBones +
              " sktSample=" + string.Join(",", sktNames) +
              " rootBone=" + (smr.rootBone != null ? smr.rootBone.name : "NULL") +
              " verts=" + smr.sharedMesh.vertexCount);
        }

        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/VRChat_Conversion/SakurabaEma_ByPOWER_VRChat.fbx"))
        {
            var clip = obj as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview__")) continue;
            var bindings = AnimationUtility.GetCurveBindings(clip);
            int skt = 0, hair = 0, body = 0, face = 0;
            var paths = new HashSet<string>();
            foreach (var b in bindings)
            {
                paths.Add(b.path);
                if (b.path.Contains("Skt_") || b.path.Contains("Skirt")) skt++;
                else if (b.path.Contains("Hair")) hair++;
                else if (b.path.Contains("Face") || b.path.Contains("Eye")) face++;
                else body++;
            }
            int sktPaths = 0;
            foreach (var p in paths) if (p.Contains("Skt_") || p.Contains("Skirt")) sktPaths++;
            L("FBXCLIP " + clip.name + " len=" + clip.length.ToString("F2") +
              " curves=" + bindings.Length + " skirt=" + skt + " hair=" + hair +
              " face=" + face + " other=" + body + " skirtPaths=" + sktPaths + " totalPaths=" + paths.Count);
        }

        var arm = avatar.transform.Find("SakurabaEma_VRChat_Armature");
        if (arm != null)
        {
            var skirtRoot = arm.Find("Root/Hips/Skirt_Root");
            if (skirtRoot == null)
            {
                System.Func<Transform, Transform> findDeep = null;
                findDeep = t =>
                {
                    if (t.name == "Skirt_Root") return t;
                    for (int i = 0; i < t.childCount; i++) { var r = findDeep(t.GetChild(i)); if (r != null) return r; }
                    return null;
                };
                skirtRoot = findDeep(arm);
            }
            L("Skirt_Root found=" + (skirtRoot != null) + (skirtRoot != null ? " path=" + PathOf(skirtRoot) + " children=" + skirtRoot.childCount : ""));
            if (skirtRoot != null && skirtRoot.childCount > 0)
            {
                var c0 = skirtRoot.GetChild(0);
                L("Skirt_Root child0=" + c0.name + " children=" + c0.childCount +
                  " localPos=" + c0.localPosition.ToString("F4") + " localRot=" + c0.localRotation.eulerAngles.ToString("F1"));
            }
        }

        EditorApplication.Exit(0);
    }
}
