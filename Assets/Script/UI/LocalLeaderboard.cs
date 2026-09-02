using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ตารางสถิติในเครื่อง เก็บผลการเล่นที่ดีที่สุด (PlayerPrefs)
///
/// เรียงจาก "เวลาที่ใช้จบเกม" น้อยไปมาก ถ้าเวลาเท่ากันดูจำนวนการตาย
/// เหมาะกับการตั้งบูธ — คนแวะเล่นแข่งกันได้โดยไม่ต้องมีเซิร์ฟเวอร์
///
/// เป็น static class ไม่ต้องมี GameObject ในฉาก
/// </summary>
public static class LocalLeaderboard
{
    [Serializable]
    public class Entry
    {
        public float timeSeconds;
        public int deaths;
        public string date;

        public string FormattedTime
        {
            get
            {
                int m = Mathf.FloorToInt(timeSeconds / 60f);
                int s = Mathf.FloorToInt(timeSeconds % 60f);
                return $"{m:00}:{s:00}";
            }
        }
    }

    [Serializable]
    private class EntryList
    {
        public List<Entry> entries = new List<Entry>();
    }

    private const string SaveKey = "Economice_SYSTEMexe_Leaderboard";
    private const int MaxEntries = 5;

    public static List<Entry> GetEntries()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return new List<Entry>();

        var list = JsonUtility.FromJson<EntryList>(json);
        return list != null && list.entries != null ? list.entries : new List<Entry>();
    }

    /// <summary>บันทึกผลการเล่นที่จบเกมได้ — คืนอันดับที่ได้ (1 = ที่หนึ่ง, 0 = ไม่ติดตาราง)</summary>
    public static int Submit(float timeSeconds, int deaths)
    {
        var entries = GetEntries();

        entries.Add(new Entry
        {
            timeSeconds = timeSeconds,
            deaths = deaths,
            date = DateTime.Now.ToString("dd/MM")
        });

        // เร็วกว่าอยู่บน ถ้าเวลาเท่ากันดูคนที่ตายน้อยกว่า
        entries.Sort((a, b) =>
        {
            int cmp = a.timeSeconds.CompareTo(b.timeSeconds);
            return cmp != 0 ? cmp : a.deaths.CompareTo(b.deaths);
        });

        if (entries.Count > MaxEntries) entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

        Save(entries);

        int rank = entries.FindIndex(e => Mathf.Approximately(e.timeSeconds, timeSeconds) && e.deaths == deaths);
        return rank >= 0 ? rank + 1 : 0;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("[Leaderboard] ล้างตารางสถิติแล้ว");
    }

    private static void Save(List<Entry> entries)
    {
        var list = new EntryList { entries = entries };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }
}
