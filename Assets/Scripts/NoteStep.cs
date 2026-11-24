using System;
using UnityEngine;

/// <summary>
/// โน้ตหนึ่งตัว (หรือหนึ่งคู่ 8) ในลำดับเพลงที่ผู้เล่นต้องตี
/// keyIndex อ้างอิงชื่อ GameObject ลูกระนาด เช่น keyIndex = 18 หมายถึง "RanadKey_18"
/// displayName คือชื่อโน้ตที่จะโชว์บน UI เช่น "Sol", "Ti", "Re", "Fa", "La", "Do", "Mi"
/// beats คือความยาวโน้ตนี้ หน่วยเป็นจำนวนจังหวะ (ใช้คำนวณจังหวะที่ควรตีตอนฝึกแบบมีจังหวะ)
///
/// โน้ตคู่ 8 (ตีสองมือพร้อมกัน ห่างกัน 1 ออกเทฟ = ห่าง 7 ลูก):
///   keyIndex  = ลูกสูง (มือขวา)
///   keyIndex2 = ลูกต่ำ (มือซ้าย)  ค่า 0 = ไม่ใช่โน้ตคู่ (โน้ตเดี่ยวธรรมดา)
/// </summary>
[Serializable]
public struct NoteStep
{
    [Tooltip("หมายเลขลูกระนาด 1-21 ที่ต้องตี (ตรงกับชื่อ RanadKey_XX) ถ้าเป็นโน้ตคู่ 8 คือลูกสูง (มือขวา)")]
    [Range(1, 21)]
    public int keyIndex;

    [Tooltip("ชื่อโน้ตที่จะแสดงบน UI เช่น Sol, Ti, Re, Fa, La")]
    public string displayName;

    [Tooltip("ความยาวโน้ตนี้ หน่วยเป็นจำนวนจังหวะ (beat) ค่าเริ่มต้น 1 เครื่องหมาย \"-\" ต่อท้ายในต้นฉบับจะยืดค่านี้ขึ้นทีละจังหวะ")]
    public float beats;

    [Tooltip("โน้ตคู่ 8: หมายเลขลูกต่ำ (มือซ้าย) ที่ต้องตีพร้อมกับ keyIndex ต้องเท่ากับ keyIndex - 7 ปล่อย 0 ถ้าเป็นโน้ตเดี่ยว")]
    [Range(0, 21)]
    public int keyIndex2;

    [Tooltip("ชื่อโน้ตลูกต่ำของคู่ 8 (ใช้แสดงผลเท่านั้น)")]
    public string displayName2;

    public NoteStep(int keyIndex, string displayName) : this(keyIndex, displayName, 1f) { }

    public NoteStep(int keyIndex, string displayName, float beats)
    {
        this.keyIndex = keyIndex;
        this.displayName = displayName;
        this.beats = beats <= 0 ? 1f : beats;
        this.keyIndex2 = 0;
        this.displayName2 = null;
    }

    /// <summary>สร้างโน้ตคู่ 8 ส่ง high = ลูกสูง (มือขวา), low = ลูกต่ำ (มือซ้าย)</summary>
    public static NoteStep Pair(int highKey, string highName, int lowKey, string lowName, float beats = 1f)
    {
        NoteStep n = new NoteStep(highKey, highName, beats);
        n.keyIndex2 = lowKey;
        n.displayName2 = lowName;
        return n;
    }

    /// <summary>true ถ้าเป็นโน้ตคู่ 8 (ต้องตีสองลูกพร้อมกัน)</summary>
    public readonly bool IsPair => keyIndex2 > 0;

    /// <summary>ชื่อ GameObject ของลูกระนาดที่ตรงกับโน้ตนี้ เช่น "RanadKey_18" (ลูกสูงถ้าเป็นคู่)</summary>
    public readonly string KeyObjectName => $"RanadKey_{keyIndex:00}";

    /// <summary>ชื่อ GameObject ของลูกต่ำในคู่ 8 (ว่างถ้าไม่ใช่คู่)</summary>
    public readonly string KeyObjectName2 => keyIndex2 > 0 ? $"RanadKey_{keyIndex2:00}" : null;
}