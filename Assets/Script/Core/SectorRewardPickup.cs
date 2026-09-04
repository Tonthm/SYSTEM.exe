using UnityEngine;

/// <summary>
/// ไอเทมรางวัลในด่าน Obstacle Dodge — Big (ปลายทางหลัก ผ่านสิ่งกีดขวางเยอะ)
/// หรือ Small (ทางแยกเสี่ยงต่ำ ได้น้อยกว่า)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SectorRewardPickup : MonoBehaviour
{
    public enum RewardTier { Big, Small }

    [SerializeField] private RewardTier tier = RewardTier.Small;
    [SerializeField] private int rewardValue = 10; // ปรับตาม economy ของเกม (buff/item point ฯลฯ)

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // TODO: ต่อกับระบบ item/buff ที่มีอยู่แล้ว เช่น
        // BuffManager.Instance.GrantSectorReward(tier, rewardValue);
        // หรือ XPManager.Instance.AddLoot(rewardValue);

        Debug.Log($"[SectorRewardPickup] ได้รางวัล tier={tier} value={rewardValue}");
        gameObject.SetActive(false); // เก็บแล้วหาย
    }
}
