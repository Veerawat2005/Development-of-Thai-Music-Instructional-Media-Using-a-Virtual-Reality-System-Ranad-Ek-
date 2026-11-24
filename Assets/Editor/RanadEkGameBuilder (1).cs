using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// วางไฟล์นี้แล้วจบ — ไม่ต้องกดเมนูอะไรเลย
/// พอ Unity compile เสร็จ จะ "สร้าง Scene ใหม่แยกต่างหากให้เองอัตโนมัติ"
/// พร้อมประกอบเกม VR Ranad Ek ที่เล่นได้จริงครบวงจร
/// ไม่แตะ Scene เดิมที่เปิดอยู่เลย ปลอดภัย 100%
///
/// **ไฟล์นี้ต้องอยู่ในโฟลเดอร์ชื่อ "Editor" เท่านั้น**
/// เช่น Assets/Editor/RanadEkGameBuilder.cs
///
/// ก่อนวางไฟล์นี้ ต้องมีสิ่งเหล่านี้พร้อมแล้ว (ดูรายละเอียดใน README):
/// - import "Starter Assets" และ "XR Device Simulator" จาก XR Interaction Toolkit samples
/// - ไฟล์เสียง Ranad_01.wav ถึง Ranad_21.wav ที่ Assets/Sounds/Ranad/
/// - RanadKey.cs และ MalletPhysicsFollower.cs อยู่ใน Assets/Scripts แล้ว
///
/// ผลลัพธ์: ไฟล์ Scene ใหม่ที่ Assets/Scenes/RanadEk_AutoBuilt.unity เปิดให้พร้อมเล่นทันที
/// (ถ้าอยากสร้างใหม่อีกรอบ ลบไฟล์ Scene นี้ทิ้งแล้วรัน Tools > VR Ranad Ek > Rebuild)
/// </summary>
[InitializeOnLoad]
public static class RanadEkGameBuilder
{
    private const string GeneratedScenePath = "Assets/Scenes/RanadEk_AutoBuilt.unity";

