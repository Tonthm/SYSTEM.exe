using UnityEngine;

/// <summary>
/// ควบคุมผู้เล่นในมินิเกม "Ghost.exe Flap" (bonus level สไตล์ Flappy Bird)
/// ผู้เล่นล็อกแกน X นิ่งตลอด ขยับแค่แกน Y ด้วยแรงโน้มถ่วง + แรงกระพือตอนกดปุ่ม
/// ชนอะไรก็ตาม (สิ่งกีดขวาง/พื้น/เพดาน) = จบเกมทันที ไม่มี checkpoint กลางทาง (ตามสไตล์ Flappy Bird จริง)
///
/// วิธีติดตั้ง:
/// 1. GameObject "FlapPlayer" (คนละตัวกับ Player หลักของเกม — ไม่ต้องมี PlayerHealth/PlayerShooter)
/// 2. Rigidbody2D (ตั้ง Gravity Scale ตามความรู้สึกที่ต้องการ)
/// 3. Collider2D ติ๊ก Is Trigger
/// 4. Attach สคริปต์นี้
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FlapController : MonoBehaviour
{
    [Header("Flap")]
    [SerializeField] private float flapForce = 8f;
    [SerializeField] private KeyCode flapKey = KeyCode.Space;
    [Tooltip("อนุญาตให้คลิกซ้ายกระพือได้ด้วย")]
    [SerializeField] private bool allowMouseFlap = true;

    [Header("Tilt (juice ไม่บังคับ)")]
    [SerializeField] private bool tiltWithVelocity = true;
    [SerializeField] private float maxTiltAngle = 35f;
    [SerializeField] private float tiltSmoothing = 8f;

    private Rigidbody2D rb;
    private bool isAlive = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.constraints |= RigidbodyConstraints2D.FreezePositionX; // ล็อกแกน X ตลอด — มีแค่สิ่งกีดขวางที่เคลื่อนที่เข้าหา
    }

    private void Update()
    {
        if (!isAlive) return;

        bool flapPressed = Input.GetKeyDown(flapKey) || (allowMouseFlap && Input.GetMouseButtonDown(0));
        if (flapPressed)
        {
            rb.linearVelocity = new Vector2(0f, flapForce); // Unity 6 API — เซตตรง ๆ ไม่บวกสะสม กระพือแต่ละครั้งรู้สึกเท่ากันเสมอ
            AudioManager.Play(AudioIds.PlayerDash); // reuse เสียง dash เป็นเสียงกระพือ ไม่ต้องหาเสียงใหม่
        }

        if (tiltWithVelocity)
        {
            float targetAngle = Mathf.Clamp(rb.linearVelocity.y * 6f, -maxTiltAngle, maxTiltAngle);
            float z = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, Time.deltaTime * tiltSmoothing);
            transform.rotation = Quaternion.Euler(0f, 0f, z);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => HandleCrash();

    private void HandleCrash()
    {
        if (!isAlive) return;
        isAlive = false;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // หยุดฟิสิกส์ทันที กันตกต่อหลัง crash

        FlapGameManager.Instance?.OnCrash();
    }
}
