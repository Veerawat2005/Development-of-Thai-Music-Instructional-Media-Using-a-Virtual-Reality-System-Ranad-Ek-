using UnityEngine;

/// <summary>
/// ติดสคริปต์นี้ที่โมเดลไม้ตี (ทั้งซ้ายและขวา)
/// คำนวณความเร็วของไม้ตีในแต่ละเฟรม เพื่อนำไปใช้เป็น "แรงตี" (velocity)
/// ใช้ได้แม้ Rigidbody จะเป็น Kinematic เพราะไม่ได้พึ่งพา physics engine
/// ต้องมี Collider (แนะนำ Sphere Collider, ติ๊ก Is Trigger) ที่หัวไม้ตี
/// </summary>
public class MalletVelocityTracker : MonoBehaviour
{
    public Vector3 Velocity { get; private set; }

    private Vector3 previousPosition;

    void Start()
    {
        previousPosition = transform.position;
    }

    void FixedUpdate()
    {
        Vector3 currentPosition = transform.position;
        Velocity = (currentPosition - previousPosition) / Time.fixedDeltaTime;
        previousPosition = currentPosition;
    }
}
