using UnityEngine;

/// <summary>
/// โล่กันดาเมจเฉพาะด้าน — กระสุนที่เข้ามาในมุมที่โล่ครอบอยู่จะถูกกัน
/// บังคับให้ผู้เล่นต้องอ้อมไปยิงด้านหลัง แทนที่จะยืนยิงตรง ๆ ที่เดียว
///
/// ใช้กับ Firewall Turret เป็นหลัก (ธีม: กำแพงไฟร์วอลล์กันของที่เข้ามาจากด้านนอก)
/// EnemyHealth จะเรียกเองอัตโนมัติถ้าเจอ component นี้บน GameObject เดียวกัน
///
/// วิธีติดตั้ง:
/// 1. Attach ที่ GameObject เดียวกับ EnemyHealth
/// 2. ลาก Transform ที่หมุนตามทิศเล็ง (ปกติคือ FirePoint) เข้าช่อง Facing
/// 3. ปรับ Arc Angle เป็นมุมที่โล่ครอบ (140 = กันครึ่งหน้าค่อนข้าง)
/// </summary>
public class DirectionalShield : MonoBehaviour
{
    [Header("Shield")]
    [Tooltip("ทิศที่โล่หันไป — ใช้แกน right ของ Transform นี้ เว้นว่าง = ใช้ตัวเอง")]
    [SerializeField] private Transform facing;
    [Tooltip("มุมรวมที่โล่ครอบ (องศา) — 140 = กันด้านหน้าเป็นส่วนใหญ่, 360 = กันรอบตัว")]
    [Range(0f, 360f)]
    [SerializeField] private float arcAngle = 140f;
    [Tooltip("ดาเมจที่ผ่านโล่เข้ามาได้ (0 = กันหมด, 0.25 = เข้าแค่ 25%)")]
    [Range(0f, 1f)]
    [SerializeField] private float blockedDamageMultiplier = 0f;

    [Header("Feedback")]
    [SerializeField] private GameObject blockEffectPrefab;
    [SerializeField] private SpriteRenderer shieldVisual;
    [SerializeField] private Color flashColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private float flashDuration = 0.12f;

    private Color baseColor;
    private float flashTimer;

    public bool IsActive { get; set; } = true;

    private void Awake()
    {
        if (facing == null) facing = transform;
        if (shieldVisual != null) baseColor = shieldVisual.color;
    }

    private void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && shieldVisual != null) shieldVisual.color = baseColor;
        }
    }

    /// <summary>คืนตัวคูณดาเมจ — 1 = ผ่านเต็ม, 0 = ถูกกันทั้งหมด</summary>
    public float GetDamageMultiplier(Vector2 hitPoint)
    {
        if (!IsActive) return 1f;

        Vector2 toHit = hitPoint - (Vector2)transform.position;
        // ยิงจากจุดเดียวกับตัวเอง (เช่น ดาเมจที่ไม่มีทิศทาง) — ไม่ถือว่าโดนโล่
        if (toHit.sqrMagnitude < 0.0001f) return 1f;

        float angle = Vector2.Angle(facing.right, toHit.normalized);
        if (angle > arcAngle * 0.5f) return 1f;   // เข้าด้านที่ไม่มีโล่

        if (blockEffectPrefab != null) Instantiate(blockEffectPrefab, hitPoint, Quaternion.identity);
        Flash();
        return blockedDamageMultiplier;
    }

    private void Flash()
    {
        if (shieldVisual == null) return;
        shieldVisual.color = flashColor;
        flashTimer = flashDuration;
    }

    private void OnDrawGizmosSelected()
    {
        Transform f = facing != null ? facing : transform;
        Vector3 origin = transform.position;
        float half = arcAngle * 0.5f;

        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.9f);
        for (float a = -half; a <= half; a += 10f)
        {
            Vector3 dir = Quaternion.Euler(0f, 0f, a) * f.right;
            Gizmos.DrawLine(origin, origin + dir * 1.5f);
        }
    }
}
