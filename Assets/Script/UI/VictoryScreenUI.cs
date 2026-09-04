using TMPro;
using UnityEngine;

/// <summary>
/// หน้าจอชนะเกม
///
/// ใช้ได้ 2 แบบ
/// 1. **อยู่ใน VictoryScene แยก** (ค่าเริ่มต้น) — ติ๊ก Show On Start
///    แสดงผลทันทีที่ Scene โหลด โดยอ่านผลรอบล่าสุดจาก LocalLeaderboard
/// 2. **ขึ้นทับฉากบอส** — ปิด Show On Start แล้วรอ event OnGameWon จาก GameManager
///
/// [แก้บั๊ก] เดิมรอ event อย่างเดียว แต่ event ยิงตอนยังอยู่ในด่าน
/// พอโหลด VictoryScene แล้ว UI ตัวนี้เพิ่งเกิด จึงไม่เคยได้ยิน event → ข้อความว่างเปล่า
///
/// วิธีติดตั้ง:
/// 1. ใน VictoryScene สร้าง Panel + Text - TextMeshPro (Title / Stats)
/// 2. Attach สคริปต์นี้บน GameObject ที่ active เสมอ
/// 3. ติ๊ก Show On Start (ถ้าอยู่ใน VictoryScene)
/// </summary>
public class VictoryScreenUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statsText;

    [Header("Mode")]
    [Tooltip("อยู่ใน VictoryScene แยก → เปิดไว้ (แสดงทันทีที่ Scene โหลด)\n" +
             "ขึ้นทับฉากบอส → ปิด แล้วรอ event OnGameWon")]
    [SerializeField] private bool showOnStart = true;

    [Header("Text")]
    [SerializeField] private string titleMessage = "NULL.exe TERMINATED - SYSTEM RESTORED";

    private void Start()
    {
        if (panelRoot != null && !showOnStart) panelRoot.SetActive(false);

        if (showOnStart)
        {
            ShowVictoryScreen();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWon += ShowVictoryScreen;
        }
        else
        {
            Debug.LogWarning("[Victory Screen] ไม่พบ GameManager และไม่ได้ติ๊ก Show On Start — หน้าจอจะไม่ขึ้น");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnGameWon -= ShowVictoryScreen;
    }

    public void ShowVictoryScreen()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        if (titleText != null) titleText.text = titleMessage;
        if (statsText == null) return;

        // ── สถิติของรอบที่เพิ่งจบ ──
        float runTime = LocalLeaderboard.LastRunTime;
        int runDeaths = LocalLeaderboard.LastRunDeaths;
        int rank = LocalLeaderboard.LastRunRank;

        // GameManager ยังอยู่ไหม (กรณีขึ้นทับฉากบอส) ให้ใช้ค่าสด ๆ แทน
        if (GameManager.Instance != null && GameManager.Instance.IsGameWon)
        {
            runTime = GameManager.Instance.RunTime;
            runDeaths = GameManager.Instance.RunDeaths;
        }

        // ── สถิติสะสมทั้งเกม (manager เป็น DontDestroyOnLoad จึงยังอยู่) ──
        int totalDeaths = DeathLogManager.Instance != null
            ? DeathLogManager.Instance.AllTimeDeathRecords.Count
            : 0;

        int patternsLearned = 0;
        if (BulletPatternMemory.Instance != null)
        {
            foreach (BulletPatternType pattern in System.Enum.GetValues(typeof(BulletPatternType)))
            {
                if (BulletPatternMemory.Instance.GetResistance(pattern) > 0f) patternsLearned++;
            }
        }

        int permanentXP = XPManager.Instance != null ? XPManager.Instance.PermanentXP : 0;

        string rankLine = rank > 0 ? $"\nLeaderboard rank    : #{rank}" : "";

        statsText.text =
            $"Clear time          : {LocalLeaderboard.FormatTime(runTime)}\n" +
            $"Deaths this run     : {runDeaths}\n" +
            $"XP archived         : {permanentXP}" +
            rankLine;
    }
}