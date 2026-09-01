using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แผงดำคั่นก่อนเริ่มแต่ละ wave — บอกว่าอยู่ด่านอะไร wave ที่เท่าไหร่
///
///     ┌────────────────────────┐
///     │   SECTOR: FIREWALL     │
///     │      WAVE 3 / 10       │
///     │   Firewall Probe       │
///     └────────────────────────┘
///
/// จอดำ fade เข้า -> ค้างไว้ -> fade ออก แล้ว wave ค่อยเริ่มจริง
/// WaveManager หน่วง Start Delay ให้แล้ว ตั้งเวลารวมของแผงนี้ให้ไม่เกิน Start Delay
///
/// วิธีติดตั้ง:
/// 1. ใน Canvas สร้าง Panel เต็มจอสีดำ ชื่อ "WaveIntro"
/// 2. ใส่ Text - TextMeshPro 3 ตัว: Sector / Wave / ชื่อ wave
/// 3. ใส่ CanvasGroup บน Panel (สำหรับ fade)
/// 4. Attach สคริปต์นี้บน GameObject ที่ active เสมอ แล้วลาก reference
/// </summary>
public class WaveIntroUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text sectorText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text waveNameText;

    [Header("Sector")]
    [Tooltip("ชื่อด่านที่จะแสดง เช่น FIREWALL / RAM / REGISTRY / CORE")]
    [SerializeField] private string sectorName = "FIREWALL";
    [SerializeField] private string sectorFormat = "SECTOR: {0}";
    [SerializeField] private string waveFormat = "WAVE {0} / {1}";
    [SerializeField] private string bossFormat = "FINAL WAVE — BOSS";

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Behaviour")]
    [Tooltip("แสดงทุก wave หรือเฉพาะ wave แรกกับ wave บอส")]
    [SerializeField] private bool showEveryWave = true;

    private Coroutine routine;

    private void Start()
    {
        SetAlpha(0f);
        if (panelRoot != null) panelRoot.SetActive(false);

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
        }
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
        }
    }

    private void HandleWaveStarted(int waveNumber, WaveDefinition wave)
    {
        if (!showEveryWave && waveNumber != 1 && !wave.isBossWave) return;

        int total = WaveManager.Instance != null ? WaveManager.Instance.TotalWaves : 10;

        if (sectorText != null) sectorText.text = string.Format(sectorFormat, sectorName);
        if (waveText != null) waveText.text = wave.isBossWave ? bossFormat : string.Format(waveFormat, waveNumber, total);
        if (waveNameText != null) waveNameText.text = wave.waveName;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        yield return Fade(0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(1f, 0f, fadeOutDuration);

        if (panelRoot != null) panelRoot.SetActive(false);
        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, duration <= 0f ? 1f : t / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float value)
    {
        if (canvasGroup != null) canvasGroup.alpha = value;
    }
}
