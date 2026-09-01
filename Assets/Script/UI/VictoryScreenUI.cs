using TMPro;
using UnityEngine;

/// <summary>
/// หน้าจอชนะเกม สไตล์หน้าต่างระบบ — แสดงตอนกำจัด NULL.exe สำเร็จ
/// ดึงสถิติจาก DeathLogManager / BulletPatternMemory / XPManager มาสรุปให้ผู้เล่น
///
/// วิธีติดตั้ง:
/// 1. สร้าง Panel เต็มจอ (ปิด GameObject ไว้ตอนเริ่ม) มี Text หัวข้อ + Text สรุปสถิติ
/// 2. Attach สคริปต์นี้บน GameObject ที่ active เสมอ (ไม่ใช่ตัว panel ที่ปิด)
/// 3. ลาก panel และ Text เข้าช่อง Inspector
/// </summary>
public class VictoryScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statsText;

    [SerializeField] private string titleMessage = "NULL.exe TERMINATED - SYSTEM RESTORED";

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWon += ShowVictoryScreen;
        }
        else
        {
            Debug.LogWarning("[Victory Screen] ไม่พบ GameManager ในฉาก");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWon -= ShowVictoryScreen;
        }
    }

    private void ShowVictoryScreen()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        if (titleText != null) titleText.text = titleMessage;
        if (statsText == null) return;

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

        statsText.text =
            $"Process respawns : {totalDeaths}\n" +
            $"Patterns immunized : {patternsLearned}\n" +
            $"Total XP archived : {permanentXP}";
    }
}