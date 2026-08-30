using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// จุด spawn ศัตรูในด่าน — วางเป็น Empty GameObject กระจายรอบสนาม
/// WaveManager จะสุ่มเลือกจุดจากที่ลงทะเบียนไว้ ไม่ต้องลาก reference เข้าไปทีละอัน
///
/// วิธีติดตั้ง:
/// 1. Empty GameObject ชื่อ "SpawnPoint_01" วางตรงขอบสนาม
/// 2. Attach สคริปต์นี้
/// 3. (ไม่บังคับ) ตั้ง Group Id เช่น "top" / "sides" ถ้าอยากให้ศัตรูบางชนิดออกเฉพาะบางจุด
/// </summary>
public class WaveSpawnPoint : MonoBehaviour
{
    private static readonly List<WaveSpawnPoint> allPoints = new List<WaveSpawnPoint>();
    public static IReadOnlyList<WaveSpawnPoint> All => allPoints;

    [Tooltip("ชื่อกลุ่มของจุดนี้ — ให้ SpawnGroup เลือกใช้เฉพาะกลุ่มที่ต้องการ")]
    [SerializeField] private string groupId = "";

    [Tooltip("รัศมีสุ่มรอบจุดนี้ กันศัตรูซ้อนกันเป๊ะ")]
    [SerializeField] private float scatterRadius = 0.5f;

    public string GroupId => groupId;

    public Vector3 GetSpawnPosition()
    {
        if (scatterRadius <= 0f) return transform.position;
        Vector2 offset = Random.insideUnitCircle * scatterRadius;
        return transform.position + new Vector3(offset.x, offset.y, 0f);
    }

    private void OnEnable() { if (!allPoints.Contains(this)) allPoints.Add(this); }
    private void OnDisable() { allPoints.Remove(this); }

    /// <summary>หาจุด spawn แบบสุ่ม กรองตาม groupId (เว้นว่าง = ไม่กรอง)</summary>
    public static WaveSpawnPoint GetRandom(string filterGroupId)
    {
        if (allPoints.Count == 0) return null;

        if (string.IsNullOrEmpty(filterGroupId))
        {
            return allPoints[Random.Range(0, allPoints.Count)];
        }

        var matching = allPoints.FindAll(p => p.groupId == filterGroupId);
        if (matching.Count == 0) return allPoints[Random.Range(0, allPoints.Count)];
        return matching[Random.Range(0, matching.Count)];
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.2f, scatterRadius));
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.6f);
    }
}
