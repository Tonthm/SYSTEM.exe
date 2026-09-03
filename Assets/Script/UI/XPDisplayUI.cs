using TMPro;
using UnityEngine;

/// <summary>
/// แสดง XP ที่ใช้ได้ — วางในหน้า Skill และหน้า Item Shop
///
/// อัปเดตทันทีที่ซื้อของหรือฆ่าศัตรูได้ XP
///
/// วิธีติดตั้ง:
/// 1. ใน SkillPanel และ ShopPanel สร้าง Text - TextMeshPro ชื่อ "XPText"
/// 2. Attach สคริปต์นี้บน Text นั้น (หรือ GameObject ใกล้ ๆ แล้วลาก Text เข้าช่อง)
/// </summary>
public class XPDisplayUI : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("เว้นว่าง = ใช้ TMP_Text บน GameObject นี้")]
    [SerializeField] private TMP_Text label;

    [Header("Format")]
    [Tooltip("{0} = XP ที่ใช้ได้")]
    [SerializeField] private string format = "AVAILABLE XP: {0}";
    [Tooltip("แสดง temp XP ของรอบนี้ต่อท้ายด้วย (ยังไม่นับเป็น XP ที่ใช้ได้)")]
    [SerializeField] private bool showRunTempXP = true;
    [SerializeField] private string tempFormat = "   (+{0} uncommitted)";

    [Header("Color")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("สีตอน XP น้อยกว่าราคาที่ถูกที่สุด (ให้รู้ว่ายังซื้ออะไรไม่ได้)")]
    [SerializeField] private Color lowColor = new Color(0.7f, 0.7f, 0.75f);
    [SerializeField] private int lowThreshold = 120;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        // subscribe ทุกครั้งที่แผงถูกเปิด แล้วรีเฟรชทันที
        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnPermanentXPChanged += HandlePermanentChanged;
            XPManager.Instance.OnRunTempXPChanged += HandleTempChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnPermanentXPChanged -= HandlePermanentChanged;
            XPManager.Instance.OnRunTempXPChanged -= HandleTempChanged;
        }
    }

    private void HandlePermanentChanged(int value) => Refresh();
    private void HandleTempChanged(int value) => Refresh();

    public void Refresh()
    {
        if (label == null) return;

        if (XPManager.Instance == null)
        {
            label.text = string.Format(format, 0);
            return;
        }

        int xp = XPManager.Instance.PermanentXP;
        string text = string.Format(format, xp);

        if (showRunTempXP && XPManager.Instance.RunTempXP > 0)
        {
            text += string.Format(tempFormat, XPManager.Instance.RunTempXP);
        }

        label.text = text;
        label.color = xp < lowThreshold ? lowColor : normalColor;
    }
}
