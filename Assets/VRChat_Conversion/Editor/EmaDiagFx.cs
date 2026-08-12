using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EmaDiagFx
{
    const string FX = "Assets/VRChat_Conversion/Animation/EmaFX.controller";

    [MenuItem("Ema/Diagnose FX")]
    public static void Run()
    {
        var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(FX);
        if (c == null) { Debug.LogError("EMADIAF " + FX + " not found"); return; }
        Debug.Log("EMADIAF controller layers=" + c.layers.Length + " params=" + string.Join(",", System.Array.ConvertAll(c.parameters, p => p.name + ":" + p.type)));
        foreach (var layer in c.layers)
        {
            Debug.Log("EMADIAF layer=" + layer.name + " weight=" + layer.defaultWeight + " defaultState=" + (layer.stateMachine != null && layer.stateMachine.defaultState != null ? layer.stateMachine.defaultState.name : "NONE"));
            foreach (var state in layer.stateMachine.states)
            {
                var tree = state.state.motion as BlendTree;
                if (tree == null) { Debug.Log("EMADIAF   state " + state.state.name + " motion=" + (state.state.motion != null ? state.state.motion.name : "NULL")); continue; }
                Debug.Log("EMADIAF   state " + state.state.name + " treeParam=" + tree.blendParameter + " autoThresh=" + tree.useAutomaticThresholds + " children=" + tree.children.Length);
                for (int i = 0; i < tree.children.Length; i++)
                {
                    var clip = tree.children[i].motion as AnimationClip;
                    if (clip == null) { Debug.Log("EMADIAF     child[" + i + "] thresh=" + tree.children[i].threshold + " motion=NULL"); continue; }
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    string props = bindings.Length == 0 ? "NO-BINDINGS" : string.Join(" | ", System.Array.ConvertAll(bindings, b => b.path + "::" + b.propertyName));
                    Debug.Log("EMADIAF     child[" + i + "] thresh=" + tree.children[i].threshold + " clip=" + clip.name + " len=" + clip.length + " " + props);
                }
            }
        }
    }
}