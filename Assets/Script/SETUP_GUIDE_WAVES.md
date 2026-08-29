# SETUP GUIDE ภาค 3 — ระบบ Wave + โครงสร้าง 7 ด่าน
### SYSTEM.exe : Ghost in the Kernel

ต่อจาก `SETUP_GUIDE_ADDENDUM.md` — เอกสารนี้คือข้อ 17–20

---

# 17) ระบบ Wave

## 17.1 กติกา

```
1 ด่าน = 10 wave
wave 1–9  = wave ละ 60 วินาที  (ศัตรูทยอย spawn)
wave 10   = wave บอส (ไม่จับเวลา จบเมื่อบอสตาย)

เคลียร์ศัตรูของ wave ไม่หมด → ตัวที่ค้างถูกตีตรา "stale"
                            → ให้ XP เหลือ 50%
                            → ยังอยู่ในสนามต่อ ทับซ้อนกับ wave ใหม่
```

**ผลของกติกา:** ยิ่งเคลียร์ช้า ศัตรูยิ่งสะสม จอยิ่งแน่น แต่ได้ XP น้อยลง
เป็นการลงโทษที่ตรงกับธีมเกม (process ค้างในระบบ = ระบบหน่วง)

## 17.2 ไฟล์ใหม่

| ไฟล์ | วางที่ | หน้าที่ |
|---|---|---|
| `WaveDefinition.cs` | `Scripts/Core/` | โครงข้อมูล wave + กลุ่มศัตรู |
| `WaveManager.cs` | `Scripts/Core/` | คุมลำดับ wave, spawn, ตีตรา stale |
| `WaveSpawnPoint.cs` | `Scripts/Core/` | จุด spawn ศัตรู |
| `WaveHUD.cs` | `Scripts/UI/` | HUD เลข wave / เวลา / ศัตรูที่เหลือ |

**เขียนทับ:** `EnemyHealth.cs` (เพิ่ม `ApplyScaling` + `ApplyStalePenalty`)

## 17.3 ติดตั้งในด่าน

### ขั้นที่ 1 — จุด spawn
1. Empty GameObject ชื่อ `SpawnPoint_01` วางตรงขอบสนาม
2. `Add Component > Wave Spawn Point`
3. `Scatter Radius` = `0.5` (สุ่มรอบจุด กันศัตรูซ้อนกันเป๊ะ)
4. (ไม่บังคับ) `Group Id` เช่น `top`, `sides`, `center` ถ้าอยากให้ศัตรูบางชนิดออกเฉพาะบางจุด
5. ก็อปวางให้ครบ **อย่างน้อย 6–8 จุด** รอบสนาม

เลือกจุดแล้วจะเห็น Gizmo วงกลมสีส้มในฉาก

### ขั้นที่ 2 — WaveManager
1. Empty GameObject ชื่อ `WaveManager` → `Add Component > Wave Manager`
2. กรอกช่องตามตาราง 17.4
3. กาง `Waves` แล้วกด `+` ให้ครบ 10 อัน

### ขั้นที่ 3 — ประตูออก
1. วาง `SectorExitTrigger` ตามปกติ → ติ๊ก **Start Locked ✓**
2. ลากเข้าช่อง `Exit Trigger` ของ WaveManager
   (จบ wave 10 แล้วประตูจะปลดล็อกให้เอง)

### ขั้นที่ 4 — HUD
1. ใน Canvas สร้าง:
   - `Text` มุมบนซ้าย → `WaveText` (แสดง `WAVE 3 / 10`)
   - `Text` ข้าง ๆ → `TimerText` (แสดง `00:47`)
   - `Slider` ใต้ลงมา → `TimerSlider` (แถบเวลาไหลลง)
   - `Text` มุมบนขวา → `EnemiesText` (แสดง `Processes active: 12`)
   - `Text` ใหญ่กลางจอ ใน Panel ชื่อ `AnnouncePanel` (ปิดไว้) → ป้ายประกาศ
2. Empty GameObject ใน Canvas → `Add Component > Wave HUD` → ลาก reference ให้ครบ

## 17.4 ช่องใน WaveManager

