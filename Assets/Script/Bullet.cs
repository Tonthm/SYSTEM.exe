using UnityEngine;

/// <summary>
/// พฤติกรรมกระสุนพื้นฐาน ใช้ได้ทั้งกระสุนผู้เล่นและศัตรู (แยกด้วย isPlayerBullet)
/// กระสุนศัตรูจะพก BulletPatternType + ชื่อเจ้าของติดไปด้วย
/// เพื่อให้ Death Log / Bullet Pattern Memory รู้ว่าผู้เล่นตายจากอะไร
///
/// [อัปเดต] ส่งตำแหน่งจุดชนไปให้ EnemyHealth ด้วย (ใช้กับ DirectionalShield ของ Firewall Turret)
///
/// วิธีติดตั้งใน Unity (ทำเป็น Prefab):
/// 1. สร้าง GameObject → child ชื่อ "Visual" ใส่ SpriteRenderer (ดูข้อ 11 เรื่อง Glitch Zone)
/// 2. Add Component: Rigidbody2D (Gravity Scale = 0)
/// 3. Add Component: Collider2D ติ๊ก "Is Trigger"
/// 4. Attach สคริปต์นี้ แล้วลาก GameObject ลงเป็น Prefab
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private bool isPlayerBullet;
    private BulletPatternType patternType;
    private string sourceName;

    private Rigidbody2D rb;
    private float timer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    /// <summary>ใช้สำหรับกระสุนผู้เล่น (ไม่ต้องมี pattern type)</summary>
    public void Initialize(Vector2 direction, float speed, float damage, float lifetime, bool isPlayerBullet)
    {
        Initialize(direction, speed, damage, lifetime, isPlayerBullet, BulletPatternType.Aimed, null);
    }

    /// <summary>ใช้สำหรับกระสุนศัตรู ต้องระบุ pattern type</summary>
    public void Initialize(Vector2 direction, float speed, float damage, float lifetime, bool isPlayerBullet, BulletPatternType patternType)
    {
        Initialize(direction, speed, damage, lifetime, isPlayerBullet, patternType, null);
    }

    /// <summary>เวอร์ชันเต็ม — ระบุชื่อเจ้าของกระสุนให้ Death Log แสดงผลได้</summary>
    public void Initialize(Vector2 direction, float speed, float damage, float lifetime, bool isPlayerBullet, BulletPatternType patternType, string sourceName)
    {
        this.direction = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        this.isPlayerBullet = isPlayerBullet;
        this.patternType = patternType;
        this.sourceName = sourceName;
        timer = 0f;

        rb.linearVelocity = this.direction * this.speed;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPlayerBullet)
        {
            if (other.CompareTag("Enemy"))
            {
                var health = other.GetComponent<EnemyHealth>();
                // ส่งตำแหน่งกระสุนไปด้วย เผื่อศัตรูมีโล่กันด้านหน้า (DirectionalShield)
                if (health != null) health.TakeDamage(damage, transform.position);
                Destroy(gameObject);
            }
        }
        else
        {
            if (other.CompareTag("Player"))
            {
                var playerController = other.GetComponent<PlayerController>();
                bool invincible = playerController != null && playerController.IsInvincible;

                if (!invincible)
                {
                    var health = other.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        // Bullet Pattern Memory ลดดาเมจถ้าผู้เล่นเคยตายจากแพทเทิร์นนี้มาก่อน
                        float resistance = BulletPatternMemory.Instance != null
                            ? BulletPatternMemory.Instance.GetResistance(patternType)
                            : 0f;
                        float finalDamage = damage * (1f - resistance);
                        health.TakeDamage(finalDamage, patternType, sourceName);
                    }
                }
                Destroy(gameObject);
            }
        }

        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
