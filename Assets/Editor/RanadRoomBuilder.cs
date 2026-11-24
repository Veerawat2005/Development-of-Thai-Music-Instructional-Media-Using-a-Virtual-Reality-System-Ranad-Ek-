using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// สร้างห้องดนตรีไทยรอบฉากปัจจุบัน ด้วยรูปทรงพื้นฐาน (Cube/Cylinder/Sphere) ล้วนๆ
/// ไม่ต้องใช้โมเดลหรือรูปจากภายนอก: texture ไม้/ปูน/เสื่อ สร้างด้วยโค้ดแล้วเซฟเป็นไฟล์ใน Assets/RanadRoom
///
/// สิ่งที่สร้าง: พื้นไม้ ผนัง+แผงไม้ล่าง เพดาน+คานไม้ หน้าต่าง(มีแสงลอดเข้า) ประตู กรอบภาพ
/// เสื่อใต้เท้า ฐาน+ตัวรางระนาดใต้ลูกระนาด ฆ้องวง กลอง โคมไฟห้อยพร้อมแสงอุ่น
/// ทุกชิ้นถอด Collider ออกหมด เพื่อไม่ให้ไปรบกวนการชนของไม้ตีกับลูกระนาด
///
/// **ไฟล์นี้ต้องอยู่ในโฟลเดอร์ชื่อ "Editor" เท่านั้น** เช่น Assets/Editor/
/// วิธีใช้: เมนู Tools > VR Ranad Ek > Build Ranad Room แล้วกด Ctrl+S
/// รันซ้ำได้ (จะลบ 'RanadRoom_Decor' เดิมแล้วสร้างใหม่) ถ้าจะเอาห้องออก ลบ GameObject ชื่อนี้ทิ้ง
///
/// ผู้เล่นยืนที่ (0,0,0) มองไปทาง +Z ห้องกว้าง 6 x ลึก 6 x สูง 3 เมตร
/// ตำแหน่งฐานระนาดอ้างอิงจากลูกระนาด (RanadKey) ที่มีอยู่ในฉากจริง
/// </summary>
public static class RanadRoomBuilder
{
    private const string RootName = "RanadRoom_Decor";
    private const string AssetFolder = "Assets/RanadRoom";

    // ขอบห้อง (เมตร)
    private const float MinX = -3f, MaxX = 3f, MinZ = -2.5f, MaxZ = 3.5f, Height = 3f;
    private const float FloorTop = 0.01f;

    // สีแสงโดยรอบ (ปรับได้ถ้ามืด/สว่างไป)
    private static readonly Color AmbientColor = new Color(0.55f, 0.47f, 0.40f);

    private class Mats
    {
        public Material floor, wall, wainscot, ceiling, beam, gold, bronze;
        public Material mat, matBorder, lacquer, glass, lantern;
        public Material drumBody, drumHead, frameInner, doorWood;
    }

