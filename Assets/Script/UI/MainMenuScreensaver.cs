using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screensaver แบบจอเก่า — ปล่อยหน้า Main Menu ทิ้งไว้ 10 วินาที
/// โลโก้เกมจะเริ่มเด้งไปมาชนขอบจอแล้วเปลี่ยนสีทุกครั้งที่ชน (แบบ DVD logo)
///
/// เข้ากับธีมเกมพอดี — เป็นมุกของหน้าจอคอมยุคเก่า
/// ขยับเมาส์หรือกดปุ่มอะไรก็ได้ โลโก้จะกลับไปที่เดิม
///
/// วิธีติดตั้ง:
/// 1. ใน Canvas ของ MainMenu วาง Image โลโก้เกม
/// 2. Attach สคริปต์นี้บน Image นั้น
/// 3. ลาก RectTransform ของ Canvas (หรือ Panel ที่ใช้เป็นขอบเขต) เข้าช่อง Bounds
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MainMenuScreensaver : MonoBehaviour
{
    [Header("Idle")]
    [Tooltip("ไม่มีอินพุตกี่วินาทีถึงจะเริ่มเด้ง")]
    [SerializeField] private float idleTimeToStart = 10f;

    [Header("Bounce")]
    [SerializeField] private float moveSpeed = 180f;
    [Tooltip("ขอบเขตการเด้ง — เว้นว่าง = ใช้ RectTransform ของ parent")]
    [SerializeField] private RectTransform bounds;

    [Header("Color")]
    [Tooltip("เปลี่ยนสีทุกครั้งที่ชนขอบ")]
    [SerializeField] private bool changeColorOnBounce = true;
    [SerializeField]
    private Color[] colors = new Color[]
    {
        new Color(0.3f, 1f, 0.8f),
        new Color(1f, 0.4f, 0.7f),
        new Color(1f, 0.85f, 0.3f),
        new Color(0.5f, 0.6f, 1f),
        new Color(0.7f, 1f, 0.4f),
    };

    [Header("Return")]
    [Tooltip("กลับที่เดิมนุ่ม ๆ ตอนผู้เล่นขยับเมาส์")]
    [SerializeField] private float returnSpeed = 8f;

    private RectTransform rect;
    private Image image;
    private Vector2 homePosition;
    private Color homeColor;

    private float idleTimer;
    private bool bouncing;
    private Vector2 direction;
    private Vector3 lastMousePosition;
    private int colorIndex;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        homePosition = rect.anchoredPosition;
        if (image != null) homeColor = image.color;

        if (bounds == null) bounds = rect.parent as RectTransform;

        direction = new Vector2(Random.value < 0.5f ? -1f : 1f, Random.value < 0.5f ? -1f : 1f).normalized;
    }

    private void Start()
    {
        lastMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        if (HasInput())
        {
            idleTimer = 0f;
            if (bouncing) StopBouncing();
        }
        else
        {
            idleTimer += Time.unscaledDeltaTime;
            if (!bouncing && idleTimer >= idleTimeToStart) bouncing = true;
        }

        if (bouncing) TickBounce();
        else ReturnHome();
    }

    private bool HasInput()
    {
        if (Input.anyKey || Input.anyKeyDown) return true;

        if ((Input.mousePosition - lastMousePosition).sqrMagnitude > 4f)
        {
            lastMousePosition = Input.mousePosition;
            return true;
        }

        return false;
    }

    private void TickBounce()
    {
        if (bounds == null) return;

        rect.anchoredPosition += direction * moveSpeed * Time.unscaledDeltaTime;

        Vector2 pos = rect.anchoredPosition;
        Vector2 halfBounds = bounds.rect.size * 0.5f;
        Vector2 halfSelf = rect.rect.size * 0.5f;

        float limitX = halfBounds.x - halfSelf.x;
        float limitY = halfBounds.y - halfSelf.y;

        bool bounced = false;

        if (Mathf.Abs(pos.x) > limitX)
        {
            pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
            direction.x = -direction.x;
            bounced = true;
        }

        if (Mathf.Abs(pos.y) > limitY)
        {
            pos.y = Mathf.Clamp(pos.y, -limitY, limitY);
            direction.y = -direction.y;
            bounced = true;
        }

        rect.anchoredPosition = pos;

        if (bounced && changeColorOnBounce) NextColor();
    }

    private void NextColor()
    {
        if (image == null || colors == null || colors.Length == 0) return;

        colorIndex = (colorIndex + 1) % colors.Length;
        image.color = colors[colorIndex];
    }

    private void StopBouncing()
    {
        bouncing = false;
        if (image != null) image.color = homeColor;
    }

    private void ReturnHome()
    {
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, homePosition, Time.unscaledDeltaTime * returnSpeed);
    }
}
