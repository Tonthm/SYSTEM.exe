using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD ของระบบ Wave — เลข wave, เวลานับถอยหลัง, จำนวนศัตรูที่เหลือ, ป้ายประกาศ wave ใหม่
/// สไตล์ตามธีมเกม: ใช้ progress bar และข้อความแบบหน้าต่างระบบ
///
/// วิธีติดตั้ง:
/// 1. ใน Canvas สร้าง Text 3 ตัว (wave / timer / enemies) + Slider 1 ตัว (แถบเวลา)
/// 2. สร้าง Text ใหญ่กลางจอสำหรับป้ายประกาศ (ปิด GameObject ไว้ก็ได้)
/// 3. Attach สคริปต์นี้บน GameObject ใน Canvas แล้วลาก reference ให้ครบ
/// </summary>
public class WaveHUD : MonoBehaviour
{
    [Header("Wave Info")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text enemiesText;
    [SerializeField] private Slider timerSlider;

    [Header("Announcement")]
    [SerializeField] private GameObject announceRoot;
    [SerializeField] private TMP_Text announceText;
    [SerializeField] private float announceDuration = 2f;

    [Header("Format")]
    [SerializeField] private string waveFormat = "WAVE {0} / {1}";
    [SerializeField] private string bossLabel = "WAVE {0} / {1} — NULL PROCESS DETECTED";
    [SerializeField] private string enemiesFormat = "Processes active: {0}";

    private float currentWaveDuration = 60f;

    private void Start()
    {
        if (announceRoot != null) announceRoot.SetActive(false);

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
            WaveManager.Instance.OnWaveEnded += HandleWaveEnded;
            WaveManager.Instance.OnSpeedClearBonus += HandleSpeedClearBonus;
            WaveManager.Instance.OnAllWavesCleared += HandleAllCleared;
        }
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
            WaveManager.Instance.OnWaveEnded -= HandleWaveEnded;
            WaveManager.Instance.OnSpeedClearBonus -= HandleSpeedClearBonus;
            WaveManager.Instance.OnAllWavesCleared -= HandleAllCleared;
        }
    }

    private void Update()
    {
        var wm = WaveManager.Instance;
        if (wm == null) return;

        if (timerText != null)
        {
            timerText.text = wm.IsBossWave ? "--:--" : FormatTime(wm.WaveTimeRemaining);
        }

        if (timerSlider != null)
        {
            timerSlider.value = (wm.IsBossWave || currentWaveDuration <= 0f)
                ? 1f
                : Mathf.Clamp01(wm.WaveTimeRemaining / currentWaveDuration);
        }

        if (enemiesText != null)
        {
            enemiesText.text = string.Format(enemiesFormat, wm.AliveEnemyCount);
        }
    }

    private void HandleWaveStarted(int waveNumber, WaveDefinition wave)
    {
        currentWaveDuration = wave.duration > 0f ? wave.duration : 60f;

        int total = WaveManager.Instance != null ? WaveManager.Instance.TotalWaves : 10;

        if (waveText != null)
        {
            waveText.text = string.Format(wave.isBossWave ? bossLabel : waveFormat, waveNumber, total);
        }

        Announce(wave.isBossWave
            ? "! BOSS PROCESS SPAWNED !"
            : $"WAVE {waveNumber} INITIALIZED");
    }

    private void HandleWaveEnded(int waveNumber, int carriedOver, bool clearedEarly)
    {
        if (carriedOver > 0)
        {
            Announce($"{carriedOver} PROCESS CARRIED OVER — XP HALVED");
        }
        else if (clearedEarly)
        {
            Announce("WAVE CLEARED");
        }
    }

    private void HandleSpeedClearBonus(int bonusXp, float timeLeft)
    {
        // ประกาศทับป้าย WAVE CLEARED เพราะมาทีหลังเสมอ และมีข้อมูลมากกว่า
        Announce($"WAVE CLEARED — SPEED BONUS +{bonusXp} XP");
    }

    private void HandleAllCleared()
    {
        Announce("ALL WAVES CLEARED");
    }

    private void Announce(string message)
    {
        if (announceRoot == null || announceText == null) return;

        CancelInvoke(nameof(HideAnnounce));
        announceText.text = message;
        announceRoot.SetActive(true);
        Invoke(nameof(HideAnnounce), announceDuration);
    }

    private void HideAnnounce()
    {
        if (announceRoot != null) announceRoot.SetActive(false);
    }

    private string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
