using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// จัดการ "คลังด่าน" (Sector Pool) — สุ่มเลือกด่านให้ผู้เล่นสำรวจ
/// เมื่อผ่านด่านสำเร็จ ด่านนั้นจะถูกนำออกจากคลัง (บันทึกถาวรแม้ตาย)
/// เมื่อผ่านด่านส่วนใหญ่แล้ว จะสลับไปด่านเนื้อเรื่องหลักที่ตายตัวแทนการสุ่ม
///
/// [อัปเดต] เพิ่มลำดับ flow ให้ครบวงจร:
///   Tutorial (ครั้งแรกเท่านั้น) -> สุ่มจาก Sector Pool -> ด่านเนื้อเรื่อง -> Victory Scene
/// วิธีติดตั้ง: อยู่ Scene หลัก (persistent), ใส่ชื่อ Scene ของแต่ละ sector ใน Inspector
/// </summary>
public class SectorPoolManager : MonoBehaviour
{
    public static SectorPoolManager Instance { get; private set; }

    [Header("Tutorial")]
    [Tooltip("ชื่อ Scene ของด่านสอนเล่น — เล่นครั้งแรกครั้งเดียว")]
    [SerializeField] private string tutorialSceneName = "Sector_Tutorial";

    [Header("Sector Pool (ชื่อ Scene ที่สุ่มได้)")]
    [SerializeField] private List<string> allSectorScenes = new List<string>();

    [Header("Story Sectors (ด่านเนื้อเรื่องตายตัว เรียงตามลำดับ)")]
    [SerializeField] private List<string> storySectorScenes = new List<string>();

    [Tooltip("สัดส่วนของด่านในคลังที่ต้องผ่านก่อน ถึงจะปลดล็อกด่านเนื้อเรื่องถัดไป (0.0 - 1.0)")]
    [SerializeField] private float storyUnlockThreshold = 0.7f;

    [Header("Ending")]
    [Tooltip("ชื่อ Scene ตอนจบ (หลังกำจัด NULL.exe หรือผ่านทุกด่านแล้ว)")]
    [SerializeField] private string victorySceneName = "VictoryScene";

    private const string SaveKey_ClearedSectors = "Economice_SYSTEMexe_ClearedSectors";
    private const string SaveKey_StoryIndex = "Economice_SYSTEMexe_StoryIndex";
    private const string SaveKey_TutorialDone = "Economice_SYSTEMexe_TutorialDone";
    private const string SaveKey_GameCompleted = "Economice_SYSTEMexe_GameCompleted";

    private List<string> clearedSectors = new List<string>();
    private int storyIndex = 0;

    public bool HasCompletedTutorial { get; private set; }
    public bool HasCompletedGame { get; private set; }

