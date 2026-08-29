using System.Collections.Generic;

/// <summary>
/// ข้อมูลที่ Data Fragment "แบก" ไว้ตอนผู้เล่นตาย
/// เดิมส่งแค่ int (temp XP) — ตอนนี้รองรับทั้ง XP ชั่วคราวและรายการไอเทม/อาวุธจาก RunInventory
/// </summary>
public class FragmentPayload
{
    public int tempXP;
    public List<RunItem> items = new List<RunItem>();

    public bool IsEmpty => tempXP <= 0 && (items == null || items.Count == 0);

    public int ItemCount => items != null ? items.Count : 0;

    /// <summary>ข้อความสรุปสำหรับ UI/Debug เช่น "45 XP + 3 items"</summary>
    public string Describe()
    {
        if (IsEmpty) return "empty";
        if (ItemCount == 0) return $"{tempXP} XP";
        if (tempXP <= 0) return $"{ItemCount} items";
        return $"{tempXP} XP + {ItemCount} items";
    }
}
