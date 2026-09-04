using UnityEngine;

/// <summary>
/// วางไว้บนจุด Checkpoint ในด่าน Obstacle Dodge — ต้องเป็น trigger collider (Is Trigger = true)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CheckpointZone : MonoBehaviour
{
    [SerializeField] private bool oneTimeOnly = false; // true = ใช้ครั้งเดียวแล้วปิด (กันคนย้อนมากดซ้ำ)
    private bool used = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (oneTimeOnly && used) return;

        CheckpointManager.Instance.SetCheckpoint(transform.position);
        used = true;

        // TODO: โชว์ toast UI เล็ก ๆ "Checkpoint Saved" ผ่าน TextMeshPro
    }
}
