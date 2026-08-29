using UnityEngine;

/// <summary>
/// จัดการการดรอป Data Fragment ตรงจุดที่ผู้เล่นตาย และให้ Process ใหม่เก็บคืนได้ภายในเวลาจำกัด
/// ถ้าเก็บไม่ทัน ไอเทม/ความสามารถที่อยู่ใน fragment จะหายถาวรในรอบนั้น
///
/// [อัปเดต] ตอนนี้ดึงของจริงจาก RunInventory (อาวุธ/ไอเทมของรอบ) ไม่ใช่แค่ temp XP
/// และรองรับ skill "fragment_timer_extend" จาก Skill Tree ที่ยืดเวลาเก็บคืน
/// วิธีติดตั้ง: อยู่ Scene หลัก (persistent) + ต้องมี DataFragment.cs attach บน Prefab ของ fragment
/// </summary>
public class FragmentInheritanceManager : MonoBehaviour
{
    public static FragmentInheritanceManager Instance { get; private set; }

    [SerializeField] private GameObject dataFragmentPrefab;

    [Header("Lifetime")]
    [Tooltip("เวลาที่ fragment คงอยู่ก่อนหายไปถาวร (วินาที)")]
    [SerializeField] private float fragmentLifetime = 30f;
    [Tooltip("เวลาที่เพิ่มให้ ถ้าปลดล็อก skill 'fragment_timer_extend' แล้ว")]
    [SerializeField] private float extendedLifetimeBonus = 15f;

    [Header("Behaviour")]
    [Tooltip("ดรอป fragment แม้ตอนตายไม่ได้ถืออะไรเลย (ปิดไว้จะไม่รกฉาก)")]
    [SerializeField] private bool dropEmptyFragments = false;

    /// <summary>fragment ล่าสุดที่ยังอยู่ในฉาก (ให้ HUD ชี้ทิศทาง/นับถอยหลังได้)</summary>
    public DataFragment ActiveFragment { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void DropFragmentAt(Vector2 position)
    {
        if (dataFragmentPrefab == null)
        {
            Debug.LogWarning("[Fragment Inheritance] ยังไม่ได้ลาก Data Fragment Prefab เข้า Inspector");
            return;
        }

        // รวบของทั้งหมดที่ผู้เล่นถืออยู่ตอนตาย: temp XP + ไอเทม/อาวุธของรอบนี้
        var payload = new FragmentPayload
        {
            tempXP = XPManager.Instance != null ? XPManager.Instance.ConsumeRunTempXP() : 0,
            items = RunInventory.Instance != null
                ? RunInventory.Instance.TakeAll()
                : new System.Collections.Generic.List<RunItem>()
        };

        if (payload.IsEmpty && !dropEmptyFragments)
        {
            Debug.Log("[Fragment Inheritance] ตายมือเปล่า — ไม่ดรอป fragment");
            return;
        }

        float lifetime = fragmentLifetime;
        if (SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsUnlocked("fragment_timer_extend"))
        {
            lifetime += extendedLifetimeBonus;
        }

        GameObject fragmentObj = Instantiate(dataFragmentPrefab, position, Quaternion.identity);
        DataFragment fragment = fragmentObj.GetComponent<DataFragment>();
        if (fragment != null)
        {
            fragment.Initialize(payload, lifetime);
            ActiveFragment = fragment;
        }

        Debug.Log($"[Fragment Inheritance] Dropped fragment at {position} carrying {payload.Describe()}, expires in {lifetime}s");
    }
}
