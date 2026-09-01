using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// คุมด่านสอนเล่น (Tutorial Sector) — ไล่ทีละขั้นตอน เมื่อครบทุกขั้นจะ
/// 1) บันทึกว่าผ่าน tutorial แล้ว (SectorPoolManager.MarkTutorialComplete)
/// 2) ปลดล็อกทางออก (SectorExitTrigger) หรือโหลดด่านถัดไปจาก Sector Pool ให้เลย
///
/// ขั้นตอน Move / Shoot / Dash ตรวจจับจาก Input โดยตรง (ใช้ปุ่มชุดเดียวกับ PlayerController)
/// ขั้นตอน KillEnemies ฟังจาก EnemyHealth.OnAnyEnemyKilled
/// ขั้นตอน Custom ให้ trigger ในฉากเรียก CompleteStep("ชื่อ id") เอง
///
/// วิธีติดตั้ง:
/// 1. ใน Scene ของด่าน tutorial สร้าง Empty GameObject ชื่อ "TutorialController" attach สคริปต์นี้
/// 2. ลาก Text (UI) สำหรับแสดงคำใบ้เข้าช่อง Prompt Text
/// 3. ลาก SectorExitTrigger ของด่านเข้าช่อง Exit Trigger (ตั้ง Start Locked = ✓ ที่ตัวประตู)
/// </summary>
public class TutorialSectorController : MonoBehaviour
{
    public enum StepType
    {
        Move,           // requiredAmount = จำนวนวินาทีที่ต้องเดิน
        Shoot,          // requiredAmount = จำนวนนัดที่ต้องยิง
        Dash,           // requiredAmount = จำนวนครั้งที่ต้อง dash
        KillEnemies,    // requiredAmount = จำนวนศัตรูที่ต้องกำจัด
        Custom          // รอ CompleteStep(id) จากภายนอก
    }

    [System.Serializable]
    public class TutorialStep
    {
        public string id = "step";
        [TextArea(2, 3)] public string prompt = "ทำตามคำสั่ง";
        public StepType type = StepType.Move;
        [Tooltip("Move = วินาที, Shoot/Dash = จำนวนครั้ง, KillEnemies = จำนวนตัว")]
        public float requiredAmount = 1f;

        [Tooltip("GameObject ที่จะเปิดใช้งานตอนเริ่มขั้นตอนนี้ (เช่น ศัตรูที่ซ่อนไว้)")]
        public GameObject[] activateOnStepStart;
        [Tooltip("เอฟเฟกต์ตอนของพวกนั้นโผล่ (เช่น particle พอร์ทัล)")]
        public GameObject spawnEffectPrefab;

        [HideInInspector] public float progress;
        [HideInInspector] public bool done;
    }

    [Header("Steps (เรียงตามลำดับ)")]
    [SerializeField]
    private List<TutorialStep> steps = new List<TutorialStep>
    {
        new TutorialStep { id = "move",  prompt = "WASD / ARROW KEYS - move the Ghost Process",        type = StepType.Move,        requiredAmount = 2f },
        new TutorialStep { id = "aim",   prompt = "HOLD LEFT CLICK - fire toward the cursor",          type = StepType.Shoot,       requiredAmount = 5f },
        new TutorialStep { id = "dash",  prompt = "SHIFT / SPACE - dash (you are invulnerable mid-dash)", type = StepType.Dash,      requiredAmount = 2f },
        new TutorialStep { id = "kill",  prompt = "Terminate the background process",                  type = StepType.KillEnemies, requiredAmount = 1f },
    };

    [Header("UI")]
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string completeMessage = "TUTORIAL COMPLETE - ENTERING KERNEL CORE";

    [Header("Flow")]
    [SerializeField] private SectorExitTrigger exitTrigger;
    [Tooltip("จบ tutorial แล้วโหลดด่านถัดไปเองเลย (ไม่ต้องเดินไปที่ทางออก)")]
    [SerializeField] private bool autoLoadNextOnComplete = false;
    [SerializeField] private float autoLoadDelay = 2f;

