using System.Collections;
using UnityEngine;

/// <summary>
/// ทำให้ HUD ขยับตามการเคลื่อนที่ของผู้เล่น และสั่นตอนตาย
///
/// - เดินซ้าย HUD เลื่อนซ้าย (ตามทิศ ไม่ใช่สวนทิศ) เหมือน UI ลอยตามตัว
/// - dash แล้วสะบัดแรงกว่าปกติ
/// - ตายแล้วสั่นทั้งจอ UI
///
/// วิธีติดตั้ง: attach บน RectTransform ที่ครอบ HUD ทั้งหมด
/// (สร้าง Empty ใน Canvas ชื่อ "HUD_Root" แล้วเอา HUD ทุกชิ้นไปไว้ข้างใน)
/// </summary>
public class HUDSway : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("เว้นว่าง = ใช้ RectTransform ของตัวเอง")]
    [SerializeField] private RectTransform target;
    [Tooltip("เว้นว่าง = หาผู้เล่นจาก Tag Player เอง")]
    [SerializeField] private PlayerController player;

    [Header("Sway")]
    [Tooltip("ระยะที่ HUD เลื่อนตามทิศเดิน (พิกเซล)")]
    [SerializeField] private float swayAmount = 18f;
    [Tooltip("ระยะเพิ่มตอน dash")]
    [SerializeField] private float dashSwayAmount = 45f;
    [Tooltip("ความหนืด — ต่ำ = ตามช้า ลอย ๆ")]
    [SerializeField] private float followSpeed = 6f;
    [Tooltip("สวนทิศแทนตามทิศ (ลองทั้งสองแบบแล้วเลือกที่ชอบ)")]
    [SerializeField] private bool invertDirection = false;

    [Header("Death Shake")]
    [SerializeField] private float deathShakeAmount = 30f;
    [SerializeField] private float deathShakeDuration = 0.6f;

    [Header("Damage Shake")]
    [Tooltip("สั่นเบา ๆ ตอนโดนดาเมจด้วย")]
    [SerializeField] private bool shakeOnDamage = true;
    [SerializeField] private float damageShakeAmount = 12f;
    [SerializeField] private float damageShakeDuration = 0.2f;

    private Vector2 basePosition;
    private Vector2 swayOffset;
    private float shakeAmount;
    private Coroutine shakeRoutine;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (target == null) target = GetComponent<RectTransform>();
        if (target != null) basePosition = target.anchoredPosition;
    }

    private void Start()
    {
        if (player == null)
        {
            var obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null)
            {
                player = obj.GetComponent<PlayerController>();
                playerHealth = obj.GetComponent<PlayerHealth>();
            }
        }
        else
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (shakeOnDamage && playerHealth != null) playerHealth.OnDamaged += HandleDamaged;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDeathSequenceStarted += HandleDeath;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDamaged -= HandleDamaged;
        if (GameManager.Instance != null) GameManager.Instance.OnPlayerDeathSequenceStarted -= HandleDeath;
    }

    private void Update()
    {
        if (target == null) return;

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();

        float amount = (player != null && player.IsDashing) ? dashSwayAmount : swayAmount;
        if (invertDirection) amount = -amount;

        Vector2 wanted = input * amount;
        swayOffset = Vector2.Lerp(swayOffset, wanted, Time.unscaledDeltaTime * followSpeed);

        Vector2 shake = shakeAmount > 0.01f ? Random.insideUnitCircle * shakeAmount : Vector2.zero;

        target.anchoredPosition = basePosition + swayOffset + shake;
    }

    private void HandleDamaged(float amount, BulletPatternType cause) => Shake(damageShakeAmount, damageShakeDuration);
    private void HandleDeath() => Shake(deathShakeAmount, deathShakeDuration);

    public void Shake(float amount, float duration)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(amount, duration));
    }

    private IEnumerator ShakeRoutine(float amount, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            shakeAmount = Mathf.Lerp(amount, 0f, t / duration);
            yield return null;
        }

        shakeAmount = 0f;
        shakeRoutine = null;
    }
}
