using UnityEngine;

/// <summary>
/// Trigger Volume สำหรับตั้งจุด Checkpoint อัตโนมัติเมื่อผู้เล่นเดินผ่าน
/// เรียก GameManager.Instance.SetCheckpoint() ให้เอง ไม่ต้องเขียนโค้ดเพิ่มต่อด่าน
///
/// วิธีติดตั้งใน Unity:
/// 1. สร้าง Empty GameObject ชื่อ "Checkpoint_01" วางตรงจุดที่ต้องการ
/// 2. Add Component: BoxCollider2D (ติ๊ก Is Trigger) ขยายให้ครอบทางเดินที่ผู้เล่นต้องผ่านแน่ ๆ
/// 3. Attach สคริปต์นี้
/// 4. (ไม่บังคับ) สร้าง child ชื่อ "SpawnPoint" วางตรงจุดที่อยากให้ Process ใหม่เกิด
///    แล้วลากเข้าช่อง Spawn Point — ถ้าเว้นว่างจะใช้ตำแหน่งของ GameObject นี้เอง
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Spawn Point")]
    [Tooltip("จุดที่ Process ใหม่จะ spawn — เว้นว่าง = ใช้ตำแหน่งของ GameObject นี้")]
    [SerializeField] private Transform spawnPoint;

    [Header("Behaviour")]
    [Tooltip("ติดครั้งเดียวแล้วเลิกทำงาน (ปกติควรเปิดไว้)")]
    [SerializeField] private bool oneShot = true;
    [Tooltip("ฟื้น HP เต็มตอนแตะ checkpoint (แล้วแต่ดีไซน์ด่าน)")]
    [SerializeField] private bool healPlayerOnActivate = false;

    [Header("Visual Feedback (ไม่บังคับ)")]
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private Color inactiveColor = new Color(0.35f, 0.35f, 0.40f, 1f);
    [SerializeField] private Color activeColor = new Color(0.25f, 1f, 0.65f, 1f);

    /// <summary>ยิงตอน checkpoint นี้ถูกเปิดใช้งาน (เผื่อ UI อยากขึ้นข้อความ "Process state saved")</summary>
    public System.Action<CheckpointTrigger> OnActivated;

    public bool IsActivated { get; private set; }

    private void Reset()
    {
        // ตั้ง Is Trigger ให้อัตโนมัติตอนลากสคริปต์ใส่ครั้งแรกใน Editor
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;

        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[Checkpoint] {name}: Collider2D ยังไม่ได้ติ๊ก Is Trigger — ตั้งให้อัตโนมัติแล้ว");
        }

        if (indicator != null) indicator.color = inactiveColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (IsActivated && oneShot) return;

        IsActivated = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCheckpoint(spawnPoint);
        }
        else
        {
            Debug.LogWarning($"[Checkpoint] {name}: ไม่พบ GameManager ใน Scene — checkpoint ไม่ถูกบันทึก");
        }

        if (healPlayerOnActivate)
        {
            other.GetComponent<PlayerHealth>()?.ResetHealth();
        }

        if (indicator != null) indicator.color = activeColor;

        OnActivated?.Invoke(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.25f, 1f, 0.65f, 0.6f);
        Vector3 p = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.DrawWireSphere(p, 0.35f);
        Gizmos.DrawLine(p, p + Vector3.up * 0.9f);
    }
}
