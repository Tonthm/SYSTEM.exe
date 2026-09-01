using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// เตือนก่อนบอสมา — ข้อความเตือนกลางจอ + จุดแดงกะพริบตรงตำแหน่งที่บอสจะโผล่
///
/// หาตำแหน่งจาก WaveSpawnPoint ที่ตั้ง Group Id = "BOSS"
/// ทำงานอัตโนมัติเมื่อ WaveManager เริ่ม wave ที่ติ๊ก Is Boss Wave
///
/// วิธีติดตั้ง:
/// 1. สร้าง WaveSpawnPoint ตรงจุดที่บอสจะเข้า ตั้ง Group Id = BOSS
///    (ปกติวางบนขอบบนของแมพ ให้ตรงกับ BossEntrance)
/// 2. ทำ Prefab จุดแดง: GameObject + SpriteRenderer วงกลม/สามเหลี่ยมสีแดง
/// 3. Empty GameObject ในฉากชื่อ "BossSpawnWarning" attach สคริปต์นี้
/// 4. ลาก Prefab จุดแดงเข้าช่อง Marker Prefab และ Text เตือนเข้าช่อง Warning Text
/// </summary>
public class BossSpawnWarning : MonoBehaviour
{
    [Header("Spawn Point")]
    [Tooltip("Group Id ของ WaveSpawnPoint ที่บอสจะโผล่")]
    [SerializeField] private string bossGroupId = "BOSS";

    [Header("Marker")]
    [Tooltip("Prefab จุดแดงที่จะไปโผล่ตรงจุดเกิดบอส")]
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private Color markerColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float markerBlinkSpeed = 8f;
    [SerializeField] private float markerScale = 1.5f;

    [Header("Warning Text")]
    [SerializeField] private GameObject warningRoot;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private string warningMessage = "! WARNING !\nBOSS PROCESS INCOMING";
    [SerializeField] private Color textColorA = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color textColorB = new Color(1f, 0.8f, 0.2f, 1f);

    [Header("Timing")]
    [Tooltip("เตือนนานกี่วินาทีก่อนบอสเข้าสนาม — ควรเท่ากับ Start Delay ของ wave บอส")]
    [SerializeField] private float warningDuration = 3f;

    [Header("Interference")]
    [SerializeField] private bool triggerFormatWarning = true;

    private Coroutine routine;

    private void Start()
    {
        if (warningRoot != null) warningRoot.SetActive(false);

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
        if (!wave.isBossWave) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(WarningRoutine());
    }

    private IEnumerator WarningRoutine()
    {
        // หาจุดเกิดบอสจาก Group Id
        WaveSpawnPoint bossPoint = null;
        foreach (var point in WaveSpawnPoint.All)
        {
            if (point != null && point.GroupId == bossGroupId) { bossPoint = point; break; }
        }

        GameObject marker = null;
        SpriteRenderer markerRenderer = null;

        if (bossPoint != null && markerPrefab != null)
        {
            marker = Instantiate(markerPrefab, bossPoint.transform.position, Quaternion.identity);
            marker.transform.localScale = Vector3.one * markerScale;
            markerRenderer = marker.GetComponentInChildren<SpriteRenderer>();
        }
        else if (bossPoint == null)
        {
            Debug.LogWarning($"[Boss Warning] ไม่พบ WaveSpawnPoint ที่ Group Id = {bossGroupId}");
        }

        if (warningRoot != null) warningRoot.SetActive(true);
        if (warningText != null) warningText.text = warningMessage;

        if (triggerFormatWarning)
        {
            SystemInterferenceManager.Instance?.TriggerFormatWarning(warningDuration);
        }

        float t = 0f;
        while (t < warningDuration)
        {
            t += Time.deltaTime;
            float blink = 0.5f + 0.5f * Mathf.Sin(t * markerBlinkSpeed);

            if (markerRenderer != null)
            {
                Color c = markerColor;
                c.a = blink;
                markerRenderer.color = c;
            }

            if (warningText != null) warningText.color = Color.Lerp(textColorA, textColorB, blink);

            yield return null;
        }

        if (warningRoot != null) warningRoot.SetActive(false);
        if (marker != null) Destroy(marker);

        routine = null;
    }
}
