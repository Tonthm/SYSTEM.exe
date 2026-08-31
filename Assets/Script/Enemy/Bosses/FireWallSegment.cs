using UnityEngine;

/// <summary>
/// ชิ้นส่วนกำแพงไฟที่พ่นออกมาจากหัวฉีดของ Firewall Boss
///
/// คุณสมบัติสำคัญ 2 อย่างที่ทำให้กลไกด่านนี้ทำงาน:
/// 1. ชนผู้เล่น = ดาเมจ (แต่ dash ผ่านได้เพราะช่วง dash ผู้เล่นอมตะ)
/// 2. **เผากระสุนผู้เล่นทิ้ง** — ยืนยิงจากด้านล่างกำแพงจึงไร้ประโยชน์
///    ผู้เล่นต้อง dash ข้ามไปอยู่ฝั่งเดียวกับบอสก่อนถึงจะยิงโดน
///
/// วิธีติดตั้ง (Prefab):
/// 1. GameObject + BoxCollider2D (Is Trigger ✓) ขนาดเท่าเปลวไฟ 1 ช่อง
/// 2. Rigidbody2D → Body Type = Kinematic (จำเป็นสำหรับ trigger กับกระสุน)
/// 3. child "Visual" ใส่ SpriteRenderer เปลวไฟ
/// 4. Attach สคริปต์นี้
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FireWallSegment : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private string sourceDisplayName = "Firewall";

    [Header("Bullet Blocking")]
    [Tooltip("เผากระสุนผู้เล่นที่พุ่งเข้ามา — หัวใจของกลไกด่านนี้")]
    [SerializeField] private bool burnPlayerBullets = true;
    [SerializeField] private GameObject burnEffectPrefab;

    private Vector2 direction = Vector2.down;
    private float speed;
    private float lifetime;
    private float timer;
    private float damageTimer;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Initialize(Vector2 direction, float speed, float lifetime, float damageOverride = -1f)
    {
        this.direction = direction.normalized;
        this.speed = speed;
        this.lifetime = lifetime;
        if (damageOverride > 0f) damage = damageOverride;
        timer = 0f;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (damageTimer > 0f) damageTimer -= Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other) => HandleContact(other);
    private void OnTriggerStay2D(Collider2D other) => HandleContact(other);

    private void HandleContact(Collider2D other)
    {
        // เผากระสุนผู้เล่น
        if (burnPlayerBullets)
        {
            var bullet = other.GetComponent<Bullet>();
            if (bullet != null && bullet.IsPlayerBullet)
            {
                if (burnEffectPrefab != null) Instantiate(burnEffectPrefab, other.transform.position, Quaternion.identity);
                Destroy(other.gameObject);
                return;
            }
        }

        if (!other.CompareTag("Player")) return;
        if (damageTimer > 0f) return;

        // dash ผ่านได้ — ช่วง dash ผู้เล่นอมตะ
        var controller = other.GetComponent<PlayerController>();
        if (controller != null && controller.IsInvincible) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        float resistance = BulletPatternMemory.Instance != null
            ? BulletPatternMemory.Instance.GetResistance(BulletPatternType.WallSweep)
            : 0f;

        health.TakeDamage(damage * (1f - resistance), BulletPatternType.WallSweep, sourceDisplayName);
        damageTimer = damageCooldown;
    }
}
