using UnityEngine;

/// <summary>
/// สั่งเล่นเพลงประจำ Scene — วางใน Scene ไหนก็ได้ที่อยากให้เพลงเปลี่ยน
///
/// ถ้าเพลงเดิมกำลังเล่นอยู่แล้วจะไม่รีสตาร์ท (เดินข้ามด่านที่ใช้เพลงเดียวกันเพลงจะต่อเนื่อง)
///
/// วิธีติดตั้ง: Empty GameObject ในแต่ละ Scene ชื่อ "SceneMusic" attach สคริปต์นี้
/// - MainMenu    → music_menu
/// - ด่านทุกด่าน → music_gameplay
/// - VictoryScene → music_victory
/// </summary>
public class SceneMusic : MonoBehaviour
{
    [Tooltip("id ของเพลง (ดูรายการที่ AudioIds.cs)")]
    [SerializeField] private string musicId = AudioIds.MusicGameplay;
    [SerializeField] private float fadeDuration = 1.5f;
    [Tooltip("หน่วงก่อนเริ่มเพลง (ให้ฉากโหลดเสร็จก่อน)")]
    [SerializeField] private float startDelay = 0f;

    private void Start()
    {
        if (startDelay > 0f) Invoke(nameof(PlayNow), startDelay);
        else PlayNow();
    }

    private void PlayNow()
    {
        AudioManager.PlayMusic(musicId, fadeDuration);
    }
}
