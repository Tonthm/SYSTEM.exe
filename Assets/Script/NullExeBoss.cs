using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// บอสสุดท้าย NULL.exe — เปลี่ยนเฟสการยิงตาม HP ที่เหลือ และเป็นตัวกำหนด "เงื่อนไขชนะเกม"
/// เมื่อ HP หมด จะเรียก GameManager.OnFinalBossDefeated() ซึ่งไปแจ้ง VictoryScreenUI
/// และโหลด Victory Scene ตามที่ตั้งไว้ใน SectorPoolManager
///
/// วิธีติดตั้ง:
/// 1. สร้าง Enemy GameObject ตามปกติ (Rigidbody2D + Collider2D + Tag = Enemy)
/// 2. Attach: EnemyHealth (ตั้ง Max Health สูง ๆ, ปิด Destroy On Death ถ้าจะเล่นเอฟเฟกต์ตาย),
///    EnemyBulletEmitter, EnemyBase (หรือ AI ของบอสเอง), และสคริปต์นี้
/// 3. กรอก Phases ใน Inspector เรียงจาก HP มาก -> น้อย
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class NullExeBoss : MonoBehaviour
{
    [System.Serializable]
    public class BossPhase
    {
        public string phaseName = "Phase";
        [Range(0f, 1f)]
        [Tooltip("เข้าเฟสนี้เมื่อ HP เหลือน้อยกว่าหรือเท่ากับสัดส่วนนี้")]
        public float healthThreshold = 1f;

        public BulletPatternType pattern = BulletPatternType.RadialBurst;
        public float fireCooldown = 1f;
        public int bulletCount = 10;

        [Tooltip("ขึ้น Format Warning ตอนเข้าเฟส (เตือนล่วงหน้าตามเอกสาร)")]
        public bool formatWarningOnEnter = true;
        public float formatWarningDuration = 2f;
    }

    [Header("Phases (เรียง HP จากมากไปน้อย)")]
    [SerializeField]
    private List<BossPhase> phases = new List<BossPhase>
    {
        new BossPhase { phaseName = "Scan",      healthThreshold = 1.00f, pattern = BulletPatternType.Aimed,       fireCooldown = 0.9f, bulletCount = 1,  formatWarningOnEnter = false },
        new BossPhase { phaseName = "Overwrite", healthThreshold = 0.66f, pattern = BulletPatternType.RadialBurst, fireCooldown = 1.1f, bulletCount = 14 },
        new BossPhase { phaseName = "Corrupt",   healthThreshold = 0.33f, pattern = BulletPatternType.Spiral,      fireCooldown = 0.18f, bulletCount = 1 },
    };

    [Header("Refs")]
    [SerializeField] private EnemyHealth health;
    [SerializeField] private EnemyBulletEmitter bulletEmitter;

    [Header("Death")]
    [Tooltip("หน่วงก่อนแจ้งชนะ เผื่อเล่นเอฟเฟกต์ระเบิด/บทพูดปิดท้าย")]
    [SerializeField] private float victoryReportDelay = 1.5f;
    [SerializeField] private GameObject deathEffectPrefab;

    /// <summary>ยิงตอนเปลี่ยนเฟส (ให้ UI แสดงชื่อเฟส/หลอดเลือดบอส)</summary>
    public System.Action<int, BossPhase> OnPhaseChanged;

    private int currentPhaseIndex = -1;

    private void Awake()
    {
        if (health == null) health = GetComponent<EnemyHealth>();
        if (bulletEmitter == null) bulletEmitter = GetComponent<EnemyBulletEmitter>();

        // กันตั้งค่าพลาด: บอสห้ามถูก Destroy ทันทีที่ตาย ไม่งั้นจะแจ้งเงื่อนไขชนะไม่ทัน
        health?.SetDestroyOnDeath(false);
    }

    private void OnEnable()
    {
        if (health == null) return;
        health.OnHealthChanged += HandleHealthChanged;
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.OnHealthChanged -= HandleHealthChanged;
        health.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        EnterPhase(0);
    }

    private void HandleHealthChanged(float current, float max)
    {
        float percent = max > 0f ? current / max : 0f;

        // หาเฟสล่าสุดที่เงื่อนไข HP ผ่านแล้ว (ไล่จากท้าย = เฟสหนักสุด)
        for (int i = phases.Count - 1; i > currentPhaseIndex; i--)
        {
            if (percent <= phases[i].healthThreshold)
            {
                EnterPhase(i);
                return;
            }
        }
    }

    private void EnterPhase(int index)
    {
        if (index < 0 || index >= phases.Count || index == currentPhaseIndex) return;

        currentPhaseIndex = index;
        BossPhase phase = phases[index];

        bulletEmitter?.ConfigurePattern(phase.pattern, phase.fireCooldown, phase.bulletCount);

        if (phase.formatWarningOnEnter)
        {
            SystemInterferenceManager.Instance?.TriggerFormatWarning(phase.formatWarningDuration);
        }

        Debug.Log($"[NULL.exe] Entering phase {index}: {phase.phaseName} ({phase.pattern})");
        OnPhaseChanged?.Invoke(index, phase);
    }

    private void HandleDeath()
    {
        if (bulletEmitter != null) bulletEmitter.enabled = false;
        if (deathEffectPrefab != null) Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        Debug.Log("[NULL.exe] Process terminated — victory condition met");

        // ใช้ Invoke บน GameManager เพราะตัวบอสอาจถูก Destroy ไปแล้ว
        if (victoryReportDelay <= 0f) ReportVictory();
        else Invoke(nameof(ReportVictory), victoryReportDelay);
    }

    private void ReportVictory()
    {
        GameManager.Instance?.OnFinalBossDefeated();
        Destroy(gameObject, 0.1f);
    }
}
