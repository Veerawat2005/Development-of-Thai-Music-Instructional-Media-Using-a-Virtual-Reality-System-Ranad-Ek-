using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ตัวควบคุมเมนู "Exercises" (runtime) — ผูกกับปุ่ม/toggle ที่ ExercisesMenuUIBuilder สร้างไว้
/// ไม่ต้องแก้ไฟล์นี้เพื่อเพิ่มบทเรียนใหม่ แค่สร้าง ExerciseChapter asset เพิ่ม แล้วรัน
/// Tools > VR Ranad Ek > Build Exercises Menu UI ใหม่อีกครั้ง
/// </summary>
public class ExercisesMenuController : MonoBehaviour
{
    [Header("ข้อมูลบทเรียนทั้งหมด (เรียงตามลำดับที่จะโชว์)")]
    public List<ExerciseChapter> chapters = new List<ExerciseChapter>();

    [Header("อ้างอิงระบบฝึก")]
    public ExerciseTrainer trainer;

    [Header("UI: รายการ Chapter (ปุ่มถูกสร้างเป็นลูกของอันนี้)")]
    public Transform chapterListContainer;

    [Header("UI: รายละเอียด Chapter ที่เลือก")]
    public Text chapterTitleText;
    public Text chapterDescriptionText;
    public Text chapterNotesText;

    [Header("UI: ปุ่มควบคุม")]
    public Button previewButton;
    public Button trainButton;
    public Button playButton;

    [Header("UI: Toggle ตัวช่วยฝึก")]
    [Tooltip("ผูกกับ ExerciseTrainer.highlightEnabled จริง")]
    public Toggle highlightBarsToggle;
    [Tooltip("สำรองไว้สำหรับฟีเจอร์ในอนาคต ยังไม่ผูก logic จริง")]
    public Toggle keysAssistToggle;
    [Tooltip("สำรองไว้สำหรับฟีเจอร์ในอนาคต (feedback ระยะห่างมือ) ยังไม่ผูก logic จริง")]
    public Toggle distanceToggle;
    [Tooltip("สำรองไว้สำหรับฟีเจอร์ในอนาคต (feedback มุมไม้ตี) ยังไม่ผูก logic จริง")]
    public Toggle angleToggle;

    private int selectedIndex = -1;

    void Start()
    {
        WireChapterButtons();
        WireActionButtons();
        WireToggles();

        if (chapters.Count > 0) SelectChapter(0);
    }

    private void WireChapterButtons()
    {
        if (chapterListContainer == null) return;

        // สำคัญ: ไล่ดูลูกทุกตัว ไม่จำกัดจำนวนรอบด้วย chapters.Count
        // เพราะ chapterListContainer อาจมีลูกอื่นที่ไม่ใช่ปุ่มปนอยู่ก่อนปุ่มจริง
        // (เช่น Border_Top, Border_Bottom, Border_Left, Border_Right ที่ ExercisesMenuUIBuilder สร้างไว้)
        // นับเฉพาะลูกที่มี component Button จริงๆ เรียงตามลำดับที่เจอ จับคู่กับ chapters ตามลำดับนั้น
        int chapterIndex = 0;
        for (int i = 0; i < chapterListContainer.childCount; i++)
        {
            Button btn = chapterListContainer.GetChild(i).GetComponent<Button>();
            if (btn == null) continue; // ไม่ใช่ปุ่ม (เช่น Border_*) ข้ามไป ไม่นับโควต้า

            if (chapterIndex >= chapters.Count) break; // ปุ่มเกินกว่าจำนวน Chapter ที่มีจริง หยุดได้เลย

            int capturedIndex = chapterIndex; // กัน closure จับตัวแปรตัวเดียวกันผิด
            btn.onClick.AddListener(() => SelectChapter(capturedIndex));
            chapterIndex++;
        }
    }

    private void WireActionButtons()
    {
        if (previewButton != null) previewButton.onClick.AddListener(() => trainer?.PreviewChapter());
        if (trainButton != null) trainButton.onClick.AddListener(() => trainer?.StartTraining());
        if (playButton != null) playButton.onClick.AddListener(() => trainer?.PlayChapter());
    }

    private void WireToggles()
    {
        if (highlightBarsToggle != null)
        {
            highlightBarsToggle.onValueChanged.AddListener(isOn =>
            {
                if (trainer != null) trainer.highlightEnabled = isOn;
            });
        }
        // keysAssistToggle / distanceToggle / angleToggle: จุดต่อยอดในอนาคต
        // ตอนนี้แค่เก็บสถานะไว้เฉยๆ ยังไม่มีผลต่อเกมเพลย์
    }

    /// <summary>เลือก Chapter ตามลำดับ index แล้วอัปเดตข้อความรายละเอียดในแผงกลาง</summary>
    public void SelectChapter(int index)
    {
        if (index < 0 || index >= chapters.Count) return;

        selectedIndex = index;
        ExerciseChapter selected = chapters[index];

        if (trainer != null) trainer.chapter = selected;

        if (chapterTitleText != null) chapterTitleText.text = selected.chapterTitle;
        if (chapterDescriptionText != null) chapterDescriptionText.text = selected.description;
        if (chapterNotesText != null) chapterNotesText.text = BuildNotesPreviewText(selected);
    }

    /// <summary>จัดกลุ่มโน้ตทีละ 3 ตัวต่อบรรทัด เช่น "Sol - Sol - Sol" ให้อ่านง่ายเหมือนโน้ตเพลงจริง</summary>
    private string BuildNotesPreviewText(ExerciseChapter chapter)
    {
        StringBuilder sb = new StringBuilder();
        const int groupSize = 3;

        for (int i = 0; i < chapter.NoteCount; i += groupSize)
        {
            var group = chapter.notes.Skip(i).Take(groupSize).Select(n => n.displayName);
            sb.AppendLine(string.Join(" - ", group));
        }

        return sb.ToString();
    }
}