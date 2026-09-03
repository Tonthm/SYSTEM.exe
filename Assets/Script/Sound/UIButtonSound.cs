using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ใส่เสียงคลิกให้ปุ่มทุกปุ่มใน Canvas อัตโนมัติ — ไม่ต้อง attach ทีละปุ่ม
///
/// หาปุ่มทั้งหมดใต้ Canvas (รวมที่ปิดอยู่) แล้วผูก onClick ให้เอง
/// ปุ่มที่กดไม่ได้ (interactable = false) จะไม่มีเสียงอยู่แล้วเพราะ onClick ไม่ยิง
///
/// วิธีติดตั้ง: attach บน Canvas ของแต่ละ Scene หนึ่งตัวพอ
/// ถ้ามีปุ่มที่สร้างตอนรันไทม์ (เช่นรายการสกิลที่ spawn เอง) เรียก Refresh() หลังสร้างเสร็จ
/// </summary>
public class UIButtonSound : MonoBehaviour
{
    [SerializeField] private string clickSoundId = AudioIds.UIClick;
    [Tooltip("หาปุ่มที่ปิดอยู่ด้วย (แผงที่ยังไม่เปิด)")]
    [SerializeField] private bool includeInactive = true;

    private void Start()
    {
        Refresh();
    }

    /// <summary>เรียกใหม่หลังสร้างปุ่มเพิ่มตอนรันไทม์</summary>
    public void Refresh()
    {
        var buttons = GetComponentsInChildren<Button>(includeInactive);

        foreach (var button in buttons)
        {
            if (button == null) continue;

            Button captured = button;
            captured.onClick.RemoveListener(PlayClick);   // กันผูกซ้ำตอนเรียก Refresh หลายรอบ
            captured.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        AudioManager.Play(clickSoundId);
    }
}
