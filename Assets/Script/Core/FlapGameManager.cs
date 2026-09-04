using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// คุมมินิเกม Flap ทั้งหมด: คะแนน, จบเกมตอนชน, แจกรางวัลถาวรเข้า RunInventory,
/// โชว์ผลรางวัลใน crash panel, แล้วโหลดด่านถัดไป "ตัวจริง" ต่อ (ไม่ใช่กลับด่านเดิม)
///
/// [เปลี่ยนจากเดิม] Scene นี้เข้ามาหลังเคลียร์ sector ต้นทางจบไปแล้วเท่านั้น (ผ่าน GameManager
/// ตอนเคลียร์ด่าน) จึงโหลดแบบเปลี่ยน Scene ตรง ๆ ได้เลย ไม่ต้อง Additive/ซ่อน player-camera
/// เหมือนเวอร์ชันก่อนหน้า เพราะไม่มี wave/บอสค้างให้ต้องรักษาสถานะอีกแล้ว
///
/// รางวัลจากมินิเกมนี้ตั้ง isPermanent = true เสมอ — ไม่โดนดรอปเป็น Data Fragment ตอนตายปกติ
/// (ต้องมี field isPermanent ใน RunItem.cs และ RunInventory.TakeAll()/ClearRun() รองรับแล้ว)
///
/// วิธีติดตั้ง: Empty GameObject "FlapGameManager" ใน Scene มินิเกม, ลาก UI เข้าช่องตามต้องการ
/// </summary>
public class FlapGameManager : MonoBehaviour
{
    public static FlapGameManager Instance { get; private set; }

    [Header("Score UI")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Crash Panel")]
    [SerializeField] private GameObject crashPanel;
    [SerializeField] private TMP_Text crashScoreText;
    [SerializeField] private Image rewardIconImage;
    [SerializeField] private TMP_Text rewardNameText;
    [SerializeField] private TMP_Text rewardBuffText;

    [Header("Reward Tiers (เรียงจากน้อยไปมาก — ทุกอันตั้ง isPermanent = true ไว้แล้ว)")]
    [SerializeField] private int mediumScoreThreshold = 3;
    [SerializeField] private int bigScoreThreshold = 8;
    [SerializeField]
    private RunItem smallReward = new RunItem { id = "flap_small", displayName = "Cache Fragment", type = RunItemType.PassiveBuff, moveSpeedMultiplier = 1.05f, isPermanent = true };
    [SerializeField]
    private RunItem mediumReward = new RunItem { id = "flap_medium", displayName = "Overclock.dll", type = RunItemType.WeaponUpgrade, fireRateMultiplier = 1.2f, isPermanent = true };
    [SerializeField]
    private RunItem bigReward = new RunItem { id = "flap_big", displayName = "Shotgun.sys", type = RunItemType.WeaponUpgrade, bonusBulletsPerShot = 4, bonusSpreadAngle = 40f, isPermanent = true };

    [Header("Flow")]
    [Tooltip("หน่วงกี่วินาทีหลัง crash ก่อนไปด่านถัดไปอัตโนมัติ")]
    [SerializeField] private float continueDelay = 2.5f;

    public int Score { get; private set; }
    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (crashPanel != null) crashPanel.SetActive(false);
        UpdateScoreUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddScore(int amount)
    {
        if (IsGameOver) return;
        Score += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = Score.ToString();
    }

    public void OnCrash()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        AudioManager.Play(AudioIds.PlayerHit);

        RunItem reward = PickReward();
        bool granted = RunInventory.Instance != null && RunInventory.Instance.AddItem(reward);
        if (granted) AudioManager.Play(AudioIds.ItemPickup);

        ShowCrashPanel(reward, granted);

        Invoke(nameof(ContinueToNextSector), continueDelay);
    }

    private RunItem PickReward()
    {
        if (Score >= bigScoreThreshold) return bigReward;
        if (Score >= mediumScoreThreshold) return mediumReward;
        return smallReward;
    }

    private void ShowCrashPanel(RunItem reward, bool granted)
    {
        if (crashPanel != null) crashPanel.SetActive(true);
        if (crashScoreText != null) crashScoreText.text = $"PROCESS CRASHED\nScore: {Score}";

        if (!granted)
        {
            if (rewardNameText != null) rewardNameText.text = "Inventory full — no reward";
            if (rewardBuffText != null) rewardBuffText.text = "";
            return;
        }

        if (rewardIconImage != null)
        {
            rewardIconImage.sprite = reward.icon;
            rewardIconImage.enabled = reward.icon != null;
        }
        if (rewardNameText != null) rewardNameText.text = $"{reward.displayName} (Permanent)";
        if (rewardBuffText != null) rewardBuffText.text = BuildBuffDescription(reward);
    }

    /// <summary>สร้างข้อความอธิบาย buff จากค่าตัวคูณ/โบนัสจริงของไอเทม — ไม่ต้องพิมพ์ description ซ้ำมือ</summary>
    private string BuildBuffDescription(RunItem item)
    {
        var parts = new System.Collections.Generic.List<string>();

        if (item.type == RunItemType.WeaponUpgrade)
        {
            if (!Mathf.Approximately(item.fireRateMultiplier, 1f)) parts.Add($"Fire Rate x{item.fireRateMultiplier:0.##}");
            if (!Mathf.Approximately(item.damageMultiplier, 1f)) parts.Add($"Damage x{item.damageMultiplier:0.##}");
            if (item.bonusBulletsPerShot != 0) parts.Add($"+{item.bonusBulletsPerShot} Bullets");
            if (!Mathf.Approximately(item.bonusSpreadAngle, 0f)) parts.Add($"+{item.bonusSpreadAngle:0}° Spread");
        }
        else if (item.type == RunItemType.PassiveBuff)
        {
            if (!Mathf.Approximately(item.moveSpeedMultiplier, 1f)) parts.Add($"Move Speed x{item.moveSpeedMultiplier:0.##}");
        }

        return parts.Count > 0 ? string.Join(" / ", parts) : "Passive Bonus";
    }

    private void ContinueToNextSector()
    {
        string next = FlapBonusContext.PendingNextSector;
        if (string.IsNullOrEmpty(next))
        {
            Debug.LogWarning("[FlapGameManager] ไม่พบด่านถัดไปที่ค้างไว้ — ผู้เล่นน่าจะเข้ามินิเกมนี้โดยไม่ผ่านระบบตั๋ว (GameManager.OnSectorCleared)");
            return;
        }

        SectorPoolManager.Instance?.LoadSector(next);
    }
}