    [MenuItem("Tools/VR Ranad Ek/Build Ranad Room")]
    public static void Build()
    {
        Directory.CreateDirectory(AssetFolder + "/Materials");
        Directory.CreateDirectory(AssetFolder + "/Textures");
        AssetDatabase.Refresh();

        GameObject old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject rootGO = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(rootGO, "Build Ranad Room");
        Transform root = rootGO.transform;

        Texture2D woodTex = MakeTexture("WoodPlanks", 256, WoodPixel);
        Texture2D plasterTex = MakeTexture("Plaster", 128, PlasterPixel);
        Texture2D weaveTex = MakeTexture("Weave", 128, WeavePixel);

        Mats m = new Mats
        {
            floor = Mat("Floor", Color.white, 0.35f, 0f, woodTex, new Vector2(3f, 3f)),
            wall = Mat("Wall", Color.white, 0.05f, 0f, plasterTex, new Vector2(2f, 1f)),
            wainscot = Mat("Wainscot", new Color(0.62f, 0.45f, 0.34f), 0.3f, 0f, woodTex, new Vector2(3f, 1f)),
            ceiling = Mat("Ceiling", new Color(0.45f, 0.33f, 0.26f), 0.15f, 0f, woodTex, new Vector2(3f, 3f)),
            beam = Mat("Beam", new Color(0.28f, 0.18f, 0.12f), 0.25f),
            gold = Mat("Gold", new Color(0.85f, 0.62f, 0.2f), 0.6f, 0.7f),
            bronze = Mat("Bronze", new Color(0.72f, 0.48f, 0.16f), 0.55f, 0.8f),
            mat = Mat("MatWeave", Color.white, 0.05f, 0f, weaveTex, new Vector2(4f, 3f)),
            matBorder = Mat("MatBorder", new Color(0.55f, 0.1f, 0.1f), 0.1f),
            lacquer = Mat("Lacquer", new Color(0.25f, 0.12f, 0.07f), 0.65f),
            glass = Mat("WindowGlass", new Color(0.7f, 0.85f, 1f), 0.9f, 0f, null, null, new Color(0.9f, 1.1f, 1.4f)),
            lantern = Mat("Lantern", new Color(1f, 0.75f, 0.4f), 0.3f, 0f, null, null, new Color(2f, 1.2f, 0.5f)),
            drumBody = Mat("DrumBody", new Color(0.4f, 0.15f, 0.1f), 0.4f),
            drumHead = Mat("DrumHead", new Color(0.9f, 0.85f, 0.7f), 0.2f),
            frameInner = Mat("FrameInner", new Color(0.8f, 0.7f, 0.5f), 0.1f),
            doorWood = Mat("DoorWood", new Color(0.35f, 0.2f, 0.12f), 0.35f),
        };

        BuildShell(root, m);
        BuildWindowAndDoor(root, m);
        BuildFrames(root, m);
        BuildLanterns(root, m);

        Bounds keyBounds;
        bool foundKeys = TryGetKeyBounds(out keyBounds);
        BuildMat(root, m, keyBounds.center.z);
        BuildRanadStand(root, m, keyBounds);

        BuildKhongWong(root, m, new Vector3(2.0f, 0f, 2.3f));
        BuildDrum(root, m, new Vector3(-2.1f, 0f, 2.2f));

        // แสงโดยรอบ: ในห้องปิดไม่มี directional light จึงตั้งสีแสงรอบทิศให้อบอุ่นแทน
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientColor;

        string floorNote = DisableOldFloor();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = rootGO;

        Debug.Log("[RanadRoom] สร้างห้องเสร็จแล้ว กด Ctrl+S เพื่อเซฟ Scene\n" +
                  (foundKeys ? "" : "⚠ ไม่พบลูกระนาด (RanadKey) ในฉาก จึงวางฐานระนาดตามตำแหน่งมาตรฐาน (0, 0.9, 0.55)\n") +
                  floorNote);
    }

    // ---------- โครงห้อง ----------

