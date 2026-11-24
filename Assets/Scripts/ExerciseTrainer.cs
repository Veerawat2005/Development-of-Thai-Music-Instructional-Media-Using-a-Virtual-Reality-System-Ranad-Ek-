using System.Collections;
using UnityEngine;

/// <summary>
/// ควบคุมโหมด "ฝึก" (Train) และ "เล่น" (Play) ของบทเรียนหนึ่ง Chapter
/// ไฮไลต์ลูกระนาดที่ต้องตีทีละตัวตามลำดับใน ExerciseChapter
/// รอให้ผู้เล่นตีลูกที่ถูกต้องก่อน ถึงจะไปโน้ตถัดไปอัตโนมัติ
///
/// วิธีใช้: ติดสคริปต์นี้ไว้ที่ GameObject ไหนก็ได้ในฉาก (เช่น root "VR_RanadEk_Generated")
/// ลาก ExerciseChapter asset ที่สร้างไว้มาใส่ช่อง Chapter แล้วเรียก StartTraining()
///
/// ระบบจังหวะ (ใหม่):
/// - highlightEnabled: ผูกกับ Toggle "Highlight Bars" ใน UI ปิดได้เพื่อฝึกแบบไม่มีตัวช่วย
/// - PreviewChapter(): เล่นเดโมไล่ทีละโน้ตให้ฟัง ไม่ต้องตีเอง ใช้กับปุ่ม "Preview"
/// - accompaniment: ถ้าใส่ไว้ จะเล่นฉิ่งคุมจังหวะระหว่าง Train/Play และฆ้องวง+กลองระหว่าง Play
/// - ตอนตีถูกลูก จะเทียบเวลาที่ตีกับจังหวะที่ "ควรจะตี" (คำนวณจาก bpm ของ Chapter)
///   แล้วแจ้งผลเป็น log ว่าเป๊ะ/เกือบตรง/เร็วไป/ช้าไป (ไม่บล็อกไม่ให้ไปต่อ แค่ให้ feedback)
/// </summary>
public class ExerciseTrainer : MonoBehaviour
{
    [Header("ข้อมูลบทเรียนที่จะฝึก")]
    public ExerciseChapter chapter;

    [Header("อ้างอิงลูกระนาดทั้งหมดในฉาก (ปล่อยว่างได้ จะหาอัตโนมัติตอนเริ่มเกม)")]
    public RanadKey[] allKeys;

    [Header("ตัวช่วยฝึก (ผูกกับ Toggle ใน UI)")]
    [Tooltip("ปิดถ้าอยากฝึกโดยไม่มีสีไฮไลต์ช่วยบอกลูกถัดไป")]
    public bool highlightEnabled = true;

    [Header("เสียงประกอบจังหวะ (ไม่ใส่ก็ได้ ถ้าไม่ใส่จะไม่มีฉิ่ง/ฆ้องวง/กลอง)")]
    public RhythmAccompanimentManager accompaniment;

    [Header("จังหวะเดโมตอน Preview")]
    public float previewNoteDuration = 0.5f;
    public float previewGapDuration = 0.15f;

    [Header("เกณฑ์แจ้งความแม่นจังหวะ (วินาที)")]
    public float perfectWindow = 0.15f;
    public float goodWindow = 0.35f;

    [Header("โน้ตคู่ 8 (ตีสองมือพร้อมกัน ห่างกัน 1 ออกเทฟ)")]
    [Tooltip("ช่วงเวลา (วินาที) ที่สองลูกต้องถูกตีห่างกันไม่เกิน ถึงจะนับว่าพร้อมกัน")]
    public float pairWindow = 0.15f;
    [Tooltip("เปิด = ตามหนังสือ: มือขวาต้องตีลูกสูง มือซ้ายต้องตีลูกต่ำ ปิด = มือไหนตีก็ได้ (โหมดผ่อนปรนสำหรับผู้เริ่มต้น)")]
    public bool strictHands = true;

    private int currentNoteIndex = -1;
    private RanadKey currentHighlighted;
    private RanadKey currentHighlighted2;   // ลูกต่ำของโน้ตคู่ 8 (null ถ้าเป็นโน้ตเดี่ยว)
    private float highHitTime = -1f;        // เวลาที่ตีลูกสูงล่าสุด (โน้ตคู่)
    private float lowHitTime = -1f;         // เวลาที่ตีลูกต่ำล่าสุด (โน้ตคู่)
    private bool isTraining = false;

    private float[] onsetTimes;   // เวลาที่ "ควรตี" ของแต่ละโน้ต นับจากเริ่มบท (วินาที)
    private float chapterStartTime;

    void Awake()
    {
        if (allKeys == null || allKeys.Length == 0)
        {
            allKeys = FindObjectsOfType<RanadKey>();
        }
    }

