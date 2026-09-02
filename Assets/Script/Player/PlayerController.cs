using System.Collections;
using UnityEngine;

/// <summary>
/// ควบคุมการเคลื่อนที่ของ Ghost Process แบบ Top-down 360 องศา + ระบบ Dash
///
/// อมตะมี 2 แหล่ง รวมกันเป็น IsInvincible:
/// 1. ระหว่าง Dash           — หลบกระสุนและทะลุตัวศัตรูได้
/// 2. Spawn Protection       — ช่วงสั้น ๆ หลังเกิดใหม่ ให้เดินออกจากฝูงศัตรูได้ก่อน
///
/// ทั้งสองแบบใช้ excludeLayers ปิดการชนกับ layer ศัตรู ไม่ได้ปิด Rigidbody
/// (ปิด Rigidbody จริงจะทะลุกำแพงหลุดออกนอกแมพด้วย)
///
/// วิธีติดตั้ง:
/// 1. GameObject "Player" + Rigidbody2D (Gravity 0, Freeze Rotation Z ✓) + Collider2D
/// 2. Tag = Player, Layer = Player
/// 3. ตั้ง Dash Pass Through Layers = Enemy เท่านั้น (ห้ามใส่ Wall)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("ความเร็วเคลื่อนที่พื้นฐาน (หน่วย/วินาที)")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.8f;
    [Tooltip("ระหว่าง Dash ผู้เล่นอมตะหรือไม่ (i-frame)")]
    [SerializeField] private bool invincibleDuringDash = true;

    [Header("Pass Through")]
    [Tooltip("ทะลุตัวศัตรูได้ระหว่าง Dash และช่วง Spawn Protection")]
    [SerializeField] private bool passThroughEnemies = true;
    [Tooltip("Layer ที่จะทะลุได้ — เลือก Enemy อย่างเดียว ห้ามใส่ Wall")]
    [SerializeField] private LayerMask passThroughLayers;

    [Header("Spawn Protection")]
    [Tooltip("หลังเกิดใหม่ อมตะและทะลุศัตรูได้กี่วินาที (ให้เดินออกจากฝูงศัตรูก่อน)")]
    [SerializeField] private float spawnProtectionDuration = 1.5f;
    [Tooltip("ความถี่การกะพริบตอนอยู่ในช่วงคุ้มกัน")]
    [SerializeField] private float spawnProtectionBlinkSpeed = 12f;

    [Header("Dash Effect")]
    [SerializeField] private GameObject dashEffectPrefab;
    [SerializeField] private TrailRenderer dashTrail;
    [Tooltip("SpriteRenderer ของผู้เล่น — ใช้เปลี่ยนสีตอน dash และกะพริบตอนคุ้มกัน")]
    [SerializeField] private SpriteRenderer[] tintTargets;
    [SerializeField] private Color dashTintColor = new Color(0.4f, 1f, 1f, 0.7f);
    [SerializeField] private float dashEffectLifetime = 1f;

    [Header("Skill: Dodge Boost")]
    [Tooltip("ปลดล็อกสกิลแล้ว dash cooldown คูณด้วยเท่านี้ (0.7 = เร็วขึ้น 30%)")]
    [SerializeField] private float dodgeBoostCooldownMultiplier = 0.7f;

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;

    // ── Runtime ──
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;
    private bool dashInvincible;

    private bool spawnProtected;
    private Coroutine protectionRoutine;

    private float tempSpeedMultiplier = 1f;
    private Coroutine speedRoutine;

    private Color[] baseTints;
    private Collider2D[] playerColliders;
    private LayerMask[] baseExcludeLayers;

    /// <summary>อมตะอยู่หรือไม่ — รวมทั้ง dash และช่วงคุ้มกันหลังเกิดใหม่</summary>
    public bool IsInvincible => dashInvincible || spawnProtected;
    public bool IsDashing => isDashing;
    /// <summary>อยู่ในช่วงคุ้มกันหลังเกิดใหม่หรือไม่ (ให้ HUD แสดงได้)</summary>
    public bool IsSpawnProtected => spawnProtected;

    public float EffectiveDashCooldown =>
        dashCooldown * SkillEffects.Multiplier(SkillEffects.DodgeBoost, dodgeBoostCooldownMultiplier);

    public float DashCooldownPercent =>
        EffectiveDashCooldown <= 0f ? 0f : Mathf.Clamp01(dashCooldownTimer / EffectiveDashCooldown);

    public float CurrentMoveSpeed => moveSpeed * tempSpeedMultiplier * RunItemSpeedMultiplier;

    private float RunItemSpeedMultiplier =>
        RunInventory.Instance != null ? RunInventory.Instance.GetMoveSpeedMultiplier() : 1f;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        playerColliders = GetComponentsInChildren<Collider2D>();
        if (playerColliders != null)
        {
            baseExcludeLayers = new LayerMask[playerColliders.Length];
            for (int i = 0; i < playerColliders.Length; i++)
            {
                if (playerColliders[i] != null) baseExcludeLayers[i] = playerColliders[i].excludeLayers;
            }
        }

        if (dashTrail != null) dashTrail.emitting = false;

        if (tintTargets != null)
        {
            baseTints = new Color[tintTargets.Length];
            for (int i = 0; i < tintTargets.Length; i++)
            {
                if (tintTargets[i] != null) baseTints[i] = tintTargets[i].color;
            }
        }
    }

    private void Update()
    {
        ReadInput();
        TickTimers();

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space)) TryDash();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = isDashing ? dashDirection * dashSpeed : moveInput * CurrentMoveSpeed;
    }

    private void ReadInput()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (moveInput.sqrMagnitude > 0.01f) lastMoveDirection = moveInput;
    }

    private void TickTimers()
    {
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        if (!isDashing) return;

        dashTimer -= Time.deltaTime;
        if (dashTimer > 0f) return;

        isDashing = false;
        dashInvincible = false;
        SetDashVisual(false);
        RefreshPassThrough();
    }

    private void TryDash()
    {
        if (isDashing || dashCooldownTimer > 0f) return;

        dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput : lastMoveDirection;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = EffectiveDashCooldown;
        dashInvincible = invincibleDuringDash;

        SetDashVisual(true);
        RefreshPassThrough();

        if (dashEffectPrefab != null)
        {
            float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
            GameObject fx = Instantiate(dashEffectPrefab, transform.position, Quaternion.Euler(0f, 0f, angle));
            if (dashEffectLifetime > 0f) Destroy(fx, dashEffectLifetime);
        }
    }

    // ── Spawn Protection ──

    /// <summary>
    /// GameManager เรียกหลังผู้เล่นเกิดใหม่ — อมตะและทะลุศัตรูได้ชั่วครู่
    /// แก้ปัญหาเกิดกลางฝูงศัตรูแล้วโดนดันหรือตายซ้ำทันที
    /// </summary>
    public void BeginSpawnProtection(float duration = -1f)
    {
        float length = duration > 0f ? duration : spawnProtectionDuration;
        if (length <= 0f) return;

        if (protectionRoutine != null) StopCoroutine(protectionRoutine);
        protectionRoutine = StartCoroutine(SpawnProtectionRoutine(length));
    }

    private IEnumerator SpawnProtectionRoutine(float duration)
    {
        spawnProtected = true;
        RefreshPassThrough();

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;

            // กะพริบบอกว่ายังอมตะอยู่
            bool visible = Mathf.Sin(t * spawnProtectionBlinkSpeed) > 0f;
            SetRenderersEnabled(visible);

            yield return null;
        }

        SetRenderersEnabled(true);
        spawnProtected = false;
        RefreshPassThrough();
        protectionRoutine = null;
    }

    // ── Collision / Visual ──

    /// <summary>เปิดการทะลุศัตรูเมื่ออยู่ในช่วง dash หรือช่วงคุ้มกัน</summary>
    private void RefreshPassThrough()
    {
        if (!passThroughEnemies || playerColliders == null || baseExcludeLayers == null) return;

        bool on = isDashing || spawnProtected;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (playerColliders[i] == null) continue;

            playerColliders[i].excludeLayers = on
                ? (baseExcludeLayers[i] | passThroughLayers)
                : baseExcludeLayers[i];
        }
    }

    private void SetDashVisual(bool on)
    {
        if (dashTrail != null) dashTrail.emitting = on;

        if (tintTargets == null || baseTints == null) return;
        for (int i = 0; i < tintTargets.Length; i++)
        {
            if (tintTargets[i] == null) continue;
            tintTargets[i].color = on ? dashTintColor : baseTints[i];
        }
    }

    private void SetRenderersEnabled(bool value)
    {
        if (tintTargets == null) return;
        foreach (var sr in tintTargets)
        {
            if (sr != null && sr.enabled != value) sr.enabled = value;
        }
    }

    // ── External ──

    /// <summary>ให้ระบบอื่น (เช่น Latency Spike) หน่วงความเร็วผู้เล่นชั่วคราว</summary>
    public void ApplySpeedMultiplier(float multiplier, float duration)
    {
        if (speedRoutine != null) StopCoroutine(speedRoutine);
        speedRoutine = StartCoroutine(SpeedMultiplierRoutine(multiplier, duration));
    }

    private IEnumerator SpeedMultiplierRoutine(float multiplier, float duration)
    {
        tempSpeedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        tempSpeedMultiplier = 1f;
        speedRoutine = null;
    }

    /// <summary>GameManager เรียกตอนตาย — ล้างสถานะค้างทั้งหมด</summary>
    public void ResetState()
    {
        isDashing = false;
        dashInvincible = false;
        spawnProtected = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
        tempSpeedMultiplier = 1f;

        if (speedRoutine != null) { StopCoroutine(speedRoutine); speedRoutine = null; }
        if (protectionRoutine != null) { StopCoroutine(protectionRoutine); protectionRoutine = null; }

        rb.linearVelocity = Vector2.zero;
        SetDashVisual(false);
        SetRenderersEnabled(true);
        RefreshPassThrough();
    }
}
