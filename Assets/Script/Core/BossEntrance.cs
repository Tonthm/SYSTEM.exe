using System.Collections;
using UnityEngine;

/// <summary>
/// บอสเข้าสนามจากขอบบนของแมพ แล้วเลื่อนลงมาประจำที่
///
/// ระหว่างเข้าสนามบอสจะ **ยิงไม่โดนและไม่ทำดาเมจ** — AI กับ Collider ถูกปิดไว้
/// กันไม่ให้ผู้เล่นยิงบอสตายตั้งแต่ยังไม่ทันเห็นตัว และกันบอสชนผู้เล่นตอนไถลลงมา
///
/// วิธีติดตั้ง:
/// 1. วางบอสไว้ตรงตำแหน่ง **ที่ต้องการให้ยืนจริง** ในฉาก (ไม่ใช่นอกจอ)
///    สคริปต์จะย้ายขึ้นไปนอกจอให้เองตอน Start แล้วค่อยเลื่อนลงมาที่เดิม
/// 2. Attach สคริปต์นี้
/// 3. ลากสคริปต์ AI ของบอส (FirewallBoss / CentipedeBoss / RegistryBoss / NullExeBoss)
///    เข้า list Disable During Entrance
/// </summary>
public class BossEntrance : MonoBehaviour
{
    [Header("Entrance")]
    [Tooltip("เริ่มจากสูงกว่าตำแหน่งจริงกี่หน่วย (ให้พ้นขอบจอ)")]
    [SerializeField] private float entryHeight = 8f;
    [Tooltip("หน่วงก่อนเริ่มเลื่อนลง (ให้ผู้เล่นได้ยินเสียง/เห็นเงาก่อน)")]
    [SerializeField] private float entryDelay = 1f;
    [SerializeField] private float entryDuration = 2f;
    [Tooltip("ชะลอตอนใกล้ถึงที่ ให้ดูมีน้ำหนัก")]
    [SerializeField] private bool easeOut = true;

    [Header("ปิดระหว่างเข้าสนาม")]
    [Tooltip("สคริปต์ AI ของบอส — จะถูกปิดจนกว่าจะเข้าที่")]
    [SerializeField] private MonoBehaviour[] disableDuringEntrance;
    [Tooltip("ปิด Collider ด้วย = ยิงไม่โดนและไม่ชนผู้เล่นระหว่างลงมา")]
    [SerializeField] private bool disableColliders = true;

    [Header("Feedback")]
    [SerializeField] private bool formatWarningOnEntry = true;

    /// <summary>ยิงตอนบอสเข้าที่แล้วเริ่มสู้จริง</summary>
    public System.Action OnEntranceComplete;

    public bool IsEntering { get; private set; }

    private Collider2D[] colliders;

    private void Start()
    {
        StartCoroutine(EntranceRoutine());
    }

    private IEnumerator EntranceRoutine()
    {
        IsEntering = true;

        Vector3 targetPosition = transform.position;
        transform.position = targetPosition + Vector3.up * entryHeight;

        SetComponentsEnabled(false);

        if (formatWarningOnEntry)
        {
            SystemInterferenceManager.Instance?.TriggerFormatWarning(entryDelay + entryDuration);
        }

        yield return new WaitForSeconds(entryDelay);

        Vector3 start = transform.position;
        float t = 0f;
        while (t < entryDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / entryDuration);
            if (easeOut) p = 1f - (1f - p) * (1f - p);
            transform.position = Vector3.Lerp(start, targetPosition, p);
            yield return null;
        }

        transform.position = targetPosition;

        SetComponentsEnabled(true);
        IsEntering = false;

        Debug.Log($"[Boss Entrance] {name} เข้าสนามแล้ว");
        OnEntranceComplete?.Invoke();
    }

    private void SetComponentsEnabled(bool value)
    {
        if (disableDuringEntrance != null)
        {
            foreach (var script in disableDuringEntrance)
            {
                if (script != null) script.enabled = value;
            }
        }

        if (!disableColliders) return;

        if (colliders == null) colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            if (col != null) col.enabled = value;
        }
    }
}
