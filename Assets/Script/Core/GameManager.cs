using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ศูนย์กลางควบคุมโฟลว์เกม: จุด Checkpoint ล่าสุด, การ Respawn (Kill Process -> Spawn ใหม่),
/// การผ่านด่าน และเงื่อนไขชนะเกม
///
/// [อัปเดต]
/// - กันตายซ้อน/respawn ซ้อน (isRespawning)
/// - ตั้ง checkpoint อัตโนมัติจากตำแหน่งเริ่มต้นของผู้เล่น ถ้ายังไม่ได้ตั้งใน Inspector
/// - เพิ่ม OnFinalBossDefeated() + OnGameWon สำหรับเงื่อนไขชนะ (กำจัด NULL.exe)
///
/// วิธีติดตั้ง: สร้าง Empty GameObject ชื่อ "GameManager" attach สคริปต์นี้ในทุก Scene ที่เล่นได้จริง
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private Transform currentCheckpoint;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1.2f;

    [Header("Victory")]
    [Tooltip("ชนะแล้วโหลด Victory Scene ให้อัตโนมัติ (ตั้งชื่อ Scene ที่ SectorPoolManager)")]
    [SerializeField] private bool loadVictorySceneOnWin = true;
    [SerializeField] private float victoryDelay = 3f;

    public System.Action OnPlayerRespawned;
    public System.Action OnPlayerDeathSequenceStarted;
    /// <summary>ยิงตอนกำจัด NULL.exe สำเร็จ — ให้ VictoryScreenUI ไปแสดงผล</summary>
    public System.Action OnGameWon;

    public bool IsGameWon { get; private set; }
    public bool IsRespawning { get; private set; }

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

        // ถ้าด่านนี้ยังไม่ได้วาง checkpoint ไว้ ใช้ตำแหน่งเริ่มต้นของผู้เล่นเป็นจุดเกิดแรก
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

    /// <summary>เรียกจาก PlayerHealth.Die() — เริ่มลำดับ "Kill Process -> Spawn Process ใหม่"</summary>
    public void OnPlayerDied()
    {
        if (IsGameWon) return;      // ชนะแล้วไม่ต้อง respawn
        if (IsRespawning) return;   // กันเรียกซ้อนจากกระสุนหลายนัดในเฟรมเดียว

        IsRespawning = true;
        OnPlayerDeathSequenceStarted?.Invoke(); // UI ไปแสดงหน้าจอ Task Manager ตรงนี้
        Invoke(nameof(RespawnPlayer), respawnDelay);
    }

    public void RespawnPlayer()
    {
        IsRespawning = false;

        if (playerObject == null || currentCheckpoint == null)
        {
            Debug.LogWarning("[GameManager] Missing playerObject or checkpoint reference for respawn");
            return;
        }

        playerObject.transform.position = currentCheckpoint.position;

        var health = playerObject.GetComponent<PlayerHealth>();
        health?.ResetHealth();

        playerObject.SetActive(true);

        OnPlayerRespawned?.Invoke();
        Debug.Log("[GameManager] New Ghost Process spawned at checkpoint");
    }

    /// <summary>เรียกเมื่อผู้เล่นผ่านด่านสำเร็จ (ไม่ใช่ตาย) — ไปเลือกด่านถัดไปจาก Sector Pool</summary>
    public void OnSectorCleared(string currentSceneName)
    {
        if (IsGameWon) return;

        SectorPoolManager.Instance?.MarkSectorCleared(currentSceneName);
        string next = SectorPoolManager.Instance?.GetNextSector();
        SectorPoolManager.Instance?.LoadSector(next);
    }

    /// <summary>เวอร์ชันไม่ต้องส่งชื่อ — ใช้ชื่อ Scene ปัจจุบันให้เอง</summary>
    public void OnSectorCleared()
    {
        OnSectorCleared(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// เงื่อนไขชนะเกม — เรียกจาก NullExeBoss เมื่อ NULL.exe ถูกกำจัด
    /// ลำดับ: บันทึกด่านนี้ว่าผ่าน -> ยกเลิก respawn ค้าง -> แจ้ง UI -> โหลด Victory Scene
    /// </summary>
    public void OnFinalBossDefeated()
    {
        if (IsGameWon) return;
        IsGameWon = true;

        CancelInvoke(nameof(RespawnPlayer));
        IsRespawning = false;

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
