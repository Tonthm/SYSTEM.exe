using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// ศูนย์กลางเสียงทั้งเกม — เรียกด้วย id เป็นสตริงจากที่ไหนก็ได้
///
///     AudioManager.Play(AudioIds.PlayerShoot);
///     AudioManager.PlayMusic(AudioIds.MusicBossFirewall);
///
/// ออกแบบให้ลาก AudioClip ที่เดียวจบ (ในตัว AudioManager) ไม่ต้องไปลากทีละสคริปต์
///
/// จุดสำคัญของเกม bullet hell: กระสุนยิงรัวเป็นร้อยนัดต่อวินาที
/// ถ้าเล่นเสียงทุกนัดจะกลายเป็นเสียงแตกและเฟรมตก
/// ทุกเสียงจึงมี Min Interval (เว้นระยะขั้นต่ำ) และ Max Simultaneous (จำกัดจำนวนที่ดังพร้อมกัน)
///
/// วิธีติดตั้ง:
/// 1. ใน Scene Bootstrap สร้าง Empty GameObject ชื่อ "AudioManager" attach สคริปต์นี้
/// 2. กาง list Sounds แล้วกด + ใส่ id กับ AudioClip ตามตารางในคู่มือ
/// 3. เสร็จ — สคริปต์อื่นเรียกใช้เองหมดแล้ว
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class SoundEntry
    {
        [Tooltip("ชื่อเรียกใช้ในโค้ด — ดูรายการทั้งหมดที่ AudioIds.cs")]
        public string id;

        [Tooltip("ใส่ได้หลายไฟล์ ระบบจะสุ่มเล่น ทำให้เสียงซ้ำ ๆ ไม่น่าเบื่อ")]
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("สุ่มระดับเสียงสูงต่ำ ±ค่านี้ (0 = ไม่สุ่ม)")]
        [Range(0f, 0.5f)] public float pitchVariance = 0.08f;

        [Tooltip("เว้นระยะขั้นต่ำระหว่างเสียงเดียวกัน (วินาที) — สำคัญมากกับเสียงยิง")]
        public float minInterval = 0.04f;

        [Tooltip("ดังพร้อมกันได้สูงสุดกี่เสียง (0 = ไม่จำกัด)")]
        public int maxSimultaneous = 4;

        [HideInInspector] public float lastPlayTime = -99f;
        [HideInInspector] public int activeCount;
    }

    [Header("Mixer (ไม่บังคับ)")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float masterSfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float masterMusicVolume = 0.6f;

    [Header("Pool")]
    [Tooltip("จำนวน AudioSource สำหรับ SFX — กระสุนเยอะควรมี 16-24")]
    [SerializeField] private int sfxSourceCount = 20;

    [Header("Sounds")]
    [SerializeField] private List<SoundEntry> sounds = new List<SoundEntry>();

    [Header("Debug")]
    [Tooltip("เตือนใน Console เมื่อเรียก id ที่ยังไม่ได้ใส่คลิป")]
    [SerializeField] private bool warnOnMissingId = true;

    private readonly Dictionary<string, SoundEntry> lookup = new Dictionary<string, SoundEntry>();
    private AudioSource[] sfxSources;
    private AudioSource musicA;
    private AudioSource musicB;
    private bool usingA = true;
    private string currentMusicId;
    private Coroutine fadeRoutine;

    private float duckMultiplier = 1f;
    private Coroutine duckRoutine;

    /// <summary>เพลงที่กำลังเล่นอยู่ (ใช้เช็คก่อนสั่งเปลี่ยน จะได้ไม่รีสตาร์ทเพลงเดิม)</summary>
    public string CurrentMusicId => currentMusicId;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookup();
        BuildSources();
    }

    private void BuildLookup()
    {
        lookup.Clear();
        foreach (var entry in sounds)
        {
            if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
            if (lookup.ContainsKey(entry.id))
            {
                Debug.LogWarning($"[Audio] id ซ้ำ: {entry.id}");
                continue;
            }
            lookup[entry.id] = entry;
        }
    }

    private void BuildSources()
    {
        sfxSources = new AudioSource[Mathf.Max(1, sfxSourceCount)];
        for (int i = 0; i < sfxSources.Length; i++)
        {
            var obj = new GameObject($"SFX_{i:00}");
            obj.transform.SetParent(transform);

            var src = obj.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = sfxMixerGroup;
            sfxSources[i] = src;
        }

        musicA = CreateMusicSource("Music_A");
        musicB = CreateMusicSource("Music_B");
    }

    private AudioSource CreateMusicSource(string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform);

        var src = obj.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.volume = 0f;
        src.outputAudioMixerGroup = musicMixerGroup;
        return src;
    }

    // ================= SFX =================

    /// <summary>เล่นเสียงเอฟเฟกต์ตาม id — เรียกได้จากทุกที่ ปลอดภัยแม้ยังไม่มี AudioManager ในฉาก</summary>
    public static void Play(string id)
    {
        if (Instance == null || string.IsNullOrEmpty(id)) return;
        Instance.PlaySFX(id);
    }

    public void PlaySFX(string id)
    {
        if (!lookup.TryGetValue(id, out SoundEntry entry))
        {
            if (warnOnMissingId) Debug.LogWarning($"[Audio] ไม่พบเสียง id = {id} (ยังไม่ได้ใส่ใน AudioManager)");
            return;
        }

        if (entry.clips == null || entry.clips.Length == 0) return;

        // กันเสียงถี่เกินจนแตก
        if (Time.unscaledTime - entry.lastPlayTime < entry.minInterval) return;
        if (entry.maxSimultaneous > 0 && entry.activeCount >= entry.maxSimultaneous) return;

        AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
        if (clip == null) return;

        AudioSource src = GetFreeSource();
        if (src == null) return;

        src.clip = clip;
        src.volume = entry.volume * masterSfxVolume;
        src.pitch = 1f + Random.Range(-entry.pitchVariance, entry.pitchVariance);
        src.Play();

        entry.lastPlayTime = Time.unscaledTime;
        entry.activeCount++;
        StartCoroutine(ReleaseAfter(entry, clip.length / Mathf.Max(0.01f, src.pitch)));
    }

    private IEnumerator ReleaseAfter(SoundEntry entry, float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        entry.activeCount = Mathf.Max(0, entry.activeCount - 1);
    }

    private AudioSource GetFreeSource()
    {
        foreach (var src in sfxSources)
        {
            if (src != null && !src.isPlaying) return src;
        }

        // ทุกช่องไม่ว่าง — ทับช่องแรก (เสียงเก่าสุดหายไป ดีกว่าเสียงใหม่ไม่ดัง)
        return sfxSources.Length > 0 ? sfxSources[0] : null;
    }

    // ================= Music =================

    public static void PlayMusic(string id, float fadeDuration = 1.5f)
    {
        if (Instance == null) return;
        Instance.PlayMusicInternal(id, fadeDuration);
    }

    public static void StopMusic(float fadeDuration = 1f)
    {
        if (Instance == null) return;
        Instance.PlayMusicInternal(null, fadeDuration);
    }

    public void PlayMusicInternal(string id, float fadeDuration)
    {
        // เพลงเดิมอยู่แล้ว ไม่ต้องรีสตาร์ท (สำคัญตอนเปลี่ยน Scene ที่ใช้เพลงเดียวกัน)
        if (id == currentMusicId) return;

        AudioClip clip = null;
        if (!string.IsNullOrEmpty(id))
        {
            if (lookup.TryGetValue(id, out SoundEntry entry) && entry.clips != null && entry.clips.Length > 0)
            {
                clip = entry.clips[0];
            }
            else if (warnOnMissingId)
            {
                Debug.LogWarning($"[Audio] ไม่พบเพลง id = {id}");
            }
        }

        currentMusicId = id;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossfadeRoutine(clip, fadeDuration));
    }

    private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
    {
        AudioSource from = usingA ? musicA : musicB;
        AudioSource to = usingA ? musicB : musicA;
        usingA = !usingA;

        float targetVolume = masterMusicVolume * duckMultiplier;

        if (newClip != null)
        {
            to.clip = newClip;
            to.volume = 0f;
            to.Play();
        }

        float startVolume = from.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

            targetVolume = masterMusicVolume * duckMultiplier;   // duck ระหว่าง fade ได้ด้วย
            from.volume = Mathf.Lerp(startVolume, 0f, p);
            if (newClip != null) to.volume = Mathf.Lerp(0f, targetVolume, p);

            yield return null;
        }

        from.Stop();
        from.volume = 0f;
        if (newClip != null) to.volume = masterMusicVolume * duckMultiplier;

        fadeRoutine = null;
    }

    // ================= Ducking =================

    /// <summary>
    /// หรี่เพลงชั่วคราวเพื่อให้เสียงสำคัญเด่นขึ้น (เตือนบอส / Force Format / ตาย)
    /// ไม่ต้องใช้ AudioMixer — ปรับระดับเสียงเพลงตรง ๆ
    /// </summary>
    /// <param name="amount">เหลือกี่ส่วนของเสียงเดิม (0.25 = หรี่เหลือ 25%)</param>
    /// <param name="holdDuration">หรี่ค้างไว้กี่วินาที</param>
    public static void DuckMusic(float amount = 0.25f, float holdDuration = 2f, float fadeBack = 0.8f)
    {
        if (Instance == null) return;
        Instance.DuckMusicInternal(amount, holdDuration, fadeBack);
    }

    public void DuckMusicInternal(float amount, float holdDuration, float fadeBack)
    {
        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(DuckRoutine(Mathf.Clamp01(amount), holdDuration, fadeBack));
    }

    private IEnumerator DuckRoutine(float amount, float hold, float fadeBack)
    {
        duckMultiplier = amount;
        yield return new WaitForSecondsRealtime(hold);

        float t = 0f;
        while (t < fadeBack)
        {
            t += Time.unscaledDeltaTime;
            duckMultiplier = Mathf.Lerp(amount, 1f, fadeBack <= 0f ? 1f : t / fadeBack);
            yield return null;
        }

        duckMultiplier = 1f;
        duckRoutine = null;
    }

    private void Update()
    {
        // คุมระดับเสียงเพลงตอนไม่ได้ crossfade อยู่ (ให้ duck มีผลทันที)
        if (fadeRoutine != null) return;

        AudioSource active = usingA ? musicB : musicA;
        if (active != null && active.isPlaying)
        {
            active.volume = masterMusicVolume * duckMultiplier;
        }
    }

    // ================= Volume =================

    public void SetSfxVolume(float value) => masterSfxVolume = Mathf.Clamp01(value);

    public void SetMusicVolume(float value)
    {
        masterMusicVolume = Mathf.Clamp01(value);
        var active = usingA ? musicB : musicA;   // ตัวที่กำลังเล่นอยู่
        if (active != null && active.isPlaying) active.volume = masterMusicVolume;
    }
}
