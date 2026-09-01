using System.Collections;
using UnityEngine;

/// <summary>
/// จุดเกิดศัตรูแบบมองเห็นได้ — "จุดดำ" บนพื้นที่ที่ขยายตัวก่อนศัตรูโผล่
///
/// ทำงานร่วมกับ WaveSpawnPoint: WaveManager สั่ง PlayTelegraph() ก่อน spawn จริง
/// ผู้เล่นจึงมีเวลาถอยออกจากจุดนั้น
///
/// **สำคัญ:** ถ้าไม่มีสัญญาณเตือน ศัตรูจะโผล่ทับตัวผู้เล่นแล้วเสียเลือดฟรี
/// ผู้เล่นจะรู้สึกว่าเกมโกง ไม่ใช่ว่าตัวเองพลาด
///
/// วิธีติดตั้ง:
/// 1. บน GameObject เดียวกับ WaveSpawnPoint
/// 2. สร้าง child "Dot" ใส่ SpriteRenderer วงกลมดำ (scale เริ่มต้นเล็ก ๆ)
/// 3. Attach สคริปต์นี้ ลาก "Dot" เข้าช่อง Portal Visual
/// </summary>
public class SpawnPortal : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform portalVisual;
    [SerializeField] private SpriteRenderer portalRenderer;

    [Header("Idle (ตอนไม่ได้ spawn)")]
    [Tooltip("ขนาดตอนอยู่เฉย ๆ — 0 = ซ่อนสนิทจนกว่าจะถึงเวลา")]
    [SerializeField] private float idleScale = 0.35f;
    [SerializeField] private Color idleColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);
    [Tooltip("เต้นเบา ๆ ตอนอยู่เฉย ให้รู้ว่าเป็นจุดเกิด")]
    [SerializeField] private float idlePulseAmount = 0.06f;
    [SerializeField] private float idlePulseSpeed = 2f;

    [Header("Telegraph (ก่อนศัตรูโผล่)")]
    [Tooltip("ขยายใหญ่สุดตอนกำลังจะปล่อยศัตรู")]
    [SerializeField] private float openScale = 1.1f;
    [SerializeField] private Color openColor = new Color(0.6f, 0.15f, 0.9f, 1f);
    [Tooltip("หดกลับใช้เวลากี่วินาทีหลังศัตรูออกมาแล้ว")]
    [SerializeField] private float closeDuration = 0.35f;

    private Coroutine routine;
    private bool busy;

    private void Awake()
    {
        if (portalVisual == null && transform.childCount > 0) portalVisual = transform.GetChild(0);
        if (portalRenderer == null && portalVisual != null) portalRenderer = portalVisual.GetComponent<SpriteRenderer>();

        ApplyIdle();
    }

    private void Update()
    {
        if (busy || portalVisual == null) return;

        float pulse = idleScale + Mathf.Sin(Time.time * idlePulseSpeed) * idlePulseAmount;
        portalVisual.localScale = Vector3.one * pulse;
    }

    /// <summary>WaveManager เรียกก่อน spawn — พอร์ทัลขยายตัวเตือนตลอด duration</summary>
    public void PlayTelegraph(float duration)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(TelegraphRoutine(duration));
    }

    private IEnumerator TelegraphRoutine(float duration)
    {
        busy = true;

        // ขยาย
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            SetVisual(Mathf.Lerp(idleScale, openScale, p), Color.Lerp(idleColor, openColor, p));
            yield return null;
        }

        SetVisual(openScale, openColor);
        yield return null;   // เฟรมที่ศัตรูโผล่

        // หด
        t = 0f;
        while (t < closeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / closeDuration);
            SetVisual(Mathf.Lerp(openScale, idleScale, p), Color.Lerp(openColor, idleColor, p));
            yield return null;
        }

        ApplyIdle();
        busy = false;
        routine = null;
    }

    private void SetVisual(float scale, Color color)
    {
        if (portalVisual != null) portalVisual.localScale = Vector3.one * scale;
        if (portalRenderer != null) portalRenderer.color = color;
    }

    private void ApplyIdle() => SetVisual(idleScale, idleColor);
}
