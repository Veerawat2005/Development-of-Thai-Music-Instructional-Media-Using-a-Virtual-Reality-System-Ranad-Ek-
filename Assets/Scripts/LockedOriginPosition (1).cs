using UnityEngine;

/// <summary>
/// ล็อกตำแหน่ง (X, Z) ของผู้เล่นให้อยู่กับที่เสมอ โดยไม่กระทบการหมุนหัว
/// ใช้สำหรับโหมด "นั่งตีระนาดอยู่กับที่"
///
/// หลักการ: ระบบ tracking ของ VR จะขยับตำแหน่งกล้อง (headTransform) แบบ local
/// สัมพัทธ์กับ XR Origin โดยตรงเสมอ (ไม่ว่าจะเดินจริงในห้องตอนใส่แว่น หรือกด WASD
/// ทดสอบผ่าน XR Device Simulator) การล็อกตำแหน่งของตัว XR Origin เองเฉยๆ จึงไม่พอ
/// สคริปต์นี้จึงวัดว่ากล้องขยับห่างจากจุดยึดไปเท่าไหร่ แล้วดึง XR Origin กลับมาชดเชย
/// ให้พอดีทุกเฟรมแทน ผลคือกล้องจะนิ่งอยู่ตำแหน่งเดิมในโลกเสมอ แต่ยังหมุนมองได้อิสระ
///
/// วิธีใช้: ติดสคริปต์นี้ที่ "XR Origin (XR Rig)" แล้วลาก Main Camera ของมัน
/// (XR Origin > Camera Offset > Main Camera) ใส่ช่อง Head Transform แล้วกด Play
/// </summary>
public class LockedOriginPosition : MonoBehaviour
{
    [Tooltip("ลาก Main Camera (ตัวที่อยู่ใต้ Camera Offset) มาใส่ ถ้าปล่อยว่างจะหาจาก Camera.main ให้อัตโนมัติ")]
    public Transform headTransform;

    [Tooltip("ล็อกความสูง (Y) ด้วยไหม ปกติปล่อยไว้ (ไม่ติ๊ก) เพื่อให้ปรับความสูงผ่าน Camera Offset ได้ตามปกติ")]
    public bool lockHeightToo = false;

    [Tooltip("รัศมี (เมตร) ที่ผู้เล่นขยับหัว/โน้มตัวจากจุดนั่งได้อิสระ เช่น 0.25 = โน้มตัวไปดูปลายระนาดได้ 25 ซม. เกินกว่านี้ถึงจะถูกดึงกลับ ใส่ 0 = ล็อกแน่นเหมือนเดิม")]
    [Min(0f)]
    public float freeLeanRadius = 0.25f;

    private Vector3 anchorWorldPos;

    void Start()
    {
        if (headTransform == null && Camera.main != null) headTransform = Camera.main.transform;

        if (headTransform == null)
        {
            Debug.LogError("[LockedOriginPosition] ไม่พบ Head Transform กรุณาลาก Main Camera ใส่ช่องนี้เอง");
            enabled = false;
            return;
        }

        anchorWorldPos = headTransform.position;
    }

    void LateUpdate()
    {
        if (headTransform == null) return;

        Vector3 head = headTransform.position;
        Vector3 origin = transform.position;

        // กล้องขยับห่างจากจุดยึดไปเท่าไหร่ (เฉพาะแนวราบ)
        Vector3 offset = head - anchorWorldPos;
        offset.y = 0f;

        // ขยับได้อิสระภายใน freeLeanRadius เกินจากนั้นถึงดึง XR Origin กลับมาชดเชยเฉพาะส่วนที่เกิน
        float dist = offset.magnitude;
        if (dist > freeLeanRadius)
        {
            Vector3 excess = offset * ((dist - freeLeanRadius) / dist);
            origin.x -= excess.x;
            origin.z -= excess.z;
        }
        if (lockHeightToo) origin.y += anchorWorldPos.y - head.y;

        transform.position = origin;
    }
}
