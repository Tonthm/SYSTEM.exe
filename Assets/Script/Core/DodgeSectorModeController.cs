using UnityEngine;

/// <summary>
/// ปิดระบบต่อสู้ (ยิง/enemy spawner) ตอนอยู่ในด่าน Obstacle Dodge
/// วางบน GameObject ของ scene/section นี้ แล้วลาก component ที่ต้องปิดมาใส่ใน Inspector
/// </summary>
public class DodgeSectorModeController : MonoBehaviour
{
    [Header("ปิดตอนเข้าโหมดนี้")]
    [SerializeField] private MonoBehaviour[] combatComponentsToDisable; // เช่น WeaponScript, EnemySpawner

    private void Start()
    {
        foreach (var comp in combatComponentsToDisable)
        {
            if (comp != null) comp.enabled = false;
        }

        // ตั้ง Checkpoint เริ่มต้นที่จุด spawn ของผู้เล่นใน scene นี้
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CheckpointManager.Instance.SetInitialCheckpoint(player.transform.position);
        }
    }
}
