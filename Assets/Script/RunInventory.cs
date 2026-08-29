using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// กระเป๋าไอเทม/อาวุธ "ของรอบนี้" (run-scoped) ตามดีไซน์ Roguelite:
/// ไอเทมที่เก็บระหว่างรอบจะถูกรีเซ็ตเมื่อตาย เว้นแต่จะกลับไปเก็บ Data Fragment คืนทัน
///
/// ระบบอื่นดึงค่ารวมไปใช้ผ่าน GetXxxMultiplier() แทนที่จะไล่ list เอง
/// และฟัง OnInventoryChanged เพื่อคำนวณสเตตใหม่ (เช่น PlayerShooter)
///
/// วิธีติดตั้ง: สร้าง Empty GameObject ชื่อ "RunInventory" ใน Scene หลัก attach สคริปต์นี้
/// (มี DontDestroyOnLoad ในตัวเหมือน manager ตัวอื่น)
/// </summary>
public class RunInventory : MonoBehaviour
{
    public static RunInventory Instance { get; private set; }

    [Tooltip("จำนวนไอเทมสูงสุดที่ถือได้ในหนึ่งรอบ (0 = ไม่จำกัด)")]
    [SerializeField] private int maxItems = 0;

    private readonly List<RunItem> items = new List<RunItem>();

    public IReadOnlyList<RunItem> Items => items;
    public int Count => items.Count;

    /// <summary>ยิงทุกครั้งที่รายการไอเทมเปลี่ยน — ให้ PlayerShooter/HUD คำนวณใหม่</summary>
    public System.Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool AddItem(RunItem item)
    {
        if (item == null) return false;
        if (maxItems > 0 && items.Count >= maxItems)
        {
            Debug.Log("[Run Inventory] เต็มแล้ว — เก็บไอเทมเพิ่มไม่ได้");
            return false;
        }

        items.Add(item.Clone());
        OnInventoryChanged?.Invoke();
        Debug.Log($"[Run Inventory] +{item.displayName} (ทั้งหมด {items.Count} ชิ้น)");
        return true;
    }

    public bool RemoveItem(string itemId)
    {
        int index = items.FindIndex(i => i.id == itemId);
        if (index < 0) return false;

        items.RemoveAt(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool Has(string itemId) => items.Exists(i => i.id == itemId);

    /// <summary>เรียกตอนตาย — ดึงไอเทมทั้งหมดออกไปใส่ Data Fragment แล้วล้างกระเป๋า</summary>
    public List<RunItem> TakeAll()
    {
        var taken = new List<RunItem>(items);
        items.Clear();
        OnInventoryChanged?.Invoke();
        return taken;
    }

    /// <summary>เรียกตอนเก็บ Data Fragment คืนได้ทัน — คืนไอเทมเข้ากระเป๋า</summary>
    public void RestoreAll(List<RunItem> restored)
    {
        if (restored == null || restored.Count == 0) return;

        foreach (var item in restored)
        {
            if (maxItems > 0 && items.Count >= maxItems) break;
            items.Add(item);
        }
        OnInventoryChanged?.Invoke();
        Debug.Log($"[Run Inventory] กู้คืน {restored.Count} ชิ้น");
    }

    /// <summary>เรียกตอนเริ่มรอบใหม่ทั้งหมด หรือตอนโดน Force Format</summary>
    public void ClearRun()
    {
        if (items.Count == 0) return;
        items.Clear();
        OnInventoryChanged?.Invoke();
        Debug.Log("[Run Inventory] ล้างไอเทมของรอบนี้ทั้งหมด");
    }

    // ---------- ค่ารวมสำหรับระบบอื่นดึงไปใช้ ----------

    public float GetFireRateMultiplier()
    {
        float m = 1f;
        foreach (var i in items) if (i.type == RunItemType.WeaponUpgrade) m *= i.fireRateMultiplier;
        return m;
    }

    public float GetDamageMultiplier()
    {
        float m = 1f;
        foreach (var i in items) if (i.type == RunItemType.WeaponUpgrade) m *= i.damageMultiplier;
        return m;
    }

    public int GetBonusBulletsPerShot()
    {
        int total = 0;
        foreach (var i in items) if (i.type == RunItemType.WeaponUpgrade) total += i.bonusBulletsPerShot;
        return total;
    }

    public float GetBonusSpreadAngle()
    {
        float total = 0f;
        foreach (var i in items) if (i.type == RunItemType.WeaponUpgrade) total += i.bonusSpreadAngle;
        return total;
    }

    public float GetMoveSpeedMultiplier()
    {
        float m = 1f;
        foreach (var i in items) if (i.type == RunItemType.PassiveBuff) m *= i.moveSpeedMultiplier;
        return m;
    }
}
