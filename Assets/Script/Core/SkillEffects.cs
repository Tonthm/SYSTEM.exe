using UnityEngine;

/// <summary>
/// ศูนย์รวม id ของสกิลทั้งหมด + ตัวช่วยอ่านค่าสกิล
///
/// ทุกระบบที่ได้รับผลจากสกิลเรียกผ่านคลาสนี้ที่เดียว ไม่ต้องพิมพ์ id เป็นสตริงกระจายทั่วโปรเจกต์
/// (พิมพ์ id ผิดแล้วสกิลจะเงียบ ๆ ไม่ทำงาน หาบั๊กยากมาก)
///
/// เพิ่มสกิลใหม่ให้ทำ 3 อย่าง:
/// 1. เพิ่ม const id ที่นี่
/// 2. เพิ่ม SkillNode ใน SkillTreeManager (Inspector หรือค่าเริ่มต้นในโค้ด)
/// 3. ไปเช็ค SkillEffects.IsUnlocked(...) ในระบบที่เกี่ยวข้อง
/// </summary>
public static class SkillEffects
{
    // ── สาย Fragment ──
    public const string FragmentTimerExtend = "fragment_timer_extend";
    public const string FragmentBeacon      = "fragment_beacon";

    // ── สาย Corruption ──
    public const string CorruptionShield    = "corruption_shield";
    public const string CorruptionPurge     = "corruption_purge";

    // ── สาย Resistance ──
    public const string PatternAnalyzer     = "pattern_analyzer";
    public const string ResistanceCap       = "resistance_cap";

    // ── สาย Combat ──
    public const string DodgeBoost          = "dodge_boost";
    public const string OverclockStreak     = "overclock_streak";

    /// <summary>ปลอดภัยต่อการที่ SkillTreeManager ยังไม่มีในฉาก (เช่นตอนทดสอบด่านเดี่ยว)</summary>
    public static bool IsUnlocked(string skillId)
    {
        return SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsUnlocked(skillId);
    }

    /// <summary>คืน multiplier ตามว่าปลดล็อกสกิลนั้นหรือยัง</summary>
    public static float Multiplier(string skillId, float whenUnlocked, float whenLocked = 1f)
    {
        return IsUnlocked(skillId) ? whenUnlocked : whenLocked;
    }
}
