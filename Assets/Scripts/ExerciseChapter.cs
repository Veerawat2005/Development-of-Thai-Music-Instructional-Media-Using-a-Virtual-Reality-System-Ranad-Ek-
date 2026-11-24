using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ข้อมูลบทเรียนฝึกตีระนาด 1 บท (1 Chapter)
/// สร้าง Asset ได้จากเมนู: คลิกขวาใน Project window > Create > Ranad Ek > Exercise Chapter
/// ลากมาใส่ในลิสต์ของตัวควบคุมเมนู Exercises เพื่อแสดงผลใน UI
/// </summary>
[CreateAssetMenu(fileName = "NewChapter", menuName = "Ranad Ek/Exercise Chapter", order = 1)]
public class ExerciseChapter : ScriptableObject
{
    [Header("ข้อมูลทั่วไป")]
    [Tooltip("ชื่อบทเรียนที่แสดงในลิสต์ เช่น Chapter 1")]
    public string chapterTitle = "Chapter 1";

    [Tooltip("คำอธิบายสั้นๆ เช่น 'ตีระนาดเสียง 3 พยางค์'")]
    [TextArea(1, 2)]
    public string description = "";

    [Tooltip("ความเร็วจังหวะของบทนี้ (จังหวะต่อนาที) ใช้คุมฉิ่งและเสียงประกอบตอนฝึก")]
    [Range(40, 220)]
    public float bpm = 100f;

    [Header("ลำดับโน้ตทั้งหมดในบทนี้")]
    [Tooltip("เรียงตามลำดับที่ต้องตี บรรทัดแรกในลิสต์ = โน้ตแรกที่ต้องตี")]
    public List<NoteStep> notes = new List<NoteStep>();

    /// <summary>จำนวนโน้ตทั้งหมดในบทนี้</summary>
    public int NoteCount => notes.Count;

    /// <summary>ดึงโน้ตลำดับที่ index (0-based) อย่างปลอดภัย คืนค่า null ถ้า index เกินขอบเขต</summary>
    public NoteStep? GetNote(int index)
    {
        if (index < 0 || index >= notes.Count) return null;
        return notes[index];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // เตือนใน Editor ถ้ามี keyIndex ที่ไม่สมเหตุสมผล (นอกช่วง 1-21)
        for (int i = 0; i < notes.Count; i++)
        {
            if (notes[i].keyIndex < 1 || notes[i].keyIndex > 21)
            {
                Debug.LogWarning($"[{name}] โน้ตลำดับที่ {i + 1} มี keyIndex = {notes[i].keyIndex} " +
                    "ซึ่งอยู่นอกช่วง 1-21 ลูกระนาดจริง โปรดตรวจสอบ");
            }
            if (notes[i].keyIndex2 != 0 && (notes[i].keyIndex2 < 1 || notes[i].keyIndex - notes[i].keyIndex2 != 7))
            {
                Debug.LogWarning($"[{name}] โน้ตลำดับที่ {i + 1} เป็นคู่ 8 ไม่ถูกต้อง: keyIndex2 ต้องเท่ากับ keyIndex - 7 " +
                    $"(ตอนนี้ {notes[i].keyIndex} กับ {notes[i].keyIndex2})");
            }
        }
    }
#endif
}