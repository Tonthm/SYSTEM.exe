using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ปุ่มซื้อไอเทม 1 ชิ้น — ใช้ระบบเดียวกับปุ่มสกิล ทำ Prefab เองแล้วกรอกข้อมูลไอเทมในปุ่ม
///
/// ต่างจากสกิลตรงที่ **ไอเทมเป็นของรอบนี้เท่านั้น** (เข้า RunInventory)
/// ตายแล้วหลุดไปกับ Data Fragment ตามระบบเดิม ส่วนสกิลเป็นของถาวร
///
/// จ่ายด้วย Permanent XP เหมือนกัน — ผู้เล่นจึงต้องเลือกว่าจะเก็บ XP ไว้ปลดสกิลถาวร
/// หรือเอามาซื้อของช่วยเฉพาะรอบนี้ นั่นคือการตัดสินใจที่ทำให้ร้านค้ามีความหมาย
///
/// วิธีติดตั้ง:
/// 1. ทำ Prefab ปุ่มร้านค้า (Button - TextMeshPro + ไอคอน + ราคา)
/// 2. Attach สคริปต์นี้ กรอก Item (ชื่อ ประเภท ค่าคูณ) และ Cost
/// 3. วางในแผง Item Shop ของเมนู ESC
/// </summary>
public class ItemPurchaseButton : MonoBehaviour
{
    /// <summary>ยิงเมื่อซื้อของสำเร็จ — ปุ่มทุกอันฟังเพื่อรีเฟรชราคา/สถานะ</summary>
    public static System.Action OnAnyItemPurchased;

    [Header("Item")]
    [SerializeField] private RunItem item = new RunItem();
    [SerializeField] private int cost = 100;

    [Header("Limit")]
    [Tooltip("ซื้อได้กี่ครั้งต่อรอบ (0 = ไม่จำกัด)")]
    [SerializeField] private int maxPurchasesPerRun = 1;
    [Tooltip("ราคาเพิ่มขึ้นกี่เท่าหลังซื้อแต่ละครั้ง (1 = ไม่เพิ่ม)")]
    [SerializeField] private float costGrowth = 1.5f;

    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image frameImage;
    [SerializeField] private GameObject soldOutMark;

    [Header("Colors")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = new Color(0.4f, 0.4f, 0.45f);
    [SerializeField] private Color soldOutColor = new Color(0.6f, 0.3f, 0.3f);

    [Header("Feedback")]
    [SerializeField] private GameObject purchaseEffectPrefab;

    private int purchases;
    private int CurrentCost => Mathf.RoundToInt(cost * Mathf.Pow(costGrowth, purchases));
    private bool SoldOut => maxPurchasesPerRun > 0 && purchases >= maxPurchasesPerRun;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(TryPurchase);
    }

    private void OnEnable()
    {
        OnAnyItemPurchased += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        OnAnyItemPurchased -= Refresh;
    }

    public void TryPurchase()
    {
        if (SoldOut) return;

        if (XPManager.Instance == null || RunInventory.Instance == null)
        {
            Debug.LogWarning("[Item Shop] ไม่พบ XPManager หรือ RunInventory");
            return;
        }

        int price = CurrentCost;
        if (!XPManager.Instance.SpendPermanentXP(price))
        {
            Debug.Log($"[Item Shop] XP ไม่พอ ({XPManager.Instance.PermanentXP}/{price})");
            return;
        }

        if (!RunInventory.Instance.AddItem(item))
        {
            // กระเป๋าเต็ม — คืน XP ให้ ไม่งั้นผู้เล่นเสียของฟรี
            XPManager.Instance.AddXP(price);
            Debug.Log("[Item Shop] กระเป๋าเต็ม คืน XP แล้ว");
            return;
        }

        purchases++;
        if (purchaseEffectPrefab != null) Instantiate(purchaseEffectPrefab, transform.position, Quaternion.identity, transform);

        OnAnyItemPurchased?.Invoke();
    }

    /// <summary>รีเซ็ตจำนวนที่ซื้อไปแล้ว (เรียกตอนเริ่มรอบใหม่)</summary>
    public void ResetPurchases()
    {
        purchases = 0;
        Refresh();
    }

    public void Refresh()
    {
        if (nameText != null) nameText.text = item.displayName;
        if (descriptionText != null) descriptionText.text = DescribeItem();

        bool canAfford = XPManager.Instance != null && XPManager.Instance.PermanentXP >= CurrentCost;

        if (costText != null) costText.text = SoldOut ? "SOLD OUT" : $"{CurrentCost} XP";
        if (button != null) button.interactable = !SoldOut && canAfford;
        if (soldOutMark != null) soldOutMark.SetActive(SoldOut);

        if (frameImage != null)
        {
            frameImage.color = SoldOut ? soldOutColor : (canAfford ? affordableColor : unaffordableColor);
        }
    }

    /// <summary>สร้างคำอธิบายจากค่าที่กรอกไว้ ไม่ต้องพิมพ์เอง</summary>
    private string DescribeItem()
    {
        var parts = new System.Collections.Generic.List<string>();

        if (!Mathf.Approximately(item.fireRateMultiplier, 1f))
            parts.Add($"Fire rate x{item.fireRateMultiplier:0.##}");
        if (!Mathf.Approximately(item.damageMultiplier, 1f))
            parts.Add($"Damage x{item.damageMultiplier:0.##}");
        if (item.bonusBulletsPerShot != 0)
            parts.Add($"+{item.bonusBulletsPerShot} bullets");
        if (!Mathf.Approximately(item.bonusSpreadAngle, 0f))
            parts.Add($"+{item.bonusSpreadAngle:0}° spread");
        if (!Mathf.Approximately(item.moveSpeedMultiplier, 1f))
            parts.Add($"Move speed x{item.moveSpeedMultiplier:0.##}");

        return parts.Count > 0 ? string.Join("  ·  ", parts) : "";
    }
}