    private int currentIndex = 0;
    private bool finished = false;

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyKilled -= HandleEnemyKilled;
    }

    private void Start()
    {
        exitTrigger?.Lock();

        // ซ่อนของทุกขั้นตอนไว้ก่อน แล้วค่อยเปิดทีละขั้น
        foreach (var step in steps)
        {
            if (step.activateOnStepStart == null) continue;
            foreach (var obj in step.activateOnStepStart)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        ShowCurrentPrompt();
        ActivateStepObjects(GetCurrentStep());
    }

    /// <summary>เปิดของที่ผูกไว้กับขั้นตอนนี้ (เช่น ศัตรูที่ซ่อนไว้จนถึงขั้นให้ยิง)</summary>
    private void ActivateStepObjects(TutorialStep step)
    {
        if (step == null || step.activateOnStepStart == null) return;

        foreach (var obj in step.activateOnStepStart)
        {
            if (obj == null) continue;

            obj.SetActive(true);
            if (step.spawnEffectPrefab != null)
            {
                Instantiate(step.spawnEffectPrefab, obj.transform.position, Quaternion.identity);
            }
        }
    }

    private void Update()
    {
        if (finished) return;

        TutorialStep step = GetCurrentStep();
        if (step == null) return;

        float progressBefore = step.progress;

        switch (step.type)
        {
            case StepType.Move:
                float inputMag = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).sqrMagnitude;
                if (inputMag > 0.01f) step.progress += Time.deltaTime;
                break;

            case StepType.Shoot:
                if (Input.GetMouseButtonDown(0)) step.progress += 1f;
                break;

            case StepType.Dash:
                if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space)) step.progress += 1f;
                break;
        }

        if (step.progress >= step.requiredAmount) AdvanceStep();
        else if (!Mathf.Approximately(progressBefore, step.progress) && step.type != StepType.Move) ShowCurrentPrompt();
    }

    private void HandleEnemyKilled(EnemyHealth enemy)
    {
        if (finished) return;
        TutorialStep step = GetCurrentStep();
        if (step == null || step.type != StepType.KillEnemies) return;

        step.progress += 1f;
        if (step.progress >= step.requiredAmount) AdvanceStep();
        else ShowCurrentPrompt();
    }

    /// <summary>ให้ trigger/สคริปต์ในฉากเรียกเพื่อจบขั้นตอนแบบ Custom</summary>
    public void CompleteStep(string stepId)
    {
        TutorialStep step = GetCurrentStep();
        if (step == null || step.id != stepId) return;
        AdvanceStep();
    }

    private TutorialStep GetCurrentStep()
    {
        if (currentIndex < 0 || currentIndex >= steps.Count) return null;
        return steps[currentIndex];
    }

    private void AdvanceStep()
    {
        TutorialStep step = GetCurrentStep();
        if (step != null) step.done = true;

        currentIndex++;

        if (currentIndex >= steps.Count)
        {
            Finish();
        }
        else
        {
            ShowCurrentPrompt();
            ActivateStepObjects(GetCurrentStep());
        }
    }

    private void ShowCurrentPrompt()
    {
        if (promptText == null) return;

        TutorialStep step = GetCurrentStep();
        if (step == null) { promptText.text = completeMessage; return; }

        string counter = "";
        if (step.type == StepType.Shoot || step.type == StepType.Dash || step.type == StepType.KillEnemies)
        {
            counter = $"  [{Mathf.FloorToInt(step.progress)}/{Mathf.FloorToInt(step.requiredAmount)}]";
        }
        promptText.text = step.prompt + counter;
    }

    private void Finish()
    {
        finished = true;
        if (promptText != null) promptText.text = completeMessage;

        SectorPoolManager.Instance?.MarkTutorialComplete();
        exitTrigger?.Unlock();

        Debug.Log("[Tutorial] เสร็จสิ้น — ปลดล็อก Sector Pool แล้ว");

        if (autoLoadNextOnComplete) Invoke(nameof(LoadNextSector), autoLoadDelay);
    }

    private void LoadNextSector()
    {
        if (SectorPoolManager.Instance == null) return;
        string next = SectorPoolManager.Instance.GetNextSector();
        SectorPoolManager.Instance.LoadSector(next);
    }
}