| ช่อง | ค่าแนะนำ | ความหมาย |
|---|---|---|
| `Waves` | 10 อัน | รายการ wave (ดูข้อ 17.5) |
| `Start Delay` | `2` | หน่วงก่อน wave แรก ให้ผู้เล่นตั้งตัว |
| `Inter Wave Delay` | `3` | พักระหว่าง wave |
| `Advance Early When Cleared` | ✓ | เคลียร์หมดก่อนหมดเวลา = ขึ้น wave ใหม่เลย |
| `Stale Penalty Multiplier` | `0.5` | XP ที่เหลือของศัตรูค้าง (0.5 = ครึ่งเดียว) |
| `Stack Stale Penalty` | ✗ | ค้างหลาย wave โดนลดซ้ำหรือไม่ |
| `Enemy Health Multiplier` | ต่างกันต่อด่าน | คูณ HP ศัตรูทุกตัวในด่านนี้ |
| `Enemy Xp Multiplier` | ต่างกันต่อด่าน | คูณ XP ศัตรูทุกตัวในด่านนี้ |
| `Exit Trigger` | (SectorExitTrigger) | ประตูที่จะปลดล็อกตอนจบ |
| `Auto Clear Sector On Finish` | ✗ | จบ wave 10 แล้วผ่านด่านเลย ไม่ต้องเดินไปประตู |
| `Is Final Sector` | ✗ (✓ เฉพาะด่าน 6) | ด่านสุดท้าย ให้ NullExeBoss แจ้งจบเกมเอง |

> **`Advance Early When Cleared` เปิดหรือปิดดี?**
> เปิด = คนเก่งจบไว ไม่ต้องยืนรอเฉย ๆ (**แนะนำ**)
> ปิด = ทุกด่านยาว 10 นาทีเป๊ะ แต่ผู้เล่นจะเบื่อตอนเคลียร์หมดแล้วเหลือเวลา 40 วิ

## 17.5 กรอก Waves

แต่ละ wave มีช่อง:

| ช่อง | ความหมาย |
|---|---|
| `Wave Name` | ชื่อไว้ดูใน Inspector/Console เช่น `Swarm Burst` |
| `Duration` | `60` (wave บอสไม่ใช้ค่านี้) |
| `Is Boss Wave` | ติ๊กเฉพาะ wave 10 |
| `Groups` | กลุ่มศัตรูที่ spawn (ใส่ได้หลายกลุ่ม) |
| `Format Warning On Start` | ยิงสัญญาณเตือนตอนเริ่ม wave |

แต่ละ `Group` มีช่อง:

| ช่อง | ความหมาย |
|---|---|
| `Enemy Prefab` | prefab ศัตรู |
| `Count` | spawn กี่ตัว |
| `Start Delay` | รอกี่วินาทีหลัง wave เริ่ม |
| `Spawn Interval` | ทยอยออกทุกกี่วินาที (0 = ออกพร้อมกันหมด) |
| `Spawn Point Group Id` | จำกัดจุด spawn — เว้นว่าง = ใช้ได้ทุกจุด |

### ตัวอย่างการไล่ระดับ 10 wave ของด่าน 1

| # | Wave Name | Duration | Groups | Boss |
|---|---|---|---|---|
| 1 | `Boot Sequence` | 60 | Swarmer ×4, interval 1.5 | |
| 2 | `Background Task` | 60 | Swarmer ×6, interval 1.2 | |
| 3 | `Cursor Trace` | 60 | Swarmer ×4 + Chaser ×2 | |
| 4 | `Firewall Probe` | 60 | Turret ×2 (จุด `sides`) + Swarmer ×5 | |
| 5 | `Packet Flood` | 60 | Swarmer ×10, interval 0.6 | ⚠ Format Warning |
| 6 | `Cross Fire` | 60 | Turret ×3 + Chaser ×3 | |
| 7 | `Memory Leak` | 60 | Swarmer ×8 + Chaser ×4 | |
| 8 | `Stack Overflow` | 60 | Swarmer ×12, interval 0.4 | ⚠ Format Warning |
| 9 | `Kernel Panic` | 60 | Turret ×4 + Chaser ×5 + Swarmer ×6 | |
| 10 | `SECTOR GUARDIAN` | — | บอสประจำด่าน ×1 | ✓ |

