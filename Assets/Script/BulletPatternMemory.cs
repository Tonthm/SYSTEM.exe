using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "ระบบภูมิคุ้มกัน" — เมื่อผู้เล่นตายจาก Bullet Pattern ใดซ้ำ Process ใหม่จะได้รับ
/// resistance (ลดดาเมจ) ต่อแพทเทิร์นนั้นเล็กน้อย ค่านี้ถาวรตลอดรอบการเล่น (รีเซ็ตเมื่อเริ่มรันใหม่
/// หรือเมื่อโดน Force Format จาก Corruption Meter)
/// วิธีติดตั้ง: อยู่ใน Scene หลักเดียวกับ DeathLogManager, ตั้งเป็น persistent (DontDestroyOnLoad)
/// </summary>
public class BulletPatternMemory : MonoBehaviour
{
    public static BulletPatternMemory Instance { get; private set; }

    [Header("Resistance Settings")]
    [Tooltip("เพิ่ม resistance เท่านี้ทุกครั้งที่ตายซ้ำจากแพทเทิร์นเดิม (0.05 = ลดดาเมจ 5%)")]
    [SerializeField] private float resistancePerDeath = 0.05f;
    [Tooltip("resistance สูงสุดที่ทำได้ต่อหนึ่งแพทเทิร์น กันไม่ให้กลายเป็นอมตะ")]
    [SerializeField] private float maxResistance = 0.5f;

    private Dictionary<BulletPatternType, float> resistanceMap = new Dictionary<BulletPatternType, float>();
    private Dictionary<BulletPatternType, int> deathCountMap = new Dictionary<BulletPatternType, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>เรียกตอนผู้เล่นตาย คืนค่า true ถ้าการตายครั้งนี้ทำให้ resistance เพิ่มขึ้นจริง (ยังไม่ชนเพดาน)</summary>
    public bool RegisterDeath(BulletPatternType cause)
    {
        if (!resistanceMap.ContainsKey(cause)) resistanceMap[cause] = 0f;
        if (!deathCountMap.ContainsKey(cause)) deathCountMap[cause] = 0;

        deathCountMap[cause]++;

        if (resistanceMap[cause] >= maxResistance)
        {
            return false; // ชนเพดานแล้ว ไม่มีการพัฒนาเพิ่ม (มีผลต่อ Corruption Meter)
        }

        resistanceMap[cause] = Mathf.Min(maxResistance, resistanceMap[cause] + resistancePerDeath);
        return true;
    }

    /// <summary>คืนค่า resistance ปัจจุบัน (0 - maxResistance) ของแพทเทิร์นนั้น</summary>
    public float GetResistance(BulletPatternType pattern)
    {
        return resistanceMap.TryGetValue(pattern, out float value) ? value : 0f;
    }

    public int GetDeathCount(BulletPatternType pattern)
    {
        return deathCountMap.TryGetValue(pattern, out int value) ? value : 0;
    }

    /// <summary>เรียกตอน Force Format (Corruption Meter เต็ม) เพื่อรีเซ็ตความต้านทานทั้งหมดของรอบนี้</summary>
    public void ResetAll()
    {
        resistanceMap.Clear();
        deathCountMap.Clear();
    }
}
