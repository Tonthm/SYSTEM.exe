using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD หลักระหว่างเล่น: HP bar (สไตล์ Progress Bar), Corruption Meter, XP ชั่วคราว
///
/// [อัปเดต] เปลี่ยนจาก UnityEngine.UI.Text เป็น TextMeshPro (TMP_Text)
/// วิธีติดตั้ง: สร้าง Canvas -> UI > Text - TextMeshPro / Slider ตามช่องด้านล่าง
/// -> attach สคริปต์นี้บน GameObject ใน Canvas
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;

    [Header("Corruption Meter")]
    [SerializeField] private Slider corruptionSlider;

    [Header("Run Temp XP")]
    [SerializeField] private TMP_Text tempXPText;

    [Header("Refs")]
    [SerializeField] private PlayerHealth playerHealth;

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged += UpdateHP;
        if (CorruptionMeter.Instance != null) CorruptionMeter.Instance.OnCorruptionChanged += UpdateCorruption;
        if (XPManager.Instance != null) XPManager.Instance.OnRunTempXPChanged += UpdateTempXP;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHP;
        if (CorruptionMeter.Instance != null) CorruptionMeter.Instance.OnCorruptionChanged -= UpdateCorruption;
        if (XPManager.Instance != null) XPManager.Instance.OnRunTempXPChanged -= UpdateTempXP;
    }

    private void Start()
    {
        // ตั้งค่าเริ่มต้นตอนเปิดฉาก เผื่อ event ยังไม่เคยยิงมาก่อน
        if (playerHealth != null) UpdateHP(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        if (CorruptionMeter.Instance != null) UpdateCorruption(CorruptionMeter.Instance.CurrentCorruption, CorruptionMeter.Instance.MaxCorruption);
        if (XPManager.Instance != null) UpdateTempXP(XPManager.Instance.RunTempXP);
    }

    private void UpdateHP(float current, float max)
    {
        if (hpSlider != null) hpSlider.value = max > 0 ? current / max : 0;
        if (hpText != null) hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void UpdateCorruption(int current, int max)
    {
        if (corruptionSlider != null) corruptionSlider.value = max > 0 ? (float)current / max : 0;
    }

    private void UpdateTempXP(int value)
    {
        if (tempXPText != null) tempXPText.text = $"+{value} XP (uncommitted)";
    }
}
