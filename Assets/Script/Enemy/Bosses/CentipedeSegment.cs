using UnityEngine;

/// <summary>
/// ปล้อง 1 ปล้องของบอสตะขาบประจำด่าน RAM
///
/// - ปล้องหัว (previousSegment = null) เคลื่อนที่เอง หันเข้าหาผู้เล่นแบบค่อยเป็นค่อยไป + ส่ายเป็นคลื่น
/// - ปล้องอื่นเดินตามปล้องหน้าโดยรักษาระยะห่าง เกิดเป็นการเลื้อยแบบตะขาบ
/// - ทุกปล้องมีปืนฝั่งละ 2 กระบอก ยิงตั้งฉากกับลำตัวทั้งสองข้าง
///
/// **การแบ่งตัว:** ปล้องกลางตาย → ปล้องถัดไปกลายเป็นหัวใหม่ทันที
/// บอสจึงแยกเป็น 2 ตัวที่เคลื่อนที่อิสระ (ยิงปล้อง 5 จาก 8 → ได้ 1-4 กับ 6-8)
///
/// วิธีติดตั้ง (Prefab):
/// 1. GameObject + Rigidbody2D (Gravity 0, Freeze Rotation Z ✗ — ต้องหมุนได้)
/// 2. Collider2D + Tag = Enemy
/// 3. Attach: EnemyHealth, ContactDamage (ไม่บังคับ), CentipedeSegment, GlitchVisualDisplacer
/// 4. child "Visual" ใส่ SpriteRenderer (ปล้องลำตัว)
/// 5. ไม่ต้องวางในฉาก — CentipedeBoss จะ spawn ให้เอง
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CentipedeSegment : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [Tooltip("ระยะห่างจากปล้องหน้า")]
    [SerializeField] private float spacing = 0.9f;
    [Tooltip("ความเร็วหมุนเข้าหาผู้เล่นตอนเป็นหัว (องศา/วินาที) — ต่ำ = เลี้ยวกว้าง")]
    [SerializeField] private float headTurnSpeed = 70f;
    [Tooltip("ความแรงการส่ายเป็นคลื่นตอนเป็นหัว")]
    [SerializeField] private float wobbleAmplitude = 25f;
    [SerializeField] private float wobbleSpeed = 2.5f;

    [Header("Guns (ฝั่งละ 2 กระบอก)")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireCooldown = 2f;
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private float bulletLifetime = 4f;
    [Tooltip("ระยะเยื้องของปืน 2 กระบอกในฝั่งเดียวกัน (ตามแนวลำตัว)")]
    [SerializeField] private float barrelOffset = 0.2f;
    [Tooltip("ระยะจากแกนกลางลำตัวถึงปากกระบอก")]
    [SerializeField] private float barrelDistance = 0.3f;
    [SerializeField] private string sourceDisplayName = "RAM Centipede";

    [Header("Head Visual (ไม่บังคับ)")]
    [Tooltip("เปิดเมื่อปล้องนี้กลายเป็นหัว — ใช้บอกผู้เล่นว่าบอสแยกร่างแล้ว")]
    [SerializeField] private GameObject headIndicator;

    // ---------- Runtime ----------
    private Rigidbody2D rb;
    private EnemyHealth health;
    private Transform player;
    private float fireTimer;
    private float wobbleSeed;
    private Vector2 heading = Vector2.left;

    public CentipedeSegment PreviousSegment { get; private set; }
    public CentipedeSegment NextSegment { get; private set; }
    public CentipedeBoss Owner { get; private set; }
    public int SegmentIndex { get; private set; }
    public bool IsHead => PreviousSegment == null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        health = GetComponent<EnemyHealth>();
        wobbleSeed = Random.Range(0f, 100f);
        fireTimer = Random.Range(0f, fireCooldown);   // กันทุกปล้องยิงพร้อมกันเป๊ะ
    }

    private void OnEnable()
    {
        if (health != null) health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        var obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null) player = obj.transform;
        RefreshHeadVisual();
    }

    /// <summary>CentipedeBoss เรียกตอนสร้างโซ่</summary>
    public void Setup(CentipedeBoss owner, int index, CentipedeSegment previous)
    {
        Owner = owner;
        SegmentIndex = index;
        PreviousSegment = previous;
        if (previous != null) previous.NextSegment = this;
        RefreshHeadVisual();
    }

    private void FixedUpdate()
    {
        if (IsHead) MoveAsHead();
        else MoveAsFollower();

        TickGuns();
    }

    private void MoveAsHead()
    {
        if (player != null)
        {
            Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;

            // หมุนเข้าหาผู้เล่นช้า ๆ — เลี้ยวกว้างแบบตะขาบ ไม่ใช่พุ่งตรง
            float currentAngle = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;
            float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, headTurnSpeed * Time.fixedDeltaTime);

            // ส่ายเป็นคลื่น ทำให้เลื้อยไม่เป็นเส้นตรง
            float wobble = Mathf.Sin((Time.time + wobbleSeed) * wobbleSpeed) * wobbleAmplitude;
            nextAngle += wobble * Time.fixedDeltaTime * 10f;

            heading = new Vector2(Mathf.Cos(nextAngle * Mathf.Deg2Rad), Mathf.Sin(nextAngle * Mathf.Deg2Rad));
        }

        rb.linearVelocity = heading * moveSpeed;
        FaceDirection(heading);
    }

    private void MoveAsFollower()
    {
        if (PreviousSegment == null) return;

        Vector2 toPrev = (Vector2)PreviousSegment.transform.position - (Vector2)transform.position;
        float distance = toPrev.magnitude;

        if (distance > spacing)
        {
            // เร่งตามถ้าห่างมาก เพื่อไม่ให้โซ่ขาดออกจากกัน
            float speed = moveSpeed * Mathf.Clamp(distance / spacing, 1f, 2.5f);
            rb.linearVelocity = toPrev.normalized * speed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (toPrev.sqrMagnitude > 0.001f) FaceDirection(toPrev.normalized);
    }

    private void FaceDirection(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rb.MoveRotation(angle);
    }

    private void TickGuns()
    {
        if (bulletPrefab == null) return;

        fireTimer -= Time.fixedDeltaTime;
        if (fireTimer > 0f) return;
        fireTimer = fireCooldown;

        Vector2 forward = transform.right;                       // แนวลำตัว
        Vector2 side = new Vector2(-forward.y, forward.x);       // ตั้งฉาก

        // ปืนฝั่งละ 2 กระบอก เยื้องกันตามแนวลำตัว
        for (int s = -1; s <= 1; s += 2)                          // -1 = ซ้าย, +1 = ขวา
        {
            for (int b = -1; b <= 1; b += 2)                      // 2 กระบอกต่อฝั่ง
            {
                Vector3 muzzle = transform.position
                               + (Vector3)(side * barrelDistance * s)
                               + (Vector3)(forward * barrelOffset * b);
                SpawnBullet(muzzle, side * s);
            }
        }
    }

    private void SpawnBullet(Vector3 position, Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GameObject obj = Instantiate(bulletPrefab, position, Quaternion.Euler(0f, 0f, angle));

        var bullet = obj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Initialize(direction, bulletSpeed, bulletDamage, bulletLifetime,
                              false, BulletPatternType.WallSweep, sourceDisplayName);
        }
    }

    private void HandleDeath()
    {
        // ตัดโซ่: ปล้องถัดไปกลายเป็นหัวใหม่ → บอสแยกเป็น 2 ตัว
        if (NextSegment != null)
        {
            NextSegment.BecomeHead();
        }
        if (PreviousSegment != null)
        {
            PreviousSegment.NextSegment = null;
        }

        Owner?.OnSegmentDied(this);
    }

    /// <summary>ปล้องนี้กลายเป็นหัวใหม่หลังโซ่ถูกตัด</summary>
    public void BecomeHead()
    {
        PreviousSegment = null;
        heading = transform.right;    // เริ่มเลื้อยต่อจากทิศที่หันอยู่
        RefreshHeadVisual();
        Debug.Log($"[Centipede] ปล้องที่ {SegmentIndex + 1} กลายเป็นหัวใหม่ — บอสแยกร่าง");
    }

    private void RefreshHeadVisual()
    {
        if (headIndicator != null) headIndicator.SetActive(IsHead);
    }
}
