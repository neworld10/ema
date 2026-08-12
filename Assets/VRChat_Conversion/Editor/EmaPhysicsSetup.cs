using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.Dynamics;

public static class EmaPhysicsSetup
{
    const string PREFIX = "PB_Collider_";

    static void L(string m) => Debug.Log("EMAPHY " + m);

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/VRChat_Conversion/Scenes/SakurabaEma_Avatar.unity");
        var avatar = GameObject.Find("SakurabaEma_ByPOWER_VRChat");
        if (avatar == null) { L("AVATAR NOT FOUND"); EditorApplication.Exit(0); return; }
        var arm = avatar.transform.Find("SakurabaEma_VRChat_Armature");
        Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindDeep(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
        Transform B(string p) => FindDeep(arm, p);

        var old = avatar.GetComponentsInChildren<VRCPhysBoneCollider>(true);
        int removed = 0;
        foreach (var c in old)
        {
            if (c.name.StartsWith(PREFIX)) { UnityEngine.Object.DestroyImmediate(c.gameObject); removed++; }
        }
        L("removed old colliders: " + removed);

        var colliders = new Dictionary<string, VRCPhysBoneCollider>();

        void AddCapsule(string bone, string id, Vector3 wA, Vector3 wB, float radius)
        {
            var t = B(bone);
            if (t == null) { L("bone not found: " + bone); return; }
            var center = (wA + wB) * 0.5f;
            var go = new GameObject(PREFIX + id);
            go.transform.SetParent(t, false);
            var c = go.AddComponent<VRCPhysBoneCollider>();
            c.shapeType = VRCPhysBoneColliderBase.ShapeType.Capsule;
            c.radius = radius;
            c.height = (wA - wB).magnitude;
            var axis = (wB - wA).normalized;
            c.rotation = Quaternion.FromToRotation(Vector3.up, Quaternion.Inverse(t.rotation) * axis);
            c.position = t.InverseTransformPoint(center);
            colliders[id] = c;
        }

        void AddSphere(string bone, string id, Vector3 wCenter, float radius)
        {
            var t = B(bone);
            if (t == null) { L("bone not found: " + bone); return; }
            var go = new GameObject(PREFIX + id);
            go.transform.SetParent(t, false);
            var c = go.AddComponent<VRCPhysBoneCollider>();
            c.shapeType = VRCPhysBoneColliderBase.ShapeType.Sphere;
            c.radius = radius;
            c.position = t.InverseTransformPoint(wCenter);
            colliders[id] = c;
        }

        Vector3 Hips = B("Hips").position, Spine = B("Spine").position, Chest = B("Chest").position;
        Vector3 Neck = B("Neck").position, Head = B("Head").position;
        Vector3 headTop = B("Head").Find("HeadTip").position;
        Vector3 LSh = B("LeftShoulder").position, LUa = B("LeftUpperArm").position, LLa = B("LeftLowerArm").position, LHa = B("LeftHand").position;
        Vector3 RSh = B("RightShoulder").position, RUa = B("RightUpperArm").position, RLa = B("RightLowerArm").position, RHa = B("RightHand").position;
        Vector3 LUg = B("LeftUpperLeg").position, LLg = B("LeftLowerLeg").position, LFo = B("LeftFoot").position;
        Vector3 RUg = B("RightUpperLeg").position, RLg = B("RightLowerLeg").position, RFo = B("RightFoot").position;

        // Head (hair)
        AddCapsule("Head", "Head", Head + Vector3.down * 0.09f, headTop, 0.13f);
        // Chest/torso (hair sides + ribbon)
        AddCapsule("Chest", "Chest", Spine - Vector3.up * 0.03f, Neck + Vector3.up * 0.04f, 0.14f);
        // Hips/waist (skirt flaps)
        AddCapsule("Hips", "Hips", Hips + Vector3.down * 0.04f, Hips + Vector3.up * 0.16f, 0.14f);
        // Arms (hair)
        AddCapsule("LeftUpperArm", "LArm", LSh, LLa, 0.07f);
        AddCapsule("LeftUpperArm", "LFore", LLa, LHa, 0.06f);
        AddSphere("LeftHand", "LHand", LHa, 0.055f);
        AddCapsule("RightUpperArm", "RArm", RSh, RLa, 0.07f);
        AddCapsule("RightUpperArm", "RFore", RLa, RHa, 0.06f);
        AddSphere("RightHand", "RHand", RHa, 0.055f);
        // Legs (skirt)
        AddCapsule("LeftUpperLeg", "LLeg", LUg, LLg, 0.10f);
        AddCapsule("LeftUpperLeg", "LShin", LLg, LFo, 0.075f);
        AddSphere("LeftFoot", "LFoot", LFo, 0.05f);
        AddCapsule("RightUpperLeg", "RLeg", RUg, RLg, 0.10f);
        AddCapsule("RightUpperLeg", "RShin", RLg, RFo, 0.075f);
        AddSphere("RightFoot", "RFoot", RFo, 0.05f);

        // assign to physbones
        var physBones = avatar.GetComponentsInChildren<VRCPhysBone>(true);
        foreach (var pb in physBones)
        {
            var rootName = pb.rootTransform != null ? pb.rootTransform.name : pb.transform.name;
            L("physbone: " + rootName + " on=" + pb.transform.name);
        }

        void Assign(VRCPhysBone pb, params string[] ids)
        {
            if (pb.colliders == null) pb.colliders = new List<VRCPhysBoneColliderBase>();
            pb.colliders.Clear();
            foreach (var id in ids)
                if (colliders.ContainsKey(id)) pb.colliders.Add(colliders[id]);
            L("assigned " + pb.transform.name + " colliders=" + pb.colliders.Count);
        }

        foreach (var pb in physBones)
        {
            string r = pb.rootTransform != null ? pb.rootTransform.name : pb.transform.name;
            if (r == "Hair_Root") { Assign(pb, "Head", "Chest", "LArm", "LFore", "LHand", "RArm", "RFore", "RHand"); pb.gravity = 0.1f; pb.gravityFalloff = 1f; }
            else if (r == "Skirt_Root") { Assign(pb, "Hips", "Chest", "LLeg", "LShin", "LFoot", "RLeg", "RShin", "RFoot"); pb.gravity = 0.2f; pb.gravityFalloff = 1f; }
            else if (r == "ChestRb_Root") { Assign(pb, "Chest"); pb.gravity = 0.15f; pb.gravityFalloff = 1f; }
        }

        EditorSceneManager.MarkSceneDirty(avatar.scene);
        EditorSceneManager.SaveScene(avatar.scene);
        L("scene saved, total colliders created=" + colliders.Count);
        EditorApplication.Exit(0);
    }
}
