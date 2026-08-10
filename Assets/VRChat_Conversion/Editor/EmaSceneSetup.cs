#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a VRChat avatar scene: instantiates the SakurabaEma FBX and configures
/// the VRC Avatar Descriptor (Viseme blend shapes, Eye Look with blink) via
/// reflection so it does not depend on the compiled VRC SDK assembly directly.
/// </summary>
public static class EmaSceneSetup
{
    private const string FbxPath = "Assets/VRChat_Conversion/SakurabaEma_ByPOWER_VRChat.fbx";
    private const string SceneDir = "Assets/VRChat_Conversion/Scenes";
    private const string ScenePath = "Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity";

    [InitializeOnLoadMethod]
    private static void AutoCreate()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenePath) && File.Exists(FbxPath))
                CreateScene();
        };
    }

    [MenuItem("Tools/Sakuraba Ema/Create Avatar Scene")]
    public static void CreateScene()
    {
        var sb = new List<string>();
        sb.Add("Scene: " + ScenePath);

        if (!Directory.Exists(SceneDir))
            Directory.CreateDirectory(SceneDir);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "SakurabaEma_Avatar";

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null)
        {
            sb.Add("FAIL FBX prefab not found");
            WriteReport(sb);
            return;
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = Vector3.zero;
        sb.Add("avatar instantiated: " + instance.name);

        RepairMaterials(instance, sb);

        var type = Type.GetType("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor, VRCSDK3A");
        if (type == null)
        {
            sb.Add("FAIL VRCAvatarDescriptor type not found");
            WriteReport(sb);
            return;
        }
        var desc = instance.AddComponent(type);
        sb.Add("VRCAvatarDescriptor added: " + type.FullName);
        DumpFields(type, "descriptor", sb);

        Configure(desc, instance, sb);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        WriteReport(sb);
        Debug.Log("EMASCENE DONE");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Sakuraba Ema", "아바타 씬 생성 완료: " + ScenePath, "확인");
    }

    private static void WriteReport(List<string> sb)
    {
        var report = "SakurabaEma avatar scene setup\n===============================\n" + string.Join("\n", sb.ToArray());
        File.WriteAllText("Assets/VRChat_Conversion/SCENE_SETUP_REPORT.txt", report);
        Debug.Log(report);
    }

    private static void DumpFields(Type type, string label, List<string> sb)
    {
        sb.Add("-- fields of " + label + " (" + type.FullName + ")");
        foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var isArray = f.FieldType.IsArray;
            var elem = isArray ? "[" + f.FieldType.GetElementType().Name + "]" : "";
            sb.Add("   " + f.Name + " : " + f.FieldType.Name + elem);
        }
    }

    private static void Configure(Component desc, GameObject avatar, List<string> sb)
    {
        var type = desc.GetType();

        var lipSyncField = GetField(type, "lipSync");
        if (lipSyncField != null)
        {
            var styleType = lipSyncField.FieldType;
            lipSyncField.SetValue(desc, Enum.ToObject(styleType, 3));
            sb.Add("lipSync = " + Enum.ToObject(styleType, 3) + " (VisemeBlendShape)");
        }

        var visemeField = GetField(type, "VisemeBlendShapes");
        if (visemeField != null)
        {
            var val = visemeField.GetValue(desc);
            if (visemeField.FieldType.IsArray && visemeField.FieldType.GetElementType() == typeof(string))
            {
                var arr = (string[])val;
                if (arr == null || arr.Length != 6)
                {
                    arr = new string[6];
                    visemeField.SetValue(desc, arr);
                }
                string[] order = { "aa", "oh", "ch", "ih", "ou", "e" };
                for (var i = 0; i < order.Length; i++)
                    arr[i] = "vrc.v_" + order[i];
                sb.Add("visemes array: " + string.Join(",", arr));
            }
            else if (val != null || HasDefaultCtor(visemeField.FieldType))
            {
                if (val == null)
                {
                    val = Activator.CreateInstance(visemeField.FieldType);
                    visemeField.SetValue(desc, val);
                }
                var map = new Dictionary<string, string>
                {
                    { "aa", "vrc.v_aa" }, { "ih", "vrc.v_ih" }, { "ou", "vrc.v_ou" },
                    { "e", "vrc.v_e" }, { "oh", "vrc.v_oh" },
                };
                foreach (var kv in map)
                {
                    var f = GetField(visemeField.FieldType, kv.Key);
                    if (f != null)
                    {
                        f.SetValue(val, kv.Value);
                        sb.Add("viseme " + kv.Key + " = " + kv.Value);
                    }
                }
            }
            else
            {
                sb.Add("WARN VisemeBlendShapes unsupported type " + visemeField.FieldType.FullName);
            }
        }

        var eyeField = GetField(type, "customEyeLookSettings");
        if (eyeField != null)
        {
            var eye = eyeField.GetValue(desc);
            if (eye == null && HasDefaultCtor(eyeField.FieldType))
            {
                eye = Activator.CreateInstance(eyeField.FieldType);
                eyeField.SetValue(desc, eye);
            }
            if (eye != null)
            {
                DumpFields(eye.GetType(), "customEyeLookSettings", sb);
                var t = eye.GetType();
                var left = FindTransform(avatar, "LeftEye");
                var right = FindTransform(avatar, "RightEye");
                if (left != null) SetField(t, eye, "leftEye", left, sb);
                if (right != null) SetField(t, eye, "rightEye", right, sb);

                var face = FindFaceRenderer(avatar);
                if (face != null)
                {
                    var mesh = face.sharedMesh;
                    var idxBlink = mesh.GetBlendShapeIndex("Blink");
                    var idxL = mesh.GetBlendShapeIndex("vrc.Blink_L");
                    var idxR = mesh.GetBlendShapeIndex("vrc.Blink_R");
                    if (idxBlink >= 0)
                    {
                        SetField(t, eye, "eyelidsSkinnedMesh", face, sb);
                        var blinkIdx = new[] { idxBlink, idxL >= 0 ? idxL : idxBlink, idxR >= 0 ? idxR : idxBlink };
                        SetField(t, eye, "eyelidsBlendshapes", blinkIdx, sb);
                        var eyelidTypeField = GetField(t, "eyelidType");
                        if (eyelidTypeField != null && eyelidTypeField.FieldType.IsEnum)
                        {
                            eyelidTypeField.SetValue(eye, Enum.ToObject(eyelidTypeField.FieldType, 1));
                            sb.Add("eyelidType = Blendshapes (1)");
                        }
                        sb.Add("blink shape idx: blink=" + idxBlink + " left=" + idxL + " right=" + idxR);
                    }
                    else
                    {
                        sb.Add("WARN Blink shape not found on face mesh");
                    }
                }

                var styleField = GetField(t, "lookStyle");
                if (styleField != null && styleField.FieldType.IsEnum)
                    styleField.SetValue(eye, Enum.ToObject(styleField.FieldType, 0));
                sb.Add("eye look configured (bones)");
            }
            else
            {
                sb.Add("WARN customEyeLookSettings not configurable: " + eyeField.FieldType.FullName);
            }
        }

        var enableEyeField = GetField(type, "enableEyeLook");
        if (enableEyeField != null)
        {
            enableEyeField.SetValue(desc, true);
            sb.Add("enableEyeLook = True");
        }

        var viewField = GetField(type, "ViewPosition");
        if (viewField != null)
        {
            var headTransform = FindTransform(avatar, "Head");
            if (headTransform != null)
            {
                Vector3 headPos = headTransform.localPosition;
                viewField.SetValue(desc, headPos);
            }
        }
    }

    private static bool HasDefaultCtor(Type t)
    {
        return !t.IsArray && t.IsClass && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null;
    }

    private static Transform FindTransform(GameObject root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    private static SkinnedMeshRenderer FindFaceRenderer(GameObject root)
    {
        foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (r.gameObject.name == "SakurabaEma_Face" || (r.sharedMesh != null && r.sharedMesh.name == "SakurabaEma_Face"))
                return r;
        return null;
    }

    private static void RepairMaterials(GameObject avatar, List<string> sb)
    {
        var standard = Shader.Find("Standard");
        if (standard == null)
        {
            sb.Add("WARN Standard shader not found");
            return;
        }
        var repaired = 0;
        foreach (var r in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mats = r.sharedMaterials;
            for (var i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                m.shader = standard;
                m.SetColor("_Color", Color.white);
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Glossiness", 0.25f);
                if (IsOverlay(m.name))
                    SetModeTransparent(m);
                else if (m.name.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0)
                    SetModeCutout(m);
                else
                    SetModeOpaque(m);
                EditorUtility.SetDirty(m);
                repaired++;
            }
        }
        AssetDatabase.SaveAssets();
        sb.Add("materials repaired: " + repaired);
    }

    private static bool IsOverlay(string name)
    {
        return name.IndexOf("Stencil", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Aozame", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Kurozame", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Tere", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("NoneEdge", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Eyelid", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void SetModeOpaque(Material m)
    {
        m.SetFloat("_Mode", 0f);
        m.SetOverrideTag("RenderType", "Opaque");
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = -1;
    }

    private static void SetModeCutout(Material m)
    {
        m.SetFloat("_Mode", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.SetOverrideTag("RenderType", "TransparentCutout");
        m.EnableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 2450;
    }

    private static void SetModeTransparent(Material m)
    {
        m.SetFloat("_Mode", 2f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
    }

    private static void SetField(Type type, object target, string name, object value, List<string> sb)
    {
        var f = GetField(type, name);
        if (f == null)
        {
            sb.Add("WARN field not found: " + name);
            return;
        }
        f.SetValue(target, value);
        sb.Add("set " + name + " = " + (value is Transform vt ? vt.name : value?.ToString()));
    }

    private static FieldInfo GetField(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var t = type;
        while (t != null)
        {
            var f = t.GetField(name, flags);
            if (f != null) return f;
            t = t.BaseType;
        }
        return null;
    }
}
#endif
