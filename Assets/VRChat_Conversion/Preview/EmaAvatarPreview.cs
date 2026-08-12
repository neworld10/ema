using System.Collections;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;

public class EmaAvatarPreview : MonoBehaviour
{
    public float expressionInterval = 1.6f;
    public float eyeInterval = 1.6f;
    public bool cycleExpressions = true;
    public bool cycleEyes = true;
    public bool diagnoseAnimatorChain = true;
    public bool lockRootY = true;
    public bool followHead = true;
    public bool disablePhysbonesOnStart = false;
    public bool pinTorsoBones = true;
    public bool hideFaceOverlays = true;
    public string[] faceOverlayHideList = { "Kurozame" };
    public bool postAlphaEye = true;
    public bool previewSway = true;
    public float swayRadius = 0.03f;
    public float swayYawDeg = 8f;

    static readonly string[] ExMorphs = { "Ex_怒る", "Ex_悲しい", "Ex_照れ", "Ex_びっくり", "Ex_びっくり+", "Ex_絶望1", "Ex_絶望1+", "Ex_絶望2", "Ex_絶望2+", "Ex_笑顔", "Ex_困る", "Ex_困る2" };
    static readonly string[] EyMorphs = { "Ey_怒る", "Ey_悲しい", "Ey_照れ", "Ey_絶望1", "Ey_笑顔", "Ey_困る", "Ey_困る2" };
    readonly string[] eyeDirs = { "Straight", "Up", "Down", "Left", "Right" };

    VRCAvatarDescriptor descriptor;
    Animator animator;
    SkinnedMeshRenderer face;
    Transform headBone;
    Transform hipsBone;
    int[] exIndices;
    int[] eyIndices;
    Transform leftEye;
    Transform rightEye;
    VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations straight, up, down, left, right;

    Camera cam;
    Vector3 rootStartY;
    Quaternion rootStartRot;
    Vector3 camPos;
    float headLogTimer;
    Transform armatureChild;
    Transform spineBone, chestBone, neckBone;
    (Transform t, Vector3 p, Quaternion r, Vector3 s)[] pinned;
    float boneTimer;
    bool didBoneLog;

