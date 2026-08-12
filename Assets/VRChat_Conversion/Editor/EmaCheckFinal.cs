using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.PhysBone.Components;

public static class EmaCheckFinal
{
    static void L(string m) => Debug.Log("EMAFINAL " + m);

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity");
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        var d = avatar.GetComponent<VRCAvatarDescriptor>();
        var animator = avatar.GetComponent<Animator>();

        L("--- Avatar ---");
        L("animator ctrl=" + (animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null") + " isHuman=" + animator.isHuman + " avatarValid=" + (animator.avatar != null ? animator.avatar.isValid : false) + " ViewPosition=" + d.ViewPosition);

        L("--- Expressions ---");
        L("customExpressions=" + d.customExpressions + " menu=" + (d.expressionsMenu != null ? d.expressionsMenu.name : "null") + " params=" + (d.expressionParameters != null ? d.expressionParameters.parameters.Length : -1));
        L("baseLayers=" + d.baseAnimationLayers.Length + " customize=" + d.customizeAnimationLayers);

        L("--- LipSync ---");
        L("lipSync=" + d.lipSync + " mesh=" + (d.VisemeSkinnedMesh != null ? d.VisemeSkinnedMesh.name : "null") + " len=" + (d.VisemeBlendShapes != null ? d.VisemeBlendShapes.Length : -1));

        L("--- Eye Look ---");
        L("enableEyeLook=" + d.enableEyeLook + " left=" + (d.customEyeLookSettings.leftEye != null ? d.customEyeLookSettings.leftEye.name : "null") + " right=" + (d.customEyeLookSettings.rightEye != null ? d.customEyeLookSettings.rightEye.name : "null") + " eyelid=" + d.customEyeLookSettings.eyelidType + " mesh=" + (d.customEyeLookSettings.eyelidsSkinnedMesh != null ? d.customEyeLookSettings.eyelidsSkinnedMesh.name : "null"));

        L("--- Physics ---");
        var pbs = avatar.GetComponentsInChildren<VRCPhysBone>(true);
        foreach (var pb in pbs)
            L("physbone " + pb.transform.name + " pull=" + pb.pull + " spring=" + pb.spring + " stiffness=" + pb.stiffness + " colliders=" + (pb.colliders != null ? pb.colliders.Count : 0));
        var cols = avatar.GetComponentsInChildren<VRCPhysBoneCollider>(true);
        L("total colliders=" + cols.Length);

        L("--- Materials ---");
        foreach (var sr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mats = sr.sharedMaterials;
            L(sr.name + " mats=" + string.Join(", ", mats.Select(m => m != null ? m.name : "null")));
            if (sr == avatar.transform.Find("SakurabaEma_Face").GetComponent<SkinnedMeshRenderer>())
            {
                foreach (var m in mats)
                {
                    var mat = m;
                    L("   face mat " + mat.name + " queue=" + mat.renderQueue + " shader=" + mat.shader.name);
                }
            }
        }

        L("--- Expression menu assets ---");
        var ep = d.expressionParameters;
        L("params: " + string.Join(",", ep.parameters.Select(p => p.name + ":" + p.valueType + " saved=" + p.saved)));
        EditorApplication.Exit(0);
    }
}
