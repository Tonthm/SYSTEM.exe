using UnityEngine;

/// <summary>
/// ศัตรูชนิดที่ 1 — Pop-up Swarmer
///
/// ธีม: หน้าต่าง pop-up ที่ปิดแล้วเด้งขึ้นมาใหม่ ยิ่งปิดยิ่งแตกตัว
///
/// พฤติกรรม: ไม่เดินเรียบ ๆ แต่ "กระโดดเป็นจังหวะ" — หยุดนิ่ง → สั่นเตือน → พุ่งเข้าหาผู้เล่น → หยุดนิ่ง
/// ฆ่าด้วยการชน (ContactDamage) ไม่ยิงกระสุน
/// จุดเด่น: ตายแล้วแตกเป็นตัวเล็ก 2 ตัว (จำกัดจำนวนรุ่นได้) ทำให้ยิงมั่วแล้วจอยิ่งรก
///
/// ทักษะที่ต้องใช้: อ่านจังหวะ — ตอนสั่นเตือนคือช่วงที่ต้องขยับหลบ พุ่งแล้วหลบไม่ทัน
///
/// วิธีติดตั้ง (Prefab):
/// 1. GameObject + Rigidbody2D (Gravity 0) + Collider2D + Tag = Enemy
/// 2. โครงสร้าง child (สำคัญ — ห้ามใช้ node เดียวกันกับ Glitch Zone):
///       Swarmer (root)
///         └─ Visual        <- ช่อง Visual Root ของ GlitchVisualDisplacer
///              └─ Shake    <- ช่อง Rotating Part ของสคริปต์นี้ + SpriteRenderer
///    ถ้าชี้ทั้งสองสคริปต์ไป node เดียวกัน GlitchVisualDisplacer จะเขียนทับการสั่นทุกเฟรม
/// 3. Attach: EnemyHealth, ContactDamage, PopupSwarmer, GlitchVisualDisplacer
/// 4. ที่ ContactDamage ตั้ง Source Display Name = "Pop-up Swarmer"
/// 5. ลาก prefab ตัวเอง (หรือเวอร์ชันเล็ก) เข้าช่อง Split Prefab ถ้าอยากให้แตกตัว
/// </summary>
public class PopupSwarmer : EnemyAIBase
{
    [Header("Hop Movement")]
    [Tooltip("ความเร็วตอนพุ่ง")]
    [SerializeField] private float hopSpeed = 7f;
    [Tooltip("พุ่งนานกี่วินาทีต่อครั้ง")]
    [SerializeField] private float hopDuration = 0.35f;
    [Tooltip("หยุดพักระหว่างการพุ่ง")]
    [SerializeField] private float restDuration = 0.7f;
    [Tooltip("สั่นเตือนกี่วินาทีก่อนพุ่ง (ให้ผู้เล่นทันอ่านจังหวะ)")]
    [SerializeField] private float telegraphDuration = 0.3f;
    [Tooltip("องศาความคลาดเคลื่อนของการเล็ง — 0 = เล็งแม่นเป๊ะ")]
    [SerializeField] private float aimJitter = 12f;

    [Header("Telegraph Visual")]
    [SerializeField] private float shakeAmount = 0.08f;
    [SerializeField] private float shakeSpeed = 45f;

    [Header("Split On Death (แตกตัวตอนตาย)")]
    [Tooltip("prefab ที่จะเกิดตอนตาย — ปกติใช้ prefab ตัวเองนี่แหละ")]
    [SerializeField] private GameObject splitPrefab;
    [SerializeField] private int splitCount = 2;
    [Tooltip("รุ่นปัจจุบัน (0 = ตัวแม่) ตัวลูกจะ +1 เอง")]
    [SerializeField] private int generation = 0;
    [Tooltip("แตกได้ถึงรุ่นที่เท่าไหร่ — 1 = ตัวแม่แตกได้ ลูกแตกต่อไม่ได้")]
    [SerializeField] private int maxGeneration = 1;
    [Tooltip("ตัวลูกตัวเล็กลงกี่เท่า")]
    [SerializeField] private float splitScale = 0.65f;
    [Tooltip("HP / XP ของตัวลูกเทียบกับตัวแม่")]
    [SerializeField] private float splitHealthMultiplier = 0.4f;
    [SerializeField] private float splitXpMultiplier = 0.5f;
    [SerializeField] private float splitSpreadRadius = 0.6f;

