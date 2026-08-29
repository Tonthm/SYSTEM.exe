using UnityEngine;

/// <summary>
/// คลาสฐานของ AI ศัตรูทุกชนิด — รวมของที่ทุกตัวต้องใช้ไว้ที่เดียว
/// (หา Player, คำนวณระยะ, หมุนหน้า, สั่งความเร็ว)
///
/// สืบทอดแล้ว override Tick() เพื่อเขียนพฤติกรรมเฉพาะตัว
/// ดูตัวอย่าง: PopupSwarmer, CursorChaser, FirewallTurret
///
/// หมายเหตุ: EnemyBase.cs ตัวเดิมยังใช้ได้อยู่ สำหรับศัตรูพื้นฐานที่แค่เดินเข้าหาแล้วยิง
/// ไม่ต้องแปลงของเก่าเป็นคลาสนี้ถ้าไม่จำเป็น
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyAIBase : MonoBehaviour
{
    [Header("Base Refs")]
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected EnemyBulletEmitter bulletEmitter;
    [Tooltip("ส่วนที่หมุนได้ (เช่น child Visual) — เว้นว่าง = ไม่หมุนอะไรเลย")]
    [SerializeField] protected Transform rotatingPart;

    protected Transform player;

    protected bool HasPlayer => player != null;
    protected Vector2 ToPlayer => HasPlayer ? (Vector2)(player.position - transform.position) : Vector2.zero;
    protected float DistanceToPlayer => HasPlayer ? ToPlayer.magnitude : float.MaxValue;
    protected Vector2 DirectionToPlayer => HasPlayer ? ToPlayer.normalized : Vector2.right;

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bulletEmitter == null) bulletEmitter = GetComponent<EnemyBulletEmitter>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    protected virtual void Start()
    {
        FindPlayer();
        OnSpawned();
    }

    /// <summary>เรียกครั้งเดียวตอนเกิด — ใช้ตั้งค่าเริ่มต้นของแต่ละชนิด</summary>
    protected virtual void OnSpawned() { }

    private void FixedUpdate()
    {
        if (!HasPlayer)
        {
            // ผู้เล่นอาจถูก SetActive(false) ชั่วคราวตอนตาย — ลองหาใหม่เป็นระยะ
            FindPlayer();
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Tick();
    }

    /// <summary>เขียนพฤติกรรมของศัตรูชนิดนั้นตรงนี้ — ถูกเรียกทุก FixedUpdate</summary>
    protected abstract void Tick();

    protected void FindPlayer()
    {
        var obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null && obj.activeInHierarchy) player = obj.transform;
    }

    protected void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }

    /// <summary>หมุน rotatingPart ไปทางที่กำหนดแบบค่อยเป็นค่อยไป (องศา/วินาที)</summary>
    protected void RotateTowards(Vector2 direction, float degreesPerSecond)
    {
        if (rotatingPart == null || direction.sqrMagnitude < 0.0001f) return;

        float target = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float current = rotatingPart.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, target, degreesPerSecond * Time.fixedDeltaTime);
        rotatingPart.rotation = Quaternion.Euler(0f, 0f, next);
    }

    /// <summary>หมุนทันทีไม่มีหน่วง</summary>
    protected void SnapRotation(Vector2 direction)
    {
        if (rotatingPart == null || direction.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rotatingPart.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
