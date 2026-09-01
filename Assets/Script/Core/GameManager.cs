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

    [Header("Wave")]
    [Tooltip("ตายแล้วเริ่ม wave ปัจจุบันใหม่ และย้อน XP กลับไปก่อนเข้า wave นั้น")]
    [SerializeField] private bool restartWaveOnDeath = true;

    [Header("Death Effect")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float deathEffectLifetime = 2f;

    [Header("Victory")]
    [SerializeField] private bool loadVictorySceneOnWin = true;
    [SerializeField] private float victoryDelay = 3f;

    public System.Action OnPlayerRespawned;
    public System.Action OnPlayerDeathSequenceStarted;
    public System.Action OnGameWon;

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

        playerObject.GetComponent<PlayerController>()?.ResetState();
        playerObject.GetComponent<PlayerHealth>()?.ResetHealth();

        // เริ่ม wave ปัจจุบันใหม่ + ย้อน XP กลับไปก่อนเข้า wave นั้น
        if (restartWaveOnDeath) WaveManager.Instance?.RestartCurrentWave();

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
        string next = SectorPoolManager.Instance?.GetNextSector();
        SectorPoolManager.Instance?.LoadSector(next);
    }

    public void OnSectorCleared()
    {
        OnSectorCleared(SceneManager.GetActiveScene().name);
    }

    /// <summary>เงื่อนไขชนะเกม — เรียกจาก NullExeBoss</summary>
    public void OnFinalBossDefeated()
    {
        if (IsGameWon) return;
        IsGameWon = true;

        CancelInvoke(nameof(RespawnPlayer));
        IsRespawning = false;
        IsWaitingForReborn = false;

        SectorPoolManager.Instance?.MarkSectorCleared(SceneManager.GetActiveScene().name);
        SectorPoolManager.Instance?.MarkGameCompleted();

        Debug.Log("[GameManager] NULL.exe terminated — SYSTEM RESTORED");
        OnGameWon?.Invoke();

        if (loadVictorySceneOnWin) Invoke(nameof(LoadVictoryScene), victoryDelay);
    }

    private void LoadVictoryScene()
    {
        SectorPoolManager.Instance?.LoadVictoryScene();
    }
}
