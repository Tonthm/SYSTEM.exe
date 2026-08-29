using UnityEngine;

/// <summary>
/// HP ของศัตรู เมื่อตายจะให้ XP แก่ผู้เล่นและทำลายตัวเอง
///
/// [อัปเดต] ของที่ระบบอื่นต้องใช้:
/// - OnHealthChanged / HealthPercent : ให้ NullExeBoss เปลี่ยนเฟสตาม HP
/// - OnDeath                          : ให้บอสแจ้งชนะ / ให้ WaveManager นับศัตรูที่เหลือ
/// - OnAnyEnemyKilled (static)        : ให้ TutorialSectorController นับจำนวนที่กำจัด
/// - ApplyScaling()                   : WaveManager ปรับความยาก/XP ต่อด่าน
/// - ApplyStalePenalty()              : ศัตรูค้างข้าม wave ให้ XP น้อยลง
/// - TakeDamage(amount, hitPoint)     : ให้ DirectionalShield กันดาเมจจากทิศที่ป้องกันได้
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private int xpReward = 10;
    [Tooltip("ทำลาย GameObject ทันทีที่ตาย (บอสควรปิดไว้ เพื่อเล่นเอฟเฟกต์/บทพูดก่อน)")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 0f;

    [Header("Shield (ไม่บังคับ — ใช้กับ Firewall Turret)")]
    [Tooltip("โล่กันดาเมจตามทิศทาง — เว้นว่างจะหาบน GameObject นี้เอง")]
    [SerializeField] private DirectionalShield shield;

    [Header("Stale (ค้างข้าม wave)")]
    [Tooltip("ตัวบอกภาพว่าศัตรูตัวนี้ค้างข้าม wave มาแล้ว (ไอคอน/สีจาง) — ไม่บังคับ")]
    [SerializeField] private GameObject staleIndicator;
    [SerializeField] private SpriteRenderer[] tintTargets;
    [SerializeField] private Color staleTint = new Color(0.6f, 0.6f, 0.7f, 1f);

    private float currentHealth;
    private bool isDead;
    private float xpMultiplier = 1f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    public bool IsDead => isDead;

    /// <summary>ศัตรูตัวนี้ค้างข้าม wave มาแล้วหรือยัง (XP ถูกลด)</summary>
    public bool IsStale { get; private set; }
    /// <summary>XP จริงที่จะได้ถ้าฆ่าตอนนี้</summary>
    public int EffectiveXpReward => Mathf.Max(0, Mathf.RoundToInt(xpReward * xpMultiplier));

    public System.Action<float, float> OnHealthChanged; // (current, max)
    public System.Action OnDeath;
    /// <summary>ยิงตอนดาเมจถูกโล่กันไว้ (ให้เอฟเฟกต์ประกายไปฟัง)</summary>
    public System.Action<Vector2> OnDamageBlocked;

    /// <summary>ยิงทุกครั้งที่ศัตรูตัวใดก็ตามถูกกำจัด (ใช้กับ tutorial / quest / คอมโบ)</summary>
    public static System.Action<EnemyHealth> OnAnyEnemyKilled;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (shield == null) shield = GetComponent<DirectionalShield>();
        if (staleIndicator != null) staleIndicator.SetActive(false);
    }

    /// <summary>ให้สคริปต์อื่น (เช่น NullExeBoss) สั่งไม่ให้ทำลายตัวเองทันทีที่ตาย</summary>
    public void SetDestroyOnDeath(bool value, float delay = 0f)
    {
        destroyOnDeath = value;
        destroyDelay = delay;
    }

    /// <summary>
    /// WaveManager เรียกทันทีหลัง Instantiate — ปรับความยาก/XP ต่อด่าน
    /// ทำให้ใช้ prefab ศัตรูชุดเดียวกันได้ทั้ง 6 ด่าน แค่ตั้งตัวคูณต่างกัน
    /// </summary>
    public void ApplyScaling(float healthMultiplier, float rewardMultiplier)
    {
        maxHealth = Mathf.Max(1f, maxHealth * healthMultiplier);
        currentHealth = maxHealth;
        xpMultiplier *= rewardMultiplier;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// ตีตราว่าศัตรูตัวนี้ค้างข้าม wave — XP ที่ได้จะลดลงตามตัวคูณ
    /// stack = false : ลดครั้งเดียวไม่ว่าค้างกี่ wave
    /// stack = true  : ค้างซ้ำโดนลดซ้ำ (0.5 -> 0.25 -> ...)
    /// </summary>
    public void ApplyStalePenalty(float multiplier, bool stack)
    {
        if (isDead) return;
        if (IsStale && !stack) return;

        xpMultiplier *= multiplier;
        IsStale = true;

        if (staleIndicator != null) staleIndicator.SetActive(true);
        if (tintTargets != null)
        {
            foreach (var sr in tintTargets)
            {
                if (sr != null) sr.color = staleTint;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position);
    }

    /// <param name="hitPoint">ตำแหน่งที่โดนยิง — ใช้เช็คว่าเข้าด้านที่มีโล่หรือไม่</param>
    public void TakeDamage(float amount, Vector2 hitPoint)
    {
        if (isDead) return;

        if (shield != null)
        {
            float multiplier = shield.GetDamageMultiplier(hitPoint);
            if (multiplier < 1f)
            {
                OnDamageBlocked?.Invoke(hitPoint);
                amount *= multiplier;
                if (amount <= 0f) return;   // กันได้ทั้งหมด ไม่ต้องคิดต่อ
            }
        }

        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        currentHealth = 0f;

        XPManager.Instance?.AddXP(EffectiveXpReward);

        OnDeath?.Invoke();
        OnAnyEnemyKilled?.Invoke(this);

        if (destroyOnDeath) Destroy(gameObject, destroyDelay);
    }
}