    private static void BuildShell(Transform root, Mats m)
    {
        Transform g = Group(root, "Shell");
        float cz = (MinZ + MaxZ) / 2f;   // 0.5
        float w = MaxX - MinX;           // 6
        float d = MaxZ - MinZ;           // 6

        Prim(PrimitiveType.Cube, g, "Floor", new Vector3(0f, FloorTop - 0.05f, cz), new Vector3(w, 0.1f, d), m.floor);
        Prim(PrimitiveType.Cube, g, "Ceiling", new Vector3(0f, Height + 0.05f, cz), new Vector3(w + 0.2f, 0.1f, d + 0.2f), m.ceiling);

        Prim(PrimitiveType.Cube, g, "Wall_Back", new Vector3(0f, Height / 2f, MaxZ + 0.05f), new Vector3(w + 0.2f, Height, 0.1f), m.wall);
        Prim(PrimitiveType.Cube, g, "Wall_Front", new Vector3(0f, Height / 2f, MinZ - 0.05f), new Vector3(w + 0.2f, Height, 0.1f), m.wall);
        Prim(PrimitiveType.Cube, g, "Wall_Left", new Vector3(MinX - 0.05f, Height / 2f, cz), new Vector3(0.1f, Height, d), m.wall);
        Prim(PrimitiveType.Cube, g, "Wall_Right", new Vector3(MaxX + 0.05f, Height / 2f, cz), new Vector3(0.1f, Height, d), m.wall);

        // แผงไม้ล่างผนัง + แถบทองคาดบน
        float wl = w - 0.04f, dl = d - 0.04f;
        Prim(PrimitiveType.Cube, g, "Wainscot_Back", new Vector3(0f, 0.45f, MaxZ - 0.02f), new Vector3(wl, 0.9f, 0.04f), m.wainscot);
        Prim(PrimitiveType.Cube, g, "Wainscot_Front", new Vector3(0f, 0.45f, MinZ + 0.02f), new Vector3(wl, 0.9f, 0.04f), m.wainscot);
        Prim(PrimitiveType.Cube, g, "Wainscot_Left", new Vector3(MinX + 0.02f, 0.45f, cz), new Vector3(0.04f, 0.9f, dl), m.wainscot);
        Prim(PrimitiveType.Cube, g, "Wainscot_Right", new Vector3(MaxX - 0.02f, 0.45f, cz), new Vector3(0.04f, 0.9f, dl), m.wainscot);

        Prim(PrimitiveType.Cube, g, "Trim_Back", new Vector3(0f, 0.915f, MaxZ - 0.035f), new Vector3(wl, 0.03f, 0.07f), m.gold);
        Prim(PrimitiveType.Cube, g, "Trim_Front", new Vector3(0f, 0.915f, MinZ + 0.035f), new Vector3(wl, 0.03f, 0.07f), m.gold);
        Prim(PrimitiveType.Cube, g, "Trim_Left", new Vector3(MinX + 0.035f, 0.915f, cz), new Vector3(0.07f, 0.03f, dl), m.gold);
        Prim(PrimitiveType.Cube, g, "Trim_Right", new Vector3(MaxX - 0.035f, 0.915f, cz), new Vector3(0.07f, 0.03f, dl), m.gold);

        // คานเพดาน
        float[] beamZ = { -1.5f, 0f, 1.5f, 3f };
        for (int i = 0; i < beamZ.Length; i++)
        {
            Prim(PrimitiveType.Cube, g, $"Beam_{i + 1}", new Vector3(0f, Height - 0.09f, beamZ[i]), new Vector3(w - 0.04f, 0.18f, 0.16f), m.beam);
        }
    }