**หลักการไล่ระดับ:** เพิ่มจำนวน → เพิ่มชนิด → ลด interval → ผสมหลายชนิดพร้อมกัน

## 17.6 บอสประจำด่าน (wave 10)

ด่าน 1–5 ควรมีบอสประจำด่านคนละตัว **ไม่ต้องใช้ `NullExeBoss`**
ใช้ศัตรูธรรมดาที่ HP สูงกว่ามากก็ได้ หรือจะทำให้เปลี่ยนเฟสก็ใช้ `NullExeBoss` ซ้ำได้
(แต่ต้อง**ไม่**ติ๊ก `Is Final Sector` ที่ WaveManager ไม่งั้นจะไปแจ้งจบเกมผิดจังหวะ)

> ถ้าใช้ `NullExeBoss` กับบอสย่อย ให้ทราบว่ามันจะเรียก `GameManager.OnFinalBossDefeated()`
> เสมอ — **ด่าน 1–5 ห้ามใช้** ให้ใช้แค่ `EnemyHealth` + `EnemyBulletEmitter` ธรรมดา
> หรือทำสคริปต์บอสย่อยแยกที่ก็อป `NullExeBoss` มาแล้วตัดบรรทัด `ReportVictory()` ทิ้ง

## 17.7 ศัตรูที่วางไว้ในฉากล่วงหน้า

ถ้าอยากให้ศัตรูที่วางมือไว้ในฉาก (ไม่ได้ spawn จาก wave) นับรวมในระบบด้วย
ให้เรียกจากสคริปต์เล็ก ๆ:

```csharp
private void Start()
{
    WaveManager.Instance?.RegisterEnemy(GetComponent<EnemyHealth>());
}
```

---

# 18) โครงสร้าง 7 ด่าน

## 18.1 รายชื่อ Scene

| # | ชื่อ Scene | บทบาท | Health Mult | XP Mult |
|---|---|---|---|---|
| 1 | `Sector_Tutorial` | ด่านสอนเล่น (ไม่มี wave) | — | — |
| 2 | `Sector_01_Firewall` | ด่าน 1 | `1.0` | `1.0` |
| 3 | `Sector_02_RAM` | ด่าน 2 | `1.3` | `1.2` |
| 4 | `Sector_03_Registry` | ด่าน 3 | `1.6` | `1.4` |
| 5 | `Sector_04_Cache` | ด่าน 4 | `2.0` | `1.6` |
| 6 | `Sector_05_Driver` | ด่าน 5 | `2.5` | `1.8` |
| 7 | `Sector_06_Core_NULL` | ด่าน 6 — บอสใหญ่ NULL.exe | `3.0` | `2.0` |

ใช้ prefab ศัตรูชุดเดียวกันได้ทั้ง 6 ด่าน แค่ปรับ `Enemy Health Multiplier` ต่างกัน
ไม่ต้องทำ prefab แยก 6 ชุด

## 18.2 ตั้งค่า SectorPoolManager

มี 2 ทางเลือก — เลือกอันเดียว

### แบบ A: เรียงตายตัว 1→6 (ง่ายกว่า)

| ช่อง | ค่า |
|---|---|
| `Tutorial Scene Name` | `Sector_Tutorial` |
| `All Sector Scenes` | *(เว้นว่าง)* |
| `Story Sector Scenes` | `Sector_01_Firewall`<br>`Sector_02_RAM`<br>`Sector_03_Registry`<br>`Sector_04_Cache`<br>`Sector_05_Driver`<br>`Sector_06_Core_NULL` |
| `Story Unlock Threshold` | `0` |
| `Victory Scene Name` | `VictoryScene` |

ผู้เล่นจะเจอด่านเรียงเหมือนกันทุกรอบ เหมาะกับการไล่ระดับความยากที่คุมได้เป๊ะ

### แบบ B: สุ่ม 5 ด่าน + บอสปิดท้าย (ตรงกับเอกสารประกวด)

| ช่อง | ค่า |
|---|---|
| `Tutorial Scene Name` | `Sector_Tutorial` |
| `All Sector Scenes` | `Sector_01_Firewall`<br>`Sector_02_RAM`<br>`Sector_03_Registry`<br>`Sector_04_Cache`<br>`Sector_05_Driver` |
| `Story Sector Scenes` | `Sector_06_Core_NULL` |
| `Story Unlock Threshold` | `1.0` |
| `Victory Scene Name` | `VictoryScene` |

