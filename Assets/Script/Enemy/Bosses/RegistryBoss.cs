using System.Collections;
using UnityEngine;

/// <summary>
/// บอสประจำด่าน Registry — ไอคอน Windows Registry ลอยอยู่กลางสนาม
///
/// กลไก: โจมตีด้วยลำแสงแนวนอนและแนวเฉียง **ระหว่างโจมตีจะยิงไม่เข้า**
/// ผู้เล่นต้องรอให้บอสหยุดโจมตีก่อนถึงจะทำดาเมจได้
///
/// ทักษะที่บังคับ: ความอดทนกับการจำจังหวะ — ยิงมั่วตอนบอสโจมตีเสียเปล่า
/// ต้องหลบให้รอดแล้วเก็บดาเมจในช่วงพักสั้น ๆ
///
/// การกันดาเมจใช้ DirectionalShield (Arc Angle = 360) โดยสคริปต์นี้เปิด/ปิด IsActive ให้
///
/// วิธีติดตั้ง:
/// 1. GameObject ชื่อ "RegistryBoss" วางกลางสนาม — Collider2D + Tag = Enemy
/// 2. Rigidbody2D → Body Type = Kinematic (ไม่ต้องขยับ)
/// 3. Attach: EnemyHealth (Max Health ~800), DirectionalShield, RegistryBoss
/// 4. DirectionalShield: Arc Angle = 360, Blocked Damage Multiplier = 0
/// 5. ทำ LaserBeam Prefab (ดูสคริปต์นั้น) → ลากเข้าช่อง Laser Beam Prefab
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class RegistryBoss : MonoBehaviour
{
    public enum AttackType { HorizontalSweep, DiagonalCross, Both }

    [Header("Refs")]
    [SerializeField] private GameObject laserBeamPrefab;
    [SerializeField] private DirectionalShield shield;
    [SerializeField] private Transform iconVisual;

    [Header("Cycle")]
    [Tooltip("ช่วงที่ยิงบอสได้ — สั้น = ยากขึ้นมาก")]
    [SerializeField] private float vulnerableDuration = 3f;
    [Tooltip("หน่วงก่อนเริ่มโจมตี (ไอคอนสั่น)")]
    [SerializeField] private float windupDuration = 0.6f;
    [Tooltip("พักหลังโจมตีจบก่อนกลับสู่ช่วงยิงได้")]
    [SerializeField] private float recoverDuration = 0.8f;
    [SerializeField] private float startDelay = 2f;

    [Header("Horizontal Sweep")]
    [Tooltip("ยิงลำแสงพาดจอกี่เส้นต่อครั้ง")]
    [SerializeField] private int horizontalBeamCount = 4;
    [SerializeField] private float horizontalSpacing = 1.8f;
    [Tooltip("เว้นระยะจากกันกี่วินาที (ไล่ทีละเส้นให้ผู้เล่นวิ่งหลบ)")]
    [SerializeField] private float horizontalStagger = 0.35f;
    [SerializeField] private float horizontalAreaHeight = 8f;

    [Header("Diagonal Cross")]
    [Tooltip("จำนวนแฉกที่ยิงออกจากตัวบอส")]
    [SerializeField] private int diagonalBeamCount = 4;
    [SerializeField] private float diagonalStartAngle = 45f;
    [Tooltip("หมุนแฉกไปกี่องศาในรอบถัดไป (ทำให้แนวเปลี่ยนทุกครั้ง)")]
    [SerializeField] private float diagonalRotationStep = 22.5f;

    [Header("Beam Settings")]
    [SerializeField] private float beamLength = 30f;
    [SerializeField] private float beamWidth = 0.7f;
    [SerializeField] private float beamTelegraph = 0.8f;
    [SerializeField] private float beamActive = 1.2f;
    [SerializeField] private float beamDamage = 25f;

    [Header("Phase (ปรับตาม HP อัตโนมัติ)")]
    [Tooltip("HP ต่ำกว่า 66% — ช่วงยิงได้สั้นลงเป็นกี่เท่า")]
    [SerializeField] private float phase2VulnerableMultiplier = 0.75f;
    [Tooltip("HP ต่ำกว่า 33% — ใช้ทั้ง 2 แบบพร้อมกัน")]
    [SerializeField] private float phase3VulnerableMultiplier = 0.55f;

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Color vulnerableColor = Color.white;
    [SerializeField] private Color attackingColor = new Color(0.6f, 0.6f, 0.75f, 1f);
    [SerializeField] private float shakeAmount = 0.1f;
    [SerializeField] private float idleSpinSpeed = 20f;

    private EnemyHealth health;
    private int currentPhase = 1;
    private float diagonalAngleOffset;
    private Vector3 iconBasePos;

    /// <summary>true = ตอนนี้ยิงบอสเข้า (ให้ UI ขึ้นข้อความบอกผู้เล่นได้)</summary>
    public bool IsVulnerable { get; private set; } = true;
    public System.Action<bool> OnVulnerabilityChanged;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (shield == null) shield = GetComponent<DirectionalShield>();
        if (iconVisual != null) iconBasePos = iconVisual.localPosition;
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDamageBlocked += HandleDamageBlocked;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDamageBlocked -= HandleDamageBlocked;
        }
    }

    private void Start()
    {
        SetVulnerable(true);
        StartCoroutine(AttackLoop());
    }

    private void Update()
    {
        if (iconVisual == null) return;

        if (IsVulnerable) iconVisual.Rotate(0f, 0f, idleSpinSpeed * Time.deltaTime);
        else iconVisual.localPosition = iconBasePos + (Vector3)(Random.insideUnitCircle * shakeAmount);
    }

    private void HandleHealthChanged(float current, float max)
    {
        float percent = max > 0f ? current / max : 0f;
        int newPhase = percent <= 0.33f ? 3 : (percent <= 0.66f ? 2 : 1);
        if (newPhase == currentPhase) return;

        currentPhase = newPhase;
        Debug.Log($"[Registry Boss] เข้าเฟส {currentPhase}");
        SystemInterferenceManager.Instance?.TriggerFormatWarning(1.5f);
    }

    private void HandleDamageBlocked(Vector2 point)
    {
        // ผู้เล่นยิงตอนบอสกำลังโจมตี — ให้ feedback ว่าไม่เข้า
        Debug.Log("[Registry Boss] ACCESS DENIED — ยิงไม่เข้าระหว่างบอสโจมตี");
    }

    private IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            // ── ช่วงยิงบอสได้ ──
            SetVulnerable(true);
            yield return new WaitForSeconds(vulnerableDuration * CurrentVulnerableMultiplier);

            // ── เริ่มโจมตี: ยิงไม่เข้าแล้ว ──
            SetVulnerable(false);
            yield return new WaitForSeconds(windupDuration);

            yield return StartCoroutine(PerformAttack(PickAttack()));

            yield return new WaitForSeconds(recoverDuration);
        }
    }

    private float CurrentVulnerableMultiplier =>
        currentPhase == 3 ? phase3VulnerableMultiplier : (currentPhase == 2 ? phase2VulnerableMultiplier : 1f);

    private AttackType PickAttack()
    {
        if (currentPhase >= 3) return AttackType.Both;
        return Random.value < 0.5f ? AttackType.HorizontalSweep : AttackType.DiagonalCross;
    }

    private IEnumerator PerformAttack(AttackType type)
    {
        switch (type)
        {
            case AttackType.HorizontalSweep:
                yield return StartCoroutine(HorizontalSweep());
                break;

            case AttackType.DiagonalCross:
                DiagonalCross();
                yield return new WaitForSeconds(beamTelegraph + beamActive);
                break;

            case AttackType.Both:
                DiagonalCross();
                yield return StartCoroutine(HorizontalSweep());
                break;
        }
    }

    /// <summary>ลำแสงแนวนอนพาดจอ ไล่ทีละเส้นให้ผู้เล่นวิ่งหลบ</summary>
    private IEnumerator HorizontalSweep()
    {
        if (laserBeamPrefab == null) yield break;

        // สุ่มว่าจะไล่จากบนลงล่างหรือล่างขึ้นบน
        bool topDown = Random.value < 0.5f;
        float startY = transform.position.y + (topDown ? horizontalAreaHeight * 0.5f : -horizontalAreaHeight * 0.5f);
        float step = (topDown ? -1f : 1f) * horizontalSpacing;

        for (int i = 0; i < horizontalBeamCount; i++)
        {
            Vector2 origin = new Vector2(transform.position.x, startY + step * i);
            SpawnBeam(origin, 0f, fromCenter: true);
            yield return new WaitForSeconds(horizontalStagger);
        }

        yield return new WaitForSeconds(beamTelegraph + beamActive - horizontalStagger);
    }

    /// <summary>ลำแสงแนวเฉียงยิงออกจากตัวบอสเป็นแฉก</summary>
    private void DiagonalCross()
    {
        if (laserBeamPrefab == null) return;

        float step = 360f / Mathf.Max(1, diagonalBeamCount);
        for (int i = 0; i < diagonalBeamCount; i++)
        {
            float angle = diagonalStartAngle + diagonalAngleOffset + step * i;
            SpawnBeam(transform.position, angle, fromCenter: false);
        }

        diagonalAngleOffset += diagonalRotationStep;   // รอบหน้าแนวจะเปลี่ยน
    }

    private void SpawnBeam(Vector2 origin, float angle, bool fromCenter)
    {
        GameObject obj = Instantiate(laserBeamPrefab, origin, Quaternion.identity);
        var beam = obj.GetComponent<LaserBeam>();
        if (beam != null)
        {
            beam.Initialize(origin, angle, beamLength, beamWidth, beamTelegraph, beamActive, beamDamage, fromCenter);
        }
    }

    private void SetVulnerable(bool vulnerable)
    {
        IsVulnerable = vulnerable;

        // โล่ 360° เปิด = กันดาเมจทุกทิศ
        if (shield != null) shield.IsActive = !vulnerable;

        if (iconRenderer != null) iconRenderer.color = vulnerable ? vulnerableColor : attackingColor;
        if (vulnerable && iconVisual != null) iconVisual.localPosition = iconBasePos;

        OnVulnerabilityChanged?.Invoke(vulnerable);
    }
}