    private static void BuildWindowAndDoor(Transform root, Mats m)
    {
        Transform g = Group(root, "WindowAndDoor");

        // หน้าต่างผนังขวา
        float wx = MaxX - 0.015f;
        Prim(PrimitiveType.Cube, g, "Window_Glass", new Vector3(wx, 1.7f, 1.6f), new Vector3(0.02f, 1.2f, 1.6f), m.glass);
        Prim(PrimitiveType.Cube, g, "Window_FrameTop", new Vector3(wx - 0.03f, 2.34f, 1.6f), new Vector3(0.08f, 0.08f, 1.76f), m.doorWood);
        Prim(PrimitiveType.Cube, g, "Window_FrameBottom", new Vector3(wx - 0.03f, 1.06f, 1.6f), new Vector3(0.08f, 0.08f, 1.76f), m.doorWood);
        Prim(PrimitiveType.Cube, g, "Window_FrameNear", new Vector3(wx - 0.03f, 1.7f, 0.76f), new Vector3(0.08f, 1.28f, 0.08f), m.doorWood);
        Prim(PrimitiveType.Cube, g, "Window_FrameFar", new Vector3(wx - 0.03f, 1.7f, 2.44f), new Vector3(0.08f, 1.28f, 0.08f), m.doorWood);
        Prim(PrimitiveType.Cube, g, "Window_MullionV", new Vector3(wx - 0.03f, 1.7f, 1.6f), new Vector3(0.06f, 1.2f, 0.04f), m.doorWood);
        Prim(PrimitiveType.Cube, g, "Window_MullionH", new Vector3(wx - 0.03f, 1.7f, 1.6f), new Vector3(0.06f, 0.04f, 1.6f), m.doorWood);
        Prim(PrimitiveType.Cube, g, "Window_Sill", new Vector3(wx - 0.06f, 1.02f, 1.6f), new Vector3(0.14f, 0.04f, 1.9f), m.doorWood);

        // แสงลอดหน้าต่าง (สีขาวอมฟ้า)
        GameObject lightGO = new GameObject("Window_Light");
        lightGO.transform.SetParent(g, false);
        lightGO.transform.position = new Vector3(MaxX - 0.5f, 1.9f, 1.6f);
        lightGO.transform.rotation = Quaternion.LookRotation(new Vector3(-1f, -0.35f, 0f));
        Light spot = lightGO.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(0.75f, 0.85f, 1f);
        spot.intensity = 3f;
        spot.range = 8f;
        spot.spotAngle = 85f;
        spot.innerSpotAngle = 45f;
        spot.shadows = LightShadows.None;

        // ประตูผนังซ้าย
        float dx = MinX + 0.03f;
        Prim(PrimitiveType.Cube, g, "Door", new Vector3(dx, 1.05f, 2.4f), new Vector3(0.06f, 2.1f, 0.95f), m.doorWood);
        Prim(PrimitiveType.Cube, g, "Door_FrameTop", new Vector3(dx + 0.01f, 2.14f, 2.4f), new Vector3(0.08f, 0.08f, 1.1f), m.beam);
        Prim(PrimitiveType.Cube, g, "Door_FrameA", new Vector3(dx + 0.01f, 1.05f, 1.9f), new Vector3(0.08f, 2.1f, 0.08f), m.beam);
        Prim(PrimitiveType.Cube, g, "Door_FrameB", new Vector3(dx + 0.01f, 1.05f, 2.9f), new Vector3(0.08f, 2.1f, 0.08f), m.beam);
        Prim(PrimitiveType.Sphere, g, "Door_Handle", new Vector3(dx + 0.06f, 1.0f, 2.05f), Vector3.one * 0.07f, m.gold);
    }

    private static void BuildFrames(Transform root, Mats m)
    {
        Transform g = Group(root, "WallFrames");
        for (int i = -1; i <= 1; i++)
        {
            float x = i * 1.7f;
            Prim(PrimitiveType.Cube, g, $"Frame_{i + 2}", new Vector3(x, 1.85f, MaxZ - 0.02f), new Vector3(0.72f, 0.92f, 0.04f), m.gold);
            Prim(PrimitiveType.Cube, g, $"FrameInner_{i + 2}", new Vector3(x, 1.85f, MaxZ - 0.055f), new Vector3(0.6f, 0.8f, 0.02f), m.frameInner);
        }
    }

