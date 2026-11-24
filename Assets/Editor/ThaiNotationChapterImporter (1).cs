using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// แปลงโน้ตไทย (ด ร ม ฟ ซ ล ท) จากไฟล์ .txt เป็น ExerciseChapter asset ให้อัตโนมัติ
/// ไม่ต้องกรอก NoteStep ทีละแถวใน Inspector
///
/// **ไฟล์นี้ต้องอยู่ในโฟลเดอร์ชื่อ "Editor" เท่านั้น** เช่น Assets/Editor/
///
/// รูปแบบไฟล์ .txt:
///   // ...            = คอมเมนต์ (ข้าม)
///   # ชื่อท่อน         = เริ่มท่อนใหม่
///   บรรทัดโน้ต         = 1 แถวของตาราง ช่องคั่นด้วย |
///   ด ร ม ฟ ซ ล ท      = Do Re Mi Fa Sol La Ti (ออกเทฟกลาง = ลูก 8-14)
///   ตัวโน้ตมี ํ (สูง)   = ออกเทฟสูง (ลูก 15-21)
///   ตัวโน้ตมี ุ หรือ ฺ (ต่ำ) = ออกเทฟต่ำ (ลูก 1-7)
///   -                 = ยืดโน้ตก่อนหน้าออกไปอีก 1 จังหวะ (sustain) ไม่ใช่ตีใหม่
///   [ซุ ซ]            = คู่ 8: ตีสองลูกห่างกัน 1 ออกเทฟ "พร้อมกัน" (ขวาตีลูกสูง ซ้ายตีลูกต่ำ)
///
/// ตามตารางกรมศิลป์: ลูกที่ 1 = ซอล จึงตั้ง "เลื่อนหมายเลขลูก" เริ่มต้น = +3 (ด ต่ำ = ลูก 4, ด กลาง = ลูก 11, ด สูง = ลูก 18)
/// ถ้าระนาดจริงเริ่มที่โน้ตอื่น
/// ให้ปรับช่อง "เลื่อนหมายเลขลูก" ในหน้าต่างเครื่องมือ
///
/// วิธีใช้: เมนู Tools > VR Ranad Ek > Import Chapter From Thai Notation
/// </summary>
public class ThaiNotationChapterImporter : EditorWindow
{
    private const char HighMark = '\u0E4D'; // ํ  นิคหิต = สูง
    private const char LowMarkU = '\u0E38'; // ุ  สระอุ = ต่ำ
    private const char LowMarkPhinthu = '\u0E3A'; // ฺ  พินทุ = ต่ำ

    private static readonly string[] SolfegeNames = { "Do", "Re", "Mi", "Fa", "Sol", "La", "Ti" };

    private TextAsset source;
    private string chapterPrefix = "Chapter 2";
    private string description = "";
    private bool splitBySection = true;
    private string outputFolder = "Assets/Exercises";
    private int keyOffset = 3; // ระนาดจริง: ลูก 1-3 = ซ ล ท, โดกลาง = ลูก 11 (ต่ำสุด ด = ลูก 4)
    private float bpm = 100f;

    [MenuItem("Tools/VR Ranad Ek/Import Chapter From Thai Notation")]
    public static void ShowWindow()
    {
        GetWindow<ThaiNotationChapterImporter>("Import Chapter");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "ลากไฟล์โน้ต (.txt) ใส่ช่องด้านล่าง แล้วกด Import จะได้ ExerciseChapter asset " +
            "ตามท่อนในไฟล์ (รูปแบบไฟล์ดูคอมเมนต์บนสุดของสคริปต์นี้)", MessageType.Info);

        source = (TextAsset)EditorGUILayout.ObjectField("ไฟล์โน้ต (.txt)", source, typeof(TextAsset), false);
        chapterPrefix = EditorGUILayout.TextField("ชื่อ Chapter", chapterPrefix);
        description = EditorGUILayout.TextField("คำอธิบายสั้นๆ", description);
        splitBySection = EditorGUILayout.Toggle("แยกแต่ละท่อนเป็น Chapter ของตัวเอง", splitBySection);
        outputFolder = EditorGUILayout.TextField("โฟลเดอร์ปลายทาง", outputFolder);
        keyOffset = EditorGUILayout.IntField("เลื่อนหมายเลขลูก (+/-)", keyOffset);
        bpm = EditorGUILayout.Slider("ความเร็วจังหวะ (BPM)", bpm, 40f, 220f);

