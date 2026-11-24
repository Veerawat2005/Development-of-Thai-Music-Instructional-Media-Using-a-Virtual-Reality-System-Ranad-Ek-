using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ตัวแปลงโน้ตไทยเป็น NoteStep ที่ใช้ร่วมกันของตัวนำเข้า .txt และ .docx
/// **ต้องอยู่ในโฟลเดอร์ Editor**
///
/// รูปแบบ:
///   ด ร ม ฟ ซ ล ท        โน้ตเดี่ยว (ออกเทฟกลาง)   ํ = สูง   ุ/ฺ = ต่ำ
///   -                    ยืดโน้ตก่อนหน้าอีก 1 จังหวะ
///   ซุุ ลุุ ทุุ          ต่ำพิเศษ (ลูก 1-3) ใส่เครื่องหมายต่ำซ้อน 2 ตัว ใช้เขียนคู่ 8 ที่ลูกสูงเป็น ซุ ลุ ทุ เช่น [ซุุ ซุ]
///   [ซุ ซ]               คู่ 8: สองลูกห่างกัน 1 ออกเทฟ ตีสองมือพร้อมกัน
///                        (ใส่ลูกต่ำก่อนหรือหลังก็ได้ ระบบจัดให้เอง: ลูกสูง = มือขวา, ลูกต่ำ = มือซ้าย)
///   [ซุ ซ] - -           คู่ 8 ที่ยืดเสียง 3 จังหวะ
/// </summary>
public static class ThaiNoteParser
{
    private const char HighMark = 'ํ';       // ํ
    private const char LowMarkU = 'ุ';       // ุ
    private const char LowMarkPhinthu = 'ฺ'; // ฺ

    private static readonly string[] SolfegeNames = { "Do", "Re", "Mi", "Fa", "Sol", "La", "Ti" };

    private struct Tone
    {
        public int key;
        public string name;
    }

    public static List<NoteStep> Parse(string row, int keyOffset, string sectionName, List<string> warnings)
    {
        List<NoteStep> notes = new List<NoteStep>();

        for (int i = 0; i < row.Length; i++)
        {
            char c = row[i];

            if (c == '-' || c == '–' || c == '—') // Word มักแปลง - เป็นขีดยาว
            {
                if (notes.Count > 0)
                {
                    NoteStep last = notes[notes.Count - 1];
                    last.beats += 1f;
                    notes[notes.Count - 1] = last;
                }
                continue;
            }

            if (c == '[' || c == '［')
            {
                int close = row.IndexOfAny(new[] { ']', '］' }, i + 1);
                if (close < 0)
                {
                    warnings.Add($"{sectionName}: มี [ แต่ไม่มี ] ปิด (ข้ามส่วนที่เหลือของแถว)");
                    break;
                }
                string inner = row.Substring(i + 1, close - i - 1);
                i = close;
                TryAddPair(inner, keyOffset, sectionName, warnings, notes);
                continue;
            }

            int idx = i;
            if (TryReadTone(row, ref idx, keyOffset, sectionName, warnings, out Tone t, out bool wasNote))
            {
                notes.Add(new NoteStep(t.key, t.name, 1f));
            }
            i = idx;
            if (!wasNote) { /* เว้นวรรค | ฯลฯ ข้าม */ }
        }
        return notes;
    }

    private static void TryAddPair(string inner, int keyOffset, string sectionName, List<string> warnings, List<NoteStep> notes)
    {
        List<Tone> tones = new List<Tone>();
        bool outOfRange = false;
        for (int i = 0; i < inner.Length; i++)
        {
            int idx = i;
            if (TryReadTone(inner, ref idx, keyOffset, sectionName, warnings, out Tone t, out bool wasNote))
                tones.Add(t);
            else if (wasNote) outOfRange = true;
            i = idx;
        }

        if (outOfRange) return; // เตือนไปแล้วใน TryReadTone

        if (tones.Count != 2)
        {
            warnings.Add($"{sectionName}: [{inner.Trim()}] ต้องมีโน้ต 2 ตัวพอดี (ตอนนี้ {tones.Count}) ข้ามคู่นี้");
            return;
        }

        Tone high = tones[0].key >= tones[1].key ? tones[0] : tones[1];
        Tone low = tones[0].key >= tones[1].key ? tones[1] : tones[0];
        if (high.key - low.key != 7)
        {
            warnings.Add($"{sectionName}: [{inner.Trim()}] ไม่ใช่คู่ 8 (ต้องเป็นโน้ตเดียวกันห่างกัน 1 ออกเทฟ " +
                         $"ตอนนี้ห่าง {high.key - low.key} ลูก) ข้ามคู่นี้");
            return;
        }

        notes.Add(NoteStep.Pair(high.key, high.name, low.key, low.name, 1f));
    }

    /// <summary>
    /// อ่านโน้ตหนึ่งตัวที่ตำแหน่ง i (i จะถูกขยับไปท้ายตัวโน้ต+เครื่องหมายออกเทฟ)
    /// wasNote = เป็นตัวโน้ตจริง (แม้จะอยู่นอกช่วงและคืนค่า false) ใช้แยกจากอักขระที่ไม่ใช่โน้ต
    /// </summary>
    private static bool TryReadTone(string row, ref int i, int keyOffset, string sectionName, List<string> warnings,
                                    out Tone tone, out bool wasNote)
    {
        tone = default;
        wasNote = false;

        int step = StepOf(row[i]);
        if (step < 0) return false;
        wasNote = true;

        int octave = 1; // -1 = ต่ำพิเศษ (ซุุ ลุุ ทุุ = ลูก 1-3), 0 = ต่ำ, 1 = กลาง, 2 = สูง
        int j = i + 1;
        while (j < row.Length && (row[j] == HighMark || row[j] == LowMarkU || row[j] == LowMarkPhinthu))
        {
            octave += row[j] == HighMark ? 1 : -1; // เครื่องหมายซ้อนได้: ซุุ = ต่ำ 2 ระดับ (ลูก 1-3 ซึ่งต่ำกว่า ดุ)
            j++;
        }
        i = j - 1;

        int keyIndex = octave * 7 + step + 1 + keyOffset;
        if (keyIndex < 1 || keyIndex > 21)
        {
            warnings.Add($"{sectionName}: โน้ต {SolfegeNames[step]} ออกเทฟที่ {octave + 1} ได้เลขลูก {keyIndex} " +
                         "ซึ่งนอกช่วง 1-21 (ข้ามโน้ตนี้) ลองปรับช่อง 'เลื่อนหมายเลขลูก'");
            return false;
        }

        string suffix = octave >= 2 ? "'" : (octave == 0 ? "," : (octave < 0 ? ",," : ""));
        tone = new Tone { key = keyIndex, name = SolfegeNames[step] + suffix };
        return true;
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