ด่าน 1–5 สุ่มลำดับ ผ่านแล้วออกจากคลัง ผ่านครบ 5 ถึงจะเจอบอสใหญ่

**แนะนำแบบ B** เพราะเอกสารที่ส่งประกวดเขียนไว้ว่า "ด่านจะถูกสุ่มเลือกจากคลังด่าน"
กรรมการอาจเทียบเอกสารกับตัวเกม แต่ถ้าใช้แบบ B ต้องออกแบบให้ทั้ง 5 ด่านเล่นได้
โดยไม่ขึ้นกับลำดับ (ตัวคูณความยากจะไม่ไล่จากง่ายไปยากตามที่เจอ)

> **ทางสายกลาง:** ใช้แบบ B แต่ตั้ง `Enemy Health Multiplier` ของทั้ง 5 ด่านให้ใกล้เคียงกัน
> (เช่น 1.0–1.4) แล้วให้ความยากมาจากชนิดศัตรูกับ pattern แทนตัวเลข HP

## 18.3 อย่าลืม Build Settings

`File > Build Settings > Scenes In Build` ต้องมีครบ 9 Scene:

```
Bootstrap
Sector_Tutorial
Sector_01_Firewall
Sector_02_RAM
Sector_03_Registry
Sector_04_Cache
Sector_05_Driver
Sector_06_Core_NULL
VictoryScene
```

## 18.4 ด่านที่ 7 (บอสใหญ่)

ต่างจากด่านอื่นตรง:
1. `WaveManager` → ติ๊ก **`Is Final Sector` ✓**
2. wave 10 ใช้ prefab `NULL_exe` ที่มี `NullExeBoss` (ดูข้อ 10.5 ในคู่มือภาค 2)
3. **ไม่ต้อง**ติ๊ก `Auto Clear Sector On Finish` — `NullExeBoss` จะแจ้งจบเกมเอง
4. wave 1–9 ควรเป็นศัตรูที่เคยเจอมาแล้วทุกชนิดผสมกัน (บทสรุปของเกม)

---

# 19) คำนวณเวลาเล่นจริง

**นี่คือจุดที่ควรคิดให้ดีก่อนลงมือทำ 60 wave**

| กรณี | เวลาต่อด่าน | รวม 6 ด่าน |
|---|---|---|
| เล่นครบทุกวินาที (ไม่เคลียร์ไว) | 10:00 + พัก 27s ≈ **10:30** | **~63 นาที** |
| เคลียร์ไวเฉลี่ย wave ละ 35s | ~6:00 | **~36 นาที** |
| เคลียร์ไวมาก wave ละ 20s | ~3:30 | **~21 นาที** |

บวกเวลาตาย + เดินกลับไปเก็บ fragment อีก

**ถ้าเอาไปตั้งบูธ Open House** คนแวะเล่นมีเวลาราว 5–10 นาที
แนะนำเพิ่ม **Demo Mode**: ตัวเลือกในหน้า Title ที่เล่นแค่ด่าน 1–2 หรือลด wave เหลือ 5
ทำง่ายมาก — ก็อป Scene ด่าน 1 แล้วลบ wave ออกครึ่งหนึ่ง

**ทางเลือกอื่นถ้าอยากให้จบไวขึ้นโดยไม่ลดจำนวน wave:**

| วิธี | ผล |
|---|---|
| `Duration` = `40` แทน `60` | 6 ด่าน ≈ 42 นาที |
| `Advance Early When Cleared` ✓ | คนเก่งจบไว คนใหม่ยังได้เวลาเต็ม (**แนะนำ**) |
| `Inter Wave Delay` = `1.5` | ประหยัดได้ ~1.5 นาทีรวม |
| ลด wave เหลือ 6 ต่อด่าน | 6 ด่าน ≈ 38 นาที |

> ตัวเลข 10 wave × 1 นาที ไม่ผิด แต่ให้รู้ตัวว่ากำลังทำเกมยาว ~1 ชั่วโมง
> ซึ่งแปลว่าต้องมีเนื้อหาให้ไม่เบื่อตลอด 60 wave — นั่นคือศัตรูอย่างน้อย 5–6 ชนิด
> และ bullet pattern ที่หลากหลายพอ

