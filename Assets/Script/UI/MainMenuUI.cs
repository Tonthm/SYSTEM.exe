using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// หน้า Title Screen — ปุ่ม PLAY / QUIT กลางจอ และตารางสถิติมุมซ้ายล่าง
///
///     ┌──────────────────────────────────┐
///     │          SYSTEM.exe              │
///     │      Ghost in the Kernel         │
///     │                                  │
///     │           [ PLAY ]               │
///     │           [ QUIT ]               │
///     │                                  │
///     │  BEST RUNS                       │
///     │  1. 12:04   3 deaths   14/02     │
///     │  2. 15:22   7 deaths   13/02     │
///     └──────────────────────────────────┘
///
/// วิธีติดตั้ง:
/// 1. สร้าง Scene ใหม่ชื่อ "MainMenu" (ใส่เป็น Scene แรกใน Build Settings)
/// 2. Canvas → ปุ่ม Button - TextMeshPro 2 ปุ่ม: PLAY / QUIT
/// 3. Text - TextMeshPro มุมซ้ายล่างสำหรับตารางสถิติ (จัด Anchor เป็น Bottom Left)
/// 4. Empty GameObject ชื่อ "MainMenu" attach สคริปต์นี้ แล้วลาก reference
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    [Header("Scene")]
    [Tooltip("Scene ที่จะโหลดตอนกด PLAY — ปกติคือ Bootstrap ที่มี manager ครบ")]
    [SerializeField] private string playSceneName = "Bootstrap";

    [Header("Leaderboard (มุมซ้ายล่าง)")]
    [SerializeField] private GameObject leaderboardRoot;
    [SerializeField] private TMP_Text leaderboardText;
    [SerializeField] private string leaderboardTitle = "BEST RUNS";
    [SerializeField] private string emptyMessage = "NO RECORDS YET";

    [Header("Reset")]
    [Tooltip("ปุ่มล้างสถิติ (ไม่บังคับ — มีไว้ตอนตั้งบูธ)")]
    [SerializeField] private Button clearLeaderboardButton;
    [Tooltip("ล้างความคืบหน้าทั้งหมดด้วย (ด่านที่ผ่าน / สกิล / XP)")]
    [SerializeField] private bool clearAlsoResetsProgress = false;

    private void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(Play);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
        if (clearLeaderboardButton != null) clearLeaderboardButton.onClick.AddListener(ClearLeaderboard);
    }

    private void Start()
    {
        Time.timeScale = 1f;   // กันค้างจาก pause ของรอบก่อน
        RefreshLeaderboard();
    }

    public void Play()
    {
        if (string.IsNullOrEmpty(playSceneName))
        {
            Debug.LogWarning("[Main Menu] ยังไม่ได้ตั้งชื่อ Scene ที่จะโหลด");
            return;
        }
        SceneManager.LoadScene(playSceneName);
    }

    public void Quit()
    {
        Debug.Log("[Main Menu] Quit");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ClearLeaderboard()
    {
        LocalLeaderboard.Clear();
        if (clearAlsoResetsProgress)
        {
            SectorPoolManager.Instance?.ResetAllProgress();
            SkillTreeManager.Instance?.ResetAllSkills();
        }
        RefreshLeaderboard();
    }

    private void RefreshLeaderboard()
    {
        if (leaderboardText == null) return;

        var entries = LocalLeaderboard.GetEntries();

        var sb = new StringBuilder();
        sb.AppendLine(leaderboardTitle);

        if (entries.Count == 0)
        {
            sb.Append(emptyMessage);
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                sb.AppendLine($"{i + 1}. {e.FormattedTime}   {e.deaths} deaths   {e.date}");
            }
        }

        leaderboardText.text = sb.ToString();
        if (leaderboardRoot != null) leaderboardRoot.SetActive(true);
    }
}
