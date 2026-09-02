using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ปุ่มซื้อสกิล 1 อัน — ทำ Prefab ปุ่มเองแล้ววางในฉาก ตั้ง Skill Id ว่าปุ่มนี้คือสกิลอะไร
///
/// ใช้แทน SkillSelectUI ที่สร้างรายการอัตโนมัติ — วิธีนี้จัดผังต้นไม้สกิลเองได้อิสระ
/// (ลากเส้นเชื่อม วางเป็นแผนผัง ใส่ไอคอนแยกแต่ละอัน)
///
/// วิธีติดตั้ง:
/// 1. ทำ Prefab ปุ่ม: Button - TextMeshPro + ไอคอน + Text ราคา
/// 2. Attach สคริปต์นี้บนปุ่ม
/// 3. วางปุ่มในแผงสกิล แล้วกรอก Skill Id ให้ตรงกับใน SkillTreeManager
///    (ก็อปจากตาราง SkillEffects ได้เลย เช่น dodge_boost)
/// 4. ไม่ต้องผูก onClick เอง สคริปต์ผูกให้ใน Awake
///
/// id ที่ใช้ได้: dodge_boost, overclock_streak, fragment_timer_extend, fragment_beacon,
///              pattern_analyzer, resistance_cap, corruption_shield, corruption_purge
/// </summary>
public class SkillPurchaseButton : MonoBehaviour
{
    /// <summary>ยิงเมื่อมีการซื้อสกิลสำเร็จ — ปุ่มทุกอันฟังเพื่อรีเฟรชตัวเอง</summary>
    public static System.Action OnAnySkillPurchased;

    [Header("Skill")]
    [Tooltip("ต้องตรงกับ id ใน SkillTreeManager เป๊ะ")]
    [SerializeField] private string skillId = "dodge_boost";

    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("ไอคอน/กรอบที่จะเปลี่ยนสีตามสถานะ")]
    [SerializeField] private Image frameImage;
    [Tooltip("เครื่องหมายถูก แสดงเมื่อปลดล็อกแล้ว")]
    [SerializeField] private GameObject unlockedMark;
    [Tooltip("แม่กุญแจ แสดงเมื่อยังปลดล็อกสกิลก่อนหน้าไม่ได้")]
    [SerializeField] private GameObject lockedMark;

    [Header("Colors")]
    [SerializeField] private Color unlockedColor = new Color(0.3f, 1f, 0.6f);
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.45f);

    [Header("Feedback")]
    [SerializeField] private GameObject purchaseEffectPrefab;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(TryPurchase);
    }

    private void OnEnable()
    {
        OnAnySkillPurchased += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        OnAnySkillPurchased -= Refresh;
    }

    public void TryPurchase()
    {
        if (SkillTreeManager.Instance == null) return;

        if (SkillTreeManager.Instance.TryUnlock(skillId))
        {
            if (purchaseEffectPrefab != null) Instantiate(purchaseEffectPrefab, transform.position, Quaternion.identity, transform);
            OnAnySkillPurchased?.Invoke();   // ให้ปุ่มลูกในสายรีเฟรชตาม
        }
        else
        {
            Debug.Log($"[Skill Button] ซื้อไม่ได้: {SkillTreeManager.Instance.GetLockReason(skillId)}");
        }
    }

    /// <summary>อัปเดตหน้าตาปุ่มตามสถานะปัจจุบัน</summary>
    public void Refresh()
    {
        if (SkillTreeManager.Instance == null) return;

        var skill = SkillTreeManager.Instance.GetSkill(skillId);
        if (skill == null)
        {
            Debug.LogWarning($"[Skill Button] ไม่พบสกิล id = {skillId} ใน SkillTreeManager");
            return;
        }

        bool unlocked = skill.unlocked;
        bool canUnlock = SkillTreeManager.Instance.CanUnlock(skillId);

        if (nameText != null) nameText.text = skill.displayName;
        if (descriptionText != null) descriptionText.text = skill.description;

        if (costText != null)
        {
            costText.text = unlocked ? "UNLOCKED" : $"{skill.cost} XP";
        }

        if (button != null) button.interactable = !unlocked && canUnlock;

        if (frameImage != null)
        {
            frameImage.color = unlocked ? unlockedColor : (canUnlock ? affordableColor : lockedColor);
        }

        if (unlockedMark != null) unlockedMark.SetActive(unlocked);
        if (lockedMark != null) lockedMark.SetActive(!unlocked && !canUnlock);
    }
}
