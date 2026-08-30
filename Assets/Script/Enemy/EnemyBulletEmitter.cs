using UnityEngine;

/// <summary>
/// ตัวยิงกระสุนศัตรู รองรับ Bullet Pattern หลักที่ใช้บ่อยในเกม Bullet Hell:
/// Aimed (ยิงตรง), RadialBurst (ยิงรอบทิศ), Spiral (ยิงหมุนเกลียว), SpreadCone (กระจายมุมแคบ),
/// WallSweep (แนวกวาด)
/// เลือก pattern ได้ผ่าน Inspector ต่อศัตรูแต่ละตัว
///
/// [อัปเดต] เพิ่ม ConfigurePattern()/SetPattern() ให้เปลี่ยนแพทเทิร์นระหว่างเล่นได้
/// (ใช้กับระบบเปลี่ยนเฟสของบอส และการสุ่มแพทเทิร์นต่อรอบตามเอกสาร)
/// </summary>
public class EnemyBulletEmitter : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("ชื่อที่จะโผล่ใน Death Log เช่น \"Pop-up Swarmer\" / \"Firewall Turret\"")]
    [SerializeField] private string sourceDisplayName = "Unknown Process";

    [Header("Pattern")]
    [SerializeField] private BulletPatternType patternType = BulletPatternType.Aimed;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Timing")]
    [SerializeField] private float fireCooldown = 1.2f;

    [Header("Bullet Stats")]
    [SerializeField] private float bulletSpeed = 6f;
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private float bulletLifetime = 4f;

    [Header("RadialBurst / SpreadCone Settings")]
    [SerializeField] private int bulletCount = 8;
    [SerializeField] private float spreadAngle = 60f; // ใช้กับ SpreadCone เท่านั้น

    [Header("Spiral Settings")]
    [SerializeField] private float spiralRotationSpeedDegPerSec = 90f;
    private float spiralCurrentAngle = 0f;

    [Header("Randomize Per Run")]
    [Tooltip("สุ่มปรับ cooldown/จำนวนกระสุนเล็กน้อยตอนเริ่มด่าน เพื่อกันการท่องจำแพทเทิร์น (ตามเอกสาร)")]
    [SerializeField] private bool randomizeOnStart = false;
    [SerializeField] private float cooldownRandomRange = 0.2f;
    [SerializeField] private int bulletCountRandomRange = 2;

    private float cooldownTimer = 0f;

    public BulletPatternType CurrentPattern => patternType;
    public string SourceDisplayName => sourceDisplayName;

    private void Awake()
    {
        if (firePoint == null) firePoint = transform;
    }

    private void Start()
    {
        if (randomizeOnStart)
        {
            fireCooldown = Mathf.Max(0.05f, fireCooldown + Random.Range(-cooldownRandomRange, cooldownRandomRange));
            bulletCount = Mathf.Max(1, bulletCount + Random.Range(-bulletCountRandomRange, bulletCountRandomRange + 1));
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
    }

    /// <summary>เปลี่ยนแพทเทิร์นอย่างเดียว (คงค่าอื่นไว้)</summary>
    public void SetPattern(BulletPatternType newPattern)
    {
        patternType = newPattern;
    }

    /// <summary>เปลี่ยนแพทเทิร์นพร้อมสเตตการยิง — ใช้ตอนบอสเปลี่ยนเฟส</summary>
    public void ConfigurePattern(BulletPatternType newPattern, float newCooldown, int newBulletCount)
    {
        patternType = newPattern;
        fireCooldown = Mathf.Max(0.05f, newCooldown);
        bulletCount = Mathf.Max(1, newBulletCount);
        cooldownTimer = 0f;
    }

    /// <summary>เรียกทุกเฟรมจาก EnemyBase หากอยู่ในระยะโจมตี — ฟังก์ชันเองจะจัดการ cooldown</summary>
    public void TryFireAt(Vector2 targetPosition)
    {
        if (cooldownTimer > 0f) return;
        cooldownTimer = fireCooldown;

        switch (patternType)
        {
            case BulletPatternType.Aimed:
                FireAimed(targetPosition);
                break;
            case BulletPatternType.RadialBurst:
                FireRadialBurst();
                break;
            case BulletPatternType.Spiral:
                FireSpiral();
                break;
            case BulletPatternType.SpreadCone:
                FireSpreadCone(targetPosition);
                break;
            case BulletPatternType.WallSweep:
                FireWallSweep();
                break;
        }
    }

    private void FireAimed(Vector2 targetPosition)
    {
        Vector2 dir = (targetPosition - (Vector2)firePoint.position).normalized;
        SpawnBullet(dir);
    }

    private void FireRadialBurst()
    {
        float step = 360f / bulletCount;
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = step * i;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            SpawnBullet(dir);
        }
    }

    private void FireSpiral()
    {
        // ยิงทีละนัด แต่หมุนมุมไปเรื่อย ๆ ทุกครั้งที่ยิง ทำให้เกิดลวดลายเกลียวเมื่อเวลาผ่านไป
        Vector2 dir = new Vector2(Mathf.Cos(spiralCurrentAngle * Mathf.Deg2Rad), Mathf.Sin(spiralCurrentAngle * Mathf.Deg2Rad));
        SpawnBullet(dir);
        spiralCurrentAngle += spiralRotationSpeedDegPerSec * fireCooldown;
        if (spiralCurrentAngle >= 360f) spiralCurrentAngle -= 360f;
    }

    private void FireSpreadCone(Vector2 targetPosition)
    {
        Vector2 baseDir = (targetPosition - (Vector2)firePoint.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        float startAngle = baseAngle - spreadAngle / 2f;
        float step = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0f;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = startAngle + step * i;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            SpawnBullet(dir);
        }
    }

    private void FireWallSweep()
    {
        // ยิงกระสุนเรียงเป็นแนวตั้งฉากกับทิศทางที่หันหน้าอยู่ จำลองแนวกวาด
        Vector2 forward = firePoint.right;
        Vector2 perpendicular = new Vector2(-forward.y, forward.x);

        float spacing = 0.6f;
        float startOffset = -(bulletCount - 1) * spacing / 2f;

        for (int i = 0; i < bulletCount; i++)
        {
            Vector3 spawnPos = firePoint.position + (Vector3)(perpendicular * (startOffset + spacing * i));
            SpawnBulletAt(spawnPos, forward);
        }
    }

    private void SpawnBullet(Vector2 direction)
    {
        SpawnBulletAt(firePoint.position, direction);
    }

    private void SpawnBulletAt(Vector3 position, Vector2 direction)
    {
        if (bulletPrefab == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GameObject bulletObj = Instantiate(bulletPrefab, position, Quaternion.Euler(0f, 0f, angle));

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Initialize(direction, bulletSpeed, bulletDamage, bulletLifetime, false, patternType, sourceDisplayName);
        }
    }
}