    private static void BuildLanterns(Transform root, Mats m)
    {
        Transform g = Group(root, "Lanterns");
        Vector2[] spots = { new Vector2(-1.6f, 0.2f), new Vector2(1.6f, 0.2f), new Vector2(0f, 2.2f) };

        for (int i = 0; i < spots.Length; i++)
        {
            Vector3 top = new Vector3(spots[i].x, Height, spots[i].y);
            Prim(PrimitiveType.Cylinder, g, $"Cord_{i + 1}", top + new Vector3(0f, -0.25f, 0f), new Vector3(0.008f, 0.25f, 0.008f), m.beam);
            Prim(PrimitiveType.Cylinder, g, $"Cap_{i + 1}", top + new Vector3(0f, -0.52f, 0f), new Vector3(0.12f, 0.02f, 0.12f), m.gold);
            Vector3 bodyPos = top + new Vector3(0f, -0.68f, 0f);
            Prim(PrimitiveType.Sphere, g, $"Lantern_{i + 1}", bodyPos, new Vector3(0.22f, 0.28f, 0.22f), m.lantern);

            GameObject lightGO = new GameObject($"LanternLight_{i + 1}");
            lightGO.transform.SetParent(g, false);
            lightGO.transform.position = bodyPos;
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.72f, 0.4f);
            l.intensity = 2.2f;
            l.range = 5.5f;
            l.shadows = LightShadows.None;
        }
    }

    // ---------- เสื่อและฐานระนาด ----------

    private static void BuildMat(Transform root, Mats m, float ranadZ)
    {
        Transform g = Group(root, "Mat");
        float cz = ranadZ * 0.5f; // ให้คลุมทั้งตำแหน่งผู้เล่นและระนาด
        Prim(PrimitiveType.Cube, g, "Mat_Border", new Vector3(0f, FloorTop + 0.003f, cz), new Vector3(3.1f, 0.006f, 2.3f), m.matBorder);
        Prim(PrimitiveType.Cube, g, "Mat_Weave", new Vector3(0f, FloorTop + 0.0095f, cz), new Vector3(2.9f, 0.006f, 2.1f), m.mat);
    }

    private static bool TryGetKeyBounds(out Bounds bounds)
    {
        bounds = new Bounds(new Vector3(0f, 0.9f, 0.55f), new Vector3(0.9f, 0.02f, 0.3f));
        RanadKey[] keys = UnityEngine.Object.FindObjectsByType<RanadKey>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        bool first = true;
        foreach (RanadKey key in keys)
        {
            Renderer r = key.GetComponent<Renderer>();
            if (r == null) continue;
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }
        return !first;
    }

    private static void BuildRanadStand(Transform root, Mats m, Bounds b)
    {
        Transform g = Group(root, "RanadStand");
        Vector3 c = b.center;
        float bx = b.size.x, bz = b.size.z, minY = b.min.y;

        // ตัวรางใต้ลูกระนาด + ปีกหัวท้าย + ขอบทอง
        Prim(PrimitiveType.Cube, g, "Trough", new Vector3(c.x, minY - 0.03f, c.z), new Vector3(bx + 0.12f, 0.06f, bz + 0.10f), m.lacquer);
        float sideX = bx / 2f + 0.07f;
        Prim(PrimitiveType.Cube, g, "EndBoard_L", new Vector3(c.x - sideX, minY + 0.05f, c.z), new Vector3(0.04f, 0.22f, bz + 0.16f), m.lacquer);
        Prim(PrimitiveType.Cube, g, "EndBoard_R", new Vector3(c.x + sideX, minY + 0.05f, c.z), new Vector3(0.04f, 0.22f, bz + 0.16f), m.lacquer);
        Prim(PrimitiveType.Cube, g, "Rail_Front", new Vector3(c.x, minY - 0.005f, c.z - (bz / 2f + 0.05f)), new Vector3(bx + 0.12f, 0.03f, 0.02f), m.gold);
        Prim(PrimitiveType.Cube, g, "Rail_Back", new Vector3(c.x, minY - 0.005f, c.z + (bz / 2f + 0.05f)), new Vector3(bx + 0.12f, 0.03f, 0.02f), m.gold);

        // ขาตั้ง (เฉพาะเมื่อลูกระนาดลอยสูงจากพื้นพอ)
        if (minY > 0.3f)
        {
            float h = minY - 0.06f;
            Prim(PrimitiveType.Cube, g, "Pedestal", new Vector3(c.x, h / 2f, c.z), new Vector3(0.26f, h, 0.30f), m.lacquer);
            Prim(PrimitiveType.Cube, g, "Foot", new Vector3(c.x, FloorTop + 0.03f, c.z), new Vector3(0.7f, 0.06f, 0.55f), m.gold);
        }
    }

    // ---------- เครื่องดนตรีประดับห้อง ----------

    private static void BuildKhongWong(Transform root, Mats m, Vector3 center)
    {
        Transform g = Group(root, "KhongWong");
        const int count = 16;
        float ringRadius = 0.6f, gongRadius = 0.46f;
        float ringY = 0.28f;

        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2f / count;
            Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));

            // ท่อนไม้ประกอบเป็นวงกลม หันแนวสัมผัส
            float yaw = Mathf.Atan2(-Mathf.Cos(a), -Mathf.Sin(a)) * Mathf.Rad2Deg;
            float segLen = 2f * ringRadius * Mathf.Sin(Mathf.PI / count) + 0.02f;
            Prim(PrimitiveType.Cube, g, $"Ring_{i + 1}", center + radial * ringRadius + new Vector3(0f, ringY, 0f),
                 new Vector3(segLen, 0.05f, 0.05f), m.lacquer, new Vector3(0f, yaw, 0f));

            // ฆ้อง: จานแบนตั้งขึ้นหันออกนอกวง ขนาดไล่จากใหญ่ไปเล็ก
            float size = Mathf.Lerp(0.2f, 0.12f, i / (float)(count - 1));
            Vector3 gongPos = center + radial * gongRadius + new Vector3(0f, ringY + 0.11f, 0f);
            GameObject gong = Prim(PrimitiveType.Sphere, g, $"Gong_{i + 1}", gongPos, new Vector3(size, size, 0.05f), m.bronze);
            gong.transform.rotation = Quaternion.LookRotation(radial);
            Prim(PrimitiveType.Sphere, g, $"GongBoss_{i + 1}", gongPos + radial * 0.03f, Vector3.one * (size * 0.3f), m.gold);
        }

        // ขาตั้ง 4 ขา
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 p = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ringRadius;
            Prim(PrimitiveType.Cube, g, $"Leg_{i + 1}", new Vector3(p.x, ringY / 2f, p.z), new Vector3(0.05f, ringY, 0.05f), m.lacquer);
        }
    }

    private static void BuildDrum(Transform root, Mats m, Vector3 center)
    {
        Transform g = Group(root, "Drum");
        Prim(PrimitiveType.Cube, g, "Drum_Base", center + new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.3f, 0.4f), m.lacquer);
        Prim(PrimitiveType.Cylinder, g, "Drum_Body", center + new Vector3(0f, 0.53f, 0f), new Vector3(0.45f, 0.3f, 0.45f), m.drumBody, new Vector3(0f, 0f, 90f));
        Prim(PrimitiveType.Cylinder, g, "Drum_Head_L", center + new Vector3(-0.3f, 0.53f, 0f), new Vector3(0.5f, 0.01f, 0.5f), m.drumHead, new Vector3(0f, 0f, 90f));
        Prim(PrimitiveType.Cylinder, g, "Drum_Head_R", center + new Vector3(0.3f, 0.53f, 0f), new Vector3(0.5f, 0.01f, 0.5f), m.drumHead, new Vector3(0f, 0f, 90f));
        Prim(PrimitiveType.Cylinder, g, "Drum_Band_L", center + new Vector3(-0.18f, 0.53f, 0f), new Vector3(0.47f, 0.015f, 0.47f), m.gold, new Vector3(0f, 0f, 90f));
        Prim(PrimitiveType.Cylinder, g, "Drum_Band_R", center + new Vector3(0.18f, 0.53f, 0f), new Vector3(0.47f, 0.015f, 0.47f), m.gold, new Vector3(0f, 0f, 90f));
    }

    // ---------- พื้นเดิม ----------

    /// <summary>ปิดพื้นเดิม (Cube ใหญ่แบนชื่อ 'Cube' หรือ 'พื้น') กันซ้อนกับพื้นห้อง เปิดกลับได้ที่ Inspector</summary>
    private static string DisableOldFloor()
    {
        foreach (GameObject go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name != "Cube" && go.name != "พื้น") continue;
            if (!go.activeSelf || go.GetComponent<MeshFilter>() == null) continue;

            Vector3 s = go.transform.lossyScale;
            if (s.x >= 4f && s.z >= 4f && s.y <= 1f && go.transform.position.y <= 0.5f)
            {
                Undo.RecordObject(go, "Disable old floor");
                go.SetActive(false);
                return $"ปิดพื้นเดิม '{go.name}' แล้ว (กันซ้อนกับพื้นห้อง) ถ้าต้องการกลับมาใช้ให้ติ๊กเปิดที่ Inspector\n";
            }
        }
        return "ถ้ามีพื้นเดิมของฉากซ้อนกับพื้นห้อง (พื้นกระพริบ) ให้ปิดพื้นเดิมนั้นเอง\n";
    }

    // ---------- ตัวช่วย ----------

    private static Transform Group(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos,
                                   Vector3 scale, Material material, Vector3 euler = default)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;

        Collider col = go.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col); // ไม่ให้ของตกแต่งไปชนกับไม้ตี

        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    private static Material Mat(string name, Color color, float smoothness = 0.2f, float metallic = 0f,
                                Texture2D tex = null, Vector2? tiling = null, Color? emission = null)
    {
        string path = $"{AssetFolder}/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.color = color;
        mat.mainTexture = tex;
        if (tex != null && tiling.HasValue) mat.mainTextureScale = tiling.Value;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Texture2D MakeTexture(string name, int size, System.Func<int, int, Color> pixel)
    {
        string path = $"{AssetFolder}/Textures/{name}.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing; // มีแล้วใช้ซ้ำ (อยากสร้างใหม่ให้ลบไฟล์ .png ก่อน)

        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                t.SetPixel(x, y, pixel(x, y));
        t.Apply();

        File.WriteAllBytes(path, t.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------- ลายพื้นผิว (สร้างด้วยโค้ด) ----------

    private static float Hash(int n)
    {
        float s = Mathf.Sin(n * 12.9898f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    private static Color WoodPixel(int x, int y)
    {
        const int plankW = 32;
        int plank = x / plankW;
        int u = x % plankW;

        float tint = 0.88f + 0.12f * Hash(plank);
        float grain = Mathf.PerlinNoise(x * 0.06f + plank * 13.7f, y * 0.012f);
        float fine = Mathf.PerlinNoise(x * 0.5f, y * 0.03f);
        float b = tint * (0.72f + 0.2f * grain + 0.08f * fine);

        if (u < 1) b *= 0.45f;                            // รอยต่อระหว่างแผ่น
        if (((y + plank * 97) % 128) < 1) b *= 0.55f;     // รอยต่อหัวแผ่น
        return new Color(0.62f * b, 0.42f * b, 0.24f * b, 1f);
    }

    private static Color PlasterPixel(int x, int y)
    {
        float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
        float f = Mathf.PerlinNoise(x * 0.6f, y * 0.6f);
        float b = 0.88f + 0.08f * n + 0.04f * f;
        return new Color(0.96f * b, 0.90f * b, 0.78f * b, 1f);
    }

    private static Color WeavePixel(int x, int y)
    {
        const int cell = 16;
        bool horizontal = ((x / cell) + (y / cell)) % 2 == 0;
        float across = horizontal ? y : x;

        float fiber = 0.82f + 0.12f * Mathf.Sin(Mathf.Repeat(across, cell) / cell * Mathf.PI);
        float edge = (x % cell == 0 || y % cell == 0) ? 0.7f : 1f;
        float b = fiber * edge * (0.95f + 0.05f * Mathf.PerlinNoise(x * 0.4f, y * 0.4f));
        return new Color(0.86f * b, 0.72f * b, 0.45f * b, 1f);
    }
}
