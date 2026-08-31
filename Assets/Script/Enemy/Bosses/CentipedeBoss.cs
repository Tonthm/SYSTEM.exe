using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// บอสประจำด่าน RAM — ตะขาบ 8 ปล้อง แต่ละปล้องมีปืนฝั่งละ 2 กระบอก
///
/// กลไก: ยิงปล้องกลางลำตัวแล้วบอสจะ **แยกเป็น 2 ตัว** ที่เคลื่อนที่อิสระ
/// (ยิงปล้อง 5 → ได้ตัว 1-4 กับตัว 6-8) แยกได้เรื่อย ๆ จนเหลือปล้องเดี่ยว
///
/// การตัดสินใจของผู้เล่น: ยิงหัวก่อน = โซ่สั้นลงทีละปล้อง จัดการง่ายแต่ช้า
/// ยิงกลาง = แยกร่างเร็ว แต่ต้องรับมือ 2 ทิศทางพร้อมกัน
///
/// สคริปต์นี้เป็นตัวคุมรวม (ไม่มี Collider ยิงไม่โดน) ทำหน้าที่:
/// สร้างโซ่ / ลงทะเบียนทุกปล้องกับ WaveManager / ตายเมื่อทุกปล้องหมด
///
/// วิธีติดตั้ง:
/// 1. GameObject ชื่อ "CentipedeBoss" — **ไม่ต้องมี Collider**
/// 2. Attach: EnemyHealth (Xp Reward = 0, ไม่มี Collider จึงยิงไม่โดน), CentipedeBoss
/// 3. ลาก CentipedeSegment Prefab เข้าช่อง Segment Prefab
/// 4. ใช้ GameObject นี้เป็น Enemy Prefab ของ wave 10
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class CentipedeBoss : MonoBehaviour
{
    [Header("Chain")]
    [SerializeField] private GameObject segmentPrefab;
    [SerializeField] private int segmentCount = 8;
    [Tooltip("ระยะห่างตอนวางปล้องเริ่มต้น")]
    [SerializeField] private float initialSpacing = 0.9f;
    [Tooltip("ทิศที่วางลำตัวตอนเกิด (ปกติวางไปทางขวาของหัว)")]
    [SerializeField] private Vector2 initialBodyDirection = Vector2.right;

    [Header("Scaling ต่อปล้อง")]
    [Tooltip("ปล้องหัวแข็งกว่าปล้องท้ายกี่เท่า (1 = เท่ากันหมด)")]
    [SerializeField] private float headHealthMultiplier = 1f;

    private readonly List<EnemyHealth> segments = new List<EnemyHealth>();
    private EnemyHealth keeperHealth;
    private bool finished;

    /// <summary>จำนวนปล้องที่ยังไม่ตาย (ให้ UI หลอดเลือดบอสไปใช้)</summary>
    public int RemainingSegments => segments.Count;
    public int TotalSegments { get; private set; }

    public System.Action<int, int> OnSegmentCountChanged;   // (เหลือ, ทั้งหมด)

    private void Awake()
    {
        keeperHealth = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        if (segmentPrefab == null)
        {
            Debug.LogWarning("[Centipede Boss] ยังไม่ได้ลาก Segment Prefab");
            return;
        }

        BuildChain();
    }

    private void BuildChain()
    {
        Vector2 dir = initialBodyDirection.normalized;
        CentipedeSegment previous = null;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 pos = transform.position + (Vector3)(dir * initialSpacing * i);
            GameObject obj = Instantiate(segmentPrefab, pos, Quaternion.identity);
            obj.name = $"Segment_{i + 1}";

            var segment = obj.GetComponent<CentipedeSegment>();
            if (segment != null) segment.Setup(this, i, previous);
            previous = segment;

            var segHealth = obj.GetComponent<EnemyHealth>();
            if (segHealth != null)
            {
                if (i == 0 && !Mathf.Approximately(headHealthMultiplier, 1f))
                {
                    segHealth.ApplyScaling(headHealthMultiplier, 1f);
                }

                segments.Add(segHealth);
                // สำคัญ: ลงทะเบียนเองเพราะ WaveManager ไม่ได้เป็นคน spawn ปล้อง
                WaveManager.Instance?.RegisterEnemy(segHealth);
            }
        }

        TotalSegments = segments.Count;
        OnSegmentCountChanged?.Invoke(RemainingSegments, TotalSegments);
        Debug.Log($"[Centipede Boss] สร้างโซ่ {TotalSegments} ปล้อง");
    }

    /// <summary>CentipedeSegment เรียกตอนตัวเองตาย</summary>
    public void OnSegmentDied(CentipedeSegment segment)
    {
        if (segment == null) return;

        var segHealth = segment.GetComponent<EnemyHealth>();
        if (segHealth != null) segments.Remove(segHealth);

        segments.RemoveAll(s => s == null);
        OnSegmentCountChanged?.Invoke(RemainingSegments, TotalSegments);

        if (segments.Count == 0) Finish();
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;

        Debug.Log("[Centipede Boss] ทุกปล้องถูกกำจัดแล้ว");

        // ฆ่าตัวคุมรวมเพื่อให้ WaveManager นับว่าบอสตายครบ
        if (keeperHealth != null && !keeperHealth.IsDead)
        {
            keeperHealth.TakeDamage(keeperHealth.MaxHealth * 10f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.8f);
        Vector2 dir = initialBodyDirection.normalized;
        for (int i = 0; i < segmentCount; i++)
        {
            Gizmos.DrawWireSphere(transform.position + (Vector3)(dir * initialSpacing * i), 0.3f);
        }
    }
}
