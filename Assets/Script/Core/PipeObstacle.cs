using UnityEngine;

/// <summary>
/// สิ่งกีดขวางคู่บนล่างในมินิเกม Flap — เคลื่อนที่จากขวาไปซ้ายเข้าหาผู้เล่นที่อยู่นิ่ง
/// วางเป็น Prefab: root เปล่า + child sprite บน/ล่าง จัดช่องว่าง (gap) ไว้ใน Editor ให้เรียบร้อย
/// สคริปต์นี้แค่ขยับซ้าย + ทำลายตัวเองเมื่อพ้นจอ + แจ้งคะแนนตอนผ่านผู้เล่นไปแล้ว
///
/// วิธีติดตั้ง:
/// 1. สร้าง Prefab คู่กำแพง/ป๊อปอัพ (2 sprite บน-ล่าง เว้นช่องว่างตรงกลางตามความยากที่ต้องการ)
/// 2. Collider2D บนแต่ละชิ้น ติ๊ก Is Trigger
/// 3. Attach สคริปต์นี้ที่ root ของ Prefab
/// </summary>
public class PipeObstacle : MonoBehaviour
{
    [HideInInspector] public float moveSpeed = 4f;
    [Tooltip("X ที่ต่ำกว่านี้ถือว่าพ้นจอแล้ว ทำลายตัวเองได้ (ปรับตามขนาดกล้อง)")]
    [SerializeField] private float despawnX = -12f;

    private bool scored;
    private float playerX; // ตำแหน่ง X คงที่ของผู้เล่น — ใช้เทียบว่าผ่านไปแล้วหรือยัง

    /// <summary>PipeSpawner เรียกตอน Instantiate เพื่อบอกความเร็ว + ตำแหน่งผู้เล่น</summary>
    public void Setup(float speed, float playerXPos)
    {
        moveSpeed = speed;
        playerX = playerXPos;
    }

    private void Update()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (!scored && transform.position.x < playerX)
        {
            scored = true;
            FlapGameManager.Instance?.AddScore(1);
        }

        if (transform.position.x < despawnX) Destroy(gameObject);
    }
}
