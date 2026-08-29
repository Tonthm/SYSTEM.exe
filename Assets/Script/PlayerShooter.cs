using UnityEngine;

/// <summary>
/// ระบบยิงของผู้เล่น: เล็งไปทางเมาส์ ยิงกระสุนตาม fire rate ที่กำหนด
///
/// [อัปเดต] เชื่อมกับ RunInventory แล้ว — ค่าใน Inspector คือ "สเตตพื้นฐาน"
/// ส่วนสเตตจริงที่ใช้ยิงจะคำนวณใหม่ทุกครั้งที่ไอเทมในกระเป๋าของรอบเปลี่ยน
/// (เก็บ Overclock.dll -> fire rate เพิ่ม, เก็บ Shotgun.sys -> กระสุนหลายนัด ฯลฯ)
///
/// วิธีติดตั้งใน Unity:
/// 1. สร้าง Empty GameObject ชื่อ "FirePoint" เป็นลูกของ Player วางไว้หน้าตัวละคร
/// 2. สร้าง Bullet Prefab (ดู Bullet.cs) มี Rigidbody2D + Collider2D (Is Trigger)
/// 3. Attach สคริปต์นี้เข้ากับ Player, ลาก FirePoint และ Bullet Prefab ใส่ในช่อง Inspector
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Header("Weapon Stats (สเตตพื้นฐาน ก่อนบวกไอเทมของรอบ)")]
    [SerializeField] private float fireRate = 6f;      // นัดต่อวินาที
    [SerializeField] private float bulletSpeed = 14f;
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private float bulletLifetime = 2.5f;

    [Header("Spread (0 = ยิงตรง, มากขึ้น = กระจายกว้าง)")]
    [SerializeField] private int bulletsPerShot = 1;
    [SerializeField] private float spreadAngle = 0f; // องศารวมของการกระจาย

    [Header("Refs")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Camera mainCamera;

    // สเตตจริงหลังคูณไอเทมของรอบแล้ว
    private float effFireRate;
    private float effDamage;
    private int effBulletsPerShot;
    private float effSpreadAngle;

    private float fireCooldownTimer = 0f;

    public float CurrentFireRate => effFireRate;
    public float CurrentDamage => effDamage;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        RecalculateStats();
    }

    private void Start()
    {
        // subscribe ที่ Start เพราะ RunInventory.Instance ถูกตั้งใน Awake ของ manager
        if (RunInventory.Instance != null)
        {
            RunInventory.Instance.OnInventoryChanged += RecalculateStats;
        }
        RecalculateStats();
    }

    private void OnDestroy()
    {
        if (RunInventory.Instance != null)
        {
            RunInventory.Instance.OnInventoryChanged -= RecalculateStats;
        }
    }

    /// <summary>คำนวณสเตตจริง = สเตตพื้นฐาน x ค่ารวมจากไอเทมของรอบนี้</summary>
    public void RecalculateStats()
    {
        var inv = RunInventory.Instance;

        effFireRate       = fireRate      * (inv != null ? inv.GetFireRateMultiplier() : 1f);
        effDamage         = bulletDamage  * (inv != null ? inv.GetDamageMultiplier()   : 1f);
        effBulletsPerShot = Mathf.Max(1, bulletsPerShot + (inv != null ? inv.GetBonusBulletsPerShot() : 0));
        effSpreadAngle    = spreadAngle   + (inv != null ? inv.GetBonusSpreadAngle()   : 0f);

        effFireRate = Mathf.Max(0.1f, effFireRate);
    }

    private void Update()
    {
        AimAtMouse();

        if (fireCooldownTimer > 0f)
        {
            fireCooldownTimer -= Time.deltaTime;
        }

        // กดค้างซ้ายเมาส์เพื่อยิงต่อเนื่อง
        if (Input.GetMouseButton(0) && fireCooldownTimer <= 0f)
        {
            Shoot();
            fireCooldownTimer = 1f / effFireRate;
        }
    }

    private void AimAtMouse()
    {
        if (mainCamera == null || firePoint == null) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2 direction = (mouseWorldPos - firePoint.position);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        float baseAngle = firePoint.eulerAngles.z;

        if (effBulletsPerShot <= 1)
        {
            SpawnBullet(baseAngle);
        }
        else
        {
            float startAngle = baseAngle - effSpreadAngle / 2f;
            float step = effSpreadAngle / (effBulletsPerShot - 1);

            for (int i = 0; i < effBulletsPerShot; i++)
            {
                SpawnBullet(startAngle + step * i);
            }
        }
    }

    private void SpawnBullet(float angleDegrees)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, angleDegrees);
        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, rotation);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            Vector2 direction = rotation * Vector2.right;
            bullet.Initialize(direction, bulletSpeed, effDamage, bulletLifetime, isPlayerBullet: true);
        }
    }

    /// <summary>เปลี่ยน "สเตตพื้นฐาน" ของอาวุธ เช่น ตอนสลับอาวุธหลัก (ไอเทมของรอบยังคูณทับอยู่)</summary>
    public void SetWeaponStats(float newFireRate, float newDamage, int newBulletsPerShot, float newSpread)
    {
        fireRate = newFireRate;
        bulletDamage = newDamage;
        bulletsPerShot = Mathf.Max(1, newBulletsPerShot);
        spreadAngle = newSpread;
        RecalculateStats();
    }
}