---

# 20) ทดสอบระบบ Wave

## 20.1 ทดสอบพื้นฐาน
```
1. ตั้ง Duration = 10 ชั่วคราว (จะได้ไม่ต้องรอนาน)
2. Play → Console: [Wave] เริ่ม Wave 1/10: Boot Sequence
3. ศัตรู spawn จากจุด SpawnPoint ที่วางไว้
4. HUD ขึ้น WAVE 1 / 10 + นับเวลาถอยหลัง
```

## 20.2 ทดสอบ stale penalty
```
1. ปล่อยให้ wave 1 หมดเวลาโดยไม่ฆ่าศัตรู
2. Console: [Wave] เคลียร์ไม่หมด — ศัตรู 4 ตัวค้างข้าม wave, XP เหลือ 50%
3. ศัตรูที่ค้างเปลี่ยนสีจาง (ถ้าตั้ง Tint Targets ไว้)
4. ฆ่าตัวที่ค้าง → XP ที่ได้ต้องเป็นครึ่งเดียวของปกติ
```

## 20.3 ทดสอบเคลียร์ไว
```
1. ฆ่าศัตรูให้หมดก่อนหมดเวลา
2. Console: [Wave] Wave 1 เคลียร์ก่อนหมดเวลา (เหลือ 42.3s)
3. ขึ้น wave 2 ทันทีหลังพัก 3 วินาที
```

## 20.4 ทดสอบจบด่าน
```
1. ตั้ง Waves เหลือ 2 อันชั่วคราว (อันที่ 2 = boss wave)
2. ฆ่าบอส → Console: [Wave] เคลียร์ครบทุก wave แล้ว
3. ประตูปลดล็อก → เดินออก → โหลดด่านถัดไป
```

## 20.5 เช็คลิสต์ก่อนถือว่าด่านเสร็จ

- [ ] มี `WaveSpawnPoint` อย่างน้อย 6 จุด กระจายรอบสนาม
- [ ] `Waves` ครบ 10 อัน อันที่ 10 ติ๊ก `Is Boss Wave`
- [ ] ทุก `Group` มี `Enemy Prefab` ครบ (ว่างจะข้ามเงียบ ๆ)
- [ ] `Exit Trigger` ลากเข้าแล้ว และประตูติ๊ก `Start Locked`
- [ ] มี `CheckpointTrigger` อย่างน้อย 1 จุด
- [ ] `Enemy Health Multiplier` ตั้งตามตารางข้อ 18.1
- [ ] ด่าน 6 เท่านั้นที่ติ๊ก `Is Final Sector`
- [ ] Scene อยู่ใน Build Settings แล้ว

---

# 21) ภาคผนวก: API ของระบบ Wave

```csharp
// สถานะปัจจุบัน
WaveManager.Instance.CurrentWaveNumber;    // 1-10
WaveManager.Instance.TotalWaves;
WaveManager.Instance.WaveTimeRemaining;    // วินาที
WaveManager.Instance.IsTimerRunning;
WaveManager.Instance.AliveEnemyCount;
WaveManager.Instance.IsBossWave;
WaveManager.Instance.CurrentWave;          // WaveDefinition

// event
WaveManager.Instance.OnWaveStarted += (waveNumber, wave) => ...;
WaveManager.Instance.OnWaveEnded   += (waveNumber, carriedOver) => ...;
WaveManager.Instance.OnAllWavesCleared += () => ...;

// ลงทะเบียนศัตรูที่วางมือไว้ในฉาก
WaveManager.Instance.RegisterEnemy(enemyHealth);

// จุด spawn
WaveSpawnPoint.GetRandom("top");   // เว้นว่าง = สุ่มทุกจุด
WaveSpawnPoint.All;

// EnemyHealth ที่เพิ่มมา
enemyHealth.ApplyScaling(healthMult, xpMult);
enemyHealth.ApplyStalePenalty(0.5f, stack: false);
enemyHealth.IsStale;
enemyHealth.EffectiveXpReward;   // XP จริงที่จะได้ตอนนี้
```
