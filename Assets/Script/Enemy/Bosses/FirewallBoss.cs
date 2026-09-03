using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// บอสประจำด่าน Firewall — กำแพงอิฐที่มีหัวฉีดไฟเรียงกันด้านบนสนาม
///
/// กลไก: หัวฉีดพ่นกำแพงไฟไล่ลงมาเป็นแถว กำแพงไฟ **เผากระสุนผู้เล่นทิ้ง**
/// ยืนยิงจากด้านล่างจึงไร้ประโยชน์ ผู้เล่นต้อง dash ข้ามกำแพงไฟขึ้นไปอยู่ฝั่งเดียวกับบอส
/// แล้วยิงในช่วงสั้น ๆ ก่อนแถวถัดไปจะมา
///
/// ทักษะที่บังคับ: จังหวะ dash — dash เร็วไปโดนแถวหน้า ช้าไปโดนแถวหลัง
///
/// วิธีติดตั้ง:
/// 1. GameObject ชื่อ "FirewallBoss" วางด้านบนสนาม — Collider2D + Tag = Enemy
/// 2. Attach: EnemyHealth (Max Health ~600), FirewallBoss
/// 3. สร้าง child "Nozzles" แล้วสร้าง Empty GameObject เรียงกัน 8–12 อันใต้กำแพง
///    (แต่ละอันคือหัวฉีด 1 ช่อง) → ลากทั้งหมดเข้า array Nozzles
/// 4. ทำ FireWallSegment Prefab (ดูสคริปต์นั้น) → ลากเข้าช่อง Fire Wall Segment Prefab
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class FirewallBoss : MonoBehaviour
{
    [Header("Nozzles (เรียงซ้าย → ขวา)")]
    [Tooltip("จุดพ่นไฟ — Empty GameObject เรียงกันใต้กำแพงอิฐ")]
    [SerializeField] private Transform[] nozzles;
    [SerializeField] private GameObject fireWallSegmentPrefab;

    [Header("Wall")]
    [SerializeField] private Vector2 wallDirection = Vector2.down;
    [SerializeField] private float wallSpeed = 3.5f;
    [Tooltip("กำแพงไฟอยู่นานกี่วินาทีก่อนหายเอง (ควรพอให้ไหลพ้นจอ)")]
    [SerializeField] private float wallLifetime = 6f;
    [SerializeField] private float wallDamage = 20f;

    [Header("Cycle")]
    [Tooltip("เว้นกี่วินาทีระหว่างแต่ละแถว — ช่วงนี้คือโอกาสยิงบอส")]
    [SerializeField] private float volleyInterval = 3f;
    [Tooltip("เตือนก่อนพ่นกี่วินาที (หัวฉีดเปลี่ยนสี)")]
    [SerializeField] private float telegraphDuration = 0.7f;
    [Tooltip("จำนวนช่องที่เว้นไว้ไม่พ่นไฟ — 0 = เต็มแถว ต้อง dash อย่างเดียว")]
    [SerializeField] private int gapCount = 2;
    [SerializeField] private float startDelay = 2f;

    [Header("Phase 2 (HP ต่ำกว่า 66%)")]
    [SerializeField] private float phase2IntervalMultiplier = 0.75f;
    [SerializeField] private int phase2GapCount = 1;

    [Header("Phase 3 (HP ต่ำกว่า 33%)")]
    [SerializeField] private float phase3IntervalMultiplier = 0.55f;
    [SerializeField] private int phase3GapCount = 1;
    [Tooltip("เฟส 3 พ่น 2 แถวติดกัน โดยแถวหลังเว้นคนละช่อง")]
    [SerializeField] private bool phase3DoubleVolley = true;
    [SerializeField] private float phase3SecondVolleyDelay = 0.8f;

    [Header("Telegraph Visual")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.4f, 0.1f, 1f);

    [Header("Format Warning")]
    [SerializeField] private bool formatWarningOnPhaseChange = true;

    private EnemyHealth health;
    private int currentPhase = 1;
    private readonly Dictionary<SpriteRenderer, Color> nozzleBaseColors = new Dictionary<SpriteRenderer, Color>();

    public System.Action<int> OnPhaseChanged;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();

        foreach (var nozzle in nozzles)
        {
            if (nozzle == null) continue;
            var sr = nozzle.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && !nozzleBaseColors.ContainsKey(sr)) nozzleBaseColors[sr] = sr.color;
        }
    }

    private void OnEnable()
    {
        if (health != null) health.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (health != null) health.OnHealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        if (nozzles == null || nozzles.Length == 0)
        {
            Debug.LogWarning("[Firewall Boss] ยังไม่ได้ลากหัวฉีดเข้า array Nozzles");
            return;
        }
        StartCoroutine(VolleyLoop());
    }

    private void HandleHealthChanged(float current, float max)
    {
        float percent = max > 0f ? current / max : 0f;

        int newPhase = percent <= 0.33f ? 3 : (percent <= 0.66f ? 2 : 1);
        if (newPhase == currentPhase) return;

        currentPhase = newPhase;
        Debug.Log($"[Firewall Boss] เข้าเฟส {currentPhase}");

        if (formatWarningOnPhaseChange)
        {
            SystemInterferenceManager.Instance?.TriggerFormatWarning(1.5f);
        }
        OnPhaseChanged?.Invoke(currentPhase);
    }

    private IEnumerator VolleyLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            yield return StartCoroutine(FireVolley(PickGaps(CurrentGapCount)));

            if (currentPhase == 3 && phase3DoubleVolley)
            {
                yield return new WaitForSeconds(phase3SecondVolleyDelay);
                yield return StartCoroutine(FireVolley(PickGaps(CurrentGapCount)));
            }

            yield return new WaitForSeconds(volleyInterval * CurrentIntervalMultiplier);
        }
    }

    private int CurrentGapCount =>
        currentPhase == 3 ? phase3GapCount : (currentPhase == 2 ? phase2GapCount : gapCount);

    private float CurrentIntervalMultiplier =>
        currentPhase == 3 ? phase3IntervalMultiplier : (currentPhase == 2 ? phase2IntervalMultiplier : 1f);

    /// <summary>สุ่มว่าช่องไหนจะเว้นไม่พ่นไฟ</summary>
    private HashSet<int> PickGaps(int count)
    {
        var gaps = new HashSet<int>();
        if (count <= 0 || nozzles.Length == 0) return gaps;

        int safeCount = Mathf.Min(count, nozzles.Length - 1);   // ห้ามเว้นทั้งแถว
        while (gaps.Count < safeCount)
        {
            gaps.Add(Random.Range(0, nozzles.Length));
        }
        return gaps;
    }

    private IEnumerator FireVolley(HashSet<int> gaps)
    {
        // เตือนก่อน — หัวฉีดที่จะพ่นเปลี่ยนสี
        SetTelegraph(gaps, true);
        yield return new WaitForSeconds(telegraphDuration);
        SetTelegraph(gaps, false);

        if (fireWallSegmentPrefab == null) yield break;

        AudioManager.Play(AudioIds.BossShootFirewall);

        for (int i = 0; i < nozzles.Length; i++)
        {
            if (gaps.Contains(i) || nozzles[i] == null) continue;

            GameObject seg = Instantiate(fireWallSegmentPrefab, nozzles[i].position, Quaternion.identity);
            var fire = seg.GetComponent<FireWallSegment>();
            if (fire != null) fire.Initialize(wallDirection, wallSpeed, wallLifetime, wallDamage);
        }
    }

    private void SetTelegraph(HashSet<int> gaps, bool on)
    {
        for (int i = 0; i < nozzles.Length; i++)
        {
            if (nozzles[i] == null) continue;
            var sr = nozzles[i].GetComponentInChildren<SpriteRenderer>();
            if (sr == null) continue;

            bool willFire = !gaps.Contains(i);
            if (on && willFire) sr.color = telegraphColor;
            else if (nozzleBaseColors.TryGetValue(sr, out Color baseColor)) sr.color = baseColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (nozzles == null) return;
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
        foreach (var n in nozzles)
        {
            if (n == null) continue;
            Gizmos.DrawLine(n.position, n.position + (Vector3)(wallDirection.normalized * 3f));
        }
    }
}
