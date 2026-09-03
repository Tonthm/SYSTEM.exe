using UnityEngine;

/// <summary>
/// เสียงซ่าตอนอยู่ใน Glitch Zone — ดังขึ้นตามความรุนแรงของโซน
///
/// ต่อกับ GlitchZoneVolume.GetIntensityAt() ที่ทำ hook ไว้แล้ว
/// ไม่ต้องลาก reference ของโซนใด ๆ ระบบถามจาก static registry เอง
///
/// เสียงช่วยบอกผู้เล่นว่า "ตอนนี้ภาพที่เห็นเชื่อไม่ได้" ซึ่งสำคัญมาก
/// เพราะ hitbox กับภาพไม่ตรงกัน ถ้าไม่มีสัญญาณผู้เล่นจะนึกว่าเกมบั๊ก
///
/// วิธีติดตั้ง:
/// 1. Attach บน GameObject ผู้เล่น (หรือ Empty ที่ตามผู้เล่น)
/// 2. Add Component: AudioSource → ใส่คลิปเสียงซ่าแบบ loop
///    ตั้ง Play On Awake = ✗, Loop = ✓, Volume = 0
/// 3. ลาก AudioSource เข้าช่อง Source
/// </summary>
public class GlitchZoneAmbience : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource source;
    [Tooltip("ระดับเสียงสูงสุดตอนอยู่กลางโซนที่ Intensity = 1")]
    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 0.5f;
    [Tooltip("ความเร็วในการไล่ระดับเสียง — ต่ำ = ค่อย ๆ ดังขึ้น")]
    [SerializeField] private float fadeSpeed = 4f;

    [Header("Pitch")]
    [Tooltip("เปลี่ยนระดับเสียงตามความรุนแรงด้วย (ยิ่งเพี้ยนยิ่งเสียงสูง)")]
    [SerializeField] private bool modulatePitch = true;
    [SerializeField] private float minPitch = 0.85f;
    [SerializeField] private float maxPitch = 1.25f;

    [Header("Target")]
    [Tooltip("จุดที่ใช้วัดความรุนแรง — เว้นว่าง = ใช้ตำแหน่งของ GameObject นี้")]
    [SerializeField] private Transform probe;

    /// <summary>ความรุนแรงที่วัดได้ล่าสุด (ให้ระบบอื่นเช่นสั่นกล้องเอาไปใช้ต่อได้)</summary>
    public float CurrentIntensity { get; private set; }

    private void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (probe == null) probe = transform;

        if (source != null)
        {
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
        }
    }

    private void Update()
    {
        if (source == null || probe == null) return;

        CurrentIntensity = Mathf.Clamp01(GlitchZoneVolume.GetIntensityAt(probe.position));

        float targetVolume = CurrentIntensity * maxVolume;
        source.volume = Mathf.MoveTowards(source.volume, targetVolume, fadeSpeed * Time.deltaTime * maxVolume);

        if (modulatePitch)
        {
            source.pitch = Mathf.Lerp(minPitch, maxPitch, CurrentIntensity);
        }

        // เล่นเฉพาะตอนต้องดังจริง ประหยัดกว่าเปิด loop ค้างไว้ตลอดเกม
        if (source.volume > 0.001f && !source.isPlaying) source.Play();
        else if (source.volume <= 0.001f && source.isPlaying) source.Stop();
    }
}