        GUILayout.Space(8);
        GUI.enabled = source != null;
        if (GUILayout.Button("Import", GUILayout.Height(32))) Import();
        GUI.enabled = true;
    }

    private class Section
    {
        public string title;
        public List<string> rows = new List<string>();
    }

    private void Import()
    {
        List<Section> sections = ParseFile(source.text);
        if (sections.Count == 0)
        {
            Debug.LogWarning("[ChapterImporter] ไม่พบโน้ตในไฟล์เลย");
            return;
        }

        Directory.CreateDirectory(outputFolder);
        AssetDatabase.Refresh();

        string fileBase = chapterPrefix.Replace(" ", "");
        List<string> warnings = new List<string>();
        StringBuilder report = new StringBuilder("[ChapterImporter] นำเข้าเสร็จ\n");
        ExerciseChapter firstAsset = null;

        if (splitBySection)
        {
            for (int i = 0; i < sections.Count; i++)
            {
                List<NoteStep> notes = ParseNotes(sections[i].rows, sections[i].title, warnings);
                string title = $"{chapterPrefix} - {sections[i].title}";
                ExerciseChapter asset = CreateOrUpdate($"{fileBase}_Part{i + 1}", title, notes);
                if (firstAsset == null) firstAsset = asset;
                report.AppendLine($" - {title}: {notes.Count} โน้ต");
            }
        }
        else
        {
            List<NoteStep> all = new List<NoteStep>();
            foreach (Section s in sections)
                all.AddRange(ParseNotes(s.rows, s.title, warnings));
            firstAsset = CreateOrUpdate(fileBase, chapterPrefix, all);
            report.AppendLine($" - {chapterPrefix}: {all.Count} โน้ต (รวม {sections.Count} ท่อน)");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (firstAsset != null) Selection.activeObject = firstAsset;

        report.AppendLine("ขั้นต่อไป: เมนู Tools > VR Ranad Ek > Rebuild Exercises Menu UI เพื่อให้ปุ่มบทใหม่โผล่ในเมนู");
        if (warnings.Count > 0)
        {
            report.AppendLine("คำเตือน:\n - " + string.Join("\n - ", warnings));
            Debug.LogWarning(report.ToString());
        }
        else
        {
            Debug.Log(report.ToString());
        }
    }

    private ExerciseChapter CreateOrUpdate(string fileName, string title, List<NoteStep> notes)
    {
        string path = $"{outputFolder}/{fileName}.asset";
        ExerciseChapter asset = AssetDatabase.LoadAssetAtPath<ExerciseChapter>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<ExerciseChapter>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.chapterTitle = title;
        asset.description = description;
        asset.bpm = bpm;
        asset.notes = notes;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static List<Section> ParseFile(string text)
    {
        List<Section> sections = new List<Section>();
        Section current = null;

        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.Trim().TrimStart('\uFEFF');
            if (line.Length == 0 || line.StartsWith("//")) continue;

            if (line.StartsWith("#"))
            {
                current = new Section { title = line.Substring(1).Trim() };
                sections.Add(current);
                continue;
            }

            if (current == null)
            {
                current = new Section { title = "Part 1" };
                sections.Add(current);
            }
            current.rows.Add(line);
        }
        return sections;
    }

    /// <summary>แปลงหลายแถวของท่อนเดียวเป็นลิสต์ NoteStep (รองรับ "-" ยืดจังหวะ และ [ต่ำ สูง] คู่ 8) ดู ThaiNoteParser</summary>
    private List<NoteStep> ParseNotes(List<string> rows, string sectionName, List<string> warnings)
    {
        return ThaiNoteParser.Parse(string.Join(" ", rows), keyOffset, sectionName, warnings);
    }

    private static bool IsOctaveMark(char c)
    {
        return c == HighMark || c == LowMarkU || c == LowMarkPhinthu;
    }

    private static int StepOf(char c)
    {
        switch (c)
        {
            case 'ด': return 0; // Do
            case 'ร': return 1; // Re
            case 'ม': return 2; // Mi
            case 'ฟ': return 3; // Fa
            case 'ซ': return 4; // Sol
            case 'ล': return 5; // La
            case 'ท': return 6; // Ti
            default: return -1;
        }
    }
}
