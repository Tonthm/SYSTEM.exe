using UnityEngine;

/// <summary>
/// จัดการ XP สองประเภท:
/// - Permanent XP: สะสมข้ามรอบ ใช้ปลดล็อก Skill Tree ถาวร (เซฟผ่าน PlayerPrefs)
/// - Run Temp XP: XP/ไอเทมชั่วคราวในรอบปัจจุบัน ที่จะถูกดรอปเป็น Data Fragment ตอนตาย
/// วิธีติดตั้ง: อยู่ Scene หลัก (persistent)
/// </summary>
public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    private const string SaveKey_PermanentXP = "Economice_SYSTEMexe_PermanentXP";

    public int PermanentXP { get; private set; }
    public int RunTempXP { get; private set; }

    public System.Action<int> OnPermanentXPChanged;
    public System.Action<int> OnRunTempXPChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PermanentXP = PlayerPrefs.GetInt(SaveKey_PermanentXP, 0);
    }

    /// <summary>เรียกตอนกำจัดศัตรู — XP ที่ได้จะนับเป็นทั้ง permanent (สำหรับ skill tree) และ run temp</summary>
    public void AddXP(int amount)
    {
        PermanentXP += amount;
        PlayerPrefs.SetInt(SaveKey_PermanentXP, PermanentXP);
        OnPermanentXPChanged?.Invoke(PermanentXP);

        AddRunTempXP(amount);
    }

    /// <summary>
    /// หัก Permanent XP ตอนปลดล็อกสกิล
    /// (เดิม SkillTreeManager.TryUnlock ไม่เคยหัก XP เลย — ปลดล็อกได้ฟรีทุกตัว)
    /// </summary>
    public bool SpendPermanentXP(int amount)
    {
        if (amount <= 0 || PermanentXP < amount) return false;

        PermanentXP -= amount;
        PlayerPrefs.SetInt(SaveKey_PermanentXP, PermanentXP);
        PlayerPrefs.Save();
        OnPermanentXPChanged?.Invoke(PermanentXP);
        return true;
    }

    public void AddRunTempXP(int amount)
    {
        RunTempXP += amount;
        OnRunTempXPChanged?.Invoke(RunTempXP);
    }

    /// <summary>เรียกตอนผู้เล่นตาย เพื่อดึง temp XP ปัจจุบันออกไปใส่ Data Fragment แล้วรีเซ็ตค่าที่ถืออยู่</summary>
    public int ConsumeRunTempXP()
    {
        int value = RunTempXP;
        RunTempXP = 0;
        OnRunTempXPChanged?.Invoke(RunTempXP);
        return value;
    }

    /// <summary>บันทึกค่า XP ปัจจุบัน (WaveManager เรียกตอนเริ่ม wave)</summary>
    public XPSnapshot TakeSnapshot() => new XPSnapshot { permanent = PermanentXP, runTemp = RunTempXP };

    /// <summary>ย้อน XP กลับไปยังค่าที่บันทึกไว้ (ตายแล้วเริ่ม wave ใหม่)</summary>
    public void RestoreSnapshot(XPSnapshot snapshot)
    {
        PermanentXP = Mathf.Max(0, snapshot.permanent);
        RunTempXP = Mathf.Max(0, snapshot.runTemp);

        PlayerPrefs.SetInt(SaveKey_PermanentXP, PermanentXP);
        PlayerPrefs.Save();

        OnPermanentXPChanged?.Invoke(PermanentXP);
        OnRunTempXPChanged?.Invoke(RunTempXP);
    }

    public struct XPSnapshot
    {
        public int permanent;
        public int runTemp;
    }

    /// <summary>เรียกตอนเริ่มรันใหม่ทั้งหมด (ไม่ใช่แค่ตาย) เพื่อล้าง temp XP ที่เหลือค้าง</summary>
    public void ResetRunTempXP()
    {
        RunTempXP = 0;
        OnRunTempXPChanged?.Invoke(RunTempXP);
    }
}