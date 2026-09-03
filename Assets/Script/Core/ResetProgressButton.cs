using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ปุ่มล้างข้อมูลผู้เล่นทั้งหมด — ความคืบหน้าด่าน, สกิล, XP, ตารางสถิติ
///
/// มีระบบยืนยัน 2 ครั้ง (กดครั้งแรกเปลี่ยนข้อความเป็น "ARE YOU SURE?" กดอีกครั้งจึงล้าง)
/// กันคนแวะบูธกดเล่นแล้วข้อมูลหายหมด
///
/// วิธีติดตั้ง:
/// 1. สร้าง Button - TextMeshPro ในหน้า MainMenu หรือเมนู ESC
/// 2. Attach สคริปต์นี้บนปุ่มนั้น — ไม่ต้องผูก onClick เอง
/// </summary>
public class ResetProgressButton : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    [Header("Labels")]
    [SerializeField] private string idleLabel = "RESET ALL DATA";
    [SerializeField] private string confirmLabel = "ARE YOU SURE?";
    [SerializeField] private string doneLabel = "DATA WIPED";

    [Header("Confirm")]
    [Tooltip("ไม่กดยืนยันภายในกี่วินาที จะกลับเป็นปกติเอง")]
    [SerializeField] private float confirmTimeout = 3f;

    [Header("Colors")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color confirmColor = new Color(1f, 0.35f, 0.35f);

    [Header("What to reset")]
    [SerializeField] private bool resetSectorProgress = true;
    [SerializeField] private bool resetSkills = true;
    [SerializeField] private bool resetXP = true;
    [SerializeField] private bool resetLeaderboard = true;

    private bool awaitingConfirm;
    private float confirmTimer;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (label == null && button != null) label = button.GetComponentInChildren<TMP_Text>();

        if (button != null) button.onClick.AddListener(OnClick);
        SetIdle();
    }

    private void Update()
    {
        if (!awaitingConfirm) return;

        confirmTimer -= Time.unscaledDeltaTime;
        if (confirmTimer <= 0f) SetIdle();
    }

    private void OnClick()
    {
        if (!awaitingConfirm)
        {
            // กดครั้งแรก — ขอยืนยันก่อน
            awaitingConfirm = true;
            confirmTimer = confirmTimeout;

            if (label != null) label.text = confirmLabel;
            if (label != null) label.color = confirmColor;
            AudioManager.Play(AudioIds.UIDenied);
            return;
        }

        DoReset();
    }

    private void DoReset()
    {
        if (resetSectorProgress) SectorPoolManager.Instance?.ResetAllProgress();
        if (resetSkills) SkillTreeManager.Instance?.ResetAllSkills();
        if (resetXP) XPManager.Instance?.ResetAllXP();
        if (resetLeaderboard) LocalLeaderboard.Clear();

        RunInventory.Instance?.ClearRun();

        // ให้ปุ่มสกิล/ร้านค้าที่เปิดอยู่รีเฟรชสถานะตาม
        SkillPurchaseButton.OnAnySkillPurchased?.Invoke();
        ItemPurchaseButton.OnAnyItemPurchased?.Invoke();

        AudioManager.Play(AudioIds.ForceFormat);
        Debug.Log("[Reset] ล้างข้อมูลผู้เล่นทั้งหมดแล้ว");

        awaitingConfirm = false;
        if (label != null)
        {
            label.text = doneLabel;
            label.color = idleColor;
        }

        Invoke(nameof(SetIdle), 1.5f);
    }

    private void SetIdle()
    {
        awaitingConfirm = false;
        if (label != null)
        {
            label.text = idleLabel;
            label.color = idleColor;
        }
    }
}
