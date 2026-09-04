/// <summary>
/// เก็บชื่อด่านถัดไป "ตัวจริง" ที่ถูกพักไว้ระหว่างแวะเล่นมินิเกมโบนัส Flap
/// GameManager เซ็ตค่านี้ตอนเคลียร์ด่านแล้วมีตั๋วค้างอยู่ ก่อนเบี่ยงไปโหลด Flap Scene แทน
/// FlapGameManager อ่านค่านี้ตอนจบมินิเกม เพื่อโหลดด่านถัดไปจริงต่อ
/// </summary>
public static class FlapBonusContext
{
    public static string PendingNextSector;
}
