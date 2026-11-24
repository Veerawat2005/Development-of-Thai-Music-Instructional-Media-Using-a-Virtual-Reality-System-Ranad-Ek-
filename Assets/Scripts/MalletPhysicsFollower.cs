using UnityEngine;

/// <summary>
/// ติดสคริปต์นี้ที่ตัวโมเดลไม้ตี (ไม่ใช่ Anchor)
/// ไม้ตีจะ "ไล่ตาม" ตำแหน่งมือ (target) ด้วยแรง physics จริง
/// แทนที่จะบังคับตำแหน่งตรงๆ แบบ Kinematic
/// เมื่อไปชนลูกระนาดที่เป็นของแข็ง จะถูกต้านไว้จริง ไม่ทะลุผ่าน
///
/// วิธีติดตั้ง:
/// 1. เอาไม้ตีออกจากการเป็นลูกของ Controller/Anchor (ลากออกมาเป็น object อิสระใน Hierarchy)
/// 2. ที่ไม้ตี: Rigidbody -> Is Kinematic = ปิด, Use Gravity = ปิด,
///    Collision Detection = Continuous Dynamic, Interpolate = Interpolate
/// 3. Collider ของไม้ตี -> Is Trigger = ปิด (ให้เป็น solid collider)
/// 4. ลาก Anchor (MalletAnchor_R / MalletAnchor_L ที่ติดมือ) ใส่ช่อง Target ด้านล่าง
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MalletPhysicsFollower : MonoBehaviour
{
    [Tooltip("จุดเป้าหมายที่ไม้ตีจะไล่ตาม เช่น MalletAnchor_R ที่ติดกับมือ")]
    public Transform target;

    [Header("ความหนึบในการไล่ตามตำแหน่ง")]
    [Tooltip("ยิ่งสูงยิ่งไล่ตามเร็ว/แข็ง แต่ถ้าสูงเกินไปจะสั่น")]
    public float followStrength = 40f;
    [Tooltip("ความเร็วสูงสุดที่ไม้ตีไล่ตามได้ (กันหลุดเร็วเกินจริงตอนขยับมือแรงๆ)")]
    public float maxFollowSpeed = 12f;
    [Range(0f, 1f)]
    [Tooltip("ความนุ่มนวล ยิ่งค่าน้อยยิ่งลื่น ยิ่งค่ามากยิ่งกระตุก")]
    public float velocitySmoothing = 0.35f;

    [Header("การหมุนตามมือ")]
    public float rotationSpeed = 25f;

    [Header("ไม้ตีอันนี้ถือด้วยมือไหน")]
    [Tooltip("Unknown = ให้เดาจากชื่อ GameObject/Target (มีคำว่า Left/_L หรือ Right/_R) ใช้ตัดสินโน้ตคู่ 8 ว่าตีถูกมือหรือไม่")]
    public MalletHand hand = MalletHand.Unknown;

    private Rigidbody rb;

    /// <summary>มือที่ใช้จริง ถ้าไม่ได้ตั้งไว้จะเดาจากชื่อ</summary>
    public MalletHand ResolvedHand
    {
        get
        {
            if (hand != MalletHand.Unknown) return hand;
            MalletHand guess = GuessFromName(gameObject.name);
            if (guess == MalletHand.Unknown && target != null) guess = GuessFromName(target.name);
            return guess;
        }
    }

    private static MalletHand GuessFromName(string n)
    {
        string l = n.ToLowerInvariant();
        if (l.Contains("left") || l.EndsWith("_l")) return MalletHand.Left;
        if (l.Contains("right") || l.EndsWith("_r")) return MalletHand.Right;
        return MalletHand.Unknown;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        if (target == null) return;

        // ไล่ตามตำแหน่ง (position) ด้วยแรงแบบ spring
        Vector3 positionError = target.position - rb.position;
        Vector3 desiredVelocity = Vector3.ClampMagnitude(positionError * followStrength, maxFollowSpeed);
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, 1f - velocitySmoothing);

        // ไล่ตามการหมุน (rotation)
        Quaternion rotationError = target.rotation * Quaternion.Inverse(rb.rotation);
        rotationError.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        if (Mathf.Abs(angle) > 0.01f)
        {
            Vector3 angularVelocity = (angle * Mathf.Deg2Rad * rotationSpeed) * axis.normalized;
            rb.angularVelocity = angularVelocity;
        }
    }
}