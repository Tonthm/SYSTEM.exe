using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// หลอดเลือดบอสด้านบนจอ พร้อมอนิเมชันตอนโผล่ — ยืดออกจากกลางไปสองข้าง
///
///        ||
///       |  |
///     |      |
/// |              |
///
/// วิธีติดตั้ง:
/// 1. ใน Canvas สร้าง Panel ชื่อ "BossHealthBar" วางกลางบนจอ (ปิด GameObject ไว้)
/// 2. ข้างในมี:
///    - Container   (RectTransform ที่จะถูกยืด — ใส่ Pivot X = 0.5 กลางเป๊ะ)
///      ├── Background (Image สีเข้ม)
///      ├── Fill       (Image, Image Type = Filled, Fill Method = Horizontal, Origin = Left)
///      └── NameText   (TMP)
/// 3. Attach สคริปต์นี้บน GameObject ที่ active เสมอ ลาก reference ให้ครบ
///
/// บอสเรียกเองอัตโนมัติผ่าน BossHealthBarUI.Instance — ไม่ต้องลากอะไรเข้าตัวบอส
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject barRoot;
    [Tooltip("RectTransform ที่จะถูกยืดออกตอนโผล่ — Pivot X ต้องเป็น 0.5")]
    [SerializeField] private RectTransform container;
    [Tooltip("Image แบบ Filled Horizontal สำหรับหลอดเลือด")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text phaseText;

    [Header("Intro Transition")]
    [Tooltip("ยืดจากกลางออกไปสองข้างใช้เวลากี่วินาที")]
    [SerializeField] private float growDuration = 0.8f;
    [Tooltip("ความกว้างเริ่มต้นเทียบกับเต็ม (0.02 = เป็นขีดเล็ก ๆ กลางจอ)")]
    [SerializeField] private float startWidthRatio = 0.02f;
    [Tooltip("กะพริบกี่ครั้งก่อนเริ่มยืด")]
    [SerializeField] private int preBlinkCount = 3;
    [SerializeField] private float preBlinkInterval = 0.12f;

    [Header("Damage Feedback")]
    [Tooltip("หลอดสีขาวที่ไล่ตามหลอดจริงช้า ๆ ให้เห็นว่าเพิ่งเสียเลือดไปเท่าไหร่")]
    [SerializeField] private Image delayedFillImage;
    [SerializeField] private float delayedFillSpeed = 0.6f;
    [SerializeField] private float shakeOnDamage = 6f;

    [Header("Outro")]
    [SerializeField] private float hideDelay = 1f;
    [SerializeField] private float shrinkDuration = 0.4f;

    private EnemyHealth boundHealth;
    private Coroutine transitionRoutine;
    private float targetFill = 1f;
    private float shakeAmount;
    private Vector2 containerBasePos;

    private void Awake()
    {
        Instance = this;
        if (container != null) containerBasePos = container.anchoredPosition;
        if (barRoot != null) barRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Unbind();
    }

    private void Update()
    {
        // หลอดสีขาวไล่ตามช้า ๆ
        if (delayedFillImage != null && delayedFillImage.fillAmount > targetFill)
        {
            delayedFillImage.fillAmount = Mathf.MoveTowards(
                delayedFillImage.fillAmount, targetFill, delayedFillSpeed * Time.deltaTime);
        }

        // สั่นตอนเพิ่งโดนตี
        if (container != null && shakeAmount > 0.01f)
        {
            shakeAmount = Mathf.Lerp(shakeAmount, 0f, Time.deltaTime * 8f);
            container.anchoredPosition = containerBasePos + Random.insideUnitCircle * shakeAmount;
        }
        else if (container != null && container.anchoredPosition != containerBasePos)
        {
            container.anchoredPosition = containerBasePos;
        }
    }

    // ── API ที่บอสเรียก ──

    /// <summary>ผูกกับ EnemyHealth ของบอส — อัปเดตหลอดให้เองทุกครั้งที่เสียเลือด</summary>
    public void Bind(EnemyHealth health, string bossName)
    {
        Unbind();

        boundHealth = health;
        if (boundHealth != null)
        {
            boundHealth.OnHealthChanged += HandleHealthChanged;
            boundHealth.OnDeath += HandleBossDeath;
        }

        Show(bossName);
        SetProgress(1f);
    }

    /// <summary>ใช้กับบอสที่ HP ไม่ได้อยู่ก้อนเดียว (เช่นตะขาบที่นับจำนวนปล้อง)</summary>
    public void Show(string bossName)
    {
        if (bossNameText != null) bossNameText.text = bossName;
        if (phaseText != null) phaseText.text = "";

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(GrowRoutine());
    }

    /// <summary>ตั้งค่าหลอดโดยตรง 0-1 (ตะขาบใช้สัดส่วนปล้องที่เหลือ)</summary>
    public void SetProgress(float percent)
    {
        targetFill = Mathf.Clamp01(percent);
        if (fillImage != null) fillImage.fillAmount = targetFill;
    }

    public void SetPhase(string phaseName)
    {
        if (phaseText != null) phaseText.text = phaseName;
    }

    public void Hide()
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(ShrinkRoutine());
    }

    // ── Internal ──

    private void HandleHealthChanged(float current, float max)
    {
        SetProgress(max > 0f ? current / max : 0f);
        shakeAmount = shakeOnDamage;
    }

    private void HandleBossDeath()
    {
        SetProgress(0f);
        Hide();
        Unbind();
    }

    private void Unbind()
    {
        if (boundHealth == null) return;
        boundHealth.OnHealthChanged -= HandleHealthChanged;
        boundHealth.OnDeath -= HandleBossDeath;
        boundHealth = null;
    }

    private IEnumerator GrowRoutine()
    {
        if (barRoot != null) barRoot.SetActive(true);
        if (container == null) yield break;

        // กะพริบเป็นขีดเล็ก ๆ กลางจอก่อน
        SetWidthRatio(startWidthRatio);
        for (int i = 0; i < preBlinkCount; i++)
        {
            container.gameObject.SetActive(false);
            yield return new WaitForSeconds(preBlinkInterval);
            container.gameObject.SetActive(true);
            yield return new WaitForSeconds(preBlinkInterval);
        }

        // ยืดออกจากกลางไปสองข้าง (ease-out ให้ดูมีน้ำหนัก)
        float t = 0f;
        while (t < growDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / growDuration);
            p = 1f - (1f - p) * (1f - p);
            SetWidthRatio(Mathf.Lerp(startWidthRatio, 1f, p));
            yield return null;
        }

        SetWidthRatio(1f);
        transitionRoutine = null;
    }

    private IEnumerator ShrinkRoutine()
    {
        yield return new WaitForSeconds(hideDelay);

        if (container != null)
        {
            float t = 0f;
            float from = container.localScale.x;
            while (t < shrinkDuration)
            {
                t += Time.deltaTime;
                SetWidthRatio(Mathf.Lerp(from, 0f, Mathf.Clamp01(t / shrinkDuration)));
                yield return null;
            }
        }

        if (barRoot != null) barRoot.SetActive(false);
        transitionRoutine = null;
    }

    /// <summary>ยืดด้วย localScale.x — Pivot กลางทำให้ยืดออกสองข้างเท่ากันเอง</summary>
    private void SetWidthRatio(float ratio)
    {
        if (container == null) return;
        Vector3 scale = container.localScale;
        scale.x = ratio;
        container.localScale = scale;
    }
}