    int phase;
    int exIndex;
    int eyIndex;
    int eyeDirIndex;
    float timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        var desc = FindObjectOfType<VRCAvatarDescriptor>();
        if (desc == null) { Debug.Log("EVAPREVIEW no avatar in this scene, skipping"); return; }
        if (desc.GetComponent<EmaAvatarPreview>() == null)
            desc.gameObject.AddComponent<EmaAvatarPreview>();
        Debug.Log("EVAPREVIEW auto-attached to " + desc.gameObject.name);
    }

    void Start()
    {
        try
        {
            descriptor = GetComponent<VRCAvatarDescriptor>() ?? GetComponentInParent<VRCAvatarDescriptor>() ?? GetComponentInChildren<VRCAvatarDescriptor>();
            animator = GetComponentInChildren<Animator>();
            if (descriptor == null) { Debug.LogWarning("EVAPREVIEW no VRCAvatarDescriptor"); return; }

            var s = descriptor.customEyeLookSettings;
            leftEye = s.leftEye;
            rightEye = s.rightEye;
            straight = s.eyesLookingStraight;
            up = s.eyesLookingUp;
            down = s.eyesLookingDown;
            left = s.eyesLookingLeft;
            right = s.eyesLookingRight;

            if (animator != null) headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (animator != null) hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);

            face = null;
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (smr.name == "SakurabaEma_Face") { face = smr; break; }
            if (face == null) Debug.LogWarning("EVAPREVIEW no SakurabaEma_Face mesh found");
            else DiagMorphs();
            if (hideFaceOverlays) HideFaceOverlays();
            if (postAlphaEye) ApplyPostAlphaEye();

            Debug.Log("EVAPREVIEW root=" + name + " pos=" + transform.position.ToString("F3") + " lossyScale=" + transform.lossyScale.ToString("F3"));
            Debug.Log("EVAPREVIEW armature=" + animator.name + " rootPos=" + animator.rootPosition.ToString("F3") + " lossyScale=" + animator.transform.lossyScale.ToString("F3"));
            Debug.Log("EVAPREVIEW headY=" + (headBone != null ? headBone.position.y.ToString("F3") : "?") + " hipsY=" + (hipsBone != null ? hipsBone.position.y.ToString("F3") : "?") + " humanScale=" + (animator != null ? animator.humanScale.ToString("F3") : "?"));
            if (face != null)
                Debug.Log("EVAPREVIEW faceBounds center=" + face.bounds.center.ToString("F3") + " size=" + face.bounds.size.ToString("F3"));
            if (face != null && animator != null)
                Debug.Log("EVAPREVIEW facePathFromAnimator=" + PathFrom(animator.transform, face.transform));
            if (animator != null)
                Debug.Log("EVAPREVIEW runtimeController=" + (animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL"));

            DiagPhysbones();

            if (disablePhysbonesOnStart)
            {
                foreach (var pb in GetComponentsInChildren<VRCPhysBone>(true)) { pb.enabled = false; Debug.Log("EVAPREVIEW physbone disabled: " + pb.name); }
            }

            foreach (var rb in GetComponentsInChildren<Rigidbody>(true))
            {
                Debug.Log("EVAPREVIEW found Rigidbody on " + rb.name + " useGravity=" + rb.useGravity);
                rb.useGravity = false;
                rb.isKinematic = true;
            }
            foreach (var cc in GetComponentsInChildren<CharacterController>(true))
            {
                Debug.Log("EVAPREVIEW found CharacterController on " + cc.name);
                cc.enabled = false;
            }

            BindingBones();

            rootStartY = transform.position;
            rootStartRot = transform.rotation;
            animator.SetInteger("FaceEmote", 0);
            animator.SetInteger("FaceEmote2", 0);
            if (leftEye != null) ApplyEyes(straight);
            SetupCameras();
            if (diagnoseAnimatorChain) StartCoroutine(AnimatorChainTest());
            LogHelp();
        }
        catch (System.Exception e)
        {
            Debug.LogError("EVAPREVIEW Start exception: " + e);
        }
    }

    void DiagMorphs()
    {
        var names = new string[face.sharedMesh.blendShapeCount];
        for (int i = 0; i < names.Length; i++) names[i] = face.sharedMesh.GetBlendShapeName(i);
        exIndices = new int[ExMorphs.Length];
        eyIndices = new int[EyMorphs.Length];
        for (int i = 0; i < ExMorphs.Length; i++)
        {
            exIndices[i] = System.Array.IndexOf(names, ExMorphs[i]);
            Debug.Log("EVAPREVIEW Ex " + ExMorphs[i] + " -> blendIdx=" + exIndices[i]);
        }
        for (int i = 0; i < EyMorphs.Length; i++)
        {
            eyIndices[i] = System.Array.IndexOf(names, EyMorphs[i]);
            Debug.Log("EVAPREVIEW Ey " + EyMorphs[i] + " -> blendIdx=" + eyIndices[i]);
        }
        Debug.Log("EVAPREVIEW animatorLayers=" + animator.layerCount);
        for (int i = 0; i < animator.layerCount; i++)
            Debug.Log("EVAPREVIEW layer[" + i + "]=" + animator.GetLayerName(i) + " weight=" + animator.GetLayerWeight(i));
    }

    IEnumerator AnimatorChainTest()
    {
        yield return null;
        if (exIndices != null && exIndices.Length > 0 && exIndices[0] >= 0)
        {
            animator.SetInteger("FaceEmote", 1);
            yield return new WaitForSeconds(0.6f);
            float w = face.GetBlendShapeWeight(exIndices[0]);
            Debug.Log("EVAPREVIEW CHAINTEST FaceEmote=1 -> Ex_怒る weight=" + w + " (100 = FX chain OK)");
            animator.SetInteger("FaceEmote", 0);
        }
        yield return new WaitForSeconds(0.4f);
        if (exIndices.Length > 1 && exIndices[1] >= 0)
        {
            animator.SetInteger("FaceEmote2", 1);
            yield return new WaitForSeconds(0.6f);
            float w = face.GetBlendShapeWeight(exIndices[1]);
            Debug.Log("EVAPREVIEW CHAINTEST FaceEmote2=1 -> Ex_悲しい weight=" + w + " (100 = deep chain OK)");
            animator.SetInteger("FaceEmote2", 0);
        }
    }

    void DiagPhysbones()
    {
        foreach (var pb in GetComponentsInChildren<VRCPhysBone>(true))
        {
            string rootN = pb.rootTransform != null ? pb.rootTransform.name : "self";
            bool headUnder = headBone != null && pb.rootTransform != null &&
                             HeadIsDescendantOf(headBone, pb.rootTransform);
            bool neckUnder = false;
            if (pb.rootTransform != null)
            {
                foreach (var b in new[] { HumanBodyBones.Neck, HumanBodyBones.Chest, HumanBodyBones.Spine })
                {
                    var bt = animator != null ? animator.GetBoneTransform(b) : null;
                    if (bt != null && HeadIsDescendantOf(bt, pb.rootTransform)) neckUnder = true;
                }
            }
            Debug.Log("EVAPREVIEW physbone=" + pb.name + " root=" + rootN +
                      " bones=" + pb.GetComponentsInChildren<Transform>(true).Length +
                      " grav=" + pb.gravity + " pull=" + pb.pull + " spring=" + pb.spring +
                      " stiff=" + pb.stiffness + " colliders=" + pb.colliders.Count +
                      " headUnderThis=" + headUnder + " spineUnderThis=" + neckUnder);
        }
    }

    static bool HeadIsDescendantOf(Transform bone, Transform possibleRoot)
    {
        Transform t = bone;
        while (t != null && t.parent != null)
        {
            t = t.parent;
            if (t == possibleRoot) return true;
        }
        return false;
    }

    void BindingBones()
    {
        spineBone = animator.GetBoneTransform(HumanBodyBones.Spine);
        chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
        neckBone = animator.GetBoneTransform(HumanBodyBones.Neck);
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        foreach (Transform c in transform)
        {
            if (FindDeep(c, "Hips") != null) { armatureChild = c; break; }
        }
        var list = new System.Collections.Generic.List<(Transform, Vector3, Quaternion, Vector3)>();
        foreach (var b in new[] { hips, spineBone, chestBone, neckBone, headBone, leftEye, rightEye })
            if (b != null) list.Add((b, b.localPosition, b.localRotation, b.localScale));
        if (armatureChild != null) list.Add((armatureChild, armatureChild.localPosition, armatureChild.localRotation, armatureChild.localScale));
        pinned = list.ToArray();
        Debug.Log("EVAPREVIEW pinned " + pinned.Length + " bones. armatureChild=" + (armatureChild != null ? armatureChild.name : "none"));
    }

    static bool FindDeep(Transform t, string name)
    {
        if (t.name == name) return true;
        foreach (Transform c in t) if (FindDeep(c, name)) return true;
        return false;
    }

    void PinBones()
    {
        if (pinned == null) return;
        foreach (var pk in pinned)
        {
            if (pk.t == null) continue;
            pk.t.localPosition = pk.p;
            pk.t.localRotation = pk.r;
            pk.t.localScale = pk.s;
        }
    }

    void HideFaceOverlays()
    {
        if (face == null) return;
        var mats = face.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            string n = mats[i].name;
            bool hide = false;
            foreach (var kw in faceOverlayHideList)
            {
                if (n.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0) { hide = true; break; }
            }
            if (hide)
            {
                var c = mats[i].color;
                c.a = 0f;
                mats[i].color = c;
                Debug.Log("EVAPREVIEW face overlay hidden: " + n);
            }
        }
    }

    void ApplyPostAlphaEye()
    {
        if (face == null) return;
        foreach (var m in face.sharedMaterials)
        {
            if (m == null) continue;
            if (m.name.IndexOf("Eyelid", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            m.renderQueue = 3001;
            m.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            Debug.Log("EVAPREVIEW PostAlphaEye applied to " + m.name + " (queue=3001, ZTest=Always)");
        }
    }

    void Update()
    {
        if (descriptor == null) return;
        timer += Time.deltaTime;
        float interval = (phase == 2) ? eyeInterval : expressionInterval;
        if (timer >= interval)
        {
            timer = 0f;
            if (phase == 0)
            {
                if (!cycleExpressions) { phase = 1; return; }
                exIndex++;
                if (exIndex >= ExMorphs.Length + 1) { exIndex = 0; phase = 1; if (face != null) ApplyEx(0); return; }
                if (face != null) ApplyEx(exIndex);
                Debug.Log("EVAPREVIEW Ex[" + exIndex + "] = " + (exIndex == 0 ? "Neutral" : ExMorphs[exIndex - 1]));
            }
            else if (phase == 1)
            {
                if (!cycleExpressions) { phase = 2; return; }
                eyIndex++;
                if (eyIndex >= EyMorphs.Length + 1) { eyIndex = 0; phase = 2; if (face != null) ApplyEy(0); return; }
                if (face != null) ApplyEy(eyIndex);
                Debug.Log("EVAPREVIEW Ey[" + eyIndex + "] = " + (eyIndex == 0 ? "Neutral" : EyMorphs[eyIndex - 1]));
            }
            else
            {
                if (!cycleEyes) { phase = 0; return; }
                eyeDirIndex++;
                if (eyeDirIndex >= eyeDirs.Length) { eyeDirIndex = 0; phase = 0; return; }
                ApplyDir(eyeDirIndex);
                Debug.Log("EVAPREVIEW Eye = " + eyeDirs[eyeDirIndex]);
            }
        }

        boneTimer += Time.deltaTime;
        if (!didBoneLog && boneTimer >= 0.25f && Time.time >= 0f)
        {
            boneTimer = 0f;
            float t = Time.time;
            string arm = armatureChild != null ? "arm=" + armatureChild.position.y.ToString("F3") : "arm=?";
            string hips = spineBone != null ? "spine=" + spineBone.position.y.ToString("F3") : "spine=?";
            string che = chestBone != null ? "chest=" + chestBone.position.y.ToString("F3") : "chest=?";
            string nek = neckBone != null ? "neck=" + neckBone.position.y.ToString("F3") : "neck=?";
            string hd = headBone != null ? "head=" + headBone.position.y.ToString("F3") : "head=?";
            Debug.Log("EVAPREVIEW TL t=" + t.ToString("F2") + " " + arm + " " + hips + " " + che + " " + nek + " " + hd + " root=" + transform.position.y.ToString("F3"));
            if (t >= 2.5f) didBoneLog = true;
        }

        if (headBone != null)
        {
            headLogTimer += Time.deltaTime;
            if (headLogTimer >= 1f)
            {
                headLogTimer = 0f;
                Debug.Log("EVAPREVIEW headY=" + headBone.position.y.ToString("F3") + " rootY=" + transform.position.y.ToString("F3"));
            }
        }
    }

    void LateUpdate()
    {
        if (descriptor == null) return;
        if (lockRootY)
        {
            Vector3 target = rootStartY;
            Quaternion rot = rootStartRot;
            if (previewSway)
            {
                float t = Time.time;
                target += rot * new Vector3(Mathf.Sin(t * 0.9f), 0f, Mathf.Sin(t * 0.53f + 1.7f)) * swayRadius;
                target.y += Mathf.Sin(t * 1.6f) * swayRadius * 0.25f;
                rot = rot * Quaternion.Euler(0f, Mathf.Sin(t * 0.4f) * swayYawDeg, 0f);
            }
            transform.position = target;
            transform.rotation = rot;
        }
        if (pinTorsoBones) PinBones();
        if (followHead && cam != null && headBone != null)
        {
            float modelHeight = Mathf.Max(0.05f, headBone.position.y - transform.position.y);
            Vector3 lookTarget = headBone.position;
            Vector3 desired = CameraPos(lookTarget, modelHeight);
            camPos = Vector3.Lerp(camPos, desired, 0.08f);
            cam.transform.position = camPos;
            cam.transform.rotation = Quaternion.LookRotation(lookTarget - camPos);
        }
    }

    static string PathFrom(Transform root, Transform t)
    {
        var parts = new System.Collections.Generic.List<string>();
        parts.Add(t.name);
        while (t.parent != null && t.parent != root)
        {
            t = t.parent;
            parts.Add(t.name);
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    void ApplyEx(int i)
    {
        foreach (var idx in exIndices) if (idx >= 0) face.SetBlendShapeWeight(idx, 0f);
        if (i > 0 && exIndices[i - 1] >= 0) face.SetBlendShapeWeight(exIndices[i - 1], 100f);
    }

    void ApplyEy(int i)
    {
        foreach (var idx in eyIndices) if (idx >= 0) face.SetBlendShapeWeight(idx, 0f);
        if (i > 0 && eyIndices[i - 1] >= 0) face.SetBlendShapeWeight(eyIndices[i - 1], 100f);
    }

    void ApplyDir(int i)
    {
        switch (i)
        {
            case 0: ApplyEyes(straight); break;
            case 1: ApplyEyes(up); break;
            case 2: ApplyEyes(down); break;
            case 3: ApplyEyes(left); break;
            case 4: ApplyEyes(right); break;
        }
    }

    void ApplyEyes(VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations r)
    {
        if (r.linked)
        {
            if (leftEye != null) leftEye.localRotation = r.left;
            if (rightEye != null) rightEye.localRotation = r.left;
        }
        else
        {
            if (leftEye != null) leftEye.localRotation = r.left;
            if (rightEye != null) rightEye.localRotation = r.right;
        }
    }

    void SetupCameras()
    {
        var lookTarget = headBone != null ? headBone.position : transform.position + Vector3.up * 1.1f;
        cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
        }
        cam.tag = "MainCamera";
        cam.name = "Main Camera";
        float modelHeight = Mathf.Max(0.05f, lookTarget.y - transform.position.y);
        Vector3 camPos = CameraPos(lookTarget, modelHeight);
        cam.transform.position = camPos;
        cam.transform.rotation = Quaternion.LookRotation(lookTarget - camPos);
        cam.fieldOfView = 45f;
        Debug.Log("EVAPREVIEW camera initial: pos=" + camPos.ToString("F3") + " look=" + lookTarget.ToString("F3") + " modelHeight=" + modelHeight.ToString("F3"));
    }

    Vector3 CameraPos(Vector3 lookTarget, float modelHeight)
    {
        Vector3 facing = headBone != null ? headBone.forward : transform.forward;
        Vector3 horiz = Vector3.ProjectOnPlane(facing, Vector3.up);
        if (horiz.sqrMagnitude < 0.0001f) horiz = Vector3.forward;
        horiz.Normalize();
        return lookTarget + (horiz * 0.97f + Vector3.up * 0.22f).normalized * (modelHeight * 0.9f);
    }

    void LogHelp()
    {
        Debug.Log("EVAPREVIEW started. Loop: Ex(0-12) -> Ey(0-7) -> Eye dirs. Game camera follows head; Scene view orbit freely (F = frame).");
    }
}