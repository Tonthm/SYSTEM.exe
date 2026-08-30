using UnityEngine;

/// <summary>
/// ควบคุมการเคลื่อนที่ของ Ghost Process (ผู้เล่น) แบบ Top-down 360 องศา
/// พร้อมระบบ Dash สำหรับหลบกระสุนในจังหวะฉุกเฉิน (Bullet Hell)
///
/// [อัปเดต] เปลี่ยนวิธีคิดความเร็วเป็น base x tempMultiplier x runItemMultiplier
/// - แก้บั๊กเดิม: Latency Spike ซ้อนกันแล้ว StopAllCoroutines ทำให้ความเร็วค้างผิดค่าถาวร
/// - รองรับ PassiveBuff จาก RunInventory (ไอเทมเพิ่มความเร็วของรอบนั้น)
///
/// วิธีติดตั้งใน Unity:
/// 1. สร้าง GameObject ชื่อ "Player" ใส่ Sprite ตัวละคร
/// 2. Add Component: Rigidbody2D (Gravity Scale = 0, Freeze Rotation Z = true)
/// 3. Add Component: Collider2D (เช่น CircleCollider2D)
/// 4. Attach สคริปต์นี้เข้ากับ GameObject
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("ความเร็วเคลื่อนที่พื้นฐาน (หน่วย/วินาที)")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Dash")]
    [Tooltip("ความเร็วขณะ Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [Tooltip("ระยะเวลาที่ Dash คงอยู่ (วินาที)")]
    [SerializeField] private float dashDuration = 0.15f;
    [Tooltip("เวลาคูลดาวน์ก่อน Dash ครั้งถัดไปได้ (วินาที)")]
    [SerializeField] private float dashCooldown = 0.8f;
    [Tooltip("ระหว่าง Dash ผู้เล่นอมตะหรือไม่ (i-frame)")]
    [SerializeField] private bool invincibleDuringDash = true;

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;

    // --- Runtime state ---
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection;

    private float tempSpeedMultiplier = 1f;   // จาก Latency Spike / เอฟเฟกต์ชั่วคราว
    private Coroutine speedRoutine;

    public bool IsInvincible { get; private set; } = false;
    public bool IsDashing => isDashing;

    /// <summary>ความเร็วจริงที่ใช้อยู่ตอนนี้ (ให้ HUD/ดีบักอ่านได้)</summary>
    public float CurrentMoveSpeed => moveSpeed * tempSpeedMultiplier * RunItemSpeedMultiplier;

    private float RunItemSpeedMultiplier =>
        RunInventory.Instance != null ? RunInventory.Instance.GetMoveSpeedMultiplier() : 1f;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        ReadInput();
        TickTimers();

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space))
        {
            TryDash();
        }
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
        }
        else
        {
            rb.linearVelocity = moveInput * CurrentMoveSpeed;
        }
    }

    private void ReadInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(x, y).normalized;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = moveInput;
        }
    }

    private void TickTimers()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                IsInvincible = false;
            }
        }
    }

    private void TryDash()
    {
        if (isDashing || dashCooldownTimer > 0f) return;

        dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput : lastMoveDirection;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        IsInvincible = invincibleDuringDash;
    }

    /// <summary>ให้ระบบอื่น (เช่น Latency Spike) หน่วงความเร็วผู้เล่นชั่วคราว</summary>
    public void ApplySpeedMultiplier(float multiplier, float duration)
    {
        if (speedRoutine != null) StopCoroutine(speedRoutine);
        speedRoutine = StartCoroutine(SpeedMultiplierRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator SpeedMultiplierRoutine(float multiplier, float duration)
    {
        tempSpeedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        tempSpeedMultiplier = 1f;   // คืนค่ากลับเป็นกลาง ไม่ยุ่งกับ base speed
        speedRoutine = null;
    }
}
