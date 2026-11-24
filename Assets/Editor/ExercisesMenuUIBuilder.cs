using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// สร้าง UI เมนู "Exercises" ให้อัตโนมัติในฉากที่เปิดอยู่ตอนนี้
/// จัดวางเป็น 3 คอลัมน์ (รายชื่อ Chapter / รายละเอียด+ปุ่มควบคุม / ตัวช่วยฝึก)
///
/// ธีม: โทนแดงเลือดหมู-ทอง-น้ำตาลไม้ (โทนสีเครื่องดนตรีไทย/วัดไทยทั่วไป ไม่ใช่ลิขสิทธิ์ของใคร)
/// ลายเส้นกรอบทองและพื้นผิวปุ่มสร้างด้วยโค้ดล้วนๆ (ลายคลื่น/ก้นหอยเรียบง่าย)
/// เป็นงานออกแบบต้นฉบับของเราเอง ไม่ได้คัดลอกลายไทยแบบใดแบบหนึ่งมาโดยเฉพาะ
///
/// **ไฟล์นี้ต้องอยู่ในโฟลเดอร์ชื่อ "Editor" เท่านั้น**
/// เช่น Assets/Editor/ExercisesMenuUIBuilder.cs
///
/// ก่อนใช้: ต้องมี ExerciseChapter asset อย่างน้อย 1 อันในโปรเจกต์
/// วิธีใช้: เมนู Tools > VR Ranad Ek > Build Exercises Menu UI
/// (ถ้าเคยสร้างไว้แล้ว ใช้ Rebuild Exercises Menu UI เพื่อรีธีมใหม่โดยไม่ต้องลบเองก่อน)
/// </summary>
public static class ExercisesMenuUIBuilder
{
    private const string TextureFolder = "Assets/RanadRoom/Textures";

    // โทนสีธีมไทย
    private static readonly Color PanelBg = new Color(0.22f, 0.05f, 0.04f, 0.95f);      // แดงเลือดหมูเข้ม
    private static readonly Color ColumnBg = new Color(0f, 0f, 0f, 0.18f);
    private static readonly Color ButtonBg = new Color(0.32f, 0.14f, 0.07f, 1f);        // น้ำตาลไม้เข้ม
    private static readonly Color ButtonPressed = new Color(0.55f, 0.30f, 0.08f, 1f);   // น้ำตาลทองสว่างตอนกด
    private static readonly Color GoldTrim = new Color(0.82f, 0.62f, 0.22f);            // เส้นทอง
    private static readonly Color GoldBright = new Color(0.95f, 0.78f, 0.35f);          // ทองสว่าง (หัวข้อ)
    private static readonly Color TextColor = new Color(0.97f, 0.93f, 0.85f);
    private static readonly Color TextMuted = new Color(0.85f, 0.75f, 0.6f);

    [MenuItem("Tools/VR Ranad Ek/Build Exercises Menu UI")]
    public static void Build()
    {
        List<ExerciseChapter> chapters = FindAllChapters();
        if (chapters.Count == 0)
        {
            Debug.LogError("[ExercisesMenuUIBuilder] ไม่พบ ExerciseChapter asset เลยในโปรเจกต์ " +
                "สร้างอย่างน้อย 1 อันก่อน (คลิกขวาใน Project > Create > Ranad Ek > Exercise Chapter)");
            return;
        }

        if (GameObject.Find("ExercisesMenu_UI") != null)
        {
            Debug.LogWarning("[ExercisesMenuUIBuilder] พบ 'ExercisesMenu_UI' อยู่แล้ว " +
                "ใช้เมนู Rebuild Exercises Menu UI แทนถ้าต้องการสร้างใหม่");
            return;
        }

        Sprite goldSprite = MakeSprite("ThaiGoldTrim", 64, GoldTrimPixel);
        Sprite woodSprite = MakeSprite("ThaiButtonWood", 128, ButtonWoodPixel);

        EnsureEventSystem();

        GameObject canvasGO = new GameObject("ExercisesMenu_UI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Exercises Menu UI");

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1000, 600);
        canvasGO.transform.position = new Vector3(0f, 1.5f, 1.1f);
        canvasGO.transform.rotation = Quaternion.identity; // หันเข้าหาผู้เล่นที่มอง +Z
        canvasGO.transform.localScale = Vector3.one * 0.0015f;

