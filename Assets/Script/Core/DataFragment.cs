using UnityEngine;

/// <summary>
/// วัตถุ Data Fragment ที่ตกอยู่ในโลกเกม หลังผู้เล่นตาย
/// ถ้า Process ใหม่ (ผู้เล่น) เดินมาชนก่อนหมดเวลา -> ได้ payload คืน (temp XP + ไอเทมทั้งหมดของรอบ)
/// ถ้าหมดเวลาก่อน -> ทำลายตัวเอง (สูญหายถาวรในรอบนั้น)
///
/// [อัปเดต] เดิมแบกแค่ int (temp XP) ตอนนี้แบก FragmentPayload ที่มีทั้ง XP และ RunItem
/// วิธีติดตั้ง: ทำเป็น Prefab มี SpriteRenderer + Collider2D (Is Trigger), ตั้ง Tag = "Fragment"
/// </summary>
public class DataFragment : MonoBehaviour
{
    [Header("Warning Blink (ไม่บังคับ)")]
    [Tooltip("เริ่มกะพริบเตือนก่อนหมดเวลากี่วินาที")]
    [SerializeField] private float blinkWarningTime = 5f;
    [SerializeField] private float blinkInterval = 0.15f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private FragmentPayload payload;
    private float lifetime;
    private float timer;
    private float blinkTimer;

    /// <summary>เวลาที่เหลือก่อน fragment หายถาวร (ให้ HUD ดึงไปแสดงนับถอยหลังได้)</summary>
    public float RemainingTime => Mathf.Max(0f, lifetime - timer);
    public FragmentPayload Payload => payload;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>เวอร์ชันเต็ม — ใช้จาก FragmentInheritanceManager</summary>
    public void Initialize(FragmentPayload payload, float lifetime)
    {
        this.payload = payload ?? new FragmentPayload();
        this.lifetime = lifetime;
        timer = 0f;
    }

    /// <summary>เวอร์ชันเดิม (XP อย่างเดียว) เก็บไว้ให้โค้ดเก่ายังคอมไพล์ผ่าน</summary>
    public void Initialize(int carriedXP, float lifetime)
    {
        Initialize(new FragmentPayload { tempXP = carriedXP }, lifetime);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (spriteRenderer != null && RemainingTime <= blinkWarningTime)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }
        }

        if (timer >= lifetime)
        {
            string lost = payload != null ? payload.Describe() : "empty";
            Debug.Log($"[Fragment Inheritance] Fragment expired — lost permanently this run ({lost})");
            AudioManager.Play(AudioIds.FragmentExpire);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (payload == null) { Destroy(gameObject); return; }

        if (payload.tempXP > 0) XPManager.Instance?.AddRunTempXP(payload.tempXP);
        if (payload.ItemCount > 0) RunInventory.Instance?.RestoreAll(payload.items);

        AudioManager.Play(AudioIds.FragmentPickup);

        Debug.Log($"[Fragment Inheritance] Recovered {payload.Describe()}");
        Destroy(gameObject);
    }
}
