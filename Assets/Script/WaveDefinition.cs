using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// นิยาม 1 wave — spawn อะไรบ้าง นานเท่าไหร่ เป็น wave บอสหรือไม่
/// กรอกใน Inspector ของ WaveManager (ปกติด่านละ 10 อัน)
/// </summary>
[System.Serializable]
public class WaveDefinition
{
    public string waveName = "Wave";

    [Tooltip("ความยาว wave (วินาที) — wave บอสไม่ใช้ค่านี้ จะจบเมื่อบอสตายเท่านั้น")]
    public float duration = 60f;

    [Tooltip("wave สุดท้ายของด่าน — จบเมื่อบอสตาย ไม่จับเวลา")]
    public bool isBossWave = false;

    [Tooltip("กลุ่มศัตรูที่ spawn ใน wave นี้ (ใส่ได้หลายกลุ่ม เวลาต่างกันได้)")]
    public List<SpawnGroup> groups = new List<SpawnGroup>();

    [Header("Interference (ไม่บังคับ)")]
    [Tooltip("ยิง Format Warning ตอนเริ่ม wave นี้")]
    public bool formatWarningOnStart = false;
    public float formatWarningDuration = 2f;
}

/// <summary>กลุ่มศัตรู 1 ชุดใน wave — prefab เดียว จำนวนหนึ่ง ทยอย spawn ตาม interval</summary>
[System.Serializable]
public class SpawnGroup
{
    public GameObject enemyPrefab;

    [Tooltip("จำนวนตัวที่ spawn ในกลุ่มนี้")]
    public int count = 5;

    [Tooltip("รอกี่วินาทีหลัง wave เริ่ม ถึงจะเริ่ม spawn กลุ่มนี้")]
    public float startDelay = 0f;

    [Tooltip("ระยะห่างระหว่างการ spawn แต่ละตัว (วินาที) — 0 = ออกมาพร้อมกันหมด")]
    public float spawnInterval = 0.6f;

    [Tooltip("จำกัดให้ spawn เฉพาะจุดที่ Group Id ตรงกัน — เว้นว่าง = ใช้ได้ทุกจุด")]
    public string spawnPointGroupId = "";
}
