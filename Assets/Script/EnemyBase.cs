using UnityEngine;

/// <summary>
/// ศัตรูพื้นฐาน: เดินเข้าหาผู้เล่นในระยะที่กำหนด แล้วสั่งให้ EnemyBulletEmitter ยิง
/// ใช้เป็นฐานสำหรับศัตรูแบบต่าง ๆ เช่น Pop-up Swarmer, Cursor Chaser, Firewall Turret
/// (ปรับ chaseSpeed = 0 และ attackRange ให้ครอบคลุมทั้งฉาก จะได้ป้อมยิงอยู่กับที่แบบ Turret)
///
/// วิธีติดตั้งใน Unity:
/// 1. สร้าง Enemy GameObject ใส่ Sprite, Rigidbody2D (Gravity Scale = 0), Collider2D
/// 2. ตั้ง Tag = "Enemy"
/// 3. Attach EnemyBase, EnemyHealth, EnemyBulletEmitter
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 2f;
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float keepDistance = 3f; // ระยะที่หยุดเดินเข้าหา ไม่ใช่ประชิดตัวผู้เล่น

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private EnemyBulletEmitter bulletEmitter;

    private Transform playerTransform;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bulletEmitter == null) bulletEmitter = GetComponent<EnemyBulletEmitter>();
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void FixedUpdate()
    {
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance > keepDistance && distance <= attackRange)
        {
            Vector2 dir = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = dir * chaseSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (distance <= attackRange && bulletEmitter != null)
        {
            bulletEmitter.TryFireAt(playerTransform.position);
        }
    }
}