    void OnEnable()
    {
        RanadKey.OnKeyHitByHand += HandleKeyHit;
    }

    void OnDisable()
    {
        RanadKey.OnKeyHitByHand -= HandleKeyHit;
    }

    /// <summary>เริ่มฝึก Chapter ที่กำหนดไว้ใน field chapter ตั้งแต่โน้ตแรก (มีฉิ่งคุมจังหวะ ไม่มีฆ้องวง/กลอง)</summary>
    [ContextMenu("Start Training")]
    public void StartTraining()
    {
        StopAllCoroutines();
        accompaniment?.StopBacking();

        if (chapter == null || chapter.NoteCount == 0)
        {
            Debug.LogWarning("[ExerciseTrainer] ยังไม่ได้กำหนด Chapter หรือ Chapter ไม่มีโน้ตเลย");
            return;
        }

        ComputeOnsetTimes();
        chapterStartTime = Time.time;
        accompaniment?.StartMetronome(chapter.bpm);

        isTraining = true;
        currentNoteIndex = -1;
        AdvanceToNextNote();
    }

    /// <summary>โหมด "Play" — เหมือน Train แต่เพิ่มฆ้องวง+กลองประกอบเต็มวง</summary>
    public void PlayChapter()
    {
        StartTraining();
        if (chapter != null) accompaniment?.StartBacking(chapter);
    }

    /// <summary>หยุดฝึกกลางคัน ปิดไฮไลต์ที่ค้างอยู่ และหยุดเสียงประกอบ</summary>
    public void StopTraining()
    {
        StopAllCoroutines();
        isTraining = false;
        SetHighlightOnCurrent(false);
        currentHighlighted = null;
        currentHighlighted2 = null;
        currentNoteIndex = -1;
        accompaniment?.StopMetronome();
        accompaniment?.StopBacking();
    }

    /// <summary>เล่นเดโมไล่ทีละโน้ตในบทที่กำหนดไว้ ให้ฟังว่าเพลงเป็นยังไง ไม่ต้องตีเอง</summary>
    public void PreviewChapter()
    {
        StopTraining(); // กันชนกับโหมดฝึกที่อาจกำลังทำงานอยู่
        if (chapter == null || chapter.NoteCount == 0)
        {
            Debug.LogWarning("[ExerciseTrainer] ยังไม่ได้กำหนด Chapter หรือ Chapter ไม่มีโน้ตเลย");
            return;
        }
        StartCoroutine(PreviewRoutine());
    }

    private IEnumerator PreviewRoutine()
    {
        for (int i = 0; i < chapter.NoteCount; i++)
        {
            NoteStep note = chapter.notes[i];
            RanadKey key = FindKeyByName(note.KeyObjectName);

            if (key == null)
            {
                Debug.LogWarning($"[ExerciseTrainer] หาลูกระนาดชื่อ {note.KeyObjectName} ไม่เจอ ข้ามโน้ตนี้ไป");
                continue;
            }

            RanadKey key2 = note.IsPair ? FindKeyByName(note.KeyObjectName2) : null;

            key.SetHighlight(true);
            key.PlayPreviewSound();
            if (key2 != null)
            {
                key2.SetHighlight(true);
                key2.PlayPreviewSound(); // คู่ 8 ดังพร้อมกัน
            }
            yield return new WaitForSeconds(previewNoteDuration);
            key.SetHighlight(false);
            if (key2 != null) key2.SetHighlight(false);
            yield return new WaitForSeconds(previewGapDuration);
        }

        Debug.Log("[ExerciseTrainer] เล่นเดโมจบแล้ว");
    }

    /// <summary>คำนวณเวลาที่ "ควรตี" ของแต่ละโน้ตล่วงหน้า จากจำนวนจังหวะสะสมและ bpm ของ Chapter</summary>
    private void ComputeOnsetTimes()
    {
        onsetTimes = new float[chapter.NoteCount];
        float beatDuration = 60f / Mathf.Max(chapter.bpm, 1f);
        float t = 0f;
        for (int i = 0; i < chapter.NoteCount; i++)
        {
            onsetTimes[i] = t;
            t += beatDuration * Mathf.Max(chapter.notes[i].beats, 1f);
        }
    }

