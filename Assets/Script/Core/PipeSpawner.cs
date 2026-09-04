using UnityEngine;

/// <summary>
/// สุ่มสร้างคู่สิ่งกีดขวาง (PipeObstacle) เป็นระยะ ๆ ที่ขอบขวาจอ
/// สุ่มตำแหน่ง Y ของแต่ละคู่ในกรอบที่กำหนด เพื่อให้ผู้เล่นต้องกระพือหลบตลอดเวลา
///
/// วิธีติดตั้ง:
/// 1. Empty GameObject "PipeSpawner" ใน Scene มินิเกม
/// 2. ลาก Prefab คู่สิ่งกีดขวางเข้าช่อง Pipe Prefab, ลาก FlapPlayer เข้าช่อง Player
/// 3. Attach สคริปต์นี้
/// </summary>
public class PipeSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject pipePrefab;
    [SerializeField] private Transform player;

    [Header("Spawn")]
    [SerializeField] private float spawnX = 12f;
    [SerializeField] private float spawnInterval = 1.6f;
    [SerializeField] private float moveSpeed = 4f;
    [Tooltip("กรอบ Y ที่สุ่มตำแหน่งคู่สิ่งกีดขวางได้ (ปรับตามขนาดจอ)")]
    [SerializeField] private float minY = -2.5f;
    [SerializeField] private float maxY = 2.5f;

    private float timer;

    private void Update()
    {
        if (FlapGameManager.Instance != null && FlapGameManager.Instance.IsGameOver) return;

        timer += Time.deltaTime;
        if (timer < spawnInterval) return;
        timer = 0f;

        SpawnPipe();
    }

    private void SpawnPipe()
    {
        if (pipePrefab == null) return;

        float y = Random.Range(minY, maxY);
        GameObject pipe = Instantiate(pipePrefab, new Vector3(spawnX, y, 0f), Quaternion.identity);

        float px = player != null ? player.position.x : 0f;
        pipe.GetComponent<PipeObstacle>()?.Setup(moveSpeed, px);
    }
}
