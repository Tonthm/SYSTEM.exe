using UnityEngine;

/// <summary>
/// ศัตรูชนิดที่ 2 — Cursor Chaser
///
/// ธีม: เคอร์เซอร์เมาส์ที่ไล่ตามไม่เลิก แต่มีความหน่วงเหมือนเมาส์ที่ลากค้าง
///
/// พฤติกรรม: ไล่ตามด้วยแรงเร่ง + ความเฉื่อย ทำให้เลี้ยวโค้งและ "เลยเป้า" เวลาผู้เล่นเปลี่ยนทิศกะทันหัน
/// เมื่อเข้าใกล้ในระยะหนึ่ง จะหยุดนิ่งเสี้ยววินาที (จำลองการเล็งคลิก) แล้วพุ่งเป็นเส้นตรงด้วยความเร็วสูง
/// ฆ่าด้วยการชน ไม่ยิงกระสุน
///
/// ทักษะที่ต้องใช้: หลอกล่อ — เลี้ยวกะทันหันให้มันเลยเป้า ต่างจาก Swarmer ที่ต้องอ่านจังหวะนิ่ง ๆ
///
/// วิธีติดตั้ง (Prefab):
/// 1. GameObject + Rigidbody2D (Gravity 0, Linear Damping ~1) + Collider2D + Tag = Enemy
/// 2. child "Visual" ใส่ SpriteRenderer (รูปลูกศรเคอร์เซอร์) → ลากเข้าช่อง Rotating Part ด้วย
/// 3. Attach: EnemyHealth, ContactDamage, CursorChaser, GlitchVisualDisplacer
/// 4. ที่ ContactDamage ตั้ง Source Display Name = "Cursor Chaser"
/// </summary>
public class CursorChaser : EnemyAIBase
{
    [Header("Chase (แบบมีความเฉื่อย)")]
    [Tooltip("แรงเร่งเข้าหาผู้เล่น — ต่ำ = เลี้ยวช้า เลยเป้าง่าย")]
    [SerializeField] private float acceleration = 22f;
    [Tooltip("ความเร็วสูงสุดตอนไล่ตามปกติ")]
    [SerializeField] private float maxChaseSpeed = 4.5f;
    [Tooltip("แรงต้าน — สูง = หยุดไว, ต่ำ = ลื่นไถลเหมือนเมาส์บนโต๊ะลื่น")]
    [SerializeField] private float drag = 1.5f;

    [Header("Click Lunge (พุ่งเข้าใส่)")]
    [Tooltip("เข้าใกล้เท่านี้แล้วเริ่มเล็ง")]
    [SerializeField] private float lungeRange = 3.5f;
    [Tooltip("หยุดเล็งกี่วินาทีก่อนพุ่ง")]
    [SerializeField] private float aimDuration = 0.4f;
    [SerializeField] private float lungeSpeed = 16f;
    [SerializeField] private float lungeDuration = 0.28f;
    [Tooltip("พุ่งเสร็จแล้วเหนื่อย หยุดนิ่งกี่วินาที (ช่วงที่ผู้เล่นสวนกลับได้)")]
    [SerializeField] private float recoverDuration = 0.6f;
    [SerializeField] private float lungeCooldown = 1.5f;

    [Header("Visual")]
    [Tooltip("หมุนรูปตามทิศที่เคลื่อนที่")]
    [SerializeField] private bool rotateToVelocity = true;
    [SerializeField] private float rotateSpeed = 540f;
    [SerializeField] private SpriteRenderer aimFlashTarget;
    [SerializeField] private Color aimFlashColor = new Color(1f, 0.35f, 0.35f, 1f);

    private enum State { Chase, Aim, Lunge, Recover }
    private State state = State.Chase;
    private float stateTimer;
    private float cooldownTimer;
    private Vector2 lungeDirection;
    private Color baseColor;

    protected override void Awake()
    {
        base.Awake();
        rb.linearDamping = drag;
        if (aimFlashTarget != null) baseColor = aimFlashTarget.color;
    }

    protected override void OnSpawned()
    {
        cooldownTimer = Random.Range(0f, lungeCooldown);
    }

    protected override void Tick()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.fixedDeltaTime;
        stateTimer -= Time.fixedDeltaTime;

        switch (state)
        {
            case State.Chase: TickChase(); break;
            case State.Aim: TickAim(); break;
            case State.Lunge: TickLunge(); break;
            case State.Recover: TickRecover(); break;
        }

        if (rotateToVelocity && rb.linearVelocity.sqrMagnitude > 0.05f)
        {
            RotateTowards(rb.linearVelocity, rotateSpeed);
        }
    }

    private void TickChase()
    {
        // เร่งเข้าหาผู้เล่นแทนการ set ความเร็วตรง ๆ — จึงเกิดความเฉื่อยและอาการเลยเป้า
        Vector2 velocity = rb.linearVelocity + DirectionToPlayer * acceleration * Time.fixedDeltaTime;
        if (velocity.magnitude > maxChaseSpeed) velocity = velocity.normalized * maxChaseSpeed;
        SetVelocity(velocity);

        if (DistanceToPlayer <= lungeRange && cooldownTimer <= 0f) EnterAim();
    }

    private void TickAim()
    {
        // หยุดนิ่งเล็ง — จังหวะนี้คือสัญญาณให้ผู้เล่นเตรียมหลบ
        SetVelocity(Vector2.zero);
        lungeDirection = DirectionToPlayer;
        SnapRotation(lungeDirection);

        if (stateTimer <= 0f) EnterLunge();
    }

    private void TickLunge()
    {
        SetVelocity(lungeDirection * lungeSpeed);
        if (stateTimer <= 0f) EnterRecover();
    }

    private void TickRecover()
    {
        SetVelocity(Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 6f));
        if (stateTimer <= 0f) EnterChase();
    }

    private void EnterAim()
    {
        state = State.Aim;
        stateTimer = aimDuration;
        if (aimFlashTarget != null) aimFlashTarget.color = aimFlashColor;
    }

    private void EnterLunge()
    {
        state = State.Lunge;
        stateTimer = lungeDuration;
        if (aimFlashTarget != null) aimFlashTarget.color = baseColor;
    }

    private void EnterRecover()
    {
        state = State.Recover;
        stateTimer = recoverDuration;
    }

    private void EnterChase()
    {
        state = State.Chase;
        cooldownTimer = lungeCooldown;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, lungeRange);
    }
}
