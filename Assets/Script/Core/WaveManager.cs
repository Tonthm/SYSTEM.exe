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
    [Tooltip("หน่วงก่อนเริ่ม wave แรก (ให้ฉากโหลดเสร็จก่อน) — ตั้งสั้น ๆ พอ")]
    [SerializeField] private float startDelay = 0.5f;
    [Tooltip("เวลารอให้แผง Wave Intro เล่นจบก่อนศัตรูจะเริ่ม spawn — ตั้งให้เท่ากับความยาวแผง")]
    [SerializeField] private float waveIntroPause = 1.8f;
    [Tooltip("พอร์ทัลขยายตัวเตือนกี่วินาทีก่อนศัตรูโผล่ — 0 = โผล่ทันที (ไม่แนะนำ ผู้เล่นจะโดนทับ)")]
    [SerializeField] private float spawnTelegraphDuration = 0.6f;
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
    [Tooltip("ปลดล็อกสกิล Overclock แล้ว โบนัส (และเพดาน) คูณด้วยเท่านี้")]
    [SerializeField] private float overclockSkillMultiplier = 2f;

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
    private int pendingSpawns;   // ศัตรูที่พอร์ทัลกำลังเตือนอยู่ ยังไม่โผล่
    private int currentWaveIndex = -1;
    private float waveTimeRemaining;
    private bool waveTimerRunning;
    private bool allWavesDone;

    // ผลลัพธ์ของ wave ที่เพิ่งจบ (ใช้ตัดสินว่าจะหน่วงนานแค่ไหน)
    private bool lastWaveClearedEarly;

    private Coroutine waveLoop;
    private XPManager.XPSnapshot waveStartXP;   // XP ตอนเริ่ม wave — ใช้ย้อนกลับตอนตาย
    private bool restarting;
    private bool diedThisWave;             // ตายใน wave นี้หรือยัง (ใช้กับ Corruption Meter)
    private bool restartedIntoThisWave;    // wave ที่กำลังจะเริ่มมาจากการตายหรือไม่

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

        waveLoop = StartCoroutine(RunAllWaves(0));
    }

    /// <summary>
    /// เรียกจาก GameManager ตอนผู้เล่นตาย — wave เดินต่อตามปกติ
    /// แค่จำไว้ว่ารอบนี้ตายแล้ว จะได้ไม่ได้รางวัลลด Corruption ตอนเคลียร์ wave
    /// </summary>
    public void NotifyPlayerDied()
    {
        diedThisWave = true;
    }

    /// <summary>
    /// เริ่ม wave ปัจจุบันใหม่ทั้งหมด — ล้างศัตรู + ย้อน XP
    /// ตอนนี้ไม่ได้ถูกเรียกอัตโนมัติแล้ว (เปลี่ยนเป็นระบบ Spawn Protection แทน)
    /// เก็บไว้ให้เรียกเองได้ถ้าอยากใช้กับบอสหรือด่านพิเศษ
    /// </summary>
    public void RestartCurrentWave()
    {
        if (allWavesDone || restarting) return;

        restarting = true;

        int restartIndex = Mathf.Max(0, currentWaveIndex);
        diedThisWave = true;            // wave นี้ตายไปแล้ว เคลียร์ได้ก็ไม่ได้ลด Corruption
        restartedIntoThisWave = true;   // กัน RunWave รีเซ็ตค่าข้างบนทิ้ง

        if (waveLoop != null) StopCoroutine(waveLoop);
        StopAllCoroutines();   // หยุด spawn routine ที่ค้างอยู่ด้วย

        ClearAllEnemies();

        // ย้อน XP กลับไปก่อนเข้า wave นี้ — ฆ่าศัตรูไปแล้วเท่าไหร่ก็ไม่นับ
        XPManager.Instance?.RestoreSnapshot(waveStartXP);

        waveTimerRunning = false;
        pendingSpawns = 0;
        restarting = false;

        Debug.Log($"[Wave] ผู้เล่นตาย — เริ่ม Wave {restartIndex + 1} ใหม่ และย้อน XP กลับ");
        waveLoop = StartCoroutine(RunAllWaves(restartIndex));
    }

    /// <summary>ลบศัตรูที่ยังอยู่ในฉากทั้งหมด (ไม่ให้ XP ไม่ดรอปอะไร)</summary>
    private void ClearAllEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] != null) Destroy(aliveEnemies[i].gameObject);
        }
        aliveEnemies.Clear();

        // เก็บกวาดกระสุนศัตรูที่ค้างอยู่ด้วย ไม่งั้นเกิดใหม่มาโดนกระสุนเก่าทันที
        foreach (var bullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
        {
            if (bullet != null && !bullet.IsPlayerBullet) Destroy(bullet.gameObject);
        }
    }

    private void Update()
    {
        if (!waveTimerRunning) return;
        waveTimeRemaining -= Time.deltaTime;
        if (waveTimeRemaining < 0f) waveTimeRemaining = 0f;
    }

    // ================= Wave loop =================

    private IEnumerator RunAllWaves(int fromIndex)
    {
        yield return new WaitForSeconds(startDelay);

        for (int i = fromIndex; i < waves.Count; i++)
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

        // รีเซ็ตเฉพาะตอนเริ่ม wave ใหม่จริง ๆ — ถ้ามาจาก RestartCurrentWave ค่าจะถูกตั้งเป็น true ไว้แล้ว
        if (!restartedIntoThisWave) diedThisWave = false;
        restartedIntoThisWave = false;

        // บันทึก XP ไว้ก่อน ถ้าตายใน wave นี้จะย้อนกลับมาที่ค่านี้
        if (XPManager.Instance != null) waveStartXP = XPManager.Instance.TakeSnapshot();

        Debug.Log($"[Wave] เริ่ม Wave {index + 1}/{waves.Count}: {wave.waveName}" + (wave.isBossWave ? " (BOSS)" : ""));

        // ยิง event ก่อน แล้วค่อยรอให้แผง Wave Intro เล่นจบ
        // ศัตรูจะยังไม่ spawn ระหว่างนี้ ผู้เล่นจึงอ่านชื่อ wave ได้ทัน (รวมถึง wave 1)
        OnWaveStarted?.Invoke(index + 1, wave);
        if (waveIntroPause > 0f) yield return new WaitForSeconds(waveIntroPause);

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

            while (spawningGroups > 0 || pendingSpawns > 0) yield return null;
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
                bool cleared = advanceEarlyWhenCleared && spawningGroups <= 0
                               && pendingSpawns <= 0 && aliveEnemies.Count == 0;
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

        // เคลียร์ wave โดยไม่ตายเลย = ลด Corruption (ทางแก้ตัวหลักของผู้เล่น)
        CorruptionMeter.Instance?.OnWaveCleared(diedThisWave);

        OnWaveEnded?.Invoke(index + 1, carriedOver, lastWaveClearedEarly);
    }

    /// <summary>ให้ XP โบนัสตามวินาทีที่เหลือ — รางวัลของการเคลียร์ไว</summary>
    private void GrantSpeedClearBonus(float timeLeft)
    {
        if (!speedClearBonus || timeLeft <= 0f) return;

        // สกิล Overclock คูณทั้งโบนัสและเพดาน จะได้ไม่ชนเพดานเดิมจนไร้ผล
        float skillMultiplier = SkillEffects.Multiplier(SkillEffects.OverclockStreak, overclockSkillMultiplier);

        int bonus = Mathf.RoundToInt(timeLeft * bonusXpPerSecond * skillMultiplier);
        int cap = Mathf.RoundToInt(maxBonusXpPerWave * skillMultiplier);
        if (maxBonusXpPerWave > 0) bonus = Mathf.Min(bonus, cap);
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
            // ไม่ block ลูป — พอร์ทัลเตือนพร้อมกันได้หลายจุด จังหวะ interval จึงไม่เพี้ยน
            StartCoroutine(SpawnOneRoutine(group));
            if (group.spawnInterval > 0f) yield return new WaitForSeconds(group.spawnInterval);
        }

        onFinished?.Invoke();
    }

    private IEnumerator SpawnOneRoutine(SpawnGroup group)
    {
        pendingSpawns++;

        WaveSpawnPoint point = WaveSpawnPoint.GetRandom(group.spawnPointGroupId);
        Vector3 pos = point != null ? point.GetSpawnPosition() : transform.position;

        // พอร์ทัลขยายตัวเตือนก่อน ผู้เล่นจะได้ถอยออกจากจุดนั้นทัน
        if (spawnTelegraphDuration > 0f && point != null)
        {
            point.PlayTelegraph(spawnTelegraphDuration);
            yield return new WaitForSeconds(spawnTelegraphDuration);
        }

        GameObject obj = Instantiate(group.enemyPrefab, pos, Quaternion.identity);

        var health = obj.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.ApplyScaling(enemyHealthMultiplier, enemyXpMultiplier);
            RegisterEnemy(health);
        }

        pendingSpawns--;
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