    private enum State { Rest, Telegraph, Hop }
    private State state = State.Rest;
    private float stateTimer;
    private Vector2 hopDirection;
    private Vector3 visualBaseLocalPos;

    private EnemyHealth health;

    protected override void Awake()
    {
        base.Awake();
        health = GetComponent<EnemyHealth>();
        if (rotatingPart != null) visualBaseLocalPos = rotatingPart.localPosition;
    }

    protected override void OnSpawned()
    {
        stateTimer = Random.Range(0f, restDuration);   // กันตัวที่เกิดพร้อมกันพุ่งพร้อมกันเป๊ะ
        if (health != null) health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDeath -= HandleDeath;
    }

    protected override void Tick()
    {
        stateTimer -= Time.fixedDeltaTime;

        switch (state)
        {
            case State.Rest:
                SetVelocity(Vector2.zero);
                if (stateTimer <= 0f) EnterTelegraph();
                break;

            case State.Telegraph:
                SetVelocity(Vector2.zero);
                ShakeVisual();
                if (stateTimer <= 0f) EnterHop();
                break;

            case State.Hop:
                SetVelocity(hopDirection * hopSpeed);
                if (stateTimer <= 0f) EnterRest();
                break;
        }
    }

    private void EnterTelegraph()
    {
        state = State.Telegraph;
        stateTimer = telegraphDuration;

        // ล็อกทิศตั้งแต่ตอนเตือน — ผู้เล่นที่ขยับทันจะรอด
        float jitter = Random.Range(-aimJitter, aimJitter);
        hopDirection = (Quaternion.Euler(0f, 0f, jitter) * DirectionToPlayer).normalized;
    }

    private void EnterHop()
    {
        state = State.Hop;
        stateTimer = hopDuration;
        ResetVisual();
    }

    private void EnterRest()
    {
        state = State.Rest;
        stateTimer = restDuration;
        SetVelocity(Vector2.zero);
    }

    private void ShakeVisual()
    {
        if (rotatingPart == null) return;
        float offset = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
        rotatingPart.localPosition = visualBaseLocalPos + new Vector3(offset, 0f, 0f);
    }

    private void ResetVisual()
    {
        if (rotatingPart != null) rotatingPart.localPosition = visualBaseLocalPos;
    }

    private void HandleDeath()
    {
        if (splitPrefab == null || generation >= maxGeneration || splitCount <= 0) return;

        AudioManager.Play(AudioIds.SwarmerSplit);

        for (int i = 0; i < splitCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * splitSpreadRadius;
            GameObject child = Instantiate(splitPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            child.transform.localScale = transform.localScale * splitScale;

            var childSwarmer = child.GetComponent<PopupSwarmer>();
            if (childSwarmer != null) childSwarmer.generation = generation + 1;

            var childHealth = child.GetComponent<EnemyHealth>();
            if (childHealth != null)
            {
                childHealth.ApplyScaling(splitHealthMultiplier, splitXpMultiplier);
                // สำคัญ: ลงทะเบียนกับ WaveManager ไม่งั้น wave จะนับว่าเคลียร์หมดทั้งที่ยังมีลูกอยู่
                WaveManager.Instance?.RegisterEnemy(childHealth);
            }
        }

        Debug.Log($"[Pop-up Swarmer] แตกตัวเป็น {splitCount} ตัว (รุ่น {generation + 1}/{maxGeneration})");
    }
}
