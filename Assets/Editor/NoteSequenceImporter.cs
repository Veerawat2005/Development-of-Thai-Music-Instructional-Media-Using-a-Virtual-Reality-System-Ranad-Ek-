using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// นำเข้าโน้ตระนาดจากไฟล์ข้อความ (รูปแบบโน้ตไทย ด ร ม ฟ ซ ล ท + ํ = สูง, ุ = ต่ำ)
/// แปลงเป็นลำดับ NoteStep แล้วใส่เข้า ExerciseChapter asset ที่เลือกไว้ให้อัตโนมัติ
///
/// ข้ามบรรทัด "// comment" และ "# ชื่อท่อน" เสมอ — รวมทุกท่อนในไฟล์เป็นลำดับเดียวต่อเนื่องกัน
/// เครื่องหมาย "-" (พัก/ยืดเสียง) จะถูกข้าม ไม่นับเป็นโน้ต ตามรูปแบบไฟล์ต้นฉบับ
/// ถ้าต้องการแยกเป็นหลาย Chapter ให้แยกไฟล์ต้นฉบับเป็นคนละไฟล์ แล้วรันเครื่องมือนี้ทีละไฟล์ต่อ Chapter
///
/// วิธีใช้:
/// 1. บันทึกข้อความโน้ตเป็นไฟล์ .txt ที่ไหนก็ได้ในเครื่อง (ไม่ต้องอยู่ใน Assets ก็ได้)
/// 2. (ไม่บังคับ) เลือก ExerciseChapter asset ปลายทางใน Project window ไว้ก่อน
///    ถ้าไม่เลือกไว้ เครื่องมือจะถามให้สร้างไฟล์ใหม่
/// 3. เมนู Tools > VR Ranad Ek > Import Notes From Text File แล้วเลือกไฟล์ .txt
/// </summary>
public static class NoteSequenceImporter
{
    private const int NotesPerOctave = 7;
    private const char HighOctaveMark = '\u0E4D'; // ํ
    private const char LowOctaveMark = '\u0E38';  // ุ

    private static readonly Dictionary<char, int> SolfegeOrder = new Dictionary<char, int>
    {
        { 'ด', 0 }, { 'ร', 1 }, { 'ม', 2 }, { 'ฟ', 3 }, { 'ซ', 4 }, { 'ล', 5 }, { 'ท', 6 },
    };

    private static readonly Dictionary<char, string> SolfegeName = new Dictionary<char, string>
    {
        { 'ด', "Do" }, { 'ร', "Re" }, { 'ม', "Mi" }, { 'ฟ', "Fa" }, { 'ซ', "Sol" }, { 'ล', "La" }, { 'ท', "Ti" },
    };

    [MenuItem("Tools/VR Ranad Ek/Import Notes From Text File")]
    public static void ImportFromFile()
    {
        string path = EditorUtility.OpenFilePanel("เลือกไฟล์โน้ต (.txt)", Application.dataPath, "txt");
        if (string.IsNullOrEmpty(path)) return;

        string rawText = File.ReadAllText(path, Encoding.UTF8);
        List<NoteStep> notes = ParseNotes(rawText);

        if (notes.Count == 0)
        {
            EditorUtility.DisplayDialog("Import Notes",
                "ไม่พบโน้ตที่แปลงได้เลยในไฟล์นี้ ตรวจสอบรูปแบบไฟล์อีกครั้ง", "ตกลง");
            return;
        }

        ExerciseChapter target = Selection.activeObject as ExerciseChapter;
        bool isNew = target == null;

        if (isNew)
        {
            string savePath = EditorUtility.SaveFilePanelInProject(
                "สร้าง Exercise Chapter ใหม่",
                Path.GetFileNameWithoutExtension(path), "asset",
                "ยังไม่ได้เลือก ExerciseChapter asset ไว้ก่อน จะสร้างไฟล์ใหม่ที่ไหน?");
            if (string.IsNullOrEmpty(savePath)) return;

            target = ScriptableObject.CreateInstance<ExerciseChapter>();
            target.chapterTitle = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(target, savePath);
        }

        Undo.RecordObject(target, "Import Notes");
        target.notes = notes;
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Import Notes",
            $"นำเข้าโน้ตสำเร็จ {notes.Count} ตัว ใส่ลงใน '{target.name}' เรียบร้อยแล้ว", "ตกลง");

        Selection.activeObject = target;
        EditorGUIUtility.PingObject(target);
    }

    private static List<NoteStep> ParseNotes(string rawText)
    {
        List<NoteStep> result = new List<NoteStep>();
        string[] lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("//")) continue; // comment — ข้าม
            if (line.StartsWith("#")) continue;   // หัวข้อท่อน เช่น "# ท่อน 1" — ข้ามเสมอ รวมเป็นเพลงเดียว

            foreach (string rawCell in line.Split('|'))
            {
                string cell = rawCell.Replace(" ", "").Replace("\t", "");
                ParseCell(cell, result);
            }
        }

        return result;
    }

    private static void ParseCell(string cell, List<NoteStep> result)
    {
        int i = 0;
        while (i < cell.Length)
        {
            char c = cell[i];

            if (c == '-')
            {
                i++; // พัก/ยืดเสียง — ยังไม่นับตามที่ระบุไว้ในไฟล์ต้นฉบับ
                continue;
            }

            if (SolfegeOrder.TryGetValue(c, out int order))
            {
                int octave = 1; // 0 = ต่ำ, 1 = กลาง, 2 = สูง
                int consumed = 1;

                if (i + 1 < cell.Length)
                {
                    char next = cell[i + 1];
                    if (next == HighOctaveMark) { octave = 2; consumed = 2; }
                    else if (next == LowOctaveMark) { octave = 0; consumed = 2; }
                }

                int keyIndex = (octave * NotesPerOctave) + order + 1; // ต่ำ=1-7, กลาง=8-14, สูง=15-21
                keyIndex = Mathf.Clamp(keyIndex, 1, 21);

                result.Add(new NoteStep(keyIndex, SolfegeName[c]));
                i += consumed;
                continue;
            }

            i++; // อักขระอื่นที่ไม่รู้จัก (เช่นช่องว่างหลงเหลือ) ข้ามไปเฉยๆ
        }
    }
}
