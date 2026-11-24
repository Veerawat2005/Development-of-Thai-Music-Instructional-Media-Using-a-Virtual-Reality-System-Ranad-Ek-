using System.Collections;
using UnityEngine;

/// <summary>
/// เล่นเสียงประกอบจังหวะระหว่างฝึก/เล่น
/// - Metronome (ฉิ่ง): เล่นตลอดเวลาตามจังหวะของ Chapter ที่กำลังฝึก สลับเสียง "ฉิ่ง" (เปิด)
///   กับ "ฉับ" (ปิด) ทุกจังหวะคี่/คู่ ตามธรรมเนียมการตีฉิ่งคุมจังหวะดนตรีไทย
/// - Backing (ฆ้องวง + กลอง): เล่นเฉพาะตอนโหมด Play ให้บรรยากาศเหมือนเล่นร่วมวง
///   ฆ้องวงเล่นโน้ตเดียวกับระนาดแต่ต่ำกว่าหนึ่งคู่แปด กลองตีจังหวะเน้น-เบา (หนัก-เบา) คุมห้อง
///   (ไม่ได้ถอดหน้าทับจริงจากโน้ตต้นฉบับ เป็นจังหวะคุมพื้นฐานทั่วไปเท่านั้น)
///
/// วิธีใช้: ติดสคริปต์นี้ไว้ที่ GameObject เดียวกับ ExerciseTrainer (เช่น VR_RanadEk_Generated)
/// ใส่ไฟล์เสียง Ching/Klong/KhongWong ตามช่องใน Inspector แล้วลาก component นี้
/// ใส่ช่อง Accompaniment ของ ExerciseTrainer
/// </summary>
public class RhythmAccompanimentManager : MonoBehaviour
{
    [Header("เสียงฉิ่งคุมจังหวะ")]
    public AudioClip chingOpenClip;   // ฉิ่ง (เปิด เสียงยาว) - จังหวะคี่
    public AudioClip chingClosedClip; // ฉับ (ปิด เสียงสั้น) - จังหวะคู่
    public bool metronomeEnabled = true;

    [Header("เสียงฆ้องวง 21 ลูก (เรียงลูก 1-21 เหมือนระนาด)")]
    public AudioClip[] khongWongClips = new AudioClip[21];

    [Header("เสียงกลอง")]
    public AudioClip klongStrongClip; // จังหวะเน้น (ต้นห้อง)
    public AudioClip klongSoftClip;   // จังหวะเบา

    private AudioSource metronomeSource;
    private AudioSource backingSource;
    private Coroutine metronomeRoutine;
    private Coroutine backingRoutine;
    private int beatCounter;

    void Awake()
    {
        metronomeSource = gameObject.AddComponent<AudioSource>();
        metronomeSource.playOnAwake = false;
        backingSource = gameObject.AddComponent<AudioSource>();
        backingSource.playOnAwake = false;
    }

    /// <summary>เริ่มเล่นฉิ่งคุมจังหวะตาม bpm ที่กำหนด</summary>
    public void StartMetronome(float bpm)
    {
        StopMetronome();
        if (!metronomeEnabled) return;
        metronomeRoutine = StartCoroutine(MetronomeRoutine(bpm));
    }

    public void StopMetronome()
    {
        if (metronomeRoutine != null) StopCoroutine(metronomeRoutine);
        metronomeRoutine = null;
        beatCounter = 0;
    }

    private IEnumerator MetronomeRoutine(float bpm)
    {
        float beatDuration = 60f / Mathf.Max(bpm, 1f);
        while (true)
        {
            AudioClip clip = (beatCounter % 2 == 0) ? chingOpenClip : chingClosedClip;
            if (clip != null) metronomeSource.PlayOneShot(clip);
            beatCounter++;
            yield return new WaitForSeconds(beatDuration);
        }
    }

    /// <summary>เริ่มเล่นฆ้องวง+กลองตามโน้ตใน Chapter ควบคู่กับโหมด Play (เรียกพร้อม StartMetronome)</summary>
    public void StartBacking(ExerciseChapter chapter)
    {
        StopBacking();
        if (chapter == null || chapter.NoteCount == 0) return;
        backingRoutine = StartCoroutine(BackingRoutine(chapter));
    }

    public void StopBacking()
    {
        if (backingRoutine != null) StopCoroutine(backingRoutine);
        backingRoutine = null;
    }

    private IEnumerator BackingRoutine(ExerciseChapter chapter)
    {
        float beatDuration = 60f / Mathf.Max(chapter.bpm, 1f);
        int beatsSinceDrum = 0;

        for (int i = 0; i < chapter.NoteCount; i++)
        {
            NoteStep note = chapter.notes[i];
            float beats = Mathf.Max(note.beats, 1f);

            // ฆ้องวง: เล่นโน้ตเดียวกับระนาด แต่ต่ำกว่าหนึ่งคู่แปด
            int khongIndex = Mathf.Clamp(note.keyIndex - 7, 1, 21) - 1;
            if (khongIndex >= 0 && khongIndex < khongWongClips.Length && khongWongClips[khongIndex] != null)
            {
                backingSource.PlayOneShot(khongWongClips[khongIndex], 0.7f);
            }

            // กลอง: ตีเน้นจังหวะแรกของทุก 4 จังหวะ นอกนั้นตีเบา
            AudioClip drum = (beatsSinceDrum % 4 == 0) ? klongStrongClip : klongSoftClip;
            if (drum != null) backingSource.PlayOneShot(drum, 0.5f);
            beatsSinceDrum += Mathf.RoundToInt(beats);

            yield return new WaitForSeconds(beatDuration * beats);
        }
    }
}
