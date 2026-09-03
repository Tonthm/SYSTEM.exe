using UnityEngine;

/// <summary>
/// ไอเทมที่วางไว้ในด่านให้ผู้เล่นเดินเก็บ — เข้า RunInventory ของรอบนั้น
///
/// วิธีติดตั้ง (ทำเป็น Prefab):
/// 1. สร้าง GameObject ใส่ Sprite ไอคอนไอเทม
/// 2. Add Component: Collider2D ติ๊ก Is Trigger
/// 3. Attach สคริปต์นี้ แล้วกรอกค่าใน Item ตามที่ต้องการ
///    เช่น "Overclock.dll" fireRateMultiplier = 1.3, "Shotgun.sys" bonusBullets = 4 / bonusSpread = 40
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private RunItem item = new RunItem();

    [Header("Feedback (ไม่บังคับ)")]
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobSpeed = 3f;

    private Vector3 startPos;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        startPos = transform.position;
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger) col.isTrigger = true;
    }

    private void Update()
    {
        if (bobAmplitude > 0f)
        {
            transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (RunInventory.Instance == null)
        {
            Debug.LogWarning("[Item Pickup] ไม่พบ RunInventory ใน Scene หลัก");
            return;
        }

        if (!RunInventory.Instance.AddItem(item)) return;

        AudioManager.Play(AudioIds.ItemPickup);
        if (pickupEffectPrefab != null) Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}