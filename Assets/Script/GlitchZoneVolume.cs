using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// พื้นที่ Glitch Zone: ทำให้ "ภาพที่เห็น" ของกระสุน/ศัตรู/สิ่งกีดขวาง เคลื่อนออกจาก hitbox จริง
/// วางเป็น GameObject ที่มี Collider2D (Is Trigger) ครอบพื้นที่ที่ต้องการ
///
/// [อัปเดต] เพิ่ม static registry — วัตถุใด ๆ ที่มี GlitchVisualDisplacer จะถามหา offset
/// ได้เองทุกเฟรมโดยไม่ต้องลาก reference ของโซนเข้าไปทีละตัว (สำคัญมากสำหรับกระสุนที่ spawn ระหว่างเล่น)
///
/// วิธีติดตั้ง:
/// 1. สร้าง GameObject ชื่อ "GlitchZone_01" + BoxCollider2D (Is Trigger) ครอบพื้นที่
/// 2. Attach สคริปต์นี้
/// 3. ที่ Prefab ของกระสุน/ศัตรู ให้แยก Sprite เป็น child แล้ว attach GlitchVisualDisplacer ที่ root
/// </summary>
public class GlitchZoneVolume : MonoBehaviour
{
    // ---------- Static registry ----------
    private static readonly List<GlitchZoneVolume> activeZones = new List<GlitchZoneVolume>();
    public static IReadOnlyList<GlitchZoneVolume> ActiveZones => activeZones;

    /// <summary>หา offset ของ glitch ณ ตำแหน่งนั้น คืนค่า false ถ้าไม่ได้อยู่ในโซนไหนเลย</summary>
    public static bool TryGetOffsetAt(Vector2 point, out Vector2 offset)
    {
        offset = Vector2.zero;
        bool found = false;

        for (int i = 0; i < activeZones.Count; i++)
        {
            var zone = activeZones[i];
            if (zone == null || !zone.ContainsPoint(point)) continue;

            // ซ้อนกันหลายโซน = อาการหนักขึ้น (บวกกัน)
            offset += zone.GetVisualOffset();
            found = true;
        }

        return found;
    }

    /// <summary>ความรุนแรงรวมของ glitch ณ ตำแหน่งนั้น (0 = ปกติ) ใช้กับเอฟเฟกต์ภาพ/เสียงอื่นได้</summary>
    public static float GetIntensityAt(Vector2 point)
    {
        float total = 0f;
        for (int i = 0; i < activeZones.Count; i++)
        {
            var zone = activeZones[i];
            if (zone != null && zone.ContainsPoint(point)) total += zone.intensity;
        }
        return total;
    }

    // ---------- Instance ----------
    [Header("Zone")]
    [SerializeField] private Collider2D zoneCollider;
    [Range(0f, 1f)]
    [Tooltip("ความรุนแรงของโซนนี้ ใช้คูณกับ offset และให้ระบบอื่นอ่านไปทำเอฟเฟกต์เสริม")]
    [SerializeField] private float intensity = 1f;

    [Header("Offset")]
    [SerializeField] private float maxVisualOffset = 0.4f;
    [Tooltip("สุ่ม offset ใหม่ทุกกี่วินาที — ค่าน้อย = สั่นถี่")]
    [SerializeField] private float offsetChangeInterval = 0.3f;
    [Tooltip("ไล่ค่านุ่ม ๆ แทนการกระตุกทันที (ปิดไว้จะได้ฟีล glitch แบบ digital มากกว่า)")]
    [SerializeField] private bool smoothOffset = false;
    [SerializeField] private float smoothSpeed = 12f;

    private Vector2 currentOffset;
    private Vector2 targetOffset;
    private float timer;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (zoneCollider == null) zoneCollider = GetComponent<Collider2D>();
        RandomizeOffset();
        currentOffset = targetOffset;
    }

    private void OnEnable()
    {
        if (!activeZones.Contains(this)) activeZones.Add(this);
    }

    private void OnDisable()
    {
        activeZones.Remove(this);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= offsetChangeInterval)
        {
            timer = 0f;
            RandomizeOffset();
            if (!smoothOffset) currentOffset = targetOffset;
        }

        if (smoothOffset)
        {
            currentOffset = Vector2.Lerp(currentOffset, targetOffset, Time.deltaTime * smoothSpeed);
        }
    }

    private void RandomizeOffset()
    {
        targetOffset = Random.insideUnitCircle * maxVisualOffset * intensity;
    }

    public bool ContainsPoint(Vector2 point)
    {
        return zoneCollider != null && zoneCollider.OverlapPoint(point);
    }

    /// <summary>offset ที่ควรบวกเข้ากับตำแหน่งภาพ (sprite) ของวัตถุที่อยู่ในโซนนี้ ณ ขณะนี้</summary>
    public Vector2 GetVisualOffset() => currentOffset;

    public float Intensity => intensity;

    private void OnDrawGizmosSelected()
    {
        var col = zoneCollider != null ? zoneCollider : GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.2f, 0.6f, 0.25f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}
