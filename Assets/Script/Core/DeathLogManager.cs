using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// บันทึกสาเหตุการตายทุกครั้ง เพื่อแสดงผลผ่านหน้าต่าง Task Manager จำลอง (DeathScreenUI)
///
/// [อัปเดต] เก็บ "ชื่อตัวที่ฆ่า" ด้วย ทำให้ข้อความตรงกับ storyboard
/// เช่น "Cause of death: Pop-up Swarmer collision" แทนที่จะบอกแค่ชนิดแพทเทิร์น
/// วิธีติดตั้ง: สร้าง Empty GameObject ชื่อ "DeathLogManager" attach สคริปต์นี้ ไว้ใน Scene หลัก
/// </summary>
public class DeathLogManager : MonoBehaviour
{
    public static DeathLogManager Instance { get; private set; }

    public struct DeathRecord
    {
        public BulletPatternType cause;
        public Vector2 position;
        public float timeOfDeath;
        /// <summary>ชื่อศัตรู/แหล่งที่ทำให้ตาย เช่น "Pop-up Swarmer" (ว่างได้)</summary>
        public string sourceName;
    }

    public List<DeathRecord> AllTimeDeathRecords { get; private set; } = new List<DeathRecord>();
    public DeathRecord? LastDeath { get; private set; }

    public System.Action<DeathRecord> OnDeathLogged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LogDeath(BulletPatternType cause, Vector2 position)
    {
        LogDeath(cause, position, null);
    }

    public void LogDeath(BulletPatternType cause, Vector2 position, string sourceName)
    {
        var record = new DeathRecord
        {
            cause = cause,
            position = position,
            timeOfDeath = Time.time,
            sourceName = sourceName
        };

        AllTimeDeathRecords.Add(record);
        LastDeath = record;
        OnDeathLogged?.Invoke(record);

        Debug.Log($"[Death Log] Process terminated. Cause: {DescribeCause(record)} at {position}");
    }

    /// <summary>ใช้แสดงข้อความสไตล์ Task Manager เช่น "explorer.exe has stopped working"</summary>
    public string GetDeathMessage(BulletPatternType cause)
    {
        switch (cause)
        {
            case BulletPatternType.Aimed: return "GhostProcess.exe stopped: Direct Fire Exception";
            case BulletPatternType.RadialBurst: return "GhostProcess.exe stopped: Radial Overflow Error";
            case BulletPatternType.Spiral: return "GhostProcess.exe stopped: Spiral Stack Overflow";
            case BulletPatternType.SpreadCone: return "GhostProcess.exe stopped: Cone Access Violation";
            case BulletPatternType.WallSweep: return "GhostProcess.exe stopped: Sweep Segmentation Fault";
            case BulletPatternType.Collision: return "GhostProcess.exe stopped: Memory Access Violation";
            case BulletPatternType.LaserBeam: return "GhostProcess.exe stopped: Registry Key Overwritten";
            default: return "GhostProcess.exe stopped: Unknown Exception";
        }
    }

    public string GetDeathMessage(DeathRecord record) => GetDeathMessage(record.cause);

    /// <summary>บรรทัด "Cause of death:" ตาม storyboard เช่น "Pop-up Swarmer collision"</summary>
    public string DescribeCause(DeathRecord record)
    {
        string action = CauseKeyword(record.cause);

        if (string.IsNullOrEmpty(record.sourceName)) return action;
        return $"{record.sourceName} {action}";
    }

    private string CauseKeyword(BulletPatternType cause)
    {
        switch (cause)
        {
            case BulletPatternType.Aimed: return "aimed shot";
            case BulletPatternType.RadialBurst: return "radial burst";
            case BulletPatternType.Spiral: return "spiral stream";
            case BulletPatternType.SpreadCone: return "spread cone";
            case BulletPatternType.WallSweep: return "wall sweep";
            case BulletPatternType.Collision: return "collision";
            case BulletPatternType.LaserBeam: return "laser beam";
            default: return "unknown fault";
        }
    }
}
