using UnityEngine;

/// <summary>
/// ดาเมจจากการชนตัวศัตรูโดยตรง (ไม่ใช่กระสุน)
/// ใช้กับ Pop-up Swarmer และ Cursor Chaser ที่ฆ่าผู้เล่นด้วยการพุ่งชน
///
/// เคารพกติกาเดียวกับกระสุนทุกอย่าง: i-frame ตอน Dash, Bullet Pattern Memory,
/// และส่งชื่อตัวเองเข้า Death Log ("Cause of death: Pop-up Swarmer collision")
///
/// วิธีติดตั้ง: Attach ที่ prefab ศัตรู — รองรับทั้ง Collider ธรรมดาและ Is Trigger
/// </summary>
public class ContactDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 15f;
    [Tooltip("เว้นกี่วินาทีก่อนทำดาเมจซ้ำได้ (กันดูดเลือดรัวตอนติดตัวผู้เล่น)")]
    [SerializeField] private float damageCooldown = 0.8f;
    [Tooltip("ชื่อที่จะโผล่ใน Death Log")]
    [SerializeField] private string sourceDisplayName = "Unknown Process";

    [Header("Kamikaze")]
    [Tooltip("ชนแล้วตัวเองตายด้วย (ศัตรูพลีชีพ)")]
    [SerializeField] private bool dieOnContact = false;

    [Header("Feedback")]
    [SerializeField] private GameObject hitEffectPrefab;

    private float cooldownTimer;

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.collider);
    private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);
    private void OnTriggerEnter2D(Collider2D other) => TryDamage(other);
    private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

    private void TryDamage(Collider2D other)
    {
        if (cooldownTimer > 0f) return;
        if (other == null || !other.CompareTag("Player")) return;

        // เคารพ i-frame ตอน Dash เหมือนกระสุน
        var controller = other.GetComponent<PlayerController>();
        if (controller != null && controller.IsInvincible) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        float resistance = BulletPatternMemory.Instance != null
            ? BulletPatternMemory.Instance.GetResistance(BulletPatternType.Collision)
            : 0f;

        health.TakeDamage(damage * (1f - resistance), BulletPatternType.Collision, sourceDisplayName);
        cooldownTimer = damageCooldown;

        if (hitEffectPrefab != null) Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        if (dieOnContact)
        {
            // ใช้ EnemyHealth ให้ตายตามระบบปกติ (WaveManager จะได้นับถูก)
            var self = GetComponent<EnemyHealth>();
            if (self != null) self.TakeDamage(self.MaxHealth * 10f);
            else Destroy(gameObject);
        }
    }
}