        GameObject panel = CreatePanel(canvasRect, "Panel", PanelBg, Vector2.zero, new Vector2(1000, 600));
        AddGoldBorder(panel.GetComponent<RectTransform>(), goldSprite, 6f);
        AddCornerOrnaments(panel.GetComponent<RectTransform>(), goldSprite);

        CreateText(panel.transform, "Header", "การฝึกซ้อม (Exercises)", 34, TextAnchor.MiddleCenter,
            new Vector2(0, 262), new Vector2(1000, 60), GoldBright, true);
        CreateThinBar(panel.transform, "HeaderUnderline", goldSprite, new Vector2(0, 232), new Vector2(560, 4));

        // คอลัมน์ซ้าย: รายชื่อ Chapter
        GameObject chapterListPanel = CreatePanel(panel.GetComponent<RectTransform>(), "ChapterListPanel",
            ColumnBg, new Vector2(-350, -10), new Vector2(260, 460));
        AddGoldBorder(chapterListPanel.GetComponent<RectTransform>(), goldSprite, 3f);
        Transform chapterListContainer = chapterListPanel.transform;

        float buttonHeight = 42f;
        float startY = 200f;
        for (int i = 0; i < chapters.Count; i++)
        {
            CreateButton(chapterListContainer, $"ChapterButton_{i}", chapters[i].chapterTitle,
                new Vector2(0, startY - i * (buttonHeight + 6)), new Vector2(230, buttonHeight), woodSprite);
        }

        // คอลัมน์กลาง: รายละเอียด + ปุ่มควบคุม
        GameObject detailPanel = CreatePanel(panel.GetComponent<RectTransform>(), "DetailPanel",
            ColumnBg, new Vector2(0, -10), new Vector2(360, 460));
        AddGoldBorder(detailPanel.GetComponent<RectTransform>(), goldSprite, 3f);

        GameObject titleText = CreateText(detailPanel.transform, "ChapterTitleText", chapters[0].chapterTitle,
            26, TextAnchor.UpperCenter, new Vector2(0, 195), new Vector2(320, 40), GoldBright, true);
        GameObject descText = CreateText(detailPanel.transform, "ChapterDescriptionText", chapters[0].description,
            16, TextAnchor.UpperCenter, new Vector2(0, 160), new Vector2(320, 30), TextMuted, false);
        CreateThinBar(detailPanel.transform, "DetailDivider", goldSprite, new Vector2(0, 140), new Vector2(300, 2));
        GameObject notesText = CreateText(detailPanel.transform, "ChapterNotesText", "",
            18, TextAnchor.UpperCenter, new Vector2(0, 40), new Vector2(320, 220), TextColor, false);

        GameObject previewBtn = CreateButton(detailPanel.transform, "PreviewButton", "ฟังตัวอย่าง",
            new Vector2(-110, -200), new Vector2(100, 44), woodSprite);
        GameObject trainBtn = CreateButton(detailPanel.transform, "TrainButton", "ฝึก",
            new Vector2(0, -200), new Vector2(100, 44), woodSprite);
        GameObject playBtn = CreateButton(detailPanel.transform, "PlayButton", "เล่น",
            new Vector2(110, -200), new Vector2(100, 44), woodSprite);

        // คอลัมน์ขวา: ตัวช่วยฝึก
        GameObject assistPanel = CreatePanel(panel.GetComponent<RectTransform>(), "AssistPanel",
            ColumnBg, new Vector2(350, -10), new Vector2(260, 460));
        AddGoldBorder(assistPanel.GetComponent<RectTransform>(), goldSprite, 3f);

        CreateText(assistPanel.transform, "AssistanceHeader", "ตัวช่วย", 20, TextAnchor.MiddleLeft,
            new Vector2(-60, 200), new Vector2(220, 30), GoldBright, true);
        GameObject highlightToggle = CreateToggle(assistPanel.transform, "HighlightBarsToggle", "ไฮไลต์ลูกระนาด",
            new Vector2(0, 155), goldSprite);
        GameObject keysAssistToggle = CreateToggle(assistPanel.transform, "KeysAssistToggle", "ช่วยระบุลูก",
            new Vector2(0, 110), goldSprite);

