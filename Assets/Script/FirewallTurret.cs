using UnityEngine;

/// <summary>
/// ศัตรูชนิดที่ 3 — Firewall Turret
///
/// ธีม: ป้อมกำแพงไฟร์วอลล์ อยู่กับที่ ยิงเป็นชุด และกันของที่เข้ามาจากด้านหน้า
///
/// พฤติกรรม: ไม่เคลื่อนที่เลย หมุนปากกระบอกตามผู้เล่นแบบช้า ๆ
/// วนลูป: พัก → ชาร์จ (เตือนล่วงหน้า) → ยิงรัวเป็นชุด → พัก
/// จุดเด่น: มี DirectionalShield กันกระสุนที่เข้าด้านหน้า ผู้เล่นต้องอ้อมไปยิงด้านหลัง
///
/// ทักษะที่ต้องใช้: จัดตำแหน่ง — ต้องวิ่งอ้อมให้เร็วกว่าที่ป้อมหมุนตาม
/// ต่างจากอีกสองชนิดที่เน้นหลบล้วน ๆ
///
/// วิธีติดตั้ง (Prefab):
/// 1. GameObject + Rigidbody2D (Body Type = Kinematic) + Collider2D + Tag = Enemy
/// 2. child ชื่อ "Turret" (ส่วนที่หมุน) → ข้างในมี child "FirePoint" อีกที
/// 3. Attach: EnemyHealth, EnemyBulletEmitter, DirectionalShield, FirewallTurret
/// 4. ลาก "Turret" เข้าช่อง Rotating Part และช่อง Facing ของ DirectionalShield
/// 5. ลาก "FirePoint" เข้าช่อง Fire Point ของ EnemyBulletEmitter
/// 6. ที่ EnemyBulletEmitter ตั้ง Source Display Name = "Firewall Turret"
/// </summary>
public class FirewallTurret : EnemyAIBase
{
    [Header("Aiming")]
    [Tooltip("ความเร็วหมุนตามผู้เล่น (องศา/วินาที) — ต่ำ = อ้อมหลังง่าย")]
    [SerializeField] private float rotationSpeed = 90f;
    [Tooltip("ระยะที่เริ่มทำงาน — ไกลกว่านี้จะนิ่งเฉย")]
    [SerializeField] private float activationRange = 12f;

    [Header("Burst Cycle")]
    [Tooltip("ชาร์จกี่วินาทีก่อนยิง (ช่วงเตือนให้ผู้เล่นหาที่กำบัง)")]
    [SerializeField] private float chargeDuration = 1f;
    [Tooltip("ยิงรัวนานกี่วินาที — จังหวะห่างของแต่ละนัดคุมที่ Fire Cooldown ของ EnemyBulletEmitter")]
    [SerializeField] private float burstDuration = 2f;
    [Tooltip("พักกี่วินาทีหลังยิงจบ (ช่วงที่ผู้เล่นเข้าไปทำดาเมจได้)")]
    [SerializeField] private float restDuration = 1.8f;
    [Tooltip("หยุดหมุนระหว่างยิง — ทำให้ผู้เล่นวิ่งอ้อมหนีลำกระสุนได้")]
    [SerializeField] private bool lockRotationWhileFiring = true;

    [Header("Charge Feedback")]
    [SerializeField] private SpriteRenderer chargeIndicator;
    [SerializeField] private Color chargeColor = new Color(1f, 0.3f, 0.2f, 1f);
    [Tooltip("ยิง Format Warning ตอนเริ่มชาร์จ (ใช้กับป้อมตัวใหญ่/บอสย่อย)")]
    [SerializeField] private bool useFormatWarning = false;

    [Header("Shield")]
    [SerializeField] private DirectionalShield shield;
    [Tooltip("ปิดโล่ระหว่างยิง — เปิดช่องให้สวนกลับตอนมันยุ่ง")]
    [SerializeField] private bool dropShieldWhileFiring = true;

    private enum State { Idle, Charge, Burst, Rest }
    private State state = State.Idle;
    private float stateTimer;
    private Color baseIndicatorColor;

    protected override void Awake()
    {
        base.Awake();

        if (shield == null) shield = GetComponent<DirectionalShield>();
        if (chargeIndicator != null) baseIndicatorColor = chargeIndicator.color;

        // ป้อมไม่ขยับ — กันโดนกระสุน/ศัตรูดันจนเลื่อน
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    protected override void OnSpawned()
    {
        stateTimer = Random.Range(0f, restDuration);
        state = State.Rest;
    }

    protected override void Tick()
    {
        SetVelocity(Vector2.zero);

        bool inRange = DistanceToPlayer <= activationRange;

        // หมุนตามผู้เล่น ยกเว้นตอนยิงถ้าตั้งให้ล็อกไว้
        bool canRotate = inRange && !(lockRotationWhileFiring && state == State.Burst);
        if (canRotate) RotateTowards(DirectionToPlayer, rotationSpeed);

        if (!inRange)
        {
            if (state != State.Idle) EnterIdle();
            return;
        }

        stateTimer -= Time.fixedDeltaTime;

        switch (state)
        {
            case State.Idle:
                EnterRest();
                break;

            case State.Rest:
                if (stateTimer <= 0f) EnterCharge();
                break;

            case State.Charge:
                PulseIndicator();
                if (stateTimer <= 0f) EnterBurst();
                break;

            case State.Burst:
                // สั่งยิงทุกเฟรม — EnemyBulletEmitter คุมจังหวะห่างด้วย Fire Cooldown ของมันเอง
                bulletEmitter?.TryFireAt(player.position);
                if (stateTimer <= 0f) EnterRest();
                break;
        }
    }

    private void EnterIdle()
    {
        state = State.Idle;
        ResetIndicator();
        if (shield != null) shield.IsActive = true;
    }

    private void EnterRest()
    {
        state = State.Rest;
        stateTimer = restDuration;
        ResetIndicator();
        if (shield != null) shield.IsActive = true;
    }

    private void EnterCharge()
    {
        state = State.Charge;
        stateTimer = chargeDuration;

        if (useFormatWarning)
        {
            SystemInterferenceManager.Instance?.TriggerFormatWarning(chargeDuration);
        }
    }

    private void EnterBurst()
    {
        state = State.Burst;
        stateTimer = burstDuration;
        ResetIndicator();

        if (shield != null && dropShieldWhileFiring) shield.IsActive = false;
    }

    private void PulseIndicator()
    {
        if (chargeIndicator == null) return;
        float t = 1f - Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, chargeDuration));
        chargeIndicator.color = Color.Lerp(baseIndicatorColor, chargeColor, t);
    }

    private void ResetIndicator()
    {
        if (chargeIndicator != null) chargeIndicator.color = baseIndicatorColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, activationRange);
    }
}
