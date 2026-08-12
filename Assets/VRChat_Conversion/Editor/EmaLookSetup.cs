using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using VRC.SDK3.Avatars.Components;

public static class EmaLookSetup
{
    static void L(string m) => Debug.Log("EMALOOK " + m);

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity");
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        var d = avatar.GetComponent<VRCAvatarDescriptor>();
        var arm = avatar.transform.Find("SakurabaEma_VRChat_Armature");
        Transform FD(Transform t, string n)
        {
            if (t.name == n) return t;
            for (int i = 0; i < t.childCount; i++) { var r = FD(t.GetChild(i), n); if (r != null) return r; }
            return null;
        }
        var face = avatar.transform.Find("SakurabaEma_Face").GetComponent<SkinnedMeshRenderer>();
        var leftEye = FD(arm, "LeftEye");
        var rightEye = FD(arm, "RightEye");

        // ---- viseme ----
        d.VisemeSkinnedMesh = face;
        var viseme = new string[15];
        viseme[10] = "vrc.v_aa";
        viseme[11] = "vrc.v_e";
        viseme[12] = "vrc.v_ih";
        viseme[13] = "vrc.v_oh";
        viseme[14] = "vrc.v_ou";
        d.VisemeBlendShapes = viseme;

        // ---- eye look ----
        d.enableEyeLook = true;
        var s = d.customEyeLookSettings;
        s.leftEye = leftEye;
        s.rightEye = rightEye;
        s.eyeMovement.confidence = 1f;
        s.eyeMovement.excitement = 1f;

        var qL = leftEye.localRotation;
        var qR = rightEye.localRotation;
        s.eyesLookingStraight = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations { linked = true, left = qL, right = qR };
        s.eyesLookingUp = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations { linked = true, left = qL * Quaternion.Euler(20, 0, 0), right = qR * Quaternion.Euler(20, 0, 0) };
        s.eyesLookingDown = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations { linked = true, left = qL * Quaternion.Euler(-20, 0, 0), right = qR * Quaternion.Euler(-20, 0, 0) };
        s.eyesLookingLeft = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations { linked = true, left = qL * Quaternion.Euler(0, 0, -20), right = qR * Quaternion.Euler(0, 0, -20) };
        s.eyesLookingRight = new VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations { linked = true, left = qL * Quaternion.Euler(0, 0, 20), right = qR * Quaternion.Euler(0, 0, 20) };

        s.eyelidType = VRCAvatarDescriptor.EyelidType.Blendshapes;
        s.eyelidsSkinnedMesh = face;
        s.eyelidsBlendshapes = new[] { 85, 83, 84 }; // Blink, vrc.Blink_L, vrc.Blink_R
        d.customEyeLookSettings = s;

        EditorSceneManager.MarkSceneDirty(avatar.scene);
        EditorSceneManager.SaveScene(avatar.scene);
        L("look setup done: visemeMesh=" + (d.VisemeSkinnedMesh != null ? d.VisemeSkinnedMesh.name : "null") + " visemeLen=" + d.VisemeBlendShapes.Length + " enableEyeLook=" + d.enableEyeLook + " leftEye=" + (d.customEyeLookSettings.leftEye != null) + " eyelid=" + d.customEyeLookSettings.eyelidType + " [" + string.Join(",", d.customEyeLookSettings.eyelidsBlendshapes) + "]");
        EditorApplication.Exit(0);
    }
}
