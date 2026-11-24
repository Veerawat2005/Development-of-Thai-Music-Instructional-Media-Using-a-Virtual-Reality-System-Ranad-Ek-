using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ติดสคริปต์นี้ที่ลูกระนาดแต่ละลูก (21 ลูก)
/// ต้องมี Box Collider (ปกติมาพร้อม Cube primitive, isTrigger = false) และ AudioSource
/// ตรวจจับการชนกับไม้ตี (ต้องมี Tag "Mallet" และมี Rigidbody ที่ไม่ kinematic)
/// แล้วเล่นเสียงโดยปรับความดังตามความเร็วสัมพัทธ์ตอนชน (แรงตี)
///
/// หมายเหตุ: ใช้ OnCollisionEnter (ไม่ใช่ OnTriggerEnter) เพราะ RanadEkGameBuilder
/// ตั้งค่า Collider ของทั้งลูกระนาดและหัวไม้ตีเป็น isTrigger = false (solid collider)
/// เพื่อให้ไม้ตีมี physical bounce จริงตอนกระทบลูกระนาด
///
/// เพิ่มระบบไฮไลต์สี (สำหรับโหมดฝึก Train) และ static event OnKeyHit
/// ที่ตัวควบคุมโหมดฝึก (เช่น ExerciseTrainer) subscribe ไปใช้เช็คว่าผู้เล่นตีลูกที่ถูกไฮไลต์ไว้หรือเปล่า
///
/// PlayPreviewSound() ใช้สำหรับปุ่ม "Preview" ใน UI เล่นเสียงโดยตรงโดยไม่ต้องตีจริง
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RanadKey : MonoBehaviour
{
    [Header("เสียงของลูกระนาดลูกนี้")]
    public AudioClip noteSound;

    [Header("ค่าความเร็วขั้นต่ำ-สูงสุด (ปรับจูนตามจริงหลังทดสอบ)")]
    [Tooltip("ต่ำกว่านี้ถือว่าแตะเบา ไม่นับเป็นการตี")]
    public float minHitSpeed = 0.3f;
    [Tooltip("ความเร็วที่ถือว่าตีแรงสุด (volume = 1)")]
    public float maxHitSpeed = 4f;

    [Header("กันเสียงซ้อน")]
    [Tooltip("เวลาหน่วง (วินาที) ก่อนจะให้ลูกนี้ตีซ้ำได้อีกครั้ง")]
    public float cooldownSeconds = 0.08f;

    [Header("สีตอนไฮไลต์ (โหมดฝึก)")]
    [Tooltip("สีที่ลูกนี้จะเปลี่ยนเป็นตอนถูกไฮไลต์ว่าต้องตีต่อไป")]
    public Color highlightColor = Color.yellow;

    /// <summary>
    /// แจ้งทุกครั้งที่ลูกระนาดลูกใดก็ตามถูกตีสำเร็จ (เสียงดังจริง ไม่ใช่แตะเบา)
    /// ส่งตัวเอง (this) กับความเร็วตอนชนไปด้วย ให้ตัวควบคุมโหมดฝึกเอาไปเช็คได้ว่าตรงกับลูกที่ไฮไลต์ไว้หรือไม่
    /// </summary>
    public static event Action<RanadKey, float> OnKeyHit;

    /// <summary>
    /// เหมือน OnKeyHit แต่บอกด้วยว่าไม้ตีมือไหนเป็นคนตี (ใช้กับโน้ตคู่ 8 ที่ต้องแยกมือ)
    /// ถ้าหามือไม่ได้ จะส่ง MalletHand.Unknown
    /// </summary>
    public static event Action<RanadKey, float, MalletHand> OnKeyHitByHand;

    private AudioSource audioSource;
    private Renderer keyRenderer;
    private Color originalColor;
    private bool canTrigger = true;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        keyRenderer = GetComponent<Renderer>();
        if (keyRenderer != null)
        {
            originalColor = keyRenderer.material.color;
        }
    }

    /// <summary>เปิด/ปิดไฮไลต์สีของลูกนี้ เรียกจากตัวควบคุมโหมดฝึกภายนอก</summary>
    public void SetHighlight(bool isHighlighted)
    {
        if (keyRenderer == null) return;
        keyRenderer.material.color = isHighlighted ? highlightColor : originalColor;
    }

    /// <summary>เล่นเสียงของลูกนี้ตรงๆ โดยไม่ต้องมีการชนจริง ใช้กับปุ่ม "Preview" ใน UI</summary>
    public void PlayPreviewSound()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (noteSound == null)
        {
            Debug.LogWarning($"[RanadKey] {gameObject.name} ไม่มี noteSound เลยเล่น Preview ไม่ได้");
            return;
        }
        audioSource.pitch = 1f;
        audioSource.volume = 1f;
        audioSource.PlayOneShot(noteSound);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!canTrigger) return;
        if (!collision.collider.CompareTag("Mallet")) return;

        float speed = collision.relativeVelocity.magnitude;
        if (speed < minHitSpeed) return; // แตะเบาเกินไป ไม่นับเป็นการตี

        if (noteSound == null)
        {
            Debug.LogWarning($"[RanadKey] {gameObject.name} ไม่มี noteSound ที่กำหนดไว้ เลยเล่นเสียงไม่ได้");
            return;
        }

        float volume = Mathf.Clamp01((speed - minHitSpeed) / (maxHitSpeed - minHitSpeed));
        audioSource.volume = volume;
        audioSource.pitch = UnityEngine.Random.Range(0.98f, 1.02f); // สุ่มเพี้ยนนิดหน่อยให้เสียงเป็นธรรมชาติ
        audioSource.PlayOneShot(noteSound);

        OnKeyHit?.Invoke(this, speed);

        MalletPhysicsFollower follower = collision.collider.GetComponentInParent<MalletPhysicsFollower>();
        MalletHand hand = follower != null ? follower.ResolvedHand : MalletHand.Unknown;
        OnKeyHitByHand?.Invoke(this, speed, hand);

        StartCoroutine(TriggerCooldown());

        // Debug: เปิดบรรทัดล่างนี้เพื่อดูค่าความเร็วจริงตอนตี แล้วเอาไปปรับ minHitSpeed/maxHitSpeed
        // Debug.Log($"{gameObject.name} hit speed: {speed:F2}");
    }

    IEnumerator TriggerCooldown()
    {
        canTrigger = false;
        yield return new WaitForSeconds(cooldownSeconds);
        canTrigger = true;
    }
}