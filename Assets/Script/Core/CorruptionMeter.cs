using UnityEngine;

/// <summary>
/// Corruption Meter — ตัวนับการตายสะสมของรอบปัจจุบัน
///
/// [เปลี่ยนกติกาใหม่] เดิมนับเฉพาะ "ตายโดยไม่ได้ resistance ใหม่" ซึ่งเลี่ยงง่ายเกินไป
/// (สลับตายจากแพทเทิร์นใหม่ไปเรื่อย ๆ ค่าไม่มีวันเต็ม) และโทษคือล้าง resistance
/// ซึ่งลงโทษคนที่ติดอยู่แล้วให้ยิ่งเล่นยากขึ้น
///
/// กติกาปัจจุบัน เข้าใจง่ายและยุติธรรมกว่า:
///   ตาย 1 ครั้ง            → Corruption +1  (ไม่สนว่าตายจากอะไร)
///   เคลียร์ wave แบบไม่ตาย → Corruption -1
///   เต็ม                    → Force Format
///
/// ผู้เล่นจึงกดดันให้เล่นให้รอด ไม่ใช่ให้ตายให้ถูกวิธี
/// และมีทางแก้ตัวชัดเจน: เคลียร์ wave ให้ได้โดยไม่ตาย
///
/// วิธีติดตั้ง: อยู่ Scene หลัก เป็น persistent เหมือน manager ตัวอื่น
/// </summary>
public class CorruptionMeter : MonoBehaviour
{
    public static CorruptionMeter Instance { get; private set; }

    [Header("Meter")]
    [Tooltip("ตายสะสมกี่ครั้งก่อนเกิด Force Format")]
    [SerializeField] private int maxCorruptionCount = 5;
    [Tooltip("เคลียร์ wave โดยไม่ตาย ลด Corruption ลงกี่ขีด")]
    [SerializeField] private int cleanWaveReward = 1;

    [Header("Force Format")]
    [Tooltip("ล้างภูมิต้านทานแพทเทิร์นตอน Force Format — ปิดไว้ (ลงโทษคนที่ติดอยู่แล้วซ้ำเติม)")]
    [SerializeField] private bool wipeResistanceOnFormat = false;
    [Tooltip("ล้างไอเทมของรอบตอน Force Format")]
    [SerializeField] private bool wipeRunInventoryOnFormat = true;
    [Tooltip("ล้าง temp XP ที่ถืออยู่ตอน Force Format")]
    [SerializeField] private bool wipeTempXPOnFormat = true;

    public int CurrentCorruption { get; private set; } = 0;
    public int MaxCorruption => maxCorruptionCount;
    public float Percent => maxCorruptionCount > 0 ? (float)CurrentCorruption / maxCorruptionCount : 0f;

    /// <summary>ตายไปกี่ครั้งแล้วในด่านปัจจุบัน (ใช้กับสกิล Registry Cleaner)</summary>
    public int DeathsThisSector { get; private set; } = 0;

    /// <summary>โล่ของสกิล Corruption Shield ยังใช้ได้อยู่ไหมในด่านนี้</summary>
    public bool ShieldAvailable { get; private set; } = true;

    public System.Action<int, int> OnCorruptionChanged; // (current, max)
    public System.Action OnForceFormat;
    /// <summary>ยิงตอนสกิล Corruption Shield กัน Force Format ไว้ได้</summary>
    public System.Action OnFormatBlocked;
    /// <summary>ยิงตอน Corruption ลดลง (เคลียร์ wave ไม่ตาย / Registry Cleaner)</summary>
    public System.Action<int> OnCorruptionPurged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <param name="gainedNewResistance">
    /// ไม่มีผลต่อตัวนับแล้ว (กติกาใหม่นับทุกการตาย) แต่คงพารามิเตอร์ไว้
    /// เพื่อไม่ให้ PlayerHealth ที่เรียกอยู่พัง และเผื่อ UI อยากรู้
    /// </param>
    public void RegisterDeath(bool gainedNewResistance = false)
    {
        DeathsThisSector++;
        CurrentCorruption++;

        Debug.Log($"[Corruption Meter] ตาย +1 → {CurrentCorruption}/{maxCorruptionCount}");
        OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruptionCount);

        if (CurrentCorruption >= maxCorruptionCount) TriggerForceFormat();
    }

    /// <summary>
    /// WaveManager เรียกตอน wave จบ — เคลียร์ได้โดยไม่ตายเลยจะลด Corruption ให้
    /// นี่คือทางแก้ตัวหลักของผู้เล่น
    /// </summary>
    public void OnWaveCleared(bool diedThisWave)
    {
        if (diedThisWave || CurrentCorruption <= 0) return;

        CurrentCorruption = Mathf.Max(0, CurrentCorruption - cleanWaveReward);

        Debug.Log($"[Corruption Meter] เคลียร์ wave แบบไม่ตาย → Corruption เหลือ {CurrentCorruption}");
        OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruptionCount);
        OnCorruptionPurged?.Invoke(CurrentCorruption);
    }

    private void TriggerForceFormat()
    {
        // ── สกิล Corruption Shield: กันไว้ได้ 1 ครั้งต่อด่าน ──
        if (ShieldAvailable && SkillEffects.IsUnlocked(SkillEffects.CorruptionShield))
        {
            ShieldAvailable = false;
            CurrentCorruption = 0;

            Debug.Log("[Corruption Meter] CORRUPTION SHIELD — กัน Force Format ไว้ได้ (โล่หมดแล้วในด่านนี้)");
            OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruptionCount);
            OnFormatBlocked?.Invoke();
            return;
        }

        Debug.Log("[Corruption Meter] FORCE FORMAT triggered");
        AudioManager.Play(AudioIds.ForceFormat);

        if (wipeResistanceOnFormat) BulletPatternMemory.Instance?.ResetAll();
        if (wipeRunInventoryOnFormat) RunInventory.Instance?.ClearRun(preservePermanent: true);
        if (wipeTempXPOnFormat) XPManager.Instance?.ResetRunTempXP();

        SectorPoolManager.Instance?.ResetRunProgress();
        // หมายเหตุ: Permanent XP และ Skill Tree ถาวร ไม่ถูกแตะต้อง ตามดีไซน์ในเอกสาร

        CurrentCorruption = 0;
        OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruptionCount);
        OnForceFormat?.Invoke();
    }

    /// <summary>
    /// เรียกจาก GameManager เมื่อผู้เล่นผ่านด่าน
    /// - คืนโล่ Corruption Shield
    /// - สกิล Registry Cleaner: ผ่านด่านโดยไม่ตายเลย ลด Corruption เพิ่มอีก 1
    /// </summary>
    public void OnSectorCleared()
    {
        if (SkillEffects.IsUnlocked(SkillEffects.CorruptionPurge)
            && DeathsThisSector == 0
            && CurrentCorruption > 0)
        {
            CurrentCorruption--;
            Debug.Log($"[Corruption Meter] REGISTRY CLEANER — ผ่านด่านแบบไม่ตาย ลด Corruption เหลือ {CurrentCorruption}");
            OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruptionCount);
            OnCorruptionPurged?.Invoke(CurrentCorruption);
        }

        BeginSector();
    }

    /// <summary>เริ่มด่านใหม่ — รีเซ็ตตัวนับต่อด่าน</summary>
    public void BeginSector()
    {
        DeathsThisSector = 0;
        ShieldAvailable = true;
    }
}