using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ปุ่มซื้อ "ตั๋ว" เข้ามินิเกมโบนัส Flap ในหน้า Item Shop (เมนู ESC)
/// ใช้โครง Prefab เดียวกับ ItemPurchaseButton (Button + ชื่อ + ราคา + คำอธิบาย + กรอบสี + Sold Out)
///
/// [เปลี่ยนจากเดิม] กดซื้อแล้ว "ไม่พาเข้าเกมทันที" — แค่ mark ว่าซื้อไว้แล้ว (pending)
/// ต้องเล่นจบ sector ปัจจุบันก่อน (ไปแตะ SectorExitTrigger) ถึงจะถูกพาเข้า Flappy จริง
/// ตรรกะการเบี่ยงนี้อยู่ใน GameManager.OnSectorCleared() (ดู diff แนบท้าย) ไม่ใช่ในปุ่มนี้
///
/// [เข้าได้ครั้งเดียวทั้งรอบ ไม่ใช่ครั้งเดียวต่อ Scene] เพราะปุ่มนี้อยู่แยกกันคนละ instance
/// ใน shop ของ Firewall และ RAM ใช้ FlapTicketState (marker ใน RunInventory) เช็คแทน bool ในตัวเอง
/// ซื้อที่ไหนก็ล็อกทั้งสองที่ทันที
///
/// วิธีติดตั้ง:
/// 1. Duplicate Prefab ปุ่ม ItemPurchaseButton เดิม 1 ชุด ในแผง Item Shop ของ Scene Firewall และ RAM เท่านั้น
/// 2. ลบ ItemPurchaseButton component เดิมออก แล้ว Attach สคริปต์นี้แทนที่
/// 3. ตั้งชื่อ Scene มินิเกมใน Inspector (ต้องเพิ่มใน Build Settings ด้วย)
/// </summary>
public class FlapTicketButton : MonoBehaviour
{
    [Header("Ticket")]
    [SerializeField] private string flapSceneName = "Sector_BonusFlap";
    [SerializeField] private int cost = 100;

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

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(TryPurchase);
    }

    private void OnEnable()
    {
        ItemPurchaseButton.OnAnyItemPurchased += Refresh; // ซื้อของอื่นแล้ว XP เปลี่ยน ปุ่มนี้ต้อง refresh ด้วย
        Refresh();
    }

    private void OnDisable()
    {
        ItemPurchaseButton.OnAnyItemPurchased -= Refresh;
    }

    public void TryPurchase()
    {
        if (FlapTicketState.IsPurchased)
        {
            AudioManager.Play(AudioIds.UIDenied);
            return;
        }

        if (XPManager.Instance == null || RunInventory.Instance == null)
        {
            Debug.LogWarning("[Flap Ticket] ไม่พบ XPManager หรือ RunInventory");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(flapSceneName))
        {
            Debug.LogError($"[Flap Ticket] Scene '{flapSceneName}' โหลดไม่ได้ — เช็ค Build Settings หรือชื่อ Scene ก่อนขาย XP ผู้เล่นทิ้ง");
            AudioManager.Play(AudioIds.UIDenied);
            return;
        }

        if (!XPManager.Instance.SpendPermanentXP(cost))
        {
            AudioManager.Play(AudioIds.UIDenied);
            Debug.Log($"[Flap Ticket] XP ไม่พอ ({XPManager.Instance.PermanentXP}/{cost})");
            return;
        }

        FlapTicketState.MarkPurchased(flapSceneName);

        AudioManager.Play(AudioIds.UIPurchase);
        ItemPurchaseButton.OnAnyItemPurchased?.Invoke(); // ให้ปุ่มช็อปอื่นรีเฟรช affordability ด้วย
        Refresh();

        Debug.Log("[Flap Ticket] ซื้อแล้ว — เล่นจบ sector นี้แล้วจะถูกพาเข้ามินิเกมอัตโนมัติ");
    }

    public void Refresh()
    {
        bool used = FlapTicketState.IsPurchased;

        if (nameText != null) nameText.text = "Ghost.exe Flap";
        if (descriptionText != null) descriptionText.text = "Bonus minigame — After pass this sector";

        bool canAfford = XPManager.Instance != null && XPManager.Instance.PermanentXP >= cost;

        if (costText != null) costText.text = used ? "PURCHASED" : $"{cost} XP";
        if (button != null) button.interactable = !used && canAfford;
        if (soldOutMark != null) soldOutMark.SetActive(used);

        if (frameImage != null)
        {
            frameImage.color = used ? soldOutColor : (canAfford ? affordableColor : unaffordableColor);
        }
    }
}
