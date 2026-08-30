using UnityEngine;

/// <summary>
/// วัดจำนวนครั้งที่ผู้เล่น "ตายโดยไม่มีการพัฒนา resistance ใหม่" ติดต่อกัน
/// ถ้าเต็ม -> Force Format: รีเซ็ตความก้าวหน้าชั่วคราวของรอบนั้น (ไอเทมของรอบ, resistance)
/// แต่ไม่แตะ Skill Tree ถาวรและ XP สะสม
/// วิธีติดตั้ง: อยู่ Scene หลัก เป็น persistent เหมือน manager ตัวอื่น
/// </summary>
public class CorruptionMeter : MonoBehaviour
{
    public static CorruptionMeter Instance { get; private set; }

    [Tooltip("จำนวนตายติดต่อกันโดยไม่พัฒนา ก่อนเกิด Force Format")]
    [SerializeField] private int maxCorruptionCount = 3;

    public int CurrentCorruption { get; private set; } = 0;
    public int MaxCorruption => maxCorruptionCount;

    public System.Action<int, int> OnCorruptionChanged; // (current, max)
    public System.Action OnForceFormat;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <param name="gainedNewResistance">true ถ้าการตายครั้งนี้ทำให้เกิด resistance ใหม่ (นับว่ามีการเรียนรู้)</param>
    public void RegisterDeath(bool gainedNewResistance)
    {
        if (gainedNewResistance)
        {
            // ตายอย่างมีการเรียนรู้ -> รีเซ็ตตัวนับ
            CurrentCorruption = 0;
        }
        else
        {
            CurrentCorruption++;
        }

        OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruptionCount);

        if (CurrentCorruption >= maxCorruptionCount)
        {
            TriggerForceFormat();
        }
    }

    private void TriggerForceFormat()
    {
        Debug.Log("[Corruption Meter] FORCE FORMAT triggered — resetting temporary run progress");

        BulletPatternMemory.Instance?.ResetAll();
        SectorPoolManager.Instance?.ResetRunProgress();

        // [อัปเดต] ล้างไอเทม/อาวุธของรอบนี้ด้วย — รวมถึง fragment ที่ยังไม่ได้เก็บ
        RunInventory.Instance?.ClearRun();
        XPManager.Instance?.ResetRunTempXP();
        // หมายเหตุ: Permanent XP และ Skill Tree ถาวร ไม่ถูกแตะต้อง ตามดีไซน์ในเอกสาร

        CurrentCorruption = 0;
        OnCorruptionChanged?.Invoke(CurrentCorruption, maxCorruptionCount);
        OnForceFormat?.Invoke();
    }
}
