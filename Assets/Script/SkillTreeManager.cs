using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skill Tree ถาวรที่คงอยู่ข้ามรอบการเล่น ปลดล็อกด้วย Permanent XP (จาก XPManager)
/// ระบบนี้เป็นเวอร์ชันง่าย ๆ (list ของ node) ขยายเป็น ScriptableObject-based tree ได้ภายหลัง
/// วิธีติดตั้ง: อยู่ Scene หลัก (persistent), กำหนดรายชื่อ skill ใน Inspector หรือโค้ด
/// </summary>
public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance { get; private set; }

    [Serializable]
    public class SkillNode
    {
        public string id;
        public string displayName;
        public int cost;
        [HideInInspector] public bool unlocked;
    }

    [SerializeField] private List<SkillNode> skills = new List<SkillNode>
    {
        new SkillNode { id = "keep_resistance", displayName = "Cache Retention (เก็บ resistance ข้ามรอบ)", cost = 50 },
        new SkillNode { id = "faster_pattern_memory", displayName = "Adaptive Immunity (Bullet Pattern Memory มีผลมากขึ้น)", cost = 80 },
        new SkillNode { id = "extra_dash", displayName = "Double Dash", cost = 100 },
        new SkillNode { id = "fragment_timer_extend", displayName = "Extended Cache (เวลาเก็บ Fragment นานขึ้น)", cost = 60 },
    };

    private const string SaveKeyPrefix = "Economice_SYSTEMexe_Skill_";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var skill in skills)
        {
            skill.unlocked = PlayerPrefs.GetInt(SaveKeyPrefix + skill.id, 0) == 1;
        }
    }

    public IReadOnlyList<SkillNode> GetAllSkills() => skills;

    public bool IsUnlocked(string skillId)
    {
        var node = skills.Find(s => s.id == skillId);
        return node != null && node.unlocked;
    }

    /// <summary>พยายามปลดล็อก skill ด้วย Permanent XP — คืนค่า true ถ้าสำเร็จ</summary>
    public bool TryUnlock(string skillId)
    {
        var node = skills.Find(s => s.id == skillId);
        if (node == null || node.unlocked) return false;

        if (XPManager.Instance == null || XPManager.Instance.PermanentXP < node.cost) return false;

        node.unlocked = true;
        PlayerPrefs.SetInt(SaveKeyPrefix + node.id, 1);

        Debug.Log($"[Skill Tree] Unlocked: {node.displayName}");
        return true;
    }
}
