using UnityEngine;

/// <summary>
/// ตัวเชื่อมระหว่าง GlitchZoneVolume กับภาพจริงบนหน้าจอ
/// แยก "ภาพที่เห็น" (child sprite) ออกจาก "hitbox จริง" (collider บน root)
/// เมื่อวัตถุอยู่ใน Glitch Zone ภาพจะถูกเลื่อนออกจากตำแหน่งจริง ทำให้ผู้เล่นเห็นไม่ตรงกับที่โดน
///
/// โครงสร้าง GameObject ที่ถูกต้อง:
///   Bullet (root)            <- Rigidbody2D + Collider2D + Bullet.cs + สคริปต์นี้   << ตำแหน่งจริง
///     └─ Visual              <- SpriteRenderer เท่านั้น                              << ตำแหน่งภาพ
///
/// วิธีติดตั้ง:
/// 1. เปิด Prefab กระสุน/ศัตรู ย้าย SpriteRenderer ไปไว้ใน child ใหม่ชื่อ "Visual"
///    (root เหลือแค่ Rigidbody2D + Collider2D + สคริปต์ logic)
/// 2. Attach สคริปต์นี้ที่ root แล้วลาก child "Visual" เข้าช่อง Visual Root
/// 3. จบ — ไม่ต้องลาก reference ของ Glitch Zone ใด ๆ ระบบหาให้เองผ่าน static registry
/// </summary>
public class GlitchVisualDisplacer : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("child ที่มี SpriteRenderer — เว้นว่างจะหา SpriteRenderer ตัวแรกใน child ให้เอง")]
    [SerializeField] private Transform visualRoot;

    [Header("Displacement")]
    [Tooltip("คูณเพิ่ม/ลด offset ที่ได้จากโซน (กระสุนควรเยอะกว่าศัตรูตัวใหญ่)")]
    [SerializeField] private float offsetMultiplier = 1f;
    [Tooltip("ความต่างเฉพาะตัวของวัตถุนี้ ทำให้แต่ละชิ้นไม่กระตุกพร้อมกันเป๊ะ (0 = กระตุกพร้อมกันหมด)")]
    [SerializeField] private float perObjectVariation = 0.5f;
    [Tooltip("สุ่มค่าเฉพาะตัวใหม่ทุกกี่วินาที")]
    [SerializeField] private float variationInterval = 0.12f;

    [Header("Extra Glitch (ไม่บังคับ)")]
    [Tooltip("สลับซ่อน/แสดงภาพเป็นจังหวะสั้น ๆ ตอนอยู่ในโซน")]
    [SerializeField] private bool flickerInZone = false;
    [Range(0f, 1f)]
    [SerializeField] private float flickerChance = 0.08f;

    private Vector3 baseLocalPosition;
    private SpriteRenderer[] renderers;
    private Vector2 personalOffset;
    private float variationTimer;
    private bool isInZone;

    /// <summary>ระบบอื่นเช็คได้ว่าตอนนี้วัตถุนี้อยู่ในโซน glitch หรือไม่</summary>
    public bool IsGlitching => isInZone;

    private void Awake()
    {
        if (visualRoot == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.transform != transform) visualRoot = sr.transform;
        }

        if (visualRoot == null)
        {
            Debug.LogWarning($"[Glitch Displacer] {name}: ไม่พบ child สำหรับภาพ — ต้องแยก SpriteRenderer ออกเป็น child ก่อน");
            enabled = false;
            return;
        }

        baseLocalPosition = visualRoot.localPosition;
        renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>();
        RandomizePersonalOffset();
    }

    private void LateUpdate()
    {
        // LateUpdate: ให้การเคลื่อนที่จริง (FixedUpdate/Update) เสร็จก่อน แล้วค่อยจัดตำแหน่งภาพ
        Vector2 zoneOffset;
        isInZone = GlitchZoneVolume.TryGetOffsetAt(transform.position, out zoneOffset);

        Vector3 worldBase = transform.TransformPoint(baseLocalPosition);

        if (!isInZone)
        {
            visualRoot.position = worldBase;
            SetRenderersEnabled(true);
            return;
        }

        variationTimer += Time.deltaTime;
        if (variationTimer >= variationInterval)
        {
            variationTimer = 0f;
            RandomizePersonalOffset();
        }

        Vector2 finalOffset = (zoneOffset + personalOffset) * offsetMultiplier;
        visualRoot.position = worldBase + (Vector3)finalOffset;

        if (flickerInZone)
        {
            SetRenderersEnabled(Random.value > flickerChance);
        }
    }

    private void RandomizePersonalOffset()
    {
        personalOffset = perObjectVariation > 0f
            ? Random.insideUnitCircle * perObjectVariation
            : Vector2.zero;
    }

    private void SetRenderersEnabled(bool value)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled != value) renderers[i].enabled = value;
        }
    }

    private void OnDisable()
    {
        // คืนภาพกลับที่เดิมเสมอ กันค้างตอนถูกปิด/เข้า pool
        if (visualRoot != null) visualRoot.localPosition = baseLocalPosition;
        SetRenderersEnabled(true);
    }
}
