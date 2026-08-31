using UnityEngine;

/// <summary>
/// ลำแสงของ Registry Boss — มี 2 ช่วงชัดเจน
/// 1. **Telegraph** เส้นบางกะพริบ บอกแนวที่ลำแสงจะมา (ยังไม่ทำดาเมจ)
/// 2. **Active** ลำแสงเต็มความหนา ทำดาเมจต่อเนื่อง
///
/// ผู้เล่นมีเวลาเท่ากับ telegraph ในการหลบออกจากแนว
///
/// วิธีติดตั้ง (Prefab):
/// 1. GameObject ว่าง — Attach LaserBeam + BoxCollider2D (Is Trigger ✓) + Rigidbody2D (Kinematic)
/// 2. child "Visual" ใส่ SpriteRenderer สี่เหลี่ยมสีขาว (sprite ธรรมดา จะถูกยืดเอง)
/// 3. ลาก "Visual" เข้าช่อง Beam Visual และลาก BoxCollider2D เข้าช่อง Beam Collider
/// </summary>
public class LaserBeam : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform beamVisual;
    [SerializeField] private BoxCollider2D beamCollider;
    [SerializeField] private SpriteRenderer beamRenderer;

    [Header("Damage")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float damageCooldown = 0.4f;
    [SerializeField] private string sourceDisplayName = "Registry";

    [Header("Visual")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color activeColor = new Color(0.5f, 0.9f, 1f, 1f);
    [Tooltip("ความหนาตอน telegraph เทียบกับตอนยิงจริง")]
    [SerializeField] private float telegraphWidthRatio = 0.15f;
    [SerializeField] private float telegraphBlinkSpeed = 12f;

    private float length = 20f;
    private float width = 0.6f;
    private float telegraphDuration = 0.8f;
    private float activeDuration = 1.2f;

    private float timer;
    private float damageTimer;
    private bool isActive;

    /// <summary>
    /// ยิงลำแสงจากจุด origin ไปตามมุมที่กำหนด
    /// </summary>
    /// <param name="fromCenter">true = ลำแสงยืดออกทั้งสองข้างจาก origin (ใช้กับลำแสงพาดจอ)</param>
    public void Initialize(Vector2 origin, float angleDegrees, float length, float width,
                           float telegraphDuration, float activeDuration, float damageOverride = -1f,
                           bool fromCenter = false)
    {
        this.length = length;
        this.width = width;
        this.telegraphDuration = telegraphDuration;
        this.activeDuration = activeDuration;
        if (damageOverride > 0f) damage = damageOverride;

        transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);

        Vector2 dir = new Vector2(Mathf.Cos(angleDegrees * Mathf.Deg2Rad), Mathf.Sin(angleDegrees * Mathf.Deg2Rad));
        transform.position = fromCenter ? origin : origin + dir * (length * 0.5f);

        ApplySize(width * telegraphWidthRatio);
        if (beamCollider != null) beamCollider.enabled = false;
        if (beamRenderer != null) beamRenderer.color = telegraphColor;

        timer = 0f;
        isActive = false;
    }

    private void ApplySize(float currentWidth)
    {
        if (beamVisual != null) beamVisual.localScale = new Vector3(length, currentWidth, 1f);
        if (beamCollider != null) beamCollider.size = new Vector2(length, currentWidth);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (damageTimer > 0f) damageTimer -= Time.deltaTime;

        if (!isActive)
        {
            // ช่วงเตือน — กะพริบเส้นบาง
            if (beamRenderer != null)
            {
                Color c = telegraphColor;
                c.a = telegraphColor.a * (0.5f + 0.5f * Mathf.Sin(timer * telegraphBlinkSpeed));
                beamRenderer.color = c;
            }

            if (timer >= telegraphDuration) Activate();
        }
        else
        {
            if (timer >= telegraphDuration + activeDuration) Destroy(gameObject);
        }
    }

    private void Activate()
    {
        isActive = true;
        ApplySize(width);
        if (beamCollider != null) beamCollider.enabled = true;
        if (beamRenderer != null) beamRenderer.color = activeColor;
    }

    private void OnTriggerEnter2D(Collider2D other) => HandleContact(other);
    private void OnTriggerStay2D(Collider2D other) => HandleContact(other);

    private void HandleContact(Collider2D other)
    {
        if (!isActive || damageTimer > 0f) return;
        if (!other.CompareTag("Player")) return;

        var controller = other.GetComponent<PlayerController>();
        if (controller != null && controller.IsInvincible) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        float resistance = BulletPatternMemory.Instance != null
            ? BulletPatternMemory.Instance.GetResistance(BulletPatternType.LaserBeam)
            : 0f;

        health.TakeDamage(damage * (1f - resistance), BulletPatternType.LaserBeam, sourceDisplayName);
        damageTimer = damageCooldown;
    }
}
