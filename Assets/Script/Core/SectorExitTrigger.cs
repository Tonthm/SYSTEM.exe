using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ประตูออกจากด่าน — เมื่อผู้เล่นเดินเข้ามาถือว่า "ผ่านด่าน" (ไม่ใช่ตาย)
/// จะเรียก GameManager.OnSectorCleared() ซึ่งไปสั่ง SectorPoolManager
/// ให้นำด่านนี้ออกจากคลังแล้วโหลดด่านถัดไป
///
/// วิธีติดตั้ง:
/// 1. สร้าง GameObject ตรงทางออกด่าน + Collider2D (Is Trigger)
/// 2. Attach สคริปต์นี้
/// 3. ถ้าอยากให้ต้องเคลียร์ศัตรูก่อน ให้ติ๊ก Require All Enemies Dead
///    หรือให้ระบบอื่น (เช่น TutorialSectorController) เรียก Unlock() เอง
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SectorExitTrigger : MonoBehaviour
{
    [Header("Condition")]
    [Tooltip("เริ่มด่านมาในสถานะล็อก รอให้ระบบอื่นเรียก Unlock()")]
    [SerializeField] private bool startLocked = false;
    [Tooltip("ต้องไม่เหลือ GameObject ที่ Tag = Enemy ในฉากถึงจะผ่านได้")]
    [SerializeField] private bool requireAllEnemiesDead = false;

    [Header("Visual (ไม่บังคับ)")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;

    private bool isLocked;
    private bool used;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger) col.isTrigger = true;

        isLocked = startLocked;
        RefreshVisual();
    }

    public void Unlock()
    {
        isLocked = false;
        RefreshVisual();
        Debug.Log("[Sector Exit] Exit unlocked");
    }

    public void Lock()
    {
        isLocked = true;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (lockedVisual != null) lockedVisual.SetActive(isLocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(!isLocked);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;

        if (isLocked)
        {
            Debug.Log("[Sector Exit] ยังล็อกอยู่ — ทำเงื่อนไขของด่านให้ครบก่อน");
            return;
        }

        if (requireAllEnemiesDead)
        {
            int remaining = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (remaining > 0)
            {
                Debug.Log($"[Sector Exit] ยังเหลือศัตรู {remaining} ตัว");
                return;
            }
        }

        used = true;
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[Sector Exit] Sector cleared: {sceneName}");
        GameManager.Instance?.OnSectorCleared(sceneName);
    }
}
