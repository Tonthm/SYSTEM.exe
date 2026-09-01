using TMPro;
using UnityEngine;

/// <summary>
/// หน้าจอตายสไตล์ "Task Manager" — แสดง process ที่ถูก kill พร้อมสาเหตุจาก Death Log
///
/// [อัปเดต] แสดงบรรทัด "Cause of death: Pop-up Swarmer collision" ตาม storyboard
/// และแสดงหมายเลข Process ID ที่เพิ่มขึ้นทุกครั้งที่ตาย (GP-001, GP-002, ...)
/// วิธีติดตั้ง: สร้าง Panel เต็มจอ (ปิดไว้ตอนเริ่ม), attach สคริปต์นี้บน GameObject ที่ active เสมอ
/// </summary>
public class DeathScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [Tooltip("บรรทัดหัว เช่น 'Process GP-001 has been terminated'")]
    [SerializeField] private TMP_Text messageText;
    [Tooltip("บรรทัด 'Cause of death: ...'")]
    [SerializeField] private TMP_Text causeText;
    [SerializeField] private TMP_Text resistanceGainedText;

    [Header("Format")]
    [SerializeField] private string titleFormat = "Process GP-{0:000} has been terminated";
    [SerializeField] private string causeFormat = "Cause of death: {0}";

    // subscribe ใน Start เพราะ GameManager ตั้ง Instance ใน Awake
    // ถ้า subscribe ใน OnEnable แล้ว UI ตื่นก่อน GameManager จะไม่ได้ subscribe เลย
    // (อาการ: ตายแล้วหน้าจอ Task Manager ไม่ขึ้น)
    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDeathSequenceStarted += ShowDeathScreen;
            GameManager.Instance.OnPlayerRespawned += HideDeathScreen;
        }
        else
        {
            Debug.LogWarning("[Death Screen] ไม่พบ GameManager ในฉาก — หน้าจอตายจะไม่ทำงาน");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDeathSequenceStarted -= ShowDeathScreen;
            GameManager.Instance.OnPlayerRespawned -= HideDeathScreen;
        }
    }

    private void ShowDeathScreen()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        var log = DeathLogManager.Instance;
        if (log == null) return;

        var lastDeath = log.LastDeath;
        if (!lastDeath.HasValue) return;

        // Process ID = จำนวนครั้งที่ตายทั้งหมด (ตายครั้งแรก = GP-001 ถูก kill)
        int processId = log.AllTimeDeathRecords.Count;
        if (messageText != null) messageText.text = string.Format(titleFormat, processId);
        if (causeText != null) causeText.text = string.Format(causeFormat, log.DescribeCause(lastDeath.Value));

        if (resistanceGainedText != null)
        {
            float resistance = BulletPatternMemory.Instance != null
                ? BulletPatternMemory.Instance.GetResistance(lastDeath.Value.cause)
                : 0f;
            resistanceGainedText.text = $"Pattern resistance: {Mathf.RoundToInt(resistance * 100)}%";
        }
    }

    private void HideDeathScreen()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}