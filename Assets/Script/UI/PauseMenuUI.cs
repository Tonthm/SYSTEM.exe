using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// เมนู ESC ระหว่างเล่น — RESUME / ENEMY BOOK / SKILL / ITEM SHOP / MAIN MENU
///
/// หยุดเวลาเกม (Time.timeScale = 0) ระหว่างเปิดเมนู
/// ปุ่มแต่ละอันเปิด panel ของตัวเอง ปิด panel อื่นให้อัตโนมัติ
///
/// วิธีติดตั้ง:
/// 1. ใน Canvas สร้าง Panel เต็มจอชื่อ "PauseMenu" (ปิด GameObject ไว้)
/// 2. ข้างในมีปุ่ม 5 ปุ่ม + panel ย่อยอีก 3 อัน (EnemyBook / SkillPanel / ShopPanel)
/// 3. Attach สคริปต์นี้บน GameObject ที่ active เสมอ แล้วลาก reference
///
/// หมายเหตุ: UI ทุกตัวที่ใช้ animation ในเมนูนี้ต้องใช้ unscaledDeltaTime
/// เพราะ Time.timeScale = 0 (สคริปต์ในชุดนี้ทำไว้แล้ว)
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject pauseRoot;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button enemyBookButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Sub Panels")]
    [SerializeField] private GameObject enemyBookPanel;
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject shopPanel;
    [Tooltip("แผงเมนูหลัก (ปุ่ม 5 อัน) — ซ่อนตอนเปิด panel ย่อย ถ้าอยากให้เต็มจอ")]
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private bool hideButtonsWhenSubPanelOpen = false;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Behaviour")]
    [Tooltip("หยุดเวลาเกมตอนเปิดเมนู")]
    [SerializeField] private bool pauseTime = true;
    [Tooltip("ห้ามเปิดเมนูตอนกำลังตาย/รอเกิดใหม่")]
    [SerializeField] private bool blockWhileDead = true;

    public bool IsOpen { get; private set; }
    public System.Action<bool> OnPauseChanged;

    private void Awake()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (enemyBookButton != null) enemyBookButton.onClick.AddListener(OpenEnemyBook);
        if (skillButton != null) skillButton.onClick.AddListener(OpenSkills);
        if (shopButton != null) shopButton.onClick.AddListener(OpenShop);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (pauseRoot != null) pauseRoot.SetActive(false);
        CloseSubPanels();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey)) return;

        if (IsOpen)
        {
            // อยู่ใน panel ย่อย → ESC ครั้งแรกกลับมาหน้าเมนูหลักก่อน
            if (AnySubPanelOpen()) CloseSubPanels();
            else Resume();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (blockWhileDead && GameManager.Instance != null
            && (GameManager.Instance.IsRespawning || GameManager.Instance.IsWaitingForReborn)) return;

        IsOpen = true;
        if (pauseRoot != null) pauseRoot.SetActive(true);
        CloseSubPanels();

        if (pauseTime) Time.timeScale = 0f;
        OnPauseChanged?.Invoke(true);
    }

    public void Resume()
    {
        IsOpen = false;
        CloseSubPanels();
        if (pauseRoot != null) pauseRoot.SetActive(false);

        if (pauseTime) Time.timeScale = 1f;
        OnPauseChanged?.Invoke(false);
    }

    public void OpenEnemyBook() => ShowOnly(enemyBookPanel);
    public void OpenSkills() => ShowOnly(skillPanel);
    public void OpenShop() => ShowOnly(shopPanel);

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;   // สำคัญ: ไม่คืนค่าแล้วหน้า MainMenu จะค้าง
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ShowOnly(GameObject panel)
    {
        CloseSubPanels();
        if (panel != null) panel.SetActive(true);

        if (hideButtonsWhenSubPanelOpen && mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
    }

    private void CloseSubPanels()
    {
        if (enemyBookPanel != null) enemyBookPanel.SetActive(false);
        if (skillPanel != null) skillPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    private bool AnySubPanelOpen()
    {
        return (enemyBookPanel != null && enemyBookPanel.activeSelf)
            || (skillPanel != null && skillPanel.activeSelf)
            || (shopPanel != null && shopPanel.activeSelf);
    }
}