    public int ClearedCount => clearedSectors.Count;
    public int PoolSize => allSectorScenes.Count;
    public int StoryIndex => storyIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadProgress();
    }

    private void LoadProgress()
    {
        string saved = PlayerPrefs.GetString(SaveKey_ClearedSectors, "");
        clearedSectors = new List<string>(saved.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
        storyIndex = PlayerPrefs.GetInt(SaveKey_StoryIndex, 0);
        HasCompletedTutorial = PlayerPrefs.GetInt(SaveKey_TutorialDone, 0) == 1;
        HasCompletedGame = PlayerPrefs.GetInt(SaveKey_GameCompleted, 0) == 1;
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetString(SaveKey_ClearedSectors, string.Join(",", clearedSectors));
        PlayerPrefs.SetInt(SaveKey_StoryIndex, storyIndex);
        PlayerPrefs.SetInt(SaveKey_TutorialDone, HasCompletedTutorial ? 1 : 0);
        PlayerPrefs.SetInt(SaveKey_GameCompleted, HasCompletedGame ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// เลือกด่านถัดไปตามลำดับ: tutorial (ถ้ายังไม่ผ่าน) -> ด่านเนื้อเรื่อง (ถ้าถึงเกณฑ์)
    /// -> สุ่มจากคลังที่เหลือ -> ด่านเนื้อเรื่องที่เหลือ -> Victory
    /// </summary>
    public string GetNextSector()
    {
        if (!HasCompletedTutorial && !string.IsNullOrEmpty(tutorialSceneName))
        {
            return tutorialSceneName;
        }

        float clearedRatio = allSectorScenes.Count == 0 ? 1f : (float)clearedSectors.Count / allSectorScenes.Count;

        if (clearedRatio >= storyUnlockThreshold && storyIndex < storySectorScenes.Count)
        {
            return storySectorScenes[storyIndex];
        }

        var remaining = allSectorScenes.FindAll(s => !clearedSectors.Contains(s));
        if (remaining.Count > 0)
        {
            return remaining[Random.Range(0, remaining.Count)];
        }

        // คลังหมดแล้ว แต่ยังเหลือด่านเนื้อเรื่อง
        if (storyIndex < storySectorScenes.Count)
        {
            return storySectorScenes[storyIndex];
        }

        // ผ่านทุกอย่างแล้ว -> จบเกม
        return victorySceneName;
    }

    /// <summary>เรียกเมื่อผู้เล่นผ่านด่านสำเร็จ (ไม่ใช่ตอนตาย)</summary>
    public void MarkSectorCleared(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        if (sceneName == tutorialSceneName)
        {
            MarkTutorialComplete();
            return;
        }

        if (storySectorScenes.Contains(sceneName))
        {
            // นับเฉพาะตอนผ่านด่านเนื้อเรื่องที่ค้างอยู่จริง กันการนับซ้ำ
            if (storyIndex < storySectorScenes.Count && storySectorScenes[storyIndex] == sceneName)
            {
                storyIndex++;
            }
        }
        else if (!clearedSectors.Contains(sceneName))
        {
            clearedSectors.Add(sceneName);
        }

        SaveProgress();
        Debug.Log($"[Sector Pool] Cleared: {sceneName} ({clearedSectors.Count}/{allSectorScenes.Count} pool, story {storyIndex}/{storySectorScenes.Count})");
    }

    /// <summary>เรียกจาก TutorialSectorController เมื่อจบด่านสอนเล่น</summary>
    public void MarkTutorialComplete()
    {
        if (HasCompletedTutorial) return;
        HasCompletedTutorial = true;
        SaveProgress();
        Debug.Log("[Sector Pool] Tutorial completed — Sector Pool unlocked");
    }

    /// <summary>เรียกจาก GameManager.OnFinalBossDefeated()</summary>
    public void MarkGameCompleted()
    {
        HasCompletedGame = true;
        SaveProgress();
    }

    /// <summary>เรียกจาก Corruption Meter ตอน Force Format — รีเซ็ตความคืบหน้า "ของรอบนี้" เท่านั้น
    /// หมายเหตุ: ตามดีไซน์ ความคืบหน้าการนำด่านออกจากคลังจะ "ยังคงถูกบันทึกไว้" แม้ตาย</summary>
    public void ResetRunProgress()
    {
        Debug.Log("[Sector Pool] Force Format triggered — sector progress retained per design, only run-temp systems reset");
    }

    public void LoadSector(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[Sector Pool] ไม่มีชื่อ Scene ให้โหลด — ไปหน้าจบเกมแทน");
            LoadVictoryScene();
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void LoadVictoryScene()
    {
        if (string.IsNullOrEmpty(victorySceneName))
        {
            Debug.LogWarning("[Sector Pool] ยังไม่ได้ตั้งชื่อ Victory Scene ใน Inspector");
            return;
        }
        SceneManager.LoadScene(victorySceneName);
    }

    /// <summary>ล้าง save ทั้งหมด (ปุ่ม New Game / ใช้ทดสอบ)</summary>
    public void ResetAllProgress()
    {
        clearedSectors.Clear();
        storyIndex = 0;
        HasCompletedTutorial = false;
        HasCompletedGame = false;
        SaveProgress();
        Debug.Log("[Sector Pool] ล้างความคืบหน้าทั้งหมดแล้ว");
    }
}
