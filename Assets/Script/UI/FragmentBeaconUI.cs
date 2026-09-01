using TMPro;
using UnityEngine;

/// <summary>
/// สกิล Fragment Beacon — ลูกศรชี้ทิศ Data Fragment ที่ตกอยู่ + เวลานับถอยหลัง
///
/// ทำงานเฉพาะเมื่อ:
/// 1. ปลดล็อกสกิล fragment_beacon แล้ว
/// 2. มี fragment ตกอยู่ในฉากจริง
/// นอกนั้นจะซ่อนตัวเองทั้งหมด
///
/// ลูกศรชี้ทิศ fragment จากตำแหน่งผู้เล่น และเปลี่ยนเป็นสีเตือนเมื่อเวลาใกล้หมด
///
/// วิธีติดตั้ง:
/// 1. ใน Canvas สร้าง Panel ชื่อ "FragmentBeacon" (ปิด GameObject ไว้ได้)
/// 2. ใส่ Image ลูกศร (ให้ปลายลูกศรชี้ไปทางขวาตอน rotation = 0)
/// 3. ใส่ Text - TextMeshPro สำหรับเวลานับถอยหลัง
/// 4. Attach สคริปต์นี้บน GameObject ที่ active เสมอ แล้วลาก reference ให้ครบ
/// </summary>
public class FragmentBeaconUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject beaconRoot;
    [Tooltip("Image ลูกศร — ปลายลูกศรต้องชี้ไปทางขวาตอน Rotation Z = 0")]
    [SerializeField] private RectTransform arrow;
    [SerializeField] private TMP_Text timerText;

    [Header("Player")]
    [Tooltip("เว้นว่าง = หาจาก Tag Player เอง")]
    [SerializeField] private Transform player;

    [Header("Warning")]
    [Tooltip("เหลือกี่วินาทีถึงจะเปลี่ยนเป็นสีเตือน")]
    [SerializeField] private float warningTime = 8f;
    [SerializeField] private Color normalColor = new Color(0.4f, 1f, 0.8f, 1f);
    [SerializeField] private Color warningColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float warningBlinkSpeed = 6f;

    [Header("Distance")]
    [Tooltip("แสดงระยะห่างต่อท้ายเวลา เช่น '12.4s · 8m'")]
    [SerializeField] private bool showDistance = true;

    private UnityEngine.UI.Image arrowImage;

    private void Awake()
    {
        if (arrow != null) arrowImage = arrow.GetComponent<UnityEngine.UI.Image>();
        SetVisible(false);
    }

    private void Start()
    {
        if (player == null)
        {
            var obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) player = obj.transform;
        }
    }

    private void Update()
    {
        // ไม่ปลดล็อกสกิล = ไม่ต้องแสดงอะไรเลย
        if (!SkillEffects.IsUnlocked(SkillEffects.FragmentBeacon))
        {
            SetVisible(false);
            return;
        }

        var manager = FragmentInheritanceManager.Instance;
        DataFragment fragment = manager != null ? manager.ActiveFragment : null;

        if (fragment == null || player == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        Vector2 toFragment = (Vector2)fragment.transform.position - (Vector2)player.position;

        if (arrow != null && toFragment.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(toFragment.y, toFragment.x) * Mathf.Rad2Deg;
            arrow.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        float remaining = fragment.RemainingTime;

        if (timerText != null)
        {
            timerText.text = showDistance
                ? $"{remaining:F1}s · {toFragment.magnitude:F0}m"
                : $"{remaining:F1}s";
        }

        ApplyColor(remaining);
    }

    private void ApplyColor(float remaining)
    {
        Color color = normalColor;

        if (remaining <= warningTime)
        {
            // กะพริบถี่ขึ้นเรื่อย ๆ เมื่อใกล้หมดเวลา
            float t = 0.5f + 0.5f * Mathf.Sin(Time.time * warningBlinkSpeed);
            color = Color.Lerp(normalColor, warningColor, t);
        }

        if (arrowImage != null) arrowImage.color = color;
        if (timerText != null) timerText.color = color;
    }

    private void SetVisible(bool visible)
    {
        if (beaconRoot != null && beaconRoot.activeSelf != visible) beaconRoot.SetActive(visible);
    }
}
