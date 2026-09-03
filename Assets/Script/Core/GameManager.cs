using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ศูนย์กลางควบคุมโฟลว์เกม: checkpoint, การ Respawn, การผ่านด่าน, เงื่อนไขชนะ
///
/// [อัปเดต] ลำดับตอนตายใหม่ทั้งหมด:
/// 1. ซ่อนตัวผู้เล่นทันที (SetActive false) — ศัตรูจะเลิกไล่ตาม และเก็บ Fragment ไม่ได้ระหว่างตาย
/// 2. รอกดปุ่ม REBORN.exe (หรือ respawn อัตโนมัติถ้าปิดโหมดปุ่ม)
/// 3. ย้ายกลับจุด spawn + ฟื้น HP + ล้างสถานะ dash ค้าง
/// 4. สั่ง WaveManager เริ่ม wave ปัจจุบันใหม่ พร้อมย้อน XP กลับไปก่อนเข้า wave นั้น
///
/// วิธีติดตั้ง: Empty GameObject ชื่อ "GameManager" ในทุก Scene ที่เล่นได้จริง
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private Transform currentCheckpoint;

    [Header("Respawn")]
    [Tooltip("ให้ผู้เล่นกดปุ่ม REBORN.exe เอง (ตามหน้าจอ Task Manager ใน storyboard)")]
    [SerializeField] private bool useManualReborn = true;
    [Tooltip("ใช้เมื่อปิดโหมดปุ่ม — หน่วงกี่วินาทีก่อน respawn อัตโนมัติ")]
    [SerializeField] private float respawnDelay = 1.2f;
    [Tooltip("หน่วงหลังกดปุ่ม ก่อนตัวละครโผล่จริง (ให้หน้าจอปิดทัน)")]
    [SerializeField] private float rebornDelay = 0.3f;

    [Header("Spawn Protection")]
    [Tooltip("เกิดใหม่แล้วอมตะและทะลุศัตรูได้กี่วินาที — ให้เดินออกจากฝูงศัตรูก่อน")]
    [SerializeField] private float spawnProtectionDuration = 1.5f;

    [Header("Death Effect")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float deathEffectLifetime = 2f;

    [Header("Victory")]
    [SerializeField] private bool loadVictorySceneOnWin = true;
    [SerializeField] private float victoryDelay = 3f;

    public System.Action OnPlayerRespawned;
    public System.Action OnPlayerDeathSequenceStarted;
    public System.Action OnGameWon;

    /// <summary>เวลาที่ใช้ในรอบนี้ (วินาที) — ให้ UI หรือ leaderboard ใช้</summary>
    public float RunTime => Time.time - runStartTime;
    /// <summary>จำนวนครั้งที่ตายในรอบนี้</summary>
    public int RunDeaths { get; private set; }

    private float runStartTime;

    public bool IsGameWon { get; private set; }
    public bool IsRespawning { get; private set; }
    /// <summary>true = ตายแล้วกำลังรอกดปุ่ม REBORN.exe (ให้ UI เปิดปุ่มได้)</summary>
    public bool IsWaitingForReborn { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        runStartTime = Time.time;

        if (playerObject == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) playerObject = found;
        }

        // ด่านที่ไม่ได้วาง checkpoint ไว้ ใช้ตำแหน่งเริ่มต้นของผู้เล่นเป็นจุดเกิดแรก
        if (currentCheckpoint == null && playerObject != null)
        {
            var auto = new GameObject("Auto_Checkpoint_Start");
            auto.transform.position = playerObject.transform.position;
            currentCheckpoint = auto.transform;
            Debug.Log("[GameManager] ไม่พบ checkpoint เริ่มต้น — ใช้ตำแหน่ง spawn ของผู้เล่นแทน");
        }
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null) return;
        currentCheckpoint = checkpoint;
        Debug.Log($"[GameManager] Checkpoint updated: {checkpoint.name}");
    }

    /// <summary>เรียกจาก PlayerHealth.Die()</summary>
    public void OnPlayerDied()
    {
        if (IsGameWon) return;
        if (IsRespawning || IsWaitingForReborn) return;   // กันเรียกซ้อนจากกระสุนหลายนัดในเฟรมเดียว

        IsRespawning = true;
        RunDeaths++;

        if (playerObject != null)
        {
            if (deathEffectPrefab != null)
            {
                GameObject fx = Instantiate(deathEffectPrefab, playerObject.transform.position, Quaternion.identity);
                if (deathEffectLifetime > 0f) Destroy(fx, deathEffectLifetime);
            }

            // ซ่อนตัวทันที — ศัตรูจะหาเป้าไม่เจอ และเก็บ Fragment ไม่ได้ระหว่างตาย
            playerObject.GetComponent<PlayerController>()?.ResetState();
            playerObject.SetActive(false);
        }

        // wave เดินต่อตามปกติ แค่บอกว่ารอบนี้ตายแล้ว (Corruption Meter ใช้ตัดสินว่าเคลียร์แบบไม่ตายไหม)
        WaveManager.Instance?.NotifyPlayerDied();

        OnPlayerDeathSequenceStarted?.Invoke();   // UI แสดงหน้าจอ Task Manager ตรงนี้

        if (useManualReborn)
        {
            IsWaitingForReborn = true;   // รอผู้เล่นกดปุ่ม REBORN.exe
        }
        else
        {
            Invoke(nameof(RespawnPlayer), respawnDelay);
        }
    }

    /// <summary>เรียกจากปุ่ม REBORN.exe บนหน้าจอตาย</summary>
    public void RequestReborn()
    {
        if (!IsWaitingForReborn) return;

        IsWaitingForReborn = false;
        Invoke(nameof(RespawnPlayer), rebornDelay);
    }

    private void RespawnPlayer()
    {
        IsRespawning = false;
        IsWaitingForReborn = false;

        if (playerObject == null || currentCheckpoint == null)
        {
            Debug.LogWarning("[GameManager] Missing playerObject or checkpoint reference for respawn");
            return;
        }

        playerObject.transform.position = currentCheckpoint.position;
        playerObject.SetActive(true);

        var controller = playerObject.GetComponent<PlayerController>();
        controller?.ResetState();
        playerObject.GetComponent<PlayerHealth>()?.ResetHealth();

        // อมตะ + ทะลุศัตรูชั่วครู่ กันเกิดกลางฝูงแล้วตายซ้ำทันที
        controller?.BeginSpawnProtection(spawnProtectionDuration);

        OnPlayerRespawned?.Invoke();
        Debug.Log("[GameManager] New Ghost Process spawned at checkpoint");
    }

    /// <summary>เรียกเมื่อผู้เล่นผ่านด่านสำเร็จ</summary>
    public void OnSectorCleared(string currentSceneName)
    {
        if (IsGameWon) return;

        // แจ้งก่อนโหลดด่านถัดไป — สกิล Registry Cleaner เช็คว่าผ่านด่านโดยไม่ตายหรือไม่
        CorruptionMeter.Instance?.OnSectorCleared();

        SectorPoolManager.Instance?.MarkSectorCleared(currentSceneName);

        // ผ่านด่านสุดท้ายแล้ว = จบเกม (ไม่ต้องมีบอส NULL.exe แยกด่าน)
        if (SectorPoolManager.Instance != null && SectorPoolManager.Instance.IsNextSectorVictory())
        {
            Debug.Log("[GameManager] ผ่านครบทุกด่านแล้ว → จบเกม");
            CompleteGame();
            return;
        }

        SectorPoolManager.Instance?.LogProgress();   // ดู Console ได้ว่าทำไมยังไม่จบเกม

        string next = SectorPoolManager.Instance?.GetNextSector();

        // ไม่มีด่านถัดไป = ถือว่าจบเกม กันค้างอยู่ในด่านเดิมแบบไม่มีอะไรเกิดขึ้น
        if (string.IsNullOrEmpty(next))
        {
            Debug.LogWarning("[GameManager] ไม่มีด่านถัดไป → จบเกมแทน");
            CompleteGame();
            return;
        }

        Debug.Log($"[GameManager] ผ่านด่าน {currentSceneName} → โหลดด่านถัดไป: {next}");
        SectorPoolManager.Instance?.LoadSector(next);
    }

    public void OnSectorCleared()
    {
        OnSectorCleared(SceneManager.GetActiveScene().name);
    }

    /// <summary>เงื่อนไขชนะเกมจากการฆ่าบอสสุดท้าย — เรียกจาก NullExeBoss (ถ้ามีด่านบอสแยก)</summary>
    public void OnFinalBossDefeated()
    {
        if (IsGameWon) return;

        SectorPoolManager.Instance?.MarkSectorCleared(SceneManager.GetActiveScene().name);
        CompleteGame();
    }

    /// <summary>
    /// จบเกม — ใช้ได้ทั้งกรณีฆ่าบอสสุดท้าย และกรณีผ่านด่านสุดท้ายในคลังจนหมด
    /// บันทึกความคืบหน้า + ส่งผลเข้าตารางสถิติ + แจ้ง UI
    /// </summary>
    public void CompleteGame()
    {
        if (IsGameWon) return;
        IsGameWon = true;

        CancelInvoke(nameof(RespawnPlayer));
        IsRespawning = false;
        IsWaitingForReborn = false;

        SectorPoolManager.Instance?.MarkGameCompleted();

        // บันทึกลงตารางสถิติในเครื่อง (แสดงที่หน้า Main Menu)
        int rank = LocalLeaderboard.Submit(RunTime, RunDeaths);
        Debug.Log($"[GameManager] SYSTEM RESTORED (เวลา {RunTime:F1}s, ตาย {RunDeaths} ครั้ง, อันดับ {rank})");

        OnGameWon?.Invoke();

        if (loadVictorySceneOnWin) Invoke(nameof(LoadVictoryScene), victoryDelay);
    }

    private void LoadVictoryScene()
    {
        SectorPoolManager.Instance?.LoadVictoryScene();
    }
}
