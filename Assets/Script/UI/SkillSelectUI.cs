using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// หน้าจอเลือก Skill สไตล์ "Registry Editor" จำลอง — แสดงรายชื่อ skill จาก SkillTreeManager
/// ให้ผู้เล่นกดปลดล็อกด้วย Permanent XP
///
/// [อัปเดต] ใช้ TextMeshPro — ป้ายบนปุ่มต้องเป็น TMP_Text (Button ของ TMP มีให้อยู่แล้ว)
/// วิธีติดตั้ง: สร้าง Panel + ButtonPrefab (UI > Button - TextMeshPro), attach สคริปต์นี้บน Panel
/// ลาก ButtonPrefab กับ contentParent (เช่น Vertical Layout Group) เข้าช่อง Inspector
/// </summary>
public class SkillSelectUI : MonoBehaviour
{
    [SerializeField] private GameObject skillButtonPrefab;
    [SerializeField] private Transform contentParent;

    private void OnEnable()
    {
        RefreshList();
    }

    private void RefreshList()
    {
        if (SkillTreeManager.Instance == null || contentParent == null || skillButtonPrefab == null) return;

        // ล้างของเก่าก่อน rebuild
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var skill in SkillTreeManager.Instance.GetAllSkills())
        {
            GameObject buttonObj = Instantiate(skillButtonPrefab, contentParent);

            bool canUnlock = SkillTreeManager.Instance.CanUnlock(skill.id);

            TMP_Text label = buttonObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                string status = skill.unlocked ? "[UNLOCKED]" : $"[{skill.cost} XP]";
                string line2 = skill.unlocked
                    ? skill.description
                    : (canUnlock ? skill.description : SkillTreeManager.Instance.GetLockReason(skill.id));

                // ใช้ rich text ของ TMP ทำบรรทัดคำอธิบายให้เล็กลง
                label.text = $"{skill.displayName} {status}\n<size=70%>{line2}</size>";
            }

            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = !skill.unlocked && canUnlock;
                string skillId = skill.id; // local copy สำหรับ closure
                btn.onClick.AddListener(() =>
                {
                    if (SkillTreeManager.Instance.TryUnlock(skillId))
                    {
                        RefreshList();
                    }
                });
            }
        }
    }
}
