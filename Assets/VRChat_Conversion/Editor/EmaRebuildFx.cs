using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using VRC.SDK3.Avatars.Components;

public static class EmaRebuildFx
{
    const string FOLDER = "Assets/VRChat_Conversion/Animation";
    const string CLIPS = FOLDER + "/Clips";
    const string FX = FOLDER + "/EmaFX.controller";
    const string SCENE = "Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity";

    static readonly string[] ExNames = { "Ex_怒る", "Ex_悲しい", "Ex_照れ", "Ex_びっくり", "Ex_びっくり+", "Ex_絶望1", "Ex_絶望1+", "Ex_絶望2", "Ex_絶望2+", "Ex_笑顔", "Ex_困る", "Ex_困る2" };
    static readonly string[] EyNames = { "Ey_怒る", "Ey_悲しい", "Ey_照れ", "Ey_絶望1", "Ey_笑顔", "Ey_困る", "Ey_困る2" };

    [MenuItem("Ema/Rebuild FX Controller")]
    public static void Run()
    {
        AssetDatabase.DeleteAsset(FX);
        AssetDatabase.Refresh();
        var controller = AnimatorController.CreateAnimatorControllerAtPath(FX);
        controller.AddParameter("FaceEmote", AnimatorControllerParameterType.Int);
        controller.AddParameter("FaceEmote2", AnimatorControllerParameterType.Int);

        var neutral = LoadClip("Neutral");
        var exClips = new List<AnimationClip>();
        var exThresh = new List<float> { 0f };
        foreach (var n in ExNames)
        {
            exClips.Add(LoadClip(n));
            exThresh.Add(exClips.Count);
        }
        exClips.Insert(0, neutral);
        var eyClips = new List<AnimationClip>();
        var eyThresh = new List<float> { 0f };
        foreach (var n in EyNames)
        {
            eyClips.Add(LoadClip(n));
            eyThresh.Add(eyClips.Count);
        }
        eyClips.Insert(0, neutral);

        var layer0 = controller.layers[0];
        controller.CreateBlendTreeInController("Ex_BT", out var exTree);
        ConfigureTree(exTree, "FaceEmote", exClips.ToArray(), exThresh.ToArray());
        var exState = layer0.stateMachine.AddState("Ex");
        exState.motion = exTree;
        layer0.stateMachine.defaultState = exState;
        RemoveState(layer0.stateMachine, "Ex_BT");

        var layer1 = new AnimatorControllerLayer { name = "Ey", defaultWeight = 1f, stateMachine = new AnimatorStateMachine() };
        layer1.stateMachine.name = "Ey";
        controller.AddLayer(layer1);
        var sm1 = controller.layers[1].stateMachine;
        AssetDatabase.AddObjectToAsset(sm1, controller);

        controller.CreateBlendTreeInController("Ey_BT", out var eyTree);
        ConfigureTree(eyTree, "FaceEmote2", eyClips.ToArray(), eyThresh.ToArray());
        var eyState = sm1.AddState("Ey");
        eyState.motion = eyTree;
        sm1.defaultState = eyState;
        RemoveState(layer0.stateMachine, "Ey_BT");

        var arr = controller.layers;
        arr[0].name = "Ex";
        arr[0].defaultWeight = 1f;
        arr[1].name = "Ey";
        arr[1].defaultWeight = 1f;
        controller.layers = arr;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("EMAREB controller rebuilt: params=" + controller.parameters.Length + " layers=" + controller.layers.Length);

        EditorSceneManager.OpenScene(SCENE);
        var d = Object.FindObjectOfType<VRCAvatarDescriptor>();
        if (d == null) { Debug.LogError("EMAREB no descriptor in scene"); return; }
        d.GetComponent<Animator>().runtimeAnimatorController = controller;

        var baseLayers = d.baseAnimationLayers;
        for (int i = 0; i < baseLayers.Length; i++)
        {
            if (baseLayers[i].type == VRCAvatarDescriptor.AnimLayerType.FX)
                baseLayers[i] = new VRCAvatarDescriptor.CustomAnimLayer
                {
                    type = VRCAvatarDescriptor.AnimLayerType.FX,
                    isEnabled = true,
                    isDefault = false,
                    animatorController = controller
                };
        }
        d.baseAnimationLayers = baseLayers;
        EditorSceneManager.MarkSceneDirty(d.gameObject.scene);
        EditorSceneManager.SaveScene(d.gameObject.scene);
        Debug.Log("EMAREB attached to avatar " + d.gameObject.name);
    }

    static void ConfigureTree(BlendTree tree, string param, Motion[] motions, float[] thresholds)
    {
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = param;
        tree.useAutomaticThresholds = false;
        for (int i = 0; i < motions.Length; i++) tree.AddChild(motions[i], thresholds[i]);
    }

    static void RemoveState(AnimatorStateMachine sm, string name)
    {
        foreach (var cs in sm.states)
        {
            if (cs.state != null && cs.state.name == name)
            {
                sm.RemoveState(cs.state);
                Debug.Log("EMAREB removed auto state " + name);
                return;
            }
        }
    }

    static AnimationClip LoadClip(string name)
    {
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(CLIPS + "/" + name + ".anim");
    }
}