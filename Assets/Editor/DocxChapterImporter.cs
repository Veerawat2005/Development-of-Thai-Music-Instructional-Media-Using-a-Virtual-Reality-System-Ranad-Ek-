using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// นำเข้าบทเรียนจากไฟล์ Word (.docx) หลายบทในไฟล์เดียว -> สร้าง ExerciseChapter asset ให้ครบ
///
/// **ไฟล์นี้ต้องอยู่ในโฟลเดอร์ชื่อ "Editor" เท่านั้น** เช่น Assets/Editor/
/// วิธีใช้: เมนู Tools > VR Ranad Ek > Import Chapters From Word (.docx)
///
/// รูปแบบในเอกสาร Word (1 ย่อหน้า = 1 บรรทัด):
///   บท: ตีฉาก              = เริ่มบทใหม่ (ชื่อบท)
///   BPM: 90                = ความเร็ว (ไม่ใส่ = ใช้ค่าเริ่มต้นในหน้าต่าง)
///   คำอธิบาย: ฝึกน้ำหนักมือ = คำอธิบายสั้นๆ (ไม่ใส่ก็ได้)
///   บรรทัดอื่นๆ            = โน้ต ด ร ม ฟ ซ ล ท  (มี ํ = สูง, มี ุ = ต่ำ, - = ยืดโน้ตก่อนหน้า, | และเว้นวรรค = ตัวคั่น)
///   บรรทัดที่ขึ้นต้นด้วย # หรือ // = หมายเหตุ ข้าม
/// ใส่โน้ตในตาราง Word ได้ด้วย (อ่านทีละช่อง)
/// เลขลูกใช้ตามตารางกรมศิลป์ (ลูก 1 = ซอล) จึงมี "เลื่อนหมายเลขลูก" เริ่มต้น +3
/// </summary>
public class DocxChapterImporter : EditorWindow
{
    private const char HighMark = 'ํ';       // ํ
    private const char LowMarkU = 'ุ';       // ุ
    private const char LowMarkPhinthu = 'ฺ'; // ฺ
    private static readonly string[] SolfegeNames = { "Do", "Re", "Mi", "Fa", "Sol", "La", "Ti" };

    private Object source;
    private string outputFolder = "Assets/Exercises";
    private int keyOffset = 3;
    private float defaultBpm = 100f;

    [MenuItem("Tools/VR Ranad Ek/Import Chapters From Word (.docx)")]
    public static void ShowWindow() { GetWindow<DocxChapterImporter>("Import Word"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "ลากไฟล์ .docx (อยู่ใน Assets) ใส่ช่องด้านล่าง\n" +
            "แต่ละบทเริ่มด้วยบรรทัด  บท: ชื่อบท  ตามด้วย BPM: / คำอธิบาย: (ไม่ใส่ก็ได้) แล้วบรรทัดโน้ต",
            MessageType.Info);
        source = EditorGUILayout.ObjectField("ไฟล์ Word (.docx)", source, typeof(Object), false);
        outputFolder = EditorGUILayout.TextField("โฟลเดอร์ปลายทาง", outputFolder);
        keyOffset = EditorGUILayout.IntField("เลื่อนหมายเลขลูก (+/-)", keyOffset);
        defaultBpm = EditorGUILayout.Slider("BPM เริ่มต้น", defaultBpm, 40f, 220f);

        GUI.enabled = source != null;
        if (GUILayout.Button("Import", GUILayout.Height(32))) Import();
        GUI.enabled = true;
    }

    private class ChapterDraft
    {
        public string title;
        public float bpm;
        public string description = "";
        public List<string> rows = new List<string>();
    }

    private void Import()
    {
        string path = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(path) || !path.ToLower().EndsWith(".docx"))
        {
            Debug.LogWarning("[DocxImporter] กรุณาเลือกไฟล์ .docx ที่อยู่ใน Assets");
            return;
        }

        List<string> paragraphs;
        try { paragraphs = ReadParagraphs(path); }
        catch (System.Exception e)
        {
            Debug.LogError("[DocxImporter] อ่านไฟล์ Word ไม่ได้ (ปิดไฟล์ใน Word ก่อน และใช้ .docx ไม่ใช่ .doc): " + e.Message);
            return;
        }

