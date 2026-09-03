using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ปุ่ม REBORN.exe บนหน้าจอตาย (ตาม storyboard Scene 3)
///
/// ทำงานเมื่อ GameManager ตั้ง Use Manual Reborn = ✓
/// ปุ่มจะกดได้เฉพาะตอนที่เกมรออยู่จริง กันกดรัวจนเกิดซ้อน
///
/// วิธีติดตั้ง:
/// 1. ใน DeathPanel สร้าง Button - TextMeshPro ชื่อ "RebornButton" ป้าย "REBORN.exe"
/// 2. Attach สคริปต์นี้บนปุ่มนั้น (หรือบน DeathScreenUI แล้วลากปุ่มเข้าช่อง)
/// 3. ไม่ต้องผูก onClick เอง สคริปต์ผูกให้ใน Awake
/// </summary>
public class RebornButtonUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button rebornButton;
    [SerializeField] private TMP_Text buttonLabel;

    [Header("Label")]
    [SerializeField] private string readyLabel = "REBORN.exe";
    [SerializeField] private string waitingLabel = "SPAWNING...";

    [Header("Input")]
    [Tooltip("กดปุ่มนี้บนคีย์บอร์ดแทนคลิกได้")]
    [SerializeField] private KeyCode shortcutKey = KeyCode.Return;

    private void Awake()
    {
        if (rebornButton == null) rebornButton = GetComponent<Button>();
        if (buttonLabel == null && rebornButton != null) buttonLabel = rebornButton.GetComponentInChildren<TMP_Text>();

        if (rebornButton != null) rebornButton.onClick.AddListener(Reborn);
    }

    private void Update()
    {
        bool ready = GameManager.Instance != null && GameManager.Instance.IsWaitingForReborn;

        if (rebornButton != null) rebornButton.interactable = ready;
        if (buttonLabel != null) buttonLabel.text = ready ? readyLabel : waitingLabel;

        if (ready && shortcutKey != KeyCode.None && Input.GetKeyDown(shortcutKey)) Reborn();
    }

    public void Reborn()
    {
        AudioManager.Play(AudioIds.PlayerReborn);
        GameManager.Instance?.RequestReborn();
    }
}
