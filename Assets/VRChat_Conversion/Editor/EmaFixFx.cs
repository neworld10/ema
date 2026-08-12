using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EmaFixFx
{
    const string FX = "Assets/VRChat_Conversion/Animation/EmaFX.controller";

    [MenuItem("Ema/Fix FX Layer Weights")]
    public static void Fix()
    {
        var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(FX);
        if (c == null) { Debug.LogError("EMAFIXFX controller not found at " + FX); return; }
        var layers = c.layers;
        for (int i = 0; i < layers.Length; i++)
        {
            Debug.Log("EMAFIXFX before layer[" + i + "]=" + layers[i].name + " defaultWeight=" + layers[i].defaultWeight);
            layers[i].defaultWeight = 1f;
        }
        c.layers = layers;
        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        Debug.Log("EMAFIXFX done, layers=" + c.layers.Length);
    }
}