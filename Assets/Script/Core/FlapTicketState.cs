/// <summary>
/// รวมตรรกะเช็ค/บริโภคสถานะตั๋ว Flap ที่ซื้อไว้ — เก็บสถานะผ่าน RunInventory marker item
/// (ถาวรข้าม Scene, isPermanent = true จึงรอดจาก Force Format ด้วย)
///
/// สอง marker แยกหน้าที่กัน:
///   UsedMarkerId    — ซื้อไปแล้ว ห้ามซื้อซ้ำ (ติดถาวรตลอดรอบ ไม่มีวันถูกลบ)
///   PendingMarkerId — ซื้อแล้วแต่ยังไม่ได้เข้าเล่น (ถูกลบตอน GameManager พาเข้ามินิเกมจริง)
/// </summary>
public static class FlapTicketState
{
    public const string UsedMarkerId = "flap_ticket_used";
    public const string PendingMarkerId = "flap_ticket_pending";

    /// <summary>ชื่อ Scene มินิเกม — เซ็ตตอนซื้อตั๋ว ให้ GameManager รู้ว่าต้องโหลด Scene ไหนตอนเคลียร์ด่าน</summary>
    public static string FlapSceneName;

    public static bool IsPurchased => RunInventory.Instance != null && RunInventory.Instance.Has(UsedMarkerId);
    public static bool IsPending => RunInventory.Instance != null && RunInventory.Instance.Has(PendingMarkerId);

    public static void MarkPurchased(string flapSceneName)
    {
        FlapSceneName = flapSceneName;
        if (RunInventory.Instance == null) return;

        RunInventory.Instance.AddItem(new RunItem { id = UsedMarkerId, type = RunItemType.PassiveBuff, isPermanent = true });
        RunInventory.Instance.AddItem(new RunItem { id = PendingMarkerId, type = RunItemType.PassiveBuff, isPermanent = true });
    }

    /// <summary>เรียกตอน GameManager กำลังจะพาเข้ามินิเกมจริง — เอา pending ออก กันพาเข้าซ้ำ</summary>
    public static void ConsumePending()
    {
        RunInventory.Instance?.RemoveItem(PendingMarkerId);
    }
}
