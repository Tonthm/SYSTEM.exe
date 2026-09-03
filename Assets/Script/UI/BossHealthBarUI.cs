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
    private static BossHealthBarUI instance;

    /// <summary>
    /// หาแบบ lazy — เผื่อ Awake ยังไม่รัน (เช่นสคริปต์ถูก attach บน GameObject ที่ปิดอยู่)
    /// จะได้ยัง Bind ได้และมี log บอกว่าตั้งค่าผิดตรงไหน แทนที่จะเงียบ ๆ ไม่ขึ้นอะไรเลย
    /// </summary>
    public static BossHealthBarUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<BossHealthBarUI>(FindObjectsInactive.Include);

                if (instance == null)
                {
                    Debug.LogWarning("[Boss Health Bar] ไม่พบ BossHealthBarUI ในฉากนี้ — หลอดเลือดบอสจะไม่ขึ้น");
                }
            }
            return instance;
        }
        private set => instance = value;
    }

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

        ValidateSetup();

        if (barRoot != null && barRoot != gameObject) barRoot.SetActive(false);
    }

    /// <summary>เช็คการตั้งค่าที่พลาดกันบ่อย แล้วบอกให้ชัดว่าต้องแก้ตรงไหน</summary>
    private void ValidateSetup()
    {
        if (barRoot == null)
        {
            Debug.LogWarning("[Boss Health Bar] ยังไม่ได้ลาก Bar Root เข้า Inspector");
        }
        else if (barRoot == gameObject)
        {
            Debug.LogError("[Boss Health Bar] Bar Root ห้ามเป็น GameObject ตัวเดียวกับสคริปต์นี้ " +
                           "— สคริปต์จะปิดตัวเองแล้วทำงานไม่ได้ ให้ย้ายสคริปต์ไปไว้บน GameObject แม่ที่ active เสมอ");
        }

        if (container == null)
        {
            Debug.LogWarning("[Boss Health Bar] ยังไม่ได้ลาก Container เข้า Inspector");
        }
        else if (!Mathf.Approximately(container.pivot.x, 0.5f))
        {
            Debug.LogWarning($"[Boss Health Bar] Container Pivot X = {container.pivot.x} ควรเป็น 0.5 " +
                             "ไม่งั้นหลอดจะยืดออกข้างเดียว");
        }

        if (fillImage == null)
        {
            Debug.LogWarning("[Boss Health Bar] ยังไม่ได้ลาก Fill Image");
        }
        else if (fillImage.type != Image.Type.Filled)
        {
            Debug.LogWarning("[Boss Health Bar] Fill Image ต้องตั้ง Image Type = Filled " +
                             "(Fill Method = Horizontal, Origin = Left)");
        }
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

        Debug.Log($"[Boss Health Bar] แสดงหลอดเลือดบอส: {bossName}");

        // GameObject ที่มีสคริปต์นี้ปิดอยู่ = StartCoroutine ทำงานไม่ได้
        // แสดงแบบไม่มีอนิเมชันแทน ดีกว่าไม่ขึ้นอะไรเลย
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[Boss Health Bar] GameObject ของสคริปต์นี้ปิดอยู่ — ข้ามอนิเมชัน " +
                             "ให้ย้ายสคริปต์ไปไว้บน GameObject ที่ active เสมอ");
            ShowInstant();
            return;
        }

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(GrowRoutine());
    }

    private void ShowInstant()
    {
        if (barRoot != null) barRoot.SetActive(true);
        if (container != null)
        {
            container.gameObject.SetActive(true);
            SetWidthRatio(1f);
        }
    }

    /// <summary>ทดสอบใน Editor: คลิกขวาที่ component แล้วเลือก Test Show</summary>
    [ContextMenu("Test Show")]
    private void TestShow()
    {
        Show("TEST BOSS");
        SetProgress(0.7f);
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
        if (!gameObject.activeInHierarchy)
        {
            if (barRoot != null) barRoot.SetActive(false);
            return;
        }

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

        container.gameObject.SetActive(true);

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