using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Skill Tree ถาวรที่คงอยู่ข้ามรอบการเล่น ปลดล็อกด้วย Permanent XP (จาก XPManager)
///
/// [อัปเดต] สกิลชุดจริง 8 ตัว แบ่งเป็น 4 สาย สายละ 2 ตัว
/// ทุกตัวต่อกับระบบในเกมจริงแล้ว ไม่ใช่ชื่อเปล่า ๆ
///
/// | สาย | ตัวแรก (ถูก) | ตัวที่สอง (แพง) |
/// |-----|--------------|------------------|
/// | Combat     | Dodge Boost      | Overclock        |
/// | Fragment   | Extended Cache   | Fragment Beacon  |
/// | Resistance | Pattern Analyzer | Deep Immunity    |
/// | Corruption | Corruption Shield| Registry Cleaner |
///
/// วิธีติดตั้ง: อยู่ Scene หลัก (persistent) — ค่าเริ่มต้นกรอกมาให้แล้วใน Inspector
/// </summary>
public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance { get; private set; }

    [Serializable]
    public class SkillNode
    {
        public string id;
        public string displayName;
        [TextArea(2, 3)] public string description;
        public int cost;
        [Tooltip("ต้องปลดล็อก skill นี้ก่อน (เว้นว่าง = ปลดล็อกได้เลย)")]
        public string requiresSkillId;
        [HideInInspector] public bool unlocked;
    }

    [SerializeField]
    private List<SkillNode> skills = new List<SkillNode>
    {
        // ── สาย Combat ──
        new SkillNode {
            id = SkillEffects.DodgeBoost,
            displayName = "Dodge Boost",
            description = "Dash cooldown ลดลง 30% (0.80 → 0.56 วินาที)",
            cost = 120
        },
        new SkillNode {
            id = SkillEffects.OverclockStreak,
            displayName = "Overclock",
            description = "โบนัส XP จากการเคลียร์ wave ไว เพิ่มเป็น 2 เท่า",
            cost = 250,
            requiresSkillId = SkillEffects.DodgeBoost
        },

        // ── สาย Fragment ──
        new SkillNode {
            id = SkillEffects.FragmentTimerExtend,
            displayName = "Extended Cache",
            description = "เวลาเก็บ Data Fragment คืน 30 → 45 วินาที",
            cost = 150
        },
        new SkillNode {
            id = SkillEffects.FragmentBeacon,
            displayName = "Fragment Beacon",
            description = "เข้าใกล้ Fragment ในระยะ 3m จะถูกดูดเข้าหาตัวอัตโนมัติ + ลูกศรชี้ทิศ",
            cost = 300,
            requiresSkillId = SkillEffects.FragmentTimerExtend
        },

        // ── สาย Resistance ──
        new SkillNode {
            id = SkillEffects.PatternAnalyzer,
            displayName = "Pattern Analyzer",
            description = "ตาย 1 ครั้งได้ resistance เท่ากับตาย 2 ครั้ง",
            cost = 300
        },
        new SkillNode {
            id = SkillEffects.ResistanceCap,
            displayName = "Deep Immunity",
            description = "เพดาน resistance ต่อแพทเทิร์น 50% → 70%",
            cost = 600,
            requiresSkillId = SkillEffects.PatternAnalyzer
        },

        // ── สาย Corruption ──
        new SkillNode {
            id = SkillEffects.CorruptionShield,
            displayName = "Corruption Shield",
            description = "กัน Force Format ได้ 1 ครั้งต่อด่าน (ใช้แล้วต้องผ่านด่านถึงจะได้คืน)",
            cost = 350
        },
        new SkillNode {
            id = SkillEffects.CorruptionPurge,
            displayName = "Registry Cleaner",
            description = "ผ่านด่านโดยไม่ตายเลย ลด Corruption Meter ลง 1 ขีด",
            cost = 550,
            requiresSkillId = SkillEffects.CorruptionShield
        },
    };

    private const string SaveKeyPrefix = "Economice_SYSTEMexe_Skill_";

    public System.Action<SkillNode> OnSkillUnlocked;

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

    public SkillNode GetSkill(string skillId) => skills.Find(s => s.id == skillId);

    public bool IsUnlocked(string skillId)
    {
        var node = skills.Find(s => s.id == skillId);
        return node != null && node.unlocked;
    }

    /// <summary>ปลดล็อกได้หรือยัง (เงื่อนไขสายสกิล + XP พอ)</summary>
    public bool CanUnlock(string skillId)
    {
        var node = skills.Find(s => s.id == skillId);
        if (node == null || node.unlocked) return false;

        if (!string.IsNullOrEmpty(node.requiresSkillId) && !IsUnlocked(node.requiresSkillId)) return false;

        return XPManager.Instance != null && XPManager.Instance.PermanentXP >= node.cost;
    }

    /// <summary>ข้อความบอกเหตุผลที่ปลดล็อกไม่ได้ (ให้ UI แสดง)</summary>
    public string GetLockReason(string skillId)
    {
        var node = skills.Find(s => s.id == skillId);
        if (node == null) return "ไม่พบสกิลนี้";
        if (node.unlocked) return "ปลดล็อกแล้ว";

        if (!string.IsNullOrEmpty(node.requiresSkillId) && !IsUnlocked(node.requiresSkillId))
        {
            var required = GetSkill(node.requiresSkillId);
            return $"ต้องปลดล็อก {(required != null ? required.displayName : node.requiresSkillId)} ก่อน";
        }

        int xp = XPManager.Instance != null ? XPManager.Instance.PermanentXP : 0;
        if (xp < node.cost) return $"XP ไม่พอ ({xp}/{node.cost})";

        return "";
    }

    /// <summary>พยายามปลดล็อก skill ด้วย Permanent XP — คืนค่า true ถ้าสำเร็จ</summary>
    public bool TryUnlock(string skillId)
    {
        var node = skills.Find(s => s.id == skillId);
        if (node == null || node.unlocked) return false;
        if (!CanUnlock(skillId))
        {
            Debug.Log($"[Skill Tree] ปลดล็อกไม่ได้: {GetLockReason(skillId)}");
            return false;
        }

        XPManager.Instance.SpendPermanentXP(node.cost);

        node.unlocked = true;
        PlayerPrefs.SetInt(SaveKeyPrefix + node.id, 1);
        PlayerPrefs.Save();

        Debug.Log($"[Skill Tree] Unlocked: {node.displayName} (-{node.cost} XP)");
        OnSkillUnlocked?.Invoke(node);
        return true;
    }

    /// <summary>ล้างสกิลทั้งหมด (ใช้ทดสอบ)</summary>
    public void ResetAllSkills()
    {
        foreach (var skill in skills)
        {
            skill.unlocked = false;
            PlayerPrefs.SetInt(SaveKeyPrefix + skill.id, 0);
        }
        PlayerPrefs.Save();
        Debug.Log("[Skill Tree] ล้างสกิลทั้งหมดแล้ว");
    }
}