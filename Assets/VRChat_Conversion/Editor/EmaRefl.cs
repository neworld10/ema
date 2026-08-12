using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class EmaRefl
{
    static void L(string m) => Debug.Log("EMAREFL " + m);

    public static void Run()
    {
        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/VRChat_Conversion/Textures" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) continue;
            L("TEX " + p + " streaming=" + ti.streamingMipmaps + " filter=" + ti.mipmapFilter + " size=" + ti.maxTextureSize);
        }
        EmaCheckFinal.Run();
        EditorApplication.Exit(0);
    }
}
