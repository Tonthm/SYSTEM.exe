using UnityEngine;

/// <summary>ประเภทของไอเทมที่เก็บได้ระหว่างรอบการเล่น</summary>
public enum RunItemType
{
    WeaponUpgrade,  // อัปเกรดอาวุธ (fire rate, damage, จำนวนกระสุน, การกระจาย)
    PassiveBuff,    // บัฟติดตัว (ความเร็วเคลื่อนที่)
    Consumable      // ไอเทมใช้แล้วหมด (เก็บไว้ในกระเป๋าจนกว่าจะใช้)
}

/// <summary>
/// ไอเทม/อาวุธ 1 ชิ้นที่เก็บได้ระหว่างรอบ (run-scoped) — หายเมื่อตาย เว้นแต่เก็บ Data Fragment คืนทัน
/// ค่าคูณทั้งหมดเป็นแบบ "คูณสะสม" กับสเตตพื้นฐานของอาวุธ (1 = ไม่เปลี่ยน)
/// สร้างเป็นข้อมูลใน Inspector ของ ItemPickup หรือสร้างจากโค้ดก็ได้
/// </summary>
[System.Serializable]
public class RunItem
{
    public string id = "item";
    public string displayName = "Unknown Fragment";
    public RunItemType type = RunItemType.WeaponUpgrade;

    [Header("Weapon Modifiers (ใช้เมื่อ type = WeaponUpgrade)")]
    public float fireRateMultiplier = 1f;
    public float damageMultiplier = 1f;
    public int bonusBulletsPerShot = 0;
    [Tooltip("องศาที่บวกเข้ากับการกระจายกระสุน — ควรบวกคู่กับ bonusBulletsPerShot")]
    public float bonusSpreadAngle = 0f;

    [Header("Passive Modifiers (ใช้เมื่อ type = PassiveBuff)")]
    public float moveSpeedMultiplier = 1f;

    public RunItem Clone()
    {
        return new RunItem
        {
            id = id,
            displayName = displayName,
            type = type,
            fireRateMultiplier = fireRateMultiplier,
            damageMultiplier = damageMultiplier,
            bonusBulletsPerShot = bonusBulletsPerShot,
            bonusSpreadAngle = bonusSpreadAngle,
            moveSpeedMultiplier = moveSpeedMultiplier
        };
    }
}