    static RanadEkGameBuilder()
    {
        // รอให้ Editor ว่างก่อนค่อยทำงาน (ห้ามยุ่งกับ Scene ระหว่าง compile)
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GeneratedScenePath) != null)
        {
            // สร้างไปแล้วก่อนหน้านี้ ไม่สร้างซ้ำ (ป้องกันทับงานที่แก้ไขเพิ่มเติมไปแล้ว)
            return;
        }

        BuildInNewScene();
    }

    [MenuItem("Tools/VR Ranad Ek/Rebuild (Force New Scene)")]
    public static void RebuildManually()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GeneratedScenePath) != null)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "RanadEk Builder",
                $"พบไฟล์ {GeneratedScenePath} อยู่แล้ว ต้องการสร้างทับใหม่หรือไม่?\n" +
                "(งานที่เคยแก้ไขเพิ่มเติมใน Scene นี้จะหายไป)",
                "สร้างทับ", "ยกเลิก");
            if (!confirm) return;
        }
        BuildInNewScene();
    }

    private static void BuildInNewScene()
    {
        if (!System.IO.Directory.Exists("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("VR_RanadEk_Generated");
        EnsureTagExists("Mallet");

        GameObject xrOrigin = InstantiatePrefabByName("XR Origin (XR Rig)");
        GameObject simulator = InstantiatePrefabByName("XR Device Simulator");

        Transform leftController = xrOrigin != null ? FindDeepChild(xrOrigin.transform, "Left Controller") : null;
        Transform rightController = xrOrigin != null ? FindDeepChild(xrOrigin.transform, "Right Controller") : null;

        BuildRanadFrame(root.transform);

        if (leftController != null) BuildMallet(root.transform, leftController, "L");
        if (rightController != null) BuildMallet(root.transform, rightController, "R");

        Selection.activeGameObject = root;

        EditorSceneManager.SaveScene(newScene, GeneratedScenePath);

        string message = $"[RanadEk Builder] สร้างเสร็จแล้วอัตโนมัติ! เปิดไฟล์ {GeneratedScenePath} " +
            "แล้วกด Play ได้เลย (ใช้ XR Device Simulator: WASD เดิน, Ctrl/Shift ค้างสลับควบคุมมือ)\n" +
            (xrOrigin == null ? "⚠ ไม่พบ prefab 'XR Origin (XR Rig)' — ต้อง import Starter Assets ก่อน แล้วรัน Tools > VR Ranad Ek > Rebuild\n" : "") +
            (simulator == null ? "⚠ ไม่พบ prefab 'XR Device Simulator' — ต้อง import sample นี้ก่อน แล้วรัน Rebuild\n" : "");

        Debug.Log(message);
    }

    private static GameObject BuildRanadFrame(Transform parent)
    {
        GameObject frame = new GameObject("RanadFrame_Generated");
        frame.transform.SetParent(parent);
        frame.transform.position = new Vector3(0f, 0.9f, 0.55f);

        int keyCount = 21;
        float longestLength = 0.30f;
        float shortestLength = 0.12f;
        float keyWidth = 0.035f;
        float keyHeight = 0.02f;
        float spacing = 0.045f;
        float totalWidth = (keyCount - 1) * spacing;

        int missingClips = 0;

        for (int i = 0; i < keyCount; i++)
        {
            float t = i / (float)(keyCount - 1);
            float length = Mathf.Lerp(longestLength, shortestLength, t); // ลูกแรก = เสียงต่ำ = ยาว

            GameObject key = GameObject.CreatePrimitive(PrimitiveType.Cube);
            key.name = $"RanadKey_{i + 1:00}";
            key.transform.SetParent(frame.transform);
            key.transform.localPosition = new Vector3(-totalWidth / 2f + i * spacing, 0f, 0f);
            key.transform.localRotation = Quaternion.identity;
            key.transform.localScale = new Vector3(keyWidth, keyHeight, length);

            // ปิด Is Trigger ของ Box Collider ที่ติดมากับ Cube (ให้เป็นของแข็งจริง)
            BoxCollider col = key.GetComponent<BoxCollider>();
            col.isTrigger = false;

            AudioSource audioSource = key.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            RanadKey keyScript = key.AddComponent<RanadKey>();

            // หาไฟล์เสียงจากเลขลูก (รองรับทั้ง ranad_bar01_175Hz และ Ranad_01)
            AudioClip clip = RanadNoteSoundAssigner.FindClip(i + 1);
            if (clip != null)
            {
                keyScript.noteSound = clip;
            }
            else
            {
                missingClips++;
            }
        }

        if (missingClips > 0)
        {
            Debug.LogWarning($"[RanadEk Builder] ไม่พบไฟล์เสียง {missingClips} ลูก " +
                "ตรวจสอบว่ามีไฟล์เสียงชื่อ ranad_bar01_... ถึง ranad_bar21_... (หรือ Ranad_01 ถึง Ranad_21) อยู่ใน Assets " +
                "แล้วลากใส่ Note Sound ของลูกที่ขาดเองภายหลัง");
        }

        return frame;
    }

    private static void BuildMallet(Transform parent, Transform controller, string side)
    {
        // Anchor: จุดอ้างอิงเปล่าๆ ที่ติดมือจริง
        GameObject anchor = new GameObject($"MalletAnchor_{side}");
        anchor.transform.SetParent(controller);
        anchor.transform.localPosition = Vector3.zero;
        anchor.transform.localRotation = Quaternion.identity;

        float stickLength = 0.25f;

        // MalletBody: อิสระ ไม่ผูกกับ Controller โดยตรง ควบคุมด้วย physics
        GameObject body = new GameObject($"MalletBody_{side}");
        body.transform.SetParent(parent);
        body.transform.position = anchor.transform.position;
        body.transform.rotation = anchor.transform.rotation;

        Rigidbody rb = body.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        SphereCollider col = body.AddComponent<SphereCollider>();
        col.isTrigger = false;
        col.radius = 0.025f;
        col.center = new Vector3(0f, 0f, stickLength);

        body.tag = "Mallet";

        MalletPhysicsFollower follower = body.AddComponent<MalletPhysicsFollower>();
        follower.target = anchor.transform;
        string sideLower = side.ToLowerInvariant();
        follower.hand = (sideLower.StartsWith("l") || sideLower.Contains("left")) ? MalletHand.Left :
                        (sideLower.StartsWith("r") || sideLower.Contains("right")) ? MalletHand.Right : MalletHand.Unknown;

        // ภาพไม้ตี: ด้าม (cylinder) + หัว (sphere) - เพื่อแสดงผลเท่านั้น ไม่มี collider ซ้อน
        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = "StickVisual";
        Object.DestroyImmediate(stick.GetComponent<Collider>());
        stick.transform.SetParent(body.transform);
        stick.transform.localPosition = new Vector3(0f, 0f, stickLength / 2f);
        stick.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        stick.transform.localScale = new Vector3(0.01f, stickLength / 2f, 0.01f);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "HeadVisual";
        Object.DestroyImmediate(head.GetComponent<Collider>());
        head.transform.SetParent(body.transform);
        head.transform.localPosition = new Vector3(0f, 0f, stickLength);
        head.transform.localScale = Vector3.one * 0.05f;
    }

    private static GameObject InstantiatePrefabByName(string prefabName)
    {
        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return null;

        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        return null;
    }

    private static void EnsureTagExists(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName) return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }
}