        CreateText(assistPanel.transform, "FeedbackHeader", "ผลตอบรับ", 20, TextAnchor.MiddleLeft,
            new Vector2(-70, 50), new Vector2(220, 30), GoldBright, true);
        GameObject distanceToggle = CreateToggle(assistPanel.transform, "DistanceToggle", "ระยะห่างมือ",
            new Vector2(0, 5), goldSprite);
        GameObject angleToggle = CreateToggle(assistPanel.transform, "AngleToggle", "มุมไม้ตี",
            new Vector2(0, -40), goldSprite);

        // สร้างหรือหา VR_RanadEk_Generated + ExerciseTrainer
        GameObject root = GameObject.Find("VR_RanadEk_Generated");
        if (root == null)
        {
            root = new GameObject("VR_RanadEk_Generated");
            Undo.RegisterCreatedObjectUndo(root, "Create VR_RanadEk_Generated");
        }
        ExerciseTrainer trainer = root.GetComponent<ExerciseTrainer>();
        if (trainer == null) trainer = Undo.AddComponent<ExerciseTrainer>(root);

        // เพิ่ม/ผูก RhythmAccompanimentManager (ฉิ่ง, ฆ้องวง, กลอง) ถ้ายังไม่มี
        RhythmAccompanimentManager accompaniment = root.GetComponent<RhythmAccompanimentManager>();
        if (accompaniment == null) accompaniment = Undo.AddComponent<RhythmAccompanimentManager>(root);
        LoadRhythmSounds(accompaniment);
        trainer.accompaniment = accompaniment;

        // เพิ่ม ExercisesMenuController แล้วผูกทุกอย่างที่สร้างไว้เข้าด้วยกัน
        GameObject controllerGO = new GameObject("ExercisesMenuController");
        Undo.RegisterCreatedObjectUndo(controllerGO, "Create ExercisesMenuController");
        ExercisesMenuController controller = controllerGO.AddComponent<ExercisesMenuController>();

        controller.chapters = chapters;
        controller.trainer = trainer;
        controller.chapterListContainer = chapterListContainer;
        controller.chapterTitleText = titleText.GetComponent<Text>();
        controller.chapterDescriptionText = descText.GetComponent<Text>();
        controller.chapterNotesText = notesText.GetComponent<Text>();
        controller.previewButton = previewBtn.GetComponent<Button>();
        controller.trainButton = trainBtn.GetComponent<Button>();
        controller.playButton = playBtn.GetComponent<Button>();
        controller.highlightBarsToggle = highlightToggle.GetComponent<Toggle>();
        controller.keysAssistToggle = keysAssistToggle.GetComponent<Toggle>();
        controller.distanceToggle = distanceToggle.GetComponent<Toggle>();
        controller.angleToggle = angleToggle.GetComponent<Toggle>();

        // ใส่ XR UI Input Module + Tracked Device Graphic Raycaster ให้ controller กดปุ่มได้
        XRUIInputFixer.FixOpenScene();

        Selection.activeGameObject = canvasGO;

        Debug.Log("[ExercisesMenuUIBuilder] สร้าง UI ธีมไทยเสร็จแล้ว! กด Play แล้วดูที่หน้าจอด้านหน้าผู้เล่น " +
            "(ปรับตำแหน่ง/ขนาด Canvas ทีหลังได้ผ่าน Transform ของ 'ExercisesMenu_UI')");
    }

    /// <summary>ลบเมนูเดิมแล้วสร้างใหม่ ใช้หลังเพิ่ม/แก้ ExerciseChapter หรือเปลี่ยนธีม เพื่อให้ปุ่มบทใหม่โผล่</summary>
    [MenuItem("Tools/VR Ranad Ek/Rebuild Exercises Menu UI")]
    public static void Rebuild()
    {
        foreach (string objectName in new[] { "ExercisesMenu_UI", "ExercisesMenuController" })
        {
            GameObject old = GameObject.Find(objectName);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }
        Build();
    }

    private static List<ExerciseChapter> FindAllChapters()
    {
        List<ExerciseChapter> result = new List<ExerciseChapter>();
        string[] guids = AssetDatabase.FindAssets("t:ExerciseChapter");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ExerciseChapter chapter = AssetDatabase.LoadAssetAtPath<ExerciseChapter>(path);
            if (chapter != null) result.Add(chapter);
        }
        result.Sort((a, b) => string.Compare(a.chapterTitle, b.chapterTitle));
        return result;
    }

    /// <summary>หาไฟล์เสียง Ching/Klong/KhongWong ในโปรเจกต์ (จากโฟลเดอร์ RhythmSounds ที่ให้มา) แล้วผูกให้อัตโนมัติ</summary>
    private static void LoadRhythmSounds(RhythmAccompanimentManager accompaniment)
    {
        accompaniment.chingOpenClip = FindClipByName("Ching_Open");
        accompaniment.chingClosedClip = FindClipByName("Ching_Closed");
        accompaniment.klongStrongClip = FindClipByName("Klong_Strong");
        accompaniment.klongSoftClip = FindClipByName("Klong_Soft");

        accompaniment.khongWongClips = new AudioClip[21];
        int missing = 0;
        for (int i = 0; i < 21; i++)
        {
            AudioClip clip = FindClipByName($"KhongWong_{i + 1:00}");
            accompaniment.khongWongClips[i] = clip;
            if (clip == null) missing++;
        }

        if (missing > 0)
        {
            Debug.LogWarning($"[ExercisesMenuUIBuilder] หาไฟล์เสียงฆ้องวงไม่เจอ {missing} ลูก " +
                "ตรวจสอบว่าวางโฟลเดอร์ RhythmSounds ไว้ใน Assets แล้ว");
        }
    }

    private static AudioClip FindClipByName(string exactName)
    {
        string[] guids = AssetDatabase.FindAssets($"{exactName} t:AudioClip");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == exactName)
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    // ---------- ชิ้นส่วน UI พื้นฐาน ----------

    private static GameObject CreatePanel(RectTransform parent, string name, Color bg, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        go.GetComponent<Image>().color = bg;
        return go;
    }

    private static GameObject CreateText(Transform parent, string name, string text, int fontSize,
        TextAnchor alignment, Vector2 anchoredPos, Vector2 size, Color color, bool bold)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        Text t = go.GetComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.alignment = alignment;
        t.color = color;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchoredPos,
        Vector2 size, Sprite woodSprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        Image img = go.GetComponent<Image>();
        img.sprite = woodSprite;
        img.type = Image.Type.Sliced;
        img.color = ButtonBg;

        Button btn = go.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white; // สีจริงมาจาก img.color ด้านบน ปุ่มนี้ให้ multiply เฉยๆ
        colors.highlightedColor = new Color(1.15f, 1.05f, 0.9f);
        colors.pressedColor = ButtonPressed;
        colors.selectedColor = Color.white;
        btn.colors = colors;

        CreateText(go.transform, "Label", label, 18, TextAnchor.MiddleCenter, Vector2.zero, size, TextColor, false);

        return go;
    }

    private static GameObject CreateToggle(Transform parent, string name, string label, Vector2 anchoredPos, Sprite goldSprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220, 30);
        rt.anchoredPosition = anchoredPos;

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(go.transform, false);
        RectTransform bgRt = background.GetComponent<RectTransform>();
        bgRt.sizeDelta = new Vector2(24, 24);
        bgRt.anchoredPosition = new Vector2(-90, 0);
        background.GetComponent<Image>().color = ButtonBg;
        AddGoldBorder(bgRt, goldSprite, 2f);

        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(background.transform, false);
        RectTransform checkRt = checkmark.GetComponent<RectTransform>();
        checkRt.sizeDelta = new Vector2(14, 14);
        checkRt.anchoredPosition = Vector2.zero;
        checkmark.GetComponent<Image>().color = GoldBright;

        Toggle toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = background.GetComponent<Image>();
        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.isOn = true;

        CreateText(go.transform, "Label", label, 16, TextAnchor.MiddleLeft,
            new Vector2(10, 0), new Vector2(160, 24), TextColor, false);

        return go;
    }

    /// <summary>แถบทองบาง ใช้เป็นเส้นคั่น/ขีดใต้หัวข้อ</summary>
    private static GameObject CreateThinBar(Transform parent, string name, Sprite goldSprite, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        Image img = go.GetComponent<Image>();
        img.sprite = goldSprite;
        img.color = GoldTrim;
        return go;
    }

    /// <summary>กรอบเส้นทองบางล้อมรอบ RectTransform ที่กำหนด (4 แถบ บน/ล่าง/ซ้าย/ขวา)</summary>
    private static void AddGoldBorder(RectTransform target, Sprite goldSprite, float thickness)
    {
        Vector2 size = target.sizeDelta;

        GameObject top = new GameObject("Border_Top", typeof(RectTransform), typeof(Image));
        top.transform.SetParent(target, false);
        RectTransform topRt = top.GetComponent<RectTransform>();
        topRt.sizeDelta = new Vector2(size.x, thickness);
        topRt.anchoredPosition = new Vector2(0, size.y / 2f - thickness / 2f);
        top.GetComponent<Image>().color = GoldTrim;

        GameObject bottom = Object.Instantiate(top, target);
        bottom.name = "Border_Bottom";
        bottom.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -size.y / 2f + thickness / 2f);

        GameObject left = new GameObject("Border_Left", typeof(RectTransform), typeof(Image));
        left.transform.SetParent(target, false);
        RectTransform leftRt = left.GetComponent<RectTransform>();
        leftRt.sizeDelta = new Vector2(thickness, size.y);
        leftRt.anchoredPosition = new Vector2(-size.x / 2f + thickness / 2f, 0);
        left.GetComponent<Image>().color = GoldTrim;

        GameObject right = Object.Instantiate(left, target);
        right.name = "Border_Right";
        right.GetComponent<RectTransform>().anchoredPosition = new Vector2(size.x / 2f - thickness / 2f, 0);
    }

    /// <summary>เพชรทองเล็กๆ ประดับ 4 มุมของแผงหลัก ให้ดูมีลูกเล่นแบบกรอบลายไทย</summary>
    private static void AddCornerOrnaments(RectTransform target, Sprite goldSprite)
    {
        Vector2 size = target.sizeDelta;
        Vector2[] corners =
        {
            new Vector2(-size.x / 2f + 14, size.y / 2f - 14),
            new Vector2(size.x / 2f - 14, size.y / 2f - 14),
            new Vector2(-size.x / 2f + 14, -size.y / 2f + 14),
            new Vector2(size.x / 2f - 14, -size.y / 2f + 14),
        };

        for (int i = 0; i < corners.Length; i++)
        {
            GameObject go = new GameObject($"CornerOrnament_{i + 1}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(target, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(22, 22);
            rt.anchoredPosition = corners[i];
            rt.localRotation = Quaternion.Euler(0, 0, 45f);
            go.GetComponent<Image>().color = GoldBright;
        }
    }

    // ---------- พื้นผิว (สร้างด้วยโค้ด) ----------

    private static Sprite MakeSprite(string name, int size, System.Func<int, int, Color> pixel)
    {
        Directory.CreateDirectory(TextureFolder);
        string path = $"{TextureFolder}/{name}.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing == null)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    t.SetPixel(x, y, pixel(x, y));
            t.Apply();

            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>ลายเส้นทองคลื่นเรียบง่าย (ต้นฉบับของเราเอง) ใช้กับกรอบ/แถบคั่น</summary>
    private static Color GoldTrimPixel(int x, int y)
    {
        float wave = Mathf.Sin(x * 0.35f) * 6f + 32f;
        float d = Mathf.Abs(y - wave);
        float a = Mathf.Clamp01(1f - d / 5f);
        return new Color(1f, 1f, 1f, a);
    }

    /// <summary>พื้นผิวไม้เข้มสำหรับปุ่ม (ลายเสี้ยนไม้เรียบง่าย)</summary>
    private static Color ButtonWoodPixel(int x, int y)
    {
        float grain = Mathf.PerlinNoise(x * 0.04f, y * 0.01f);
        float fine = Mathf.PerlinNoise(x * 0.4f, y * 0.05f);
        float b = 0.75f + 0.2f * grain + 0.08f * fine;
        return new Color(b, b, b, 1f);
    }
}