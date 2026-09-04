using UnityEngine;

/// <summary>
/// จัดการ Checkpoint และการ Respawn ผู้เล่นในด่านหลบสิ่งกีดขวาง (Obstacle Dodge Sector)
/// ใช้ Singleton Pattern เหมือน Manager อื่นในโปรเจกต์ (GameManager, XPManager ฯลฯ)
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Checkpoint Settings")]
    [SerializeField] private float invulnerabilityWindow = 1f; // ช่วงเวลากันชนซ้ำหลัง respawn (วินาที)

    private Vector3 currentCheckpoint;
    private float lastRespawnTime = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>เรียกตอนเข้า Start Point ครั้งแรกของด่าน (จาก DodgeSectorModeController)</summary>
    public void SetInitialCheckpoint(Vector3 startPos)
    {
        currentCheckpoint = startPos;
    }

    /// <summary>เรียกตอนผู้เล่นเดินผ่าน CheckpointZone</summary>
    public void SetCheckpoint(Vector3 pos)
    {
        currentCheckpoint = pos;
    }

    /// <summary>เช็คว่าตอนนี้ผู้เล่นอยู่ในช่วงกันชนหลัง respawn อยู่รึเปล่า</summary>
    public bool IsInvulnerable()
    {
        return Time.time - lastRespawnTime < invulnerabilityWindow;
    }

    /// <summary>ส่งผู้เล่นกลับไปจุด Checkpoint ล่าสุด (เรียกจาก ObstacleHazard)</summary>
    public void RespawnPlayerAtCheckpoint(GameObject player)
    {
        if (IsInvulnerable()) return; // กันโดน trigger ซ้ำทันทีหลัง respawn

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Unity 6 API
        }

        player.transform.position = currentCheckpoint;
        lastRespawnTime = Time.time;

        // TODO: เรียก SFX/VFX ตรงนี้ เช่น SoundManager.Instance.PlayHitObstacle()
        // TODO: เรียก popup Task Manager mock ถ้าต้องการ tie-in กับ Death Log lore
    }
}
