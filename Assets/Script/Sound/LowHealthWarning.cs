using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// เตือนตอนเลือดเหลือน้อย — เสียงเต้นถี่ขึ้นเรื่อย ๆ + ขอบจอแดงกะพริบ
///
/// ในเกม bullet hell ที่กระสุนเต็มจอ ผู้เล่นแทบไม่มีเวลามองหลอด HP
/// การเตือนด้วยเสียงกับขอบจอทำให้รู้ตัวโดยไม่ต้องละสายตาจากกระสุน
///
/// ยิ่งเลือดน้อย เสียงยิ่งเต้นถี่ — ผู้เล่นรู้ระดับอันตรายจากจังหวะเสียงได้เลย
///
/// วิธีติดตั้ง:
/// 1. ใน Canvas สร้าง Image เต็มจอชื่อ "LowHealthVignette"
///    ใช้ sprite ขอบจอจาง ๆ สีแดง (หรือสี่เหลี่ยมทึบแล้วลด Alpha ก็ได้)
///    ตั้ง Raycast Target = ✗ ไม่งั้นจะบังการคลิกปุ่ม
/// 2. Attach สคริปต์นี้บน GameObject ที่ active เสมอ ลาก Image เข้าช่อง Vignette
/// </summary>
public class LowHealthWarning : MonoBehaviour
{
    [Header("Threshold")]
    [Tooltip("เลือดเหลือต่ำกว่ากี่ส่วนถึงจะเริ่มเตือน (0.3 = 30%)")]
    [Range(0.05f, 0.8f)]
    [SerializeField] private float warningThreshold = 0.3f;

    [Header("Sound")]
    [SerializeField] private string beatSoundId = AudioIds.LowHealthBeat;
    [Tooltip("จังหวะเสียงตอนเลือดเพิ่งแตะเกณฑ์ (วินาที)")]
    [SerializeField] private float slowestInterval = 1.2f;
    [Tooltip("จังหวะเสียงตอนเลือดใกล้หมด — ถี่สุด")]
    [SerializeField] private float fastestInterval = 0.35f;

    [Header("Vignette")]
    [SerializeField] private Image vignette;
    [SerializeField] private Color vignetteColor = new Color(1f, 0.1f, 0.1f, 1f);
    [Tooltip("ความเข้มสูงสุดตอนเลือดใกล้หมด")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.45f;
    [SerializeField] private float pulseSpeed = 4f;

    [Header("Player")]
    [Tooltip("เว้นว่าง = หาจาก Tag Player เอง")]
    [SerializeField] private PlayerHealth playerHealth;

    private float beatTimer;
    private float healthPercent = 1f;
    private bool active;

    private void Start()
    {
        if (playerHealth == null)
        {
            var obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) playerHealth = obj.GetComponent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
        else
        {
            Debug.LogWarning("[Low Health] ไม่พบ PlayerHealth — ระบบเตือนเลือดต่ำจะไม่ทำงาน");
        }

        SetVignetteAlpha(0f);
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        healthPercent = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        bool wasActive = active;
        active = healthPercent > 0f && healthPercent <= warningThreshold;

        // เพิ่งเข้าเขตอันตราย — เตือนทันทีไม่ต้องรอจังหวะถัดไป
        if (active && !wasActive) beatTimer = 0f;
        if (!active) SetVignetteAlpha(0f);
    }

    private void Update()
    {
        if (!active) return;

        // 0 = เพิ่งแตะเกณฑ์, 1 = เลือดใกล้หมด
        float danger = warningThreshold <= 0f ? 1f : 1f - Mathf.Clamp01(healthPercent / warningThreshold);

        // เสียงเต้นถี่ขึ้นตามระดับอันตราย
        beatTimer -= Time.deltaTime;
        if (beatTimer <= 0f)
        {
            AudioManager.Play(beatSoundId);
            beatTimer = Mathf.Lerp(slowestInterval, fastestInterval, danger);
        }

        // ขอบจอกะพริบแรงขึ้นตามระดับอันตราย
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * (1f + danger));
        SetVignetteAlpha(maxAlpha * Mathf.Lerp(0.35f, 1f, danger) * pulse);
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (vignette == null) return;

        Color c = vignetteColor;
        c.a = alpha;
        vignette.color = c;
    }
}
