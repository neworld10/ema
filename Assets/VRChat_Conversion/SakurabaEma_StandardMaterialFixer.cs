#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Repairs the FBX materials when Unity imported them as the built-in Standard
/// shader. Select the avatar root in the Hierarchy, then run:
/// Tools > Sakuraba Ema > Repair Standard Materials
/// </summary>
public static class SakurabaEmaStandardMaterialFixer
{
    [MenuItem("Tools/Sakuraba Ema/Repair Standard Materials")]
    private static void RepairSelectedAvatar()
    {
        var root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Sakuraba Ema", "Hierarchy에서 아바타 루트를 먼저 선택하세요.", "확인");
            return;
        }

        var standard = Shader.Find("Standard");
        if (standard == null)
        {
            EditorUtility.DisplayDialog("Sakuraba Ema", "Unity Built-in Standard 셰이더를 찾을 수 없습니다.", "확인");
            return;
        }

        var repaired = 0;
        foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (var index = 0; index < materials.Length; index++)
            {
                var material = materials[index];
                if (material == null)
                    continue;

                Undo.RecordObject(material, "Repair Sakuraba Ema material");
                material.shader = standard;
                material.SetColor("_Color", Color.white);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Glossiness", 0.25f);

                var texture = FindMainTexture(material.name);
                if (texture != null)
                    material.SetTexture("_MainTex", texture);

                if (IsTransparentOverlay(material.name))
                    SetTransparent(material);
                else if (material.name.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0)
                    SetCutout(material);
                else
                    SetOpaque(material);

                EditorUtility.SetDirty(material);
                repaired++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Sakuraba Ema", $"머티리얼 {repaired}개를 복구했습니다.", "확인");
    }

    private static Texture2D FindMainTexture(string materialName)
    {
        var fileName = "T_SakurabaEma_Body.png";
        if (materialName.IndexOf("Clothes01", StringComparison.OrdinalIgnoreCase) >= 0)
            fileName = "T_SakurabaEma_Clothes01.png";
        else if (materialName.IndexOf("Clothes02", StringComparison.OrdinalIgnoreCase) >= 0)
            fileName = "T_SakurabaEma_Clothes02.png";
        else if (materialName.IndexOf("Face_Aozame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 materialName.IndexOf("Face_Kurozame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 materialName.IndexOf("Face_Tere", StringComparison.OrdinalIgnoreCase) >= 0)
            fileName = "T_SakurabaEma_Face_Ex.png";
        else if (materialName.IndexOf("Face", StringComparison.OrdinalIgnoreCase) >= 0)
            fileName = "T_SakurabaEma_Face.png";
        else if (materialName.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0)
            fileName = "T_SakurabaEma_Hair.png";
        else if (materialName.IndexOf("Shoes", StringComparison.OrdinalIgnoreCase) >= 0)
            fileName = "T_SakurabaEma_Shoes.png";

        var guids = AssetDatabase.FindAssets(PathWithoutExtension(fileName) + " t:Texture2D");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return null;
    }

    private static string PathWithoutExtension(string fileName)
    {
        var dot = fileName.LastIndexOf('.');
        return dot > 0 ? fileName.Substring(0, dot) : fileName;
    }

    private static bool IsTransparentOverlay(string materialName)
    {
        return materialName.IndexOf("Stencil", StringComparison.OrdinalIgnoreCase) >= 0 ||
               materialName.IndexOf("Aozame", StringComparison.OrdinalIgnoreCase) >= 0 ||
               materialName.IndexOf("Kurozame", StringComparison.OrdinalIgnoreCase) >= 0 ||
               materialName.IndexOf("Tere", StringComparison.OrdinalIgnoreCase) >= 0 ||
               materialName.IndexOf("NoneEdge", StringComparison.OrdinalIgnoreCase) >= 0 ||
               materialName.IndexOf("Eyelid", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void SetOpaque(Material material)
    {
        material.SetFloat("_Mode", 0f);
        material.SetOverrideTag("RenderType", "Opaque");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = -1;
    }

    private static void SetCutout(Material material)
    {
        material.SetFloat("_Mode", 1f);
        material.SetFloat("_Cutoff", 0.5f);
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.EnableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 2450;
    }

    private static void SetTransparent(Material material)
    {
        material.SetFloat("_Mode", 2f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
}
#endif