    private void AdvanceToNextNote()
    {
        SetHighlightOnCurrent(false);

        currentNoteIndex++;
        if (currentNoteIndex >= chapter.NoteCount)
        {
            Debug.Log("[ExerciseTrainer] ฝึกครบทุกโน้ตแล้ว! จบบทเรียน");
            isTraining = false;
            currentHighlighted = null;
            currentHighlighted2 = null;
            accompaniment?.StopMetronome();
            accompaniment?.StopBacking();
            return;
        }

        NoteStep note = chapter.notes[currentNoteIndex];
        currentHighlighted = FindKeyByName(note.KeyObjectName);
        currentHighlighted2 = note.IsPair ? FindKeyByName(note.KeyObjectName2) : null;
        highHitTime = -1f;
        lowHitTime = -1f;

        if (note.IsPair && currentHighlighted2 == null)
        {
            Debug.LogWarning($"[ExerciseTrainer] หาลูกระนาดชื่อ {note.KeyObjectName2} (ลูกต่ำของคู่ 8) ไม่เจอในฉาก ข้ามโน้ตนี้ไป");
            AdvanceToNextNote();
            return;
        }

        if (currentHighlighted == null)
        {
            Debug.LogWarning($"[ExerciseTrainer] หาลูกระนาดชื่อ {note.KeyObjectName} ไม่เจอในฉาก ข้ามโน้ตนี้ไป");
            AdvanceToNextNote();
            return;
        }

        SetHighlightOnCurrent(true);
    }

    private void HandleKeyHit(RanadKey hitKey, float speed, MalletHand hand)
    {
        if (!isTraining || currentHighlighted == null) return;

        // โน้ตเดี่ยว: ตีลูกที่ไฮไลต์ด้วยมือไหนก็ได้
        if (currentHighlighted2 == null)
        {
            if (hitKey != currentHighlighted) return; // ตีลูกอื่น ไม่นับ ไม่ไปต่อ
            ReportTimingAccuracy();
            AdvanceToNextNote();
            return;
        }

        // โน้ตคู่ 8: ต้องตีลูกสูง (มือขวา) กับลูกต่ำ (มือซ้าย) ภายใน pairWindow
        bool isHigh = hitKey == currentHighlighted;
        bool isLow = hitKey == currentHighlighted2;
        if (!isHigh && !isLow) return;

        if (strictHands && hand != MalletHand.Unknown)
        {
            MalletHand required = isHigh ? MalletHand.Right : MalletHand.Left;
            if (hand != required)
            {
                Debug.Log($"[ExerciseTrainer] คู่ 8: ลูก{(isHigh ? "สูง" : "ต่ำ")}ต้องตีด้วยมือ" +
                          $"{(isHigh ? "ขวา" : "ซ้าย")} (ตีด้วยมือผิดข้าง ไม่นับ)");
                return;
            }
        }

        float now = Time.time;
        if (isHigh) highHitTime = now; else lowHitTime = now;

        if (highHitTime >= 0f && lowHitTime >= 0f && Mathf.Abs(highHitTime - lowHitTime) <= pairWindow)
        {
            ReportTimingAccuracy();
            AdvanceToNextNote();
        }
        // ถ้าอีกลูกยังไม่ถูกตี หรือห่างเกิน pairWindow: เก็บเวลาไว้ รอลูกที่เหลือ
        // (การตีรอบถัดไปจะเขียนทับเวลาเก่า จึงลองตีใหม่ได้เรื่อยๆ)
    }

    /// <summary>เทียบเวลาที่ตีจริงกับเวลาที่ควรตี แล้วแจ้งผลเป็น log (ไม่บล็อกการฝึก)</summary>
    private void ReportTimingAccuracy()
    {
        if (onsetTimes == null || currentNoteIndex < 0 || currentNoteIndex >= onsetTimes.Length) return;

        float expected = chapterStartTime + onsetTimes[currentNoteIndex];
        float diff = Time.time - expected; // บวก = ช้าไป, ลบ = เร็วไป
        float absDiff = Mathf.Abs(diff);

        string grade = absDiff <= perfectWindow ? "เป๊ะ!" :
                        absDiff <= goodWindow ? "เกือบตรง" :
                        (diff < 0 ? "เร็วไป" : "ช้าไป");

        Debug.Log($"[Rhythm] โน้ตที่ {currentNoteIndex + 1} ({chapter.notes[currentNoteIndex].displayName}): " +
                  $"{grade} (ห่างจังหวะ {diff:+0.00;-0.00} วินาที)");
    }

    private void SetHighlightOnCurrent(bool isHighlighted)
    {
        if (currentHighlighted == null) return;
        // ปิดไฮไลต์ได้เสมอ แต่จะ "เปิด" ได้ก็ต่อเมื่อ highlightEnabled เท่านั้น
        bool showColor = isHighlighted && highlightEnabled;
        currentHighlighted.SetHighlight(showColor);
        if (currentHighlighted2 != null) currentHighlighted2.SetHighlight(showColor);
    }

    private RanadKey FindKeyByName(string objectName)
    {
        foreach (RanadKey key in allKeys)
        {
            if (key != null && key.gameObject.name == objectName) return key;
        }
        return null;
    }
}