        List<ChapterDraft> drafts = new List<ChapterDraft>();
        ChapterDraft cur = null;
        foreach (string raw in paragraphs)
        {
            string line = raw.Trim().TrimStart('﻿');
            if (line.Length == 0 || line.StartsWith("//") || line.StartsWith("#")) continue;

            if (StartsWithKey(line, new[] { "บท:", "บท：", "Chapter:" }, out string title))
            {
                cur = new ChapterDraft { title = title, bpm = defaultBpm };
                drafts.Add(cur);
                continue;
            }
            if (cur == null) continue; // ข้อความก่อนบทแรก (เช่น หัวเอกสาร) ข้าม

            if (StartsWithKey(line, new[] { "BPM:", "bpm:", "BPM：" }, out string bpmText))
            {
                if (float.TryParse(bpmText, out float b)) cur.bpm = Mathf.Clamp(b, 40f, 220f);
                continue;
            }
            if (StartsWithKey(line, new[] { "คำอธิบาย:", "คำอธิบาย：" }, out string desc))
            {
                cur.description = desc;
                continue;
            }
            cur.rows.Add(line);
        }

        if (drafts.Count == 0)
        {
            Debug.LogWarning("[DocxImporter] ไม่พบบรรทัด 'บท: ชื่อบท' ในเอกสารเลย");
            return;
        }

        Directory.CreateDirectory(outputFolder);
        AssetDatabase.Refresh();

        List<string> warnings = new List<string>();
        StringBuilder report = new StringBuilder("[DocxImporter] นำเข้าเสร็จ\n");
        ExerciseChapter first = null;
        for (int i = 0; i < drafts.Count; i++)
        {
            ChapterDraft d = drafts[i];
            List<NoteStep> notes = ParseNotes(string.Join(" ", d.rows), d.title, warnings);
            if (notes.Count == 0) warnings.Add($"{d.title}: ไม่มีโน้ตเลย");
            string file = $"Doc{i + 1:00}_{Sanitize(d.title)}";
            ExerciseChapter asset = CreateOrUpdate(file, d, notes);
            if (first == null) first = asset;
            report.AppendLine($" - {d.title}: {notes.Count} โน้ต, BPM {d.bpm}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (first != null) Selection.activeObject = first;

        report.AppendLine("ขั้นต่อไป: Tools > VR Ranad Ek > Rebuild Exercises Menu UI");
        if (warnings.Count > 0)
        {
            report.AppendLine("คำเตือน:\n - " + string.Join("\n - ", warnings));
            Debug.LogWarning(report.ToString());
        }
        else Debug.Log(report.ToString());
    }

    private ExerciseChapter CreateOrUpdate(string fileName, ChapterDraft d, List<NoteStep> notes)
    {
        string path = $"{outputFolder}/{fileName}.asset";
        ExerciseChapter asset = AssetDatabase.LoadAssetAtPath<ExerciseChapter>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<ExerciseChapter>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.chapterTitle = d.title;
        asset.description = d.description;
        asset.bpm = d.bpm;
        asset.notes = notes;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static bool StartsWithKey(string line, string[] keys, out string rest)
    {
        foreach (string k in keys)
        {
            if (line.StartsWith(k, System.StringComparison.OrdinalIgnoreCase))
            {
                rest = line.Substring(k.Length).Trim();
                return true;
            }
        }
        rest = null;
        return false;
    }

    private static string Sanitize(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c.ToString(), "");
        return s.Replace(" ", "");
    }

    /// <summary>อ่านข้อความของทุกย่อหน้า (รวมย่อหน้าในตาราง) จาก word/document.xml</summary>
    private static List<string> ReadParagraphs(string docxPath)
    {
        List<string> result = new List<string>();
        using (ZipArchive zip = ZipFile.OpenRead(docxPath))
        {
            ZipArchiveEntry entry = zip.GetEntry("word/document.xml");
            if (entry == null) throw new IOException("ไม่พบ word/document.xml");
            XmlDocument xml = new XmlDocument();
            using (Stream s = entry.Open()) xml.Load(s);

            XmlNamespaceManager ns = new XmlNamespaceManager(xml.NameTable);
            ns.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
            foreach (XmlNode p in xml.SelectNodes("//w:p", ns))
            {
                StringBuilder sb = new StringBuilder();
                foreach (XmlNode n in p.SelectNodes(".//w:t | .//w:tab | .//w:br", ns))
                {
                    if (n.LocalName == "t") sb.Append(n.InnerText);
                    else sb.Append(' ');
                }
                result.Add(sb.ToString());
            }
        }
        return result;
    }

    private List<NoteStep> ParseNotes(string row, string sectionName, List<string> warnings)
    {
        // รองรับ "-" ยืดจังหวะ และ [ต่ำ สูง] คู่ 8 (ดู ThaiNoteParser)
        return ThaiNoteParser.Parse(row, keyOffset, sectionName, warnings);
    }

    private static int StepOf(char c)
    {
        switch (c)
        {
            case 'ด': return 0;
            case 'ร': return 1;
            case 'ม': return 2;
            case 'ฟ': return 3;
            case 'ซ': return 4;
            case 'ล': return 5;
            case 'ท': return 6;
            default: return -1;
        }
    }
}
