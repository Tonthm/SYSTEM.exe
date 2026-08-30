using UnityEngine;

/// <summary>
/// HP ผู้เล่น + จุดเชื่อมต่อกับระบบ "1-Life and Reborn" ทั้งหมด
/// เมื่อ HP หมด: log สาเหตุตาย -> เพิ่ม resistance ต่อแพทเทิร์นนั้น -> ดรอป Data Fragment
/// -> เช็ค Corruption Meter -> สั่ง GameManager ทำ respawn (Kill Process -> Spawn ใหม่)
///
/// [อัปเดต] รับ "ชื่อตัวที่ทำดาเมจ" มาด้วย เพื่อให้ Death Log บอกได้ว่าตายเพราะใคร
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public System.Action<float, float> OnHealthChanged; // (current, max) สำหรับ HUD
    /// <summary>ยิงทุกครั้งที่โดนดาเมจ (ให้เอฟเฟกต์กระพริบ/สั่นจอไปฟัง)</summary>
    public System.Action<float, BulletPatternType> OnDamaged;

    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>สำหรับดาเมจที่ไม่มีสาเหตุจากกระสุนศัตรู (เช่น trap, self-damage)</summary>
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, BulletPatternType.Aimed, null);
    }

    public void TakeDamage(float amount, BulletPatternType cause)
    {
        TakeDamage(amount, cause, null);
    }

    /// <param name="sourceName">ชื่อศัตรูที่ทำดาเมจ เช่น "Pop-up Swarmer" (ใส่ null ได้)</param>
    public void TakeDamage(float amount, BulletPatternType cause, string sourceName)
    {
        if (isDead) return;

        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamaged?.Invoke(amount, cause);

        if (currentHealth <= 0f)
        {
            Die(cause, sourceName);
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die(BulletPatternType cause, string sourceName)
    {
        isDead = true;

        // 1. บันทึกสาเหตุการตาย (Death Log — Task Manager UI จะดึงข้อมูลนี้ไปแสดง)
        DeathLogManager.Instance?.LogDeath(cause, transform.position, sourceName);

        // 2. เพิ่มภูมิต้านทานแพทเทิร์นนี้ให้ Process ถัดไป (Bullet Pattern Memory)
        bool gainedNewResistance = BulletPatternMemory.Instance != null
            && BulletPatternMemory.Instance.RegisterDeath(cause);

        // 3. ดรอป Data Fragment ตรงจุดตาย (Fragment Inheritance)
        FragmentInheritanceManager.Instance?.DropFragmentAt(transform.position);

        // 4. อัปเดต Corruption Meter — ถ้าตายซ้ำโดยไม่มี resistance ใหม่ ค่าจะเพิ่มขึ้น
        CorruptionMeter.Instance?.RegisterDeath(gainedNewResistance);

        // 5. สั่งให้ GameManager จัดการ Kill Process -> Spawn Process ใหม่ที่ Checkpoint ล่าสุด
        GameManager.Instance?.OnPlayerDied();
    }
}
