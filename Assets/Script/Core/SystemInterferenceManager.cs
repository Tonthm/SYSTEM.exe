using UnityEngine;

/// <summary>
/// ระบบ "System Interference" — เอฟเฟกต์รบกวนผู้เล่นเฉพาะฉาก/ศัตรูบางตัว
/// - Latency Spike: ผู้เล่นช้าลงชั่วคราว
/// - Glitch Zone: hitbox กระสุน/สิ่งกีดขวางคลาดเคลื่อนจากภาพที่เห็น (ทำผ่านการสุ่ม offset ของ collider)
/// - Format Warning: เตือนล่วงหน้าก่อนโจมตีขนาดใหญ่ (broadcast event ให้ UI/ฉากตอบสนอง)
/// วิธีติดตั้ง: อยู่ Scene หลักหรือใน sector ที่ต้องการใช้เอฟเฟกต์นี้
/// </summary>
public class SystemInterferenceManager : MonoBehaviour
{
    public static SystemInterferenceManager Instance { get; private set; }

    public System.Action<float> OnFormatWarning; // ส่งเวลานับถอยหลัง (วินาที) ให้ UI แสดง

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>ทำให้ผู้เล่นช้าลงชั่วคราว — เรียกจากศัตรู/trap ในฉาก</summary>
    public void TriggerLatencySpike(PlayerController player, float slowMultiplier = 0.5f, float duration = 1.5f)
    {
        if (player == null) return;
        player.ApplySpeedMultiplier(slowMultiplier, duration);
        Debug.Log($"[System Interference] Latency Spike: player slowed to {slowMultiplier * 100}% for {duration}s");
    }

    /// <summary>สร้างพื้นที่ที่ hitbox กับภาพไม่ตรงกัน — ใช้ GlitchZoneVolume.cs แยกต่างหากต่อพื้นที่
    /// ฟังก์ชันนี้เป็นจุด hook กลาง เผื่อระบบอื่นอยากรู้ว่าผู้เล่นอยู่ใน Glitch Zone หรือไม่</summary>
    public bool IsPositionInGlitchZone(Vector2 position, GlitchZoneVolume[] zones)
    {
        foreach (var zone in zones)
        {
            if (zone.ContainsPoint(position)) return true;
        }
        return false;
    }

    /// <summary>เรียกก่อนโจมตีใหญ่ (เช่น boss attack) เพื่อเตือนผู้เล่นล่วงหน้า</summary>
    public void TriggerFormatWarning(float warningDuration)
    {
        Debug.Log($"[System Interference] FORMAT WARNING — incoming in {warningDuration}s");
        OnFormatWarning?.Invoke(warningDuration);
    }
}
