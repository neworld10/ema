using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

public static class EmaFacialSetup
{
    const string FOLDER = "Assets/VRChat_Conversion/Animation";
    const string CLIPS = FOLDER + "/Clips";

    static string[] ExMorphs = { "Ex_怒る", "Ex_悲しい", "Ex_照れ", "Ex_びっくり", "Ex_びっくり+", "Ex_絶望1", "Ex_絶望1+", "Ex_絶望2", "Ex_絶望2+", "Ex_笑顔", "Ex_困る", "Ex_困る2" };
    static string[] EyMorphs = { "Ey_怒る", "Ey_悲しい", "Ey_照れ", "Ey_絶望1", "Ey_笑顔", "Ey_困る", "Ey_困る2" };

    static void L(string m) => Debug.Log("EMAFACE " + m);

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity");
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
        var face = avatar.transform.Find("SakurabaEma_Face").GetComponent<SkinnedMeshRenderer>();

        if (!AssetDatabase.IsValidFolder(FOLDER)) AssetDatabase.CreateFolder("Assets/VRChat_Conversion", "Animation");
        if (!AssetDatabase.IsValidFolder(CLIPS)) AssetDatabase.CreateFolder(FOLDER, "Clips");
        AssetDatabase.DeleteAsset(FOLDER + "/EmaExpressionParameters.asset");
        AssetDatabase.DeleteAsset(FOLDER + "/EmaMenu.asset");
        AssetDatabase.DeleteAsset(FOLDER + "/EmaMenu_Ex.asset");
        AssetDatabase.DeleteAsset(FOLDER + "/EmaMenu_Ey.asset");
        AssetDatabase.DeleteAsset(FOLDER + "/EmaFX.controller");
        foreach (var a in AssetDatabase.FindAssets("", new[] { CLIPS }))
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(a));

        // ---- parameters ----
        var paramAsset = ScriptableObject.CreateInstance<VRCExpressionParameters>();
        paramAsset.parameters = new VRCExpressionParameters.Parameter[2];
        paramAsset.parameters[0] = new VRCExpressionParameters.Parameter { name = "FaceEmote", valueType = VRCExpressionParameters.ValueType.Int, saved = true, defaultValue = 0, networkSynced = true };
        paramAsset.parameters[1] = new VRCExpressionParameters.Parameter { name = "FaceEmote2", valueType = VRCExpressionParameters.ValueType.Int, saved = true, defaultValue = 0, networkSynced = true };
        AssetDatabase.CreateAsset(paramAsset, FOLDER + "/EmaExpressionParameters.asset");

        // ---- menus ----
        var menuEx = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
        menuEx.controls = new List<VRCExpressionsMenu.Control>();
        for (int i = 0; i < ExMorphs.Length; i++)
            menuEx.controls.Add(new VRCExpressionsMenu.Control { name = ExMorphs[i], type = VRCExpressionsMenu.Control.ControlType.Button, parameter = new VRCExpressionsMenu.Control.Parameter { name = "FaceEmote" }, value = i + 1 });
        AssetDatabase.CreateAsset(menuEx, FOLDER + "/EmaMenu_Ex.asset");

        var menuEy = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
        menuEy.controls = new List<VRCExpressionsMenu.Control>();
        for (int i = 0; i < EyMorphs.Length; i++)
            menuEy.controls.Add(new VRCExpressionsMenu.Control { name = EyMorphs[i], type = VRCExpressionsMenu.Control.ControlType.Button, parameter = new VRCExpressionsMenu.Control.Parameter { name = "FaceEmote2" }, value = i + 1 });
        AssetDatabase.CreateAsset(menuEy, FOLDER + "/EmaMenu_Ey.asset");

        var menuRoot = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
        menuRoot.controls = new List<VRCExpressionsMenu.Control>
        {
            new VRCExpressionsMenu.Control { name = "表情", type = VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu = menuEx },
            new VRCExpressionsMenu.Control { name = "目", type = VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu = menuEy },
        };
        AssetDatabase.CreateAsset(menuRoot, FOLDER + "/EmaMenu.asset");

        // ---- clips ----
        string rendererPath = face.name;
        var neutral = new AnimationClip();
        AssetDatabase.CreateAsset(neutral, CLIPS + "/Neutral.anim");

        var exClips = new List<AnimationClip>();
        var exThresh = new List<float> { 0f };
        foreach (var m in ExMorphs)
        {
            var clip = MakeMorphClip(rendererPath, m, 100f);
            AssetDatabase.CreateAsset(clip, CLIPS + "/" + m + ".anim");
            exClips.Add(clip);
            exThresh.Add(exClips.Count);
        }
        exClips.Insert(0, neutral);

        var eyClips = new List<AnimationClip>();
        var eyThresh = new List<float> { 0f };
        foreach (var m in EyMorphs)
        {
            var clip = MakeMorphClip(rendererPath, m, 100f);
            AssetDatabase.CreateAsset(clip, CLIPS + "/" + m + ".anim");
            eyClips.Add(clip);
            eyThresh.Add(eyClips.Count);
        }
        eyClips.Insert(0, neutral);

        // ---- FX controller ----
        var fxPath = FOLDER + "/EmaFX.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(fxPath);
        controller.AddParameter("FaceEmote", AnimatorControllerParameterType.Int);
        controller.AddParameter("FaceEmote2", AnimatorControllerParameterType.Int);
        var blendIdx = -1;
        for (int i = 0; i < controller.parameters.Length; i++)
            if (controller.parameters[i].name == "Blend") blendIdx = i;
        if (blendIdx >= 0) controller.RemoveParameter(blendIdx);

        var layer0 = controller.layers[0];
        layer0.stateMachine.defaultState = MakeTreeState(controller, layer0.stateMachine, "Ex", "FaceEmote", exClips.ToArray(), exThresh.ToArray());
        var layer1 = new AnimatorControllerLayer { name = "Ey", defaultWeight = 1f, stateMachine = new AnimatorStateMachine() };
        layer1.stateMachine.name = "Ey";
        controller.AddLayer(layer1);
        var sm1 = controller.layers[1].stateMachine;
        AssetDatabase.AddObjectToAsset(sm1, controller);
        sm1.defaultState = MakeTreeState(controller, sm1, "Ey", "FaceEmote2", eyClips.ToArray(), eyThresh.ToArray());
        var arr = controller.layers;
        arr[0].name = "Ex";
        arr[1].name = "Ey";
        controller.layers = arr;

        AssetDatabase.SaveAssets();

        // ---- descriptor ----
        descriptor.customExpressions = true;
        descriptor.expressionsMenu = menuRoot;
        descriptor.expressionParameters = paramAsset;
        descriptor.customizeAnimationLayers = true;

        var baseLayers = new VRCAvatarDescriptor.CustomAnimLayer[6];
        baseLayers[0] = DefaultLayer(VRCAvatarDescriptor.AnimLayerType.Base);
        baseLayers[1] = DefaultLayer(VRCAvatarDescriptor.AnimLayerType.Additive);
        baseLayers[2] = DefaultLayer(VRCAvatarDescriptor.AnimLayerType.Gesture);
        baseLayers[3] = DefaultLayer(VRCAvatarDescriptor.AnimLayerType.Action);
        baseLayers[4] = new VRCAvatarDescriptor.CustomAnimLayer { type = VRCAvatarDescriptor.AnimLayerType.FX, isEnabled = true, isDefault = false, animatorController = controller };
        baseLayers[5] = DefaultLayer(VRCAvatarDescriptor.AnimLayerType.Sitting);
        descriptor.baseAnimationLayers = baseLayers;

        descriptor.specialAnimationLayers = new[]
        {
            DefaultLayer(VRCAvatarDescriptor.AnimLayerType.TPose),
            DefaultLayer(VRCAvatarDescriptor.AnimLayerType.IKPose),
        };

        var animator = avatar.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        EditorSceneManager.MarkSceneDirty(avatar.scene);
        EditorSceneManager.SaveScene(avatar.scene);
        L("facial setup done: params=" + paramAsset.parameters.Length + " exMenu=" + menuEx.controls.Count + " eyMenu=" + menuEy.controls.Count + " fxLayers=" + controller.layers.Length);
        EditorApplication.Exit(0);
    }

    static VRCAvatarDescriptor.CustomAnimLayer DefaultLayer(VRCAvatarDescriptor.AnimLayerType t)
    {
        return new VRCAvatarDescriptor.CustomAnimLayer { type = t, isEnabled = true, isDefault = true, animatorController = null };
    }

    static AnimationClip MakeMorphClip(string rendererPath, string morphName, float val)
    {
        var clip = new AnimationClip();
        clip.name = morphName;
        var binding = new EditorCurveBinding { type = typeof(SkinnedMeshRenderer), path = rendererPath, propertyName = "blendShape." + morphName };
        var curve = new AnimationCurve();
        curve.AddKey(0f, val);
        curve.AddKey(0.1f, val);
        AnimationUtility.SetEditorCurve(clip, binding, curve);
        return clip;
    }

    static AnimatorState MakeTreeState(AnimatorController controller, AnimatorStateMachine sm, string name, string param, Motion[] motions, float[] thresholds)
    {
        var state = sm.AddState(name, new Vector3(30, 0, 0));
        controller.CreateBlendTreeInController(name + "_BT", out var tree);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = param;
        tree.useAutomaticThresholds = false;
        for (int i = 0; i < motions.Length; i++) tree.AddChild(motions[i], thresholds[i]);
        state.motion = tree;
        return state;
    }
}
