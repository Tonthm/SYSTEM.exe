using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระบบ Wave ประจำด่าน — 10 wave ต่อด่าน wave สุดท้ายเป็นบอส
///
/// เวลา 1 นาทีต่อ wave คือ "เวลาสูงสุด" ไม่ใช่ความยาวตายตัว
/// เคลียร์ศัตรูหมดเมื่อไหร่ = ขึ้น wave ถัดไปทันที (หน่วงสั้น ๆ ให้ทันเห็นป้าย)
///
/// กติกาสำคัญ 2 ข้อ:
/// - ไม้เรียว: เคลียร์ไม่ทันหมดเวลา ศัตรูที่ค้างถูกตีตรา "stale" ให้ XP น้อยลง 50%
/// - แครอท: เคลียร์ไวเหลือเวลาเท่าไหร่ ได้ XP โบนัสตามวินาทีที่เหลือ
///
/// วิธีติดตั้ง:
/// 1. ใน Scene ของด่าน สร้าง Empty GameObject ชื่อ "WaveManager" attach สคริปต์นี้
/// 2. วาง WaveSpawnPoint กระจายรอบสนาม (อย่างน้อย 6–8 จุด)
/// 3. กรอก Waves ใน Inspector — ปกติ 10 อัน อันที่ 10 ติ๊ก Is Boss Wave
/// 4. ลาก SectorExitTrigger ของด่านเข้าช่อง Exit Trigger (ตั้ง Start Locked ✓ ที่ตัวประตู)
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Waves (ปกติ 10 อัน อันสุดท้าย = บอส)")]
    [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

    [Header("Timing")]
    [Tooltip("หน่วงก่อนเริ่ม wave แรก (ให้ผู้เล่นตั้งตัว)")]
    [SerializeField] private float startDelay = 2f;
    [Tooltip("เคลียร์ศัตรูหมดก่อนหมดเวลา = ขึ้น wave ถัดไปเลย ไม่ต้องรอครบนาที")]
    [SerializeField] private bool advanceEarlyWhenCleared = true;
    [Tooltip("หน่วงหลังเคลียร์ไว (สั้น ๆ พอให้เห็นป้าย WAVE CLEARED)")]
    [SerializeField] private float clearedEarlyDelay = 1.2f;
    [Tooltip("หน่วงหลังหมดเวลาแบบเคลียร์ไม่หมด (ให้ยาวกว่า เพราะจอยังมีศัตรูค้างอยู่)")]
    [SerializeField] private float timeoutDelay = 3f;

    [Header("Speed Clear Bonus (รางวัลเคลียร์ไว)")]
    [Tooltip("ให้ XP โบนัสตามเวลาที่เหลือ — คู่กับ stale penalty เป็นแครอทกับไม้เรียว")]
    [SerializeField] private bool speedClearBonus = true;
    [Tooltip("XP ต่อ 1 วินาทีที่เหลือ")]
    [SerializeField] private float bonusXpPerSecond = 1f;
    [Tooltip("เพดานโบนัสต่อ wave (0 = ไม่จำกัด)")]
    [SerializeField] private int maxBonusXpPerWave = 60;

    [Header("Stale Penalty (ศัตรูค้างจาก wave ก่อน)")]
    [Tooltip("ตัวคูณ XP ของศัตรูที่ค้างข้าม wave (0.5 = ได้ XP ครึ่งเดียว)")]
    [Range(0f, 1f)]
    [SerializeField] private float stalePenaltyMultiplier = 0.5f;
    [Tooltip("ค้างหลาย wave แล้วโดนลดซ้ำ (0.5 -> 0.25 -> 0.125) ปิดไว้ = ลดครั้งเดียว")]
    [SerializeField] private bool stackStalePenalty = false;

    [Header("Sector Difficulty (คูณกับศัตรูทุกตัวที่ spawn ในด่านนี้)")]
    [Tooltip("ใช้ prefab ศัตรูชุดเดิมข้ามด่านได้ แค่ปรับค่านี้ต่อด่าน")]
    [SerializeField] private float enemyHealthMultiplier = 1f;
    [SerializeField] private float enemyXpMultiplier = 1f;

    [Header("Sector Flow")]
    [SerializeField] private SectorExitTrigger exitTrigger;
    [Tooltip("จบ wave สุดท้ายแล้วนับว่าผ่านด่านทันที (ไม่ต้องเดินไปประตู)")]
    [SerializeField] private bool autoClearSectorOnFinish = false;
    [Tooltip("ด่านสุดท้ายของเกม — ให้ NullExeBoss เป็นคนแจ้งจบเกมเอง WaveManager จะไม่สั่งผ่านด่าน")]
    [SerializeField] private bool isFinalSector = false;

    // ---------- Runtime ----------
    private readonly List<EnemyHealth> aliveEnemies = new List<EnemyHealth>();
    private int currentWaveIndex = -1;
    private float waveTimeRemaining;
    private bool waveTimerRunning;
    private bool allWavesDone;

    // ผลลัพธ์ของ wave ที่เพิ่งจบ (ใช้ตัดสินว่าจะหน่วงนานแค่ไหน)
    private bool lastWaveClearedEarly;

    public int CurrentWaveNumber => currentWaveIndex + 1;   // นับ 1-10 สำหรับ UI
    public int TotalWaves => waves.Count;
    public float WaveTimeRemaining => waveTimeRemaining;
    public bool IsTimerRunning => waveTimerRunning;
    public int AliveEnemyCount => aliveEnemies.Count;
    public bool IsBossWave => currentWaveIndex >= 0 && currentWaveIndex < waves.Count && waves[currentWaveIndex].isBossWave;
    public WaveDefinition CurrentWave => (currentWaveIndex >= 0 && currentWaveIndex < waves.Count) ? waves[currentWaveIndex] : null;

    /// <summary>(หมายเลข wave เริ่มที่ 1, ข้อมูล wave)</summary>
    public System.Action<int, WaveDefinition> OnWaveStarted;
    /// <summary>(หมายเลข wave, จำนวนศัตรูที่ค้างข้ามไป, เคลียร์ไวหรือไม่)</summary>
    public System.Action<int, int, bool> OnWaveEnded;
    /// <summary>(XP โบนัส, วินาทีที่เหลือ) — ยิงเฉพาะตอนเคลียร์ไวและได้โบนัสจริง</summary>
    public System.Action<int, float> OnSpeedClearBonus;
    public System.Action OnAllWavesCleared;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        exitTrigger?.Lock();

        if (waves.Count == 0)
        {
            Debug.LogWarning("[Wave] ยังไม่ได้กรอก Waves ใน Inspector");
            return;
        }

        StartCoroutine(RunAllWaves());
    }

    private void Update()
    {
        if (!waveTimerRunning) return;
        waveTimeRemaining -= Time.deltaTime;
        if (waveTimeRemaining < 0f) waveTimeRemaining = 0f;
    }

    // ================= Wave loop =================

    private IEnumerator RunAllWaves()
    {
        yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < waves.Count; i++)
        {
            yield return StartCoroutine(RunWave(i));

            if (i < waves.Count - 1)
            {
                // เคลียร์ไว = หน่วงสั้น (จบแล้วไปต่อเลย), หมดเวลา = หน่วงยาวกว่า
                float delay = lastWaveClearedEarly ? clearedEarlyDelay : timeoutDelay;
                if (delay > 0f) yield return new WaitForSeconds(delay);
            }
        }

        FinishSector();
    }

    private IEnumerator RunWave(int index)
    {
        currentWaveIndex = index;
        WaveDefinition wave = waves[index];
        lastWaveClearedEarly = false;

        Debug.Log($"[Wave] เริ่ม Wave {index + 1}/{waves.Count}: {wave.waveName}" + (wave.isBossWave ? " (BOSS)" : ""));
        OnWaveStarted?.Invoke(index + 1, wave);

        if (wave.formatWarningOnStart)
        {
            SystemInterferenceManager.Instance?.TriggerFormatWarning(wave.formatWarningDuration);
        }

        // เริ่ม spawn ทุกกลุ่มพร้อมกัน (แต่ละกลุ่มมี startDelay ของตัวเอง)
        int spawningGroups = 0;
        foreach (var group in wave.groups)
        {
            spawningGroups++;
            StartCoroutine(SpawnGroupRoutine(group, () => spawningGroups--));
        }

        if (wave.isBossWave)
        {
            // Wave บอส: ไม่จับเวลา รอจนกว่าจะเคลียร์หมด
            waveTimerRunning = false;
            waveTimeRemaining = 0f;

            while (spawningGroups > 0) yield return null;
            while (aliveEnemies.Count > 0) yield return null;

            lastWaveClearedEarly = true;
        }
        else
        {
            waveTimeRemaining = wave.duration > 0f ? wave.duration : 60f;
            waveTimerRunning = true;

            while (waveTimeRemaining > 0f)
            {
                // ต้อง spawn ครบก่อนถึงจะนับว่าเคลียร์ได้
                // (กันกรณีกลุ่มที่ตั้ง Start Delay ไว้ยังไม่ทันออกมา)
                bool cleared = advanceEarlyWhenCleared && spawningGroups <= 0 && aliveEnemies.Count == 0;
                if (cleared)
                {
                    lastWaveClearedEarly = true;
                    Debug.Log($"[Wave] Wave {index + 1} เคลียร์ก่อนหมดเวลา (เหลือ {waveTimeRemaining:F1}s)");
                    GrantSpeedClearBonus(waveTimeRemaining);
                    break;
                }
                yield return null;
            }

            waveTimerRunning = false;
        }

        int carriedOver = MarkRemainingAsStale();
        OnWaveEnded?.Invoke(index + 1, carriedOver, lastWaveClearedEarly);
    }

    /// <summary>ให้ XP โบนัสตามวินาทีที่เหลือ — รางวัลของการเคลียร์ไว</summary>
    private void GrantSpeedClearBonus(float timeLeft)
    {
        if (!speedClearBonus || timeLeft <= 0f) return;

        int bonus = Mathf.RoundToInt(timeLeft * bonusXpPerSecond);
        if (maxBonusXpPerWave > 0) bonus = Mathf.Min(bonus, maxBonusXpPerWave);
        if (bonus <= 0) return;

        XPManager.Instance?.AddXP(bonus);
        OnSpeedClearBonus?.Invoke(bonus, timeLeft);
        Debug.Log($"[Wave] Speed clear bonus: +{bonus} XP (เหลือ {timeLeft:F1}s)");
    }

    /// <summary>ตีตราศัตรูที่ยังไม่ตาย ให้ XP น้อยลงตาม stalePenaltyMultiplier</summary>
    private int MarkRemainingAsStale()
    {
        int count = 0;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            var enemy = aliveEnemies[i];
            if (enemy == null) continue;

            enemy.ApplyStalePenalty(stalePenaltyMultiplier, stackStalePenalty);
            count++;
        }

        if (count > 0)
        {
            Debug.Log($"[Wave] เคลียร์ไม่หมด — ศัตรู {count} ตัวค้างข้าม wave, XP เหลือ {stalePenaltyMultiplier * 100f}%");
        }
        return count;
    }

    private IEnumerator SpawnGroupRoutine(SpawnGroup group, System.Action onFinished)
    {
        if (group == null || group.enemyPrefab == null) { onFinished?.Invoke(); yield break; }

        if (group.startDelay > 0f) yield return new WaitForSeconds(group.startDelay);

        for (int i = 0; i < group.count; i++)
        {
            SpawnOne(group);
            if (group.spawnInterval > 0f) yield return new WaitForSeconds(group.spawnInterval);
        }

        onFinished?.Invoke();
    }

    private void SpawnOne(SpawnGroup group)
    {
        WaveSpawnPoint point = WaveSpawnPoint.GetRandom(group.spawnPointGroupId);
        Vector3 pos = point != null ? point.GetSpawnPosition() : transform.position;

        GameObject obj = Instantiate(group.enemyPrefab, pos, Quaternion.identity);

        var health = obj.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.ApplyScaling(enemyHealthMultiplier, enemyXpMultiplier);
            RegisterEnemy(health);
        }
    }

    // ================= Enemy tracking =================

    /// <summary>ให้ศัตรูที่วางไว้ในฉากล่วงหน้า (ไม่ได้ spawn จาก wave) เข้าระบบนับได้ด้วย</summary>
    public void RegisterEnemy(EnemyHealth enemy)
    {
        if (enemy == null || aliveEnemies.Contains(enemy)) return;

        aliveEnemies.Add(enemy);
        enemy.OnDeath += () => UnregisterEnemy(enemy);
    }

    private void UnregisterEnemy(EnemyHealth enemy)
    {
        aliveEnemies.Remove(enemy);
    }

    // ================= จบด่าน =================

    private void FinishSector()
    {
        if (allWavesDone) return;
        allWavesDone = true;
        waveTimerRunning = false;

        Debug.Log("[Wave] เคลียร์ครบทุก wave แล้ว");
        OnAllWavesCleared?.Invoke();

        if (isFinalSector)
        {
            // ด่านสุดท้าย: NullExeBoss เป็นคนเรียก GameManager.OnFinalBossDefeated() เอง
            return;
        }

        exitTrigger?.Unlock();

        if (autoClearSectorOnFinish)
        {
            GameManager.Instance?.OnSectorCleared();
        }
    }
}