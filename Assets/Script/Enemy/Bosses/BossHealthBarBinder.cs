using System.Collections;
using UnityEngine;

/// <summary>
/// ตัวเชื่อมศัตรูตัวใดก็ได้เข้ากับหลอดเลือดบอส
///
/// ใช้แทนการให้สคริปต์บอสเรียกเอง เพราะมีปัญหา 3 อย่าง:
/// 1. บอสประจำด่านที่ทำจากศัตรูธรรมดาแต่งให้แข็ง ไม่มีสคริปต์บอสเลย
/// 2. BossEntrance ปิดสคริปต์บอสระหว่างลงมา ทำให้ Start() ยังไม่รัน
/// 3. ลืม attach สคริปต์บอสบน prefab
///
/// component นี้ทำงานได้ตราบใดที่มี EnemyHealth บน GameObject เดียวกัน
/// และไม่ต้องอยู่ใน list Disable During Entrance ของ BossEntrance
///
/// วิธีติดตั้ง:
/// 1. Attach บน prefab บอส (GameObject เดียวกับ EnemyHealth)
/// 2. กรอก Boss Name
/// 3. **อย่าลาก component นี้เข้า Disable During Entrance ของ BossEntrance**
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class BossHealthBarBinder : MonoBehaviour
{
    [Header("Display")]
    [Tooltip("ชื่อที่แสดงบนหลอดเลือด")]
    [SerializeField] private string bossName = "BOSS";

    [Header("Timing")]
    [Tooltip("หน่วงก่อนแสดงหลอด (วินาที) — ตั้งให้เท่ากับเวลาที่บอสลงมาถึงพื้น ถ้ามี BossEntrance")]
    [SerializeField] private float showDelay = 0f;
    [Tooltip("รอให้ BossEntrance ลงมาเสร็จก่อนค่อยแสดงหลอด (ถ้ามี BossEntrance บนตัวเดียวกัน)")]
    [SerializeField] private bool waitForEntrance = true;

    [Header("Music")]
    [Tooltip("เพลงประจำบอสตัวนี้ — เว้นว่าง = ไม่เปลี่ยนเพลง")]
    [SerializeField] private string bossMusicId = "";
    [Tooltip("เพลงที่จะกลับไปเล่นหลังบอสตาย")]
    [SerializeField] private string musicAfterDeath = AudioIds.MusicGameplay;
    [SerializeField] private float musicFadeDuration = 1.5f;

    [Header("Behaviour")]
    [Tooltip("ซ่อนหลอดตอนบอสตาย")]
    [SerializeField] private bool hideOnDeath = true;

    private EnemyHealth health;
    private BossEntrance entrance;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        entrance = GetComponent<BossEntrance>();
    }

    private void Start()
    {
        StartCoroutine(BindRoutine());
    }

    private IEnumerator BindRoutine()
    {
        // รอให้บอสลงมาถึงพื้นก่อน
        if (waitForEntrance && entrance != null)
        {
            while (entrance.IsEntering) yield return null;
        }

        if (showDelay > 0f) yield return new WaitForSeconds(showDelay);

        if (health == null)
        {
            Debug.LogWarning($"[{name}] ไม่พบ EnemyHealth — ผูกหลอดเลือดบอสไม่ได้");
            yield break;
        }

        var bar = BossHealthBarUI.Instance;
        if (bar == null)
        {
            Debug.LogWarning($"[{name}] ไม่พบ BossHealthBarUI ในฉาก — หลอดเลือดบอสจะไม่ขึ้น");
            yield break;
        }

        bar.Bind(health, bossName);
        Debug.Log($"[{name}] ผูกหลอดเลือดบอสแล้ว: {bossName}");

        if (!string.IsNullOrEmpty(bossMusicId)) AudioManager.PlayMusic(bossMusicId, musicFadeDuration);

        if (hideOnDeath) health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        AudioManager.Play(AudioIds.BossDeath);
        BossHealthBarUI.Instance?.Hide();

        if (!string.IsNullOrEmpty(bossMusicId) && !string.IsNullOrEmpty(musicAfterDeath))
        {
            AudioManager.PlayMusic(musicAfterDeath, musicFadeDuration);
        }
    }
}
