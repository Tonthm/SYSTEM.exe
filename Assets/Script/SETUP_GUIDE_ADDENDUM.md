# SETUP GUIDE ภาค 2 — ระบบที่เพิ่มเข้ามา
### SYSTEM.exe : Ghost in the Kernel

เอกสารนี้ต่อจาก `SETUP_GUIDE.md` เดิม (ข้อ 0–8) ครอบคลุม 4 ระบบที่ยังขาด
ทำตามข้อ 9 → 12 เรียงลำดับได้เลย แต่ละข้อจบในตัว ทดสอบทีละข้อได้

**อ่านข้อ 0 ก่อนเสมอ** — เป็นการเตรียมไฟล์ที่ข้ออื่นใช้ร่วมกัน

---

# สารบัญ

| ข้อ | เรื่อง | เวลาโดยประมาณ |
|---|---|---|
| [0](#0-เตรียมไฟล์) | เตรียมไฟล์ + Tag ที่ต้องมี | 5 นาที |
| [0.5](#05-textmeshpro) | ตั้งค่า TextMeshPro + ฟอนต์ไทย | 15 นาที |
| [9](#9-checkpoint-trigger) | Checkpoint Trigger | 10 นาที |
| [10](#10-tutorial-sector--เงื่อนไขชนะ) | Tutorial Sector + เงื่อนไขชนะ | 40 นาที |
| [11](#11-glitch-zone--เชื่อมกับภาพจริง) | Glitch Zone เชื่อมกับภาพจริง | 20 นาที |
| [12](#12-runinventory--ไอเทมอาวุธจริงใน-fragment) | RunInventory | 25 นาที |
| [13](#13-checklist-scene-หลัก) | Checklist Scene หลัก | 5 นาที |
| [14](#14-ลำดับการทดสอบ) | ลำดับการทดสอบ | 20 นาที |
| [15](#15-ปัญหาที่พบบ่อย) | ปัญหาที่พบบ่อย | — |
| [16](#16-ภาคผนวก-api-ที่เรียกใช้ได้) | ภาคผนวก: API ที่เรียกใช้ได้ | — |

---

# 0) เตรียมไฟล์

## 0.1 ไฟล์ใหม่ — ก็อปวางเพิ่ม

วางตามโฟลเดอร์เดิมที่ตั้งไว้ในคู่มือข้อ 0 (`Assets/Scripts/Core`, `Enemy`, `UI` ฯลฯ)

| ไฟล์ | วางที่ | หน้าที่ |
|---|---|---|
| `CheckpointTrigger.cs` | `Scripts/Core/` | เดินผ่านแล้วตั้ง checkpoint อัตโนมัติ |
| `SectorExitTrigger.cs` | `Scripts/Core/` | ประตูออกด่าน = ถือว่าผ่านด่าน |
| `TutorialSectorController.cs` | `Scripts/Core/` | คุมลำดับขั้นตอนด่านสอนเล่น |
| `RunItem.cs` | `Scripts/Core/` | โครงข้อมูลไอเทม/อาวุธ 1 ชิ้น |
| `RunInventory.cs` | `Scripts/Core/` | กระเป๋าไอเทมของรอบ + รวมค่าสเตต |
| `FragmentPayload.cs` | `Scripts/Core/` | ของที่ Data Fragment แบกไว้ |
| `ItemPickup.cs` | `Scripts/Core/` | ไอเทมที่วางให้เก็บในด่าน |
| `GlitchVisualDisplacer.cs` | `Scripts/Core/` | แยกภาพออกจาก hitbox จริง |
| `NullExeBoss.cs` | `Scripts/Enemy/` | บอสสุดท้าย + เงื่อนไขชนะ |
| `VictoryScreenUI.cs` | `Scripts/UI/` | หน้าจอจบเกม |

## 0.2 ไฟล์ที่ต้องเขียนทับของเดิม

ลากทับได้เลย **ชื่อ field เดิมไม่เปลี่ยน** ค่าที่ตั้งไว้ใน Inspector จะไม่หาย
มีแต่ field ใหม่ที่ขึ้นมาเพิ่มแล้วต้องกรอก (ระบุไว้ในแต่ละข้อ)

| ไฟล์ | เปลี่ยนอะไร |
|---|---|
| `GameManager.cs` | + เงื่อนไขชนะ, กัน respawn ซ้อน, checkpoint สำรองอัตโนมัติ |
| `SectorPoolManager.cs` | + tutorial flow, victory scene, กันนับ story ซ้ำ |
| `CorruptionMeter.cs` | Force Format ล้าง RunInventory ด้วย |
| `DataFragment.cs` | แบก `FragmentPayload` แทน `int`, กะพริบเตือนก่อนหมดเวลา |
| `FragmentInheritanceManager.cs` | ดึงไอเทมจริงจาก RunInventory, ใช้ skill ยืดเวลา |
| `GlitchZoneVolume.cs` | + static registry ให้วัตถุค้นหาโซนเองได้ |
| `EnemyHealth.cs` | + event `OnDeath` / `OnAnyEnemyKilled` / `HealthPercent` |
| `EnemyBulletEmitter.cs` | + เปลี่ยน pattern ระหว่างเล่น, สุ่มค่าต่อรอบ |
| `PlayerShooter.cs` | สเตตจริง = สเตตพื้นฐาน × ไอเทมของรอบ |
| `PlayerController.cs` | แก้บั๊กความเร็วค้าง, รองรับบัฟความเร็วจากไอเทม |

> ไฟล์ที่ **ไม่ได้แตะ**: `Bullet.cs`, `BulletPatternType.cs`, `BulletPatternMemory.cs`,
> `DeathLogManager.cs`, `XPManager.cs`, `SkillTreeManager.cs`, `SystemInterferenceManager.cs`,
> `EnemyBase.cs`, `PlayerHealth.cs`, `HUDManager.cs`, `DeathScreenUI.cs`, `SkillSelectUI.cs`

## 0.3 Tag ที่ต้องมี

`Edit > Project Settings > Tags and Layers` — ต้องมีครบ 4 ตัวนี้ (เหมือนคู่มือเดิม)

```
Player      Enemy      Wall      Fragment
```

> ไม่ต้องเพิ่ม Tag ใหม่สำหรับระบบชุดนี้ — `ItemPickup` กับ `CheckpointTrigger`
> ตรวจจับผู้เล่นด้วย Tag `Player` ที่มีอยู่แล้ว

## 0.4 เช็คว่า compile ผ่านก่อน

หลังก็อปไฟล์ครบ กลับไป Unity รอ compile ให้จบ แล้วดู Console
**ต้องไม่มี error สีแดง** ถ้ามี `The type or namespace name 'RunInventory' could not be found`
แปลว่ายังก็อปไฟล์ไม่ครบ ให้กลับไปเช็คตาราง 0.1

## 0.5 TextMeshPro

**UI ทุกตัวในโปรเจกต์นี้ใช้ TextMeshPro ไม่ใช่ Legacy Text** สคริปต์ประกาศตัวแปรเป็น
`TMP_Text` ซึ่งรับได้ทั้ง `TextMeshProUGUI` (UI ใน Canvas) และ `TextMeshPro` (ข้อความในโลก 3D)

### ติดตั้งครั้งแรก
1. `Window > TextMeshPro > Import TMP Essential Resources`
2. รอ import เสร็จ จะได้โฟลเดอร์ `Assets/TextMesh Pro/`
3. (ไม่บังคับ) `Import TMP Examples & Extras` ถ้าอยากดูตัวอย่าง

### สร้างข้อความ
ใช้ `UI > Text - TextMeshPro` ทุกครั้ง **ห้ามใช้ `UI > Legacy > Text`**
ถ้าเผลอสร้าง Legacy Text ไปแล้วจะลากเข้าช่องใน Inspector ไม่ได้ (ชนิดไม่ตรง)

ปุ่มก็เช่นกัน ใช้ `UI > Button - TextMeshPro` — สำคัญกับ Skill Button Prefab
เพราะ `SkillSelectUI` หาป้ายด้วย `GetComponentInChildren<TMP_Text>()`

### ฟอนต์ไทย — ต้องทำ ไม่งั้นตัวอักษรหาย

ฟอนต์เริ่มต้นของ TMP (LiberationSans) **ไม่มีตัวอักษรไทย** ข้อความอย่าง
"กำจัด Background Process ที่ขวางอยู่" จะขึ้นเป็นสี่เหลี่ยมเปล่าหรือหายไปเลย

วิธีสร้าง Font Asset ภาษาไทย:
1. หาไฟล์ฟอนต์ไทยที่ใช้ได้ฟรีเชิงพาณิชย์ เช่น **Sarabun**, **Kanit**, **Prompt**
   (ตระกูล Google Fonts / ฟอนต์ราชการไทย — เช็ค license ก่อนใช้ในงานส่งประกวด)
2. ลากไฟล์ `.ttf` เข้า `Assets/Fonts/`
3. `Window > TextMeshPro > Font Asset Creator`
4. ตั้งค่า:

| ช่อง | ค่า |
|---|---|
| `Source Font File` | ฟอนต์ที่เพิ่งลากเข้ามา |
| `Sampling Point Size` | `Auto Sizing` |
| `Atlas Resolution` | `1024 x 1024` (ไทยตัวอักษรเยอะ ใช้ 512 ไม่พอ) |
| `Character Set` | `Unicode Range (Hex)` |
| `Character Sequence` | `20-7E,E01-E5B,200B` |
| `Render Mode` | `SDFAA` |

> `20-7E` = อังกฤษ+ตัวเลข+สัญลักษณ์, `E01-E5B` = อักษรไทยทั้งชุด, `200B` = zero-width space
> (ไทยไม่มีเว้นวรรคระหว่างคำ ตัวนี้ช่วยเรื่องการตัดบรรทัด)

5. กด `Generate Font Atlas` → `Save as...` เก็บใน `Assets/Fonts/`
6. ตั้งเป็นค่าเริ่มต้น: `Edit > Project Settings > TextMeshPro > Settings`
   → ลาก Font Asset ที่สร้างเข้าช่อง `Default Font Asset`

### เช็คว่าใช้ได้จริง
สร้าง Text - TextMeshPro แล้วพิมพ์ `ทดสอบภาษาไทย 123 ABC`
ถ้าเห็นครบทุกตัวถือว่าผ่าน ถ้าสระลอยผิดตำแหน่งให้เพิ่ม `Character Sequence`
เป็น `20-7E,E01-E5B,200B,25CC` (25CC = dotted circle ที่ใช้แสดงสระลอย)

### ไฟล์ที่ใช้ TMP

| ไฟล์ | ช่องที่ต้องลาก TMP เข้า |
|---|---|
| `HUDManager` | `Hp Text`, `Temp XP Text` |
| `DeathScreenUI` | `Message Text`, `Cause Text`, `Resistance Gained Text` |
| `VictoryScreenUI` | `Title Text`, `Stats Text` |
| `WaveHUD` | `Wave Text`, `Timer Text`, `Enemies Text`, `Announce Text` |
| `TutorialSectorController` | `Prompt Text` |
| `SkillSelectUI` | ป้ายใน Button Prefab (หาเองอัตโนมัติ) |

> `Slider` ยังเป็นของ `UnityEngine.UI` ตามเดิม TMP ไม่มี Slider ของตัวเอง
> ใน `WaveHUD` จึงมีทั้ง `using TMPro;` และ `using UnityEngine.UI;`

---

# 9) Checkpoint Trigger

**ปัญหาเดิม:** `GameManager.SetCheckpoint()` มีอยู่ แต่ไม่มีอะไรเรียกมัน ต้องเขียน trigger เองทุกด่าน

## 9.1 สร้าง Checkpoint Prefab (ทำครั้งเดียว)

1. `GameObject > Create Empty` ตั้งชื่อ `Checkpoint`
2. `Add Component > Box Collider 2D`
   - ติ๊ก **Is Trigger** ✓ (ถ้าลืม สคริปต์จะติ๊กให้เองพร้อม warning ใน console)
   - ปรับ `Size` ให้ครอบทางเดินที่ผู้เล่นต้องผ่านแน่ ๆ เช่น `X = 3, Y = 6` สำหรับทางเดินแนวตั้ง
3. `Add Component > Checkpoint Trigger`
4. (ไม่บังคับ) ใส่ Sprite เป็นตัวบอกตำแหน่ง:
   - สร้าง child ชื่อ `Indicator` ใส่ `SpriteRenderer` (ไอคอนดิสก์/เซฟ)
   - ลาก `Indicator` เข้าช่อง `Indicator` ของสคริปต์ → เวลาผ่านจะเปลี่ยนสีให้
5. (ไม่บังคับ) ถ้าอยากให้จุดเกิดไม่ตรงกับตัว trigger:
   - สร้าง child ชื่อ `SpawnPoint` ย้ายไปตำแหน่งที่ต้องการ
   - ลากเข้าช่อง `Spawn Point`
6. ลากลง `Assets/Prefabs/` เป็น Prefab แล้วลบตัวใน Scene ทิ้ง

## 9.2 ช่องใน Inspector

| ช่อง | ค่าแนะนำ | ความหมาย |
|---|---|---|
| `Spawn Point` | ว่าง | จุดที่ Process ใหม่จะเกิด — ว่าง = ใช้ตำแหน่งของ GameObject นี้ |
| `One Shot` | ✓ | ติดครั้งเดียวแล้วเลิกทำงาน (ปกติควรเปิด) |
| `Heal Player On Activate` | ✗ | ฟื้น HP เต็มตอนแตะ — เปิดเฉพาะด่านที่อยากให้ใจดี |
| `Indicator` | (SpriteRenderer) | ตัวบอกสถานะ |
| `Inactive / Active Color` | เทา / เขียว | สีก่อน-หลังเปิดใช้งาน |

## 9.3 วางในด่าน

ลาก Prefab วางตามจุดสำคัญ เช่น
- หน้าประตูห้องบอส
- หลังผ่านโซนกระสุนหนัก ๆ
- ต้นทางเดินยาว ๆ ที่วิ่งกลับมาไกล

ตั้งชื่อให้ต่างกัน (`Checkpoint_01`, `Checkpoint_02`) จะดูใน Console ง่ายขึ้น

## 9.4 จุดเกิดแรกของด่าน

**ไม่ต้องทำอะไรเพิ่ม** — `GameManager` เวอร์ชันใหม่ ถ้าเจอว่า `Current Checkpoint` ว่าง
จะสร้าง `Auto_Checkpoint_Start` จากตำแหน่ง spawn ของผู้เล่นให้เองตอน `Start()`
(เดิมจะขึ้น `Missing playerObject or checkpoint reference for respawn` แล้วผู้เล่นค้างตรงจุดตาย)

ถ้าอยากกำหนดเอง: ลาก GameObject จุดเริ่มเข้าช่อง `Current Checkpoint` ของ `GameManager` เหมือนเดิม

## 9.5 เรียกจากโค้ดอื่น

ถ้ามีเหตุการณ์ในด่านที่ควรตั้ง checkpoint (เช่น จบ cutscene) เรียกตรง ๆ ได้:

```csharp
GameManager.Instance.SetCheckpoint(myTransform);
```

หรือฟัง event ตอน checkpoint ถูกเปิดใช้งาน:

```csharp
checkpointTrigger.OnActivated += (cp) => Debug.Log("Process state saved");
```

## 9.6 ทดสอบ

1. กด Play เดินผ่าน checkpoint → Console ขึ้น `[GameManager] Checkpoint updated: Checkpoint_01`
2. เดินต่อไปให้ตาย → ต้องเกิดที่ checkpoint ไม่ใช่จุดเริ่มด่าน

---

# 10) Tutorial Sector + เงื่อนไขชนะ

**ปัญหาเดิม:** เอกสารระบุว่าต้องมี tutorial ก่อนเข้า Sector Pool และต้องมีเงื่อนไขจบเกม
แต่โค้ดยังไม่มีทั้งคู่ และ `GetNextSector()` คืน `null` เมื่อคลังด่านหมด

## 10.1 ผัง flow ทั้งหมด

```
              เริ่มเกมใหม่
                   │
                   ▼
        ┌──────────────────────┐
        │  Sector_Tutorial     │  เล่นครั้งแรกครั้งเดียว
        │  (TutorialSector-    │  (บันทึกลง PlayerPrefs)
        │   Controller)        │
        └──────────┬───────────┘
                   │ ครบทุกขั้น → MarkTutorialComplete() → ปลดล็อกประตู
                   ▼
        ┌──────────────────────┐
        │   Sector Pool        │◄──┐  สุ่มจากด่านที่ยังไม่ผ่าน
        │   (สุ่มด่าน)          │   │
        └──────────┬───────────┘   │
                   │ ผ่านด่าน       │ ยังไม่ถึงเกณฑ์
                   │ (SectorExit-  │
                   │  Trigger)     │
                   ├───────────────┘
                   │ ผ่านครบ ≥ Story Unlock Threshold (ค่าเริ่ม 70%)
                   ▼
        ┌──────────────────────┐
        │  Story Sector 0,1,2  │  ด่านเนื้อเรื่องตายตัว เรียงตาม index
        └──────────┬───────────┘
                   │ ด่านสุดท้าย = ห้องบอส
                   ▼
        ┌──────────────────────┐
        │   NULL.exe (Boss)    │
        └──────────┬───────────┘
                   │ HP หมด → NullExeBoss.HandleDeath()
                   │        → GameManager.OnFinalBossDefeated()
                   │        → OnGameWon (VictoryScreenUI ฟังอยู่)
                   ▼
        ┌──────────────────────┐
        │   VictoryScene       │
        └──────────────────────┘
```

**จุดตัดสินใจทั้งหมดอยู่ใน `SectorPoolManager.GetNextSector()` ที่เดียว** ไล่ตามลำดับ:

```
1. ยังไม่ผ่าน tutorial?            → คืนชื่อ Tutorial Scene
2. ผ่าน pool ถึงเกณฑ์ & มี story?  → คืน story sector ตัวถัดไป
3. ยังมีด่านในคลังเหลือ?           → สุ่มคืนมา 1 ด่าน
4. คลังหมดแต่ยังมี story เหลือ?    → คืน story sector ตัวถัดไป
5. หมดทุกอย่าง                     → คืน Victory Scene
```

## 10.2 ตั้งค่า SectorPoolManager (Scene หลัก)

เลือก GameObject `SectorPoolManager` แล้วกรอก:

| ช่อง | ตัวอย่างค่า | หมายเหตุ |
|---|---|---|
| `Tutorial Scene Name` | `Sector_Tutorial` | เว้นว่าง = ข้าม tutorial ไปเลย |
| `All Sector Scenes` | `Sector_Firewall_A`<br>`Sector_RAM_A`<br>`Sector_RAM_B`<br>`Sector_Registry_A` | ด่านที่สุ่มได้ |
| `Story Sector Scenes` | `Story_Firewall_Gate`<br>`Story_Registry_Maze`<br>`Story_Core_NULL` | **เรียงตามลำดับ** ตัวสุดท้าย = ห้องบอส |
| `Story Unlock Threshold` | `0.7` | ต้องผ่านคลัง 70% ก่อนถึงจะแทรกด่านเนื้อเรื่อง |
| `Victory Scene Name` | `VictoryScene` | หน้าจอจบเกม |

**สำคัญ:** ชื่อต้องตรงกับชื่อไฟล์ Scene เป๊ะ (case-sensitive) และต้องเพิ่มทุก Scene ใน
`File > Build Settings > Scenes In Build` ไม่งั้น `SceneManager.LoadScene()` จะ error

## 10.3 สร้างด่าน Tutorial

### ขั้นตอน
1. สร้าง Scene ใหม่ชื่อ `Sector_Tutorial` (ชื่อต้องตรงกับที่กรอกในข้อ 10.2)
2. วางของพื้นฐาน: `Player`, `GameManager`, `Canvas`, พื้น/กำแพง
3. สร้าง Empty GameObject ชื่อ `TutorialController` → `Add Component > Tutorial Sector Controller`
4. ใน Canvas สร้าง `UI > Text - TextMeshPro` ชื่อ `TutorialPrompt`
   - วางไว้กลางบนจอ, `Font Size` ~24, จัดกลาง
   - ลากเข้าช่อง `Prompt Text` ของ `TutorialController`
5. สร้างประตูออก:
   - Empty GameObject ชื่อ `SectorExit` + `Box Collider 2D` (Is Trigger ✓)
   - `Add Component > Sector Exit Trigger`
   - ติ๊ก **Start Locked** ✓
   - (ไม่บังคับ) สร้าง child 2 ตัวเป็นภาพประตูปิด/เปิด ลากเข้า `Locked Visual` / `Unlocked Visual`
6. ลาก `SectorExit` เข้าช่อง `Exit Trigger` ของ `TutorialController`
7. วางศัตรู 1 ตัวไว้ให้ผู้เล่นฝึกยิง (ขั้นตอน `KillEnemies` ต้องมีอย่างน้อย 1 ตัว)

### ช่อง Steps — แก้ได้ตามใจ

ค่าเริ่มต้นมีให้ 4 ขั้น กด `+` เพิ่มหรือ `-` ลบได้

| id | prompt | type | requiredAmount |
|---|---|---|---|
| `move` | WASD / ปุ่มลูกศร — เคลื่อนที่ Ghost Process | `Move` | `2` (วินาที) |
| `aim` | คลิกซ้ายค้าง — ยิงไปทางเคอร์เซอร์ | `Shoot` | `5` (นัด) |
| `dash` | Shift / Space — Dash หลบกระสุน | `Dash` | `2` (ครั้ง) |
| `kill` | กำจัด Background Process ที่ขวางอยู่ | `KillEnemies` | `1` (ตัว) |

**หน่วยของ `requiredAmount` ต่างกันตาม type:**
- `Move` = จำนวน**วินาที**ที่ต้องกดเดินสะสม
- `Shoot` / `Dash` = จำนวน**ครั้ง**
- `KillEnemies` = จำนวน**ตัว**
- `Custom` = ไม่ใช้ ต้องให้สคริปต์อื่นเรียกจบเอง

### ขั้นตอนแบบ Custom

ถ้าอยากมีขั้น "เดินไปยืนบนแท่น" หรือ "เก็บไอเทมชิ้นแรก":

1. เพิ่ม step ใหม่ ตั้ง `type = Custom`, `id = standOnPad`
2. เขียน trigger สั้น ๆ ในฉาก:

```csharp
using UnityEngine;

public class TutorialCustomTrigger : MonoBehaviour
{
    [SerializeField] private TutorialSectorController tutorial;
    [SerializeField] private string stepId = "standOnPad";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) tutorial.CompleteStep(stepId);
    }
}
```

### ตัวเลือกจบ tutorial

| ช่อง | ผล |
|---|---|
| `Auto Load Next On Complete` = ✗ | ปลดล็อกประตู ผู้เล่นเดินออกเอง (**แนะนำ**) |
| `Auto Load Next On Complete` = ✓ | ครบขั้นแล้วโหลดด่านถัดไปให้เลยหลัง `Auto Load Delay` วินาที |

## 10.4 ด่านทั่วไป (Sector Pool)

ทุกด่านในคลังต้องมีทางออก ไม่งั้นผู้เล่นไปต่อไม่ได้:

1. Empty GameObject `SectorExit` + `Box Collider 2D` (Is Trigger ✓)
2. `Add Component > Sector Exit Trigger`
3. **ไม่ต้อง**ติ๊ก Start Locked
4. ถ้าอยากบังคับให้เคลียร์ศัตรูก่อน → ติ๊ก `Require All Enemies Dead` ✓
   (นับจาก GameObject ที่ Tag = `Enemy` ที่ยังเหลือในฉาก)

| ช่อง | ค่าแนะนำ | ความหมาย |
|---|---|---|
| `Start Locked` | ✗ | เริ่มมาล็อกไว้ รอ `Unlock()` |
| `Require All Enemies Dead` | แล้วแต่ด่าน | ต้องไม่เหลือ Tag `Enemy` ในฉาก |
| `Locked Visual` | (GameObject) | ภาพตอนล็อก |
| `Unlocked Visual` | (GameObject) | ภาพตอนปลดล็อก |

ปลดล็อกจากโค้ดอื่นได้:

```csharp
exitTrigger.Unlock();   // เช่น หลังบอสย่อยตาย
```

## 10.5 บอส NULL.exe

### สร้างตัวบอส
1. สร้าง GameObject ชื่อ `NULL_exe` ใส่ Sprite
2. `Rigidbody2D` (Gravity Scale = 0) + `Collider2D`
3. ตั้ง **Tag = Enemy**
4. Attach 4 ตัว:
   - `EnemyHealth` — ตั้ง `Max Health` สูง เช่น `1500`, `Xp Reward` เช่น `500`
   - `EnemyBulletEmitter` — ลาก Bullet Prefab + FirePoint เข้าช่อง
   - `EnemyBase` — ตั้ง `Chase Speed` ต่ำ ๆ หรือ `0` ถ้าอยากให้อยู่กับที่, `Attack Range` ให้ครอบทั้งห้อง
   - `NullExeBoss`
5. `NullExeBoss` จะหา `EnemyHealth` / `EnemyBulletEmitter` บนตัวเองอัตโนมัติ (เว้นช่องว่างได้)

### ตั้งค่า Phases

เรียงจาก **HP มาก → น้อย** ค่าเริ่มต้นให้มา 3 เฟส:

| # | phaseName | healthThreshold | pattern | fireCooldown | bulletCount | formatWarning |
|---|---|---|---|---|---|---|
| 0 | `Scan` | `1.00` | `Aimed` | `0.9` | `1` | ✗ |
| 1 | `Overwrite` | `0.66` | `RadialBurst` | `1.1` | `14` | ✓ |
| 2 | `Corrupt` | `0.33` | `Spiral` | `0.18` | `1` | ✓ |

- `healthThreshold` = เข้าเฟสนี้เมื่อ HP เหลือ **น้อยกว่าหรือเท่ากับ** สัดส่วนนี้
- เฟส 0 ต้องเป็น `1.00` เสมอ (เฟสเริ่มต้น)
- เฟส `Spiral` ให้ `bulletCount = 1` เพราะ Spiral ยิงทีละนัดแล้วหมุนมุมเอง — ลด `fireCooldown` แทน

| ช่องอื่น | ค่าแนะนำ | ความหมาย |
|---|---|---|
| `Victory Report Delay` | `1.5` | หน่วงก่อนแจ้งชนะ (เผื่อเล่นเอฟเฟกต์ระเบิด) |
| `Death Effect Prefab` | (particle) | เอฟเฟกต์ตอนบอสตาย |

> `NullExeBoss` จะสั่ง `EnemyHealth` ปิด `Destroy On Death` ให้เองตอน `Awake()`
> ป้องกันบอสถูกลบก่อนแจ้งเงื่อนไขชนะ — **ไม่ต้องไปตั้งเอง**

### ฟัง event เปลี่ยนเฟส (ถ้าอยากทำ UI หลอดเลือดบอส)

```csharp
nullExeBoss.OnPhaseChanged += (index, phase) =>
{
    phaseNameText.text = $"PHASE {index + 1}: {phase.phaseName}";
};
```

## 10.6 Victory Scene

1. สร้าง Scene ใหม่ชื่อ `VictoryScene` (ตรงกับที่กรอกใน SectorPoolManager)
2. สร้าง `Canvas`
3. สร้าง `Panel` เต็มจอ ชื่อ `VictoryPanel` — **ปิด GameObject ไว้ตอนเริ่ม**
4. ใน Panel ใส่ `Text - TextMeshPro` 2 ตัว: `TitleText` และ `StatsText`
5. สร้าง Empty GameObject ชื่อ `VictoryUI` (**ต้อง active เสมอ** ไม่ใช่ตัว panel ที่ปิด)
   → `Add Component > Victory Screen UI`
6. ลาก `VictoryPanel` → `Panel Root`, `TitleText` → `Title Text`, `StatsText` → `Stats Text`

หน้าจอจะสรุปให้อัตโนมัติ:
```
Process respawns   : 47
Patterns immunized : 4
Total XP archived  : 3820
```

> **หมายเหตุ:** ถ้าตั้ง `Load Victory Scene On Win` = ✓ ที่ `GameManager` (ค่าเริ่มต้น)
> เกมจะโหลด Victory Scene ให้ ซึ่งแปลว่า `VictoryScreenUI` ในฉากบอสจะไม่ทันแสดง
> **เลือกอย่างใดอย่างหนึ่ง:**
> - อยากได้หน้าจบแยก Scene → ปล่อยค่าเริ่มต้น แล้ววาง `VictoryScreenUI` ใน VictoryScene
>   โดยเปิด Panel ค้างไว้เลย (ไม่ต้องรอ event)
> - อยากให้ขึ้นทับฉากบอสเลย → ปิด `Load Victory Scene On Win` แล้ววาง `VictoryScreenUI` ในฉากบอส

## 10.7 ทดสอบ flow

```
1. ล้าง save ก่อน (ดูข้อ 14.5)
2. Play จาก Bootstrap → ต้องเข้า Sector_Tutorial
3. ทำครบทุกขั้น → prompt เปลี่ยนเป็น TUTORIAL COMPLETE
   Console: [Sector Pool] Tutorial completed — Sector Pool unlocked
4. เดินออกประตู → โหลดด่านสุ่มจากคลัง
   Console: [Sector Pool] Cleared: Sector_Tutorial (0/4 pool, story 0/3)
5. ผ่านไปเรื่อย ๆ จนถึงห้องบอส → ฆ่าบอส
   Console: [NULL.exe] Process terminated — victory condition met
            [GameManager] NULL.exe terminated — SYSTEM RESTORED
```

---

# 11) Glitch Zone — เชื่อมกับภาพจริง

**ปัญหาเดิม:** `GlitchZoneVolume.GetVisualOffset()` คำนวณ offset ได้ แต่ไม่มีใครเรียกใช้
ภาพเลยไม่เพี้ยนจริง

## 11.1 หลักการ

```
ปกติ:                          ในโซน Glitch:

   ┌─────┐                         ┌─────┐
   │ ◉   │ ← ภาพ + hitbox          │  ◉  │ ← ภาพ (เลื่อนไปแล้ว)
   └─────┘   ทับกันพอดี            └─────┘
                                 ┌─────┐
                                 │     │ ← hitbox จริง (อยู่ที่เดิม)
                                 └─────┘
                                 ผู้เล่นเห็นไม่ตรงกับที่โดน
```

ทำได้โดย **แยก SpriteRenderer ออกเป็น child** แล้วเลื่อนเฉพาะ child
ส่วน Collider2D คงอยู่ที่ root ตามเดิม

## 11.2 แก้โครงสร้าง Prefab (ทำครั้งเดียวต่อ prefab)

### ก่อนแก้
```
Bullet
├── Rigidbody2D
├── CircleCollider2D (Is Trigger)
├── SpriteRenderer          ← อยู่ที่ root
└── Bullet.cs
```

### หลังแก้
```
Bullet                          ← ตำแหน่งจริง (hitbox)
├── Rigidbody2D
├── CircleCollider2D (Is Trigger)
├── Bullet.cs
├── GlitchVisualDisplacer.cs    ← เพิ่มใหม่
└── Visual                      ← ตำแหน่งภาพ (child)
    └── SpriteRenderer
```

### ขั้นตอนใน Unity
1. Double-click Prefab กระสุนเพื่อเข้า Prefab Mode
2. คลิกขวาที่ root → `Create Empty` → ตั้งชื่อ `Visual`
3. เช็คว่า `Visual` มี `Position = (0, 0, 0)` (Transform > Reset)
4. เลือก root → คลิกขวาที่ component `SpriteRenderer` → `Copy Component`
5. เลือก `Visual` → `Add Component > Sprite Renderer` → คลิกขวา → `Paste Component Values`
6. กลับไปที่ root → คลิกขวาที่ `SpriteRenderer` → `Remove Component`
7. root → `Add Component > Glitch Visual Displacer`
8. ลาก `Visual` เข้าช่อง `Visual Root`
   (ถ้าเว้นว่าง สคริปต์จะหา `SpriteRenderer` ตัวแรกใน child ให้เอง)
9. Save Prefab

**ทำซ้ำกับ:** prefab กระสุน (ทั้งของผู้เล่นและศัตรู), prefab ศัตรู, สิ่งกีดขวางที่อยากให้เพี้ยน

> ถ้าลืมย้าย SpriteRenderer ไป child สคริปต์จะขึ้น warning
> `ไม่พบ child สำหรับภาพ — ต้องแยก SpriteRenderer ออกเป็น child ก่อน` แล้วปิดตัวเอง (ไม่พัง)

## 11.3 ช่องใน GlitchVisualDisplacer

| ช่อง | กระสุน | ศัตรู | ความหมาย |
|---|---|---|---|
| `Visual Root` | `Visual` | `Visual` | child ที่มีภาพ |
| `Offset Multiplier` | `1.2` | `0.6` | คูณ offset ที่ได้จากโซน |
| `Per Object Variation` | `0.5` | `0.3` | ความต่างเฉพาะตัว (0 = ทุกชิ้นกระตุกพร้อมกัน) |
| `Variation Interval` | `0.12` | `0.2` | สุ่มค่าเฉพาะตัวใหม่ทุกกี่วินาที |
| `Flicker In Zone` | ✓ | ✗ | สลับซ่อน/แสดงภาพเป็นจังหวะ |
| `Flicker Chance` | `0.08` | — | โอกาสหายต่อเฟรม (0.08 = 8%) |

**เคล็ดลับการจูน:** ถ้า `Per Object Variation = 0` ทุกอย่างในโซนจะเลื่อนพร้อมกันเป๊ะ
ซึ่งดูเหมือน "กล้องสั่น" มากกว่า "ระบบพัง" — ตั้ง 0.3–0.6 จะได้ฟีล glitch จริง

## 11.4 วางโซนในด่าน

1. Empty GameObject ชื่อ `GlitchZone_01`
2. `Add Component > Box Collider 2D` → **Is Trigger ✓** → ขยายครอบพื้นที่ที่ต้องการ
3. `Add Component > Glitch Zone Volume`
4. ปรับค่า:

| ช่อง | ค่าแนะนำ | ความหมาย |
|---|---|---|
| `Zone Collider` | ว่าง | เว้นว่าง = ใช้ Collider2D บนตัวเอง |
| `Intensity` | `0.5` – `1.0` | ความรุนแรงของโซนนี้ (คูณกับ offset) |
| `Max Visual Offset` | `0.4` | ระยะเลื่อนสูงสุด (หน่วย Unity) |
| `Offset Change Interval` | `0.3` | สุ่มใหม่ทุกกี่วินาที — น้อย = สั่นถี่ |
| `Smooth Offset` | ✗ | ปิดไว้จะได้ฟีล digital ที่กระตุกเป็นช่วง ๆ |
| `Smooth Speed` | `12` | ใช้เมื่อเปิด Smooth Offset |

เลือกโซนแล้วจะเห็น Gizmo สีชมพูโปร่งใสในฉาก บอกขอบเขตโซน

**โซนซ้อนกันได้** — offset จะบวกกัน ทำให้พื้นที่ตรงกลางเพี้ยนหนักกว่าขอบ

## 11.5 ทำไมต้องใช้ static registry

กระสุน spawn ระหว่างเล่น จะลาก reference ของโซนเข้า prefab ล่วงหน้าไม่ได้
`GlitchZoneVolume` เลยลงทะเบียนตัวเองเข้า static list ตอน `OnEnable()`
แล้ว `GlitchVisualDisplacer` ถามผ่าน `GlitchZoneVolume.TryGetOffsetAt(position, out offset)`
ทุกเฟรมใน `LateUpdate()` (หลังการเคลื่อนที่จริงเสร็จแล้ว)

**ผลคือไม่ต้องลาก reference อะไรเลยระหว่างโซนกับวัตถุ**

## 11.6 ใช้ค่า intensity ทำเอฟเฟกต์อื่น

```csharp
float intensity = GlitchZoneVolume.GetIntensityAt(player.position);
if (intensity > 0f)
{
    // เช่น เพิ่ม chromatic aberration, เล่นเสียงซ่า, สั่นกล้อง
}
```

## 11.7 ทดสอบ

1. Play เดินเข้าโซน → ภาพกระสุนต้องเหลื่อมจากจุดที่โดนจริง
2. ยิงกระสุนเข้าโซน → เห็นกระสุนสั่น ๆ ไม่ตรงตำแหน่ง
3. เดินออกจากโซน → ภาพต้องกลับมาตรงทันที (ไม่ค้างเหลื่อม)

---

# 12) RunInventory — ไอเทม/อาวุธจริงใน Fragment

**ปัญหาเดิม:** `FragmentInheritanceManager` ส่งแค่ temp XP เป็นตัวอย่าง
ไม่มีระบบเก็บอาวุธ/ไอเทมระหว่างรอบจริงตามที่เอกสารออกแบบไว้

## 12.1 ติดตั้ง Manager

1. ใน Scene หลัก (Bootstrap) สร้าง Empty GameObject ชื่อ `RunInventory`
2. `Add Component > Run Inventory`
3. `Max Items` = `0` (ไม่จำกัด) หรือใส่เลขถ้าอยากบังคับให้เลือก เช่น `6`

มี `DontDestroyOnLoad` ในตัวเหมือน manager อื่น

## 12.2 ผังการไหลของข้อมูล

```
    เก็บไอเทมในด่าน
          │
          ▼
  RunInventory.AddItem()
          │
          ├──────────────────► OnInventoryChanged (event)
          │                          │
          │                          ▼
          │                 PlayerShooter.RecalculateStats()
          │                 สเตตจริง = สเตตพื้นฐาน × ค่ารวมไอเทม
          │
          ▼
     ═══ ผู้เล่นตาย ═══
          │
          ▼
  PlayerHealth.Die()
          │
          ▼
  FragmentInheritanceManager.DropFragmentAt(pos)
          ├─► XPManager.ConsumeRunTempXP()  ──► payload.tempXP
          └─► RunInventory.TakeAll()         ──► payload.items
                                                 (กระเป๋าว่างทันที)
          │
          ▼
  DataFragment ถือ FragmentPayload ไว้ + นับถอยหลัง
          │
          ├── เก็บคืนทัน ──► XPManager.AddRunTempXP()
          │                  RunInventory.RestoreAll()   ← ได้อาวุธคืนครบ
          │
          └── หมดเวลา ────► Destroy → หายถาวรในรอบนั้น

    ═══ Corruption Meter เต็ม ═══
          │
          ▼
  Force Format → RunInventory.ClearRun() + XPManager.ResetRunTempXP()
                 (Skill Tree ถาวรและ Permanent XP ไม่ถูกแตะ)
```

## 12.3 สร้างไอเทมให้เก็บ

1. สร้าง GameObject ใส่ Sprite ไอคอน (เช่น ไอคอนไฟล์ .dll)
2. `Add Component > Collider2D` → **Is Trigger ✓**
3. `Add Component > Item Pickup`
4. กรอกช่อง `Item` (กางออกมาจะเห็นทุกช่อง)
5. ทำเป็น Prefab

### ตัวอย่างค่าไอเทม

| ชื่อไฟล์ในเกม | type | ค่าที่ตั้ง | ผลที่ได้ |
|---|---|---|---|
| `Overclock.dll` | `WeaponUpgrade` | `fireRateMultiplier = 1.4` | ยิงเร็วขึ้น 40% |
| `Shotgun.sys` | `WeaponUpgrade` | `bonusBulletsPerShot = 4`<br>`bonusSpreadAngle = 45` | กระสุน 5 นัดกระจาย 45° |
| `Kernel.patch` | `WeaponUpgrade` | `damageMultiplier = 1.5` | ดาเมจ +50% |
| `Cache.tmp` | `PassiveBuff` | `moveSpeedMultiplier = 1.25` | เดินเร็วขึ้น 25% |
| `Defrag.exe` | `WeaponUpgrade` | `fireRateMultiplier = 0.7`<br>`damageMultiplier = 2.2` | ยิงช้าแต่แรงมาก |

**ค่าคูณเป็นแบบสะสม** — เก็บ `Overclock.dll` 2 ชิ้น = 1.4 × 1.4 = ยิงเร็วขึ้น 96%
ส่วน `bonusBulletsPerShot` / `bonusSpreadAngle` เป็นแบบ**บวก**

### ช่องอื่นใน ItemPickup

| ช่อง | ค่าแนะนำ | ความหมาย |
|---|---|---|
| `Pickup Effect Prefab` | (particle) | เอฟเฟกต์ตอนเก็บ |
| `Bob Amplitude` | `0.12` | ระยะลอยขึ้นลง (0 = ไม่ลอย) |
| `Bob Speed` | `3` | ความเร็วลอย |

## 12.4 สเตตอาวุธเปลี่ยนความหมายแล้ว

ค่าใน Inspector ของ `PlayerShooter` ตอนนี้คือ **สเตตพื้นฐาน**

```
สเตตจริง = สเตตพื้นฐาน (Inspector) × ค่ารวมจาก RunInventory
```

อ่านสเตตจริงได้จาก:
```csharp
playerShooter.CurrentFireRate
playerShooter.CurrentDamage
```

ถ้ามีระบบสลับอาวุธหลัก ให้เรียก `SetWeaponStats()` เหมือนเดิม —
มันจะเปลี่ยน**สเตตพื้นฐาน** แล้วคำนวณใหม่ทับด้วยไอเทมของรอบให้อัตโนมัติ

## 12.5 Data Fragment เวอร์ชันใหม่

ช่องใหม่ใน `DataFragment` prefab:

| ช่อง | ค่าแนะนำ | ความหมาย |
|---|---|---|
| `Blink Warning Time` | `5` | เริ่มกะพริบเตือนก่อนหมดเวลากี่วินาที |
| `Blink Interval` | `0.15` | ความถี่การกะพริบ |
| `Sprite Renderer` | ว่าง | เว้นว่าง = หาใน child เอง |

ช่องใหม่ใน `FragmentInheritanceManager`:

| ช่อง | ค่าแนะนำ | ความหมาย |
|---|---|---|
| `Fragment Lifetime` | `30` | เวลาก่อนหายถาวร |
| `Extended Lifetime Bonus` | `15` | เพิ่มให้ถ้าปลดล็อก skill `fragment_timer_extend` |
| `Drop Empty Fragments` | ✗ | ตายมือเปล่าไม่ต้องดรอป จะได้ไม่รกฉาก |

**Skill `fragment_timer_extend` ใน Skill Tree ใช้งานได้จริงแล้ว** —
ปลดล็อกแล้วเวลาเก็บ fragment จะเป็น 30 + 15 = 45 วินาที

## 12.6 อ่านข้อมูลกระเป๋าไปทำ HUD

ถ้าอยากโชว์รายการไอเทมที่ถืออยู่:

```csharp
private void Start()
{
    if (RunInventory.Instance != null)
        RunInventory.Instance.OnInventoryChanged += RefreshItemList;
}

private void RefreshItemList()
{
    foreach (var item in RunInventory.Instance.Items)
        Debug.Log(item.displayName);
}
```

โชว์เวลานับถอยหลังของ fragment:

```csharp
var frag = FragmentInheritanceManager.Instance.ActiveFragment;
if (frag != null) timerText.text = $"{frag.RemainingTime:F1}s";
```

## 12.7 สร้างไอเทมจากโค้ด (เช่น ดรอปจากศัตรู)

```csharp
var item = new RunItem
{
    id = "overclock",
    displayName = "Overclock.dll",
    type = RunItemType.WeaponUpgrade,
    fireRateMultiplier = 1.4f
};
RunInventory.Instance.AddItem(item);
```

## 12.8 ทดสอบ

1. เก็บ `Overclock.dll` → ยิงต่อ ต้องเร็วขึ้นทันที
   Console: `[Run Inventory] +Overclock.dll (ทั้งหมด 1 ชิ้น)`
2. ตาย → Console: `Dropped fragment ... carrying 30 XP + 1 items`
   ยิงต่อ ต้องกลับมาช้าเท่าเดิม
3. เดินกลับไปเก็บ fragment ทัน → `Recovered 30 XP + 1 items` ยิงเร็วอีกครั้ง
4. ตายแล้วปล่อยให้หมดเวลา → `Fragment expired — lost permanently this run`

---

# 13) Checklist Scene หลัก

Scene `Bootstrap` ต้องมี GameObject ครบตามนี้ (แต่ละตัว = 1 GameObject)

| GameObject | สคริปต์ | ต้องกรอกอะไร |
|---|---|---|
| `DeathLogManager` | `DeathLogManager` | — |
| `BulletPatternMemory` | `BulletPatternMemory` | — |
| `CorruptionMeter` | `CorruptionMeter` | `Max Corruption Count` |
| `FragmentInheritanceManager` | `FragmentInheritanceManager` | **DataFragment Prefab** |
| `XPManager` | `XPManager` | — |
| `SkillTreeManager` | `SkillTreeManager` | — |
| `SectorPoolManager` | `SectorPoolManager` | **ชื่อ Scene ทั้งหมด** (ข้อ 10.2) |
| **`RunInventory`** ⬅ ใหม่ | `RunInventory` | `Max Items` |
| `GameManager` | `GameManager` | `Player Object` |

ในแต่ละ Scene ด่านต้องมี:

| GameObject | สคริปต์ | หมายเหตุ |
|---|---|---|
| `GameManager` | `GameManager` | ลาก Player เข้าช่อง (หรือปล่อยให้หาเองจาก Tag) |
| `SectorExit` | `SectorExitTrigger` | ทางออกด่าน |
| `Checkpoint_XX` | `CheckpointTrigger` | ตามจุดสำคัญ |
| `SystemInterferenceManager` | `SystemInterferenceManager` | เฉพาะด่านที่ใช้เอฟเฟกต์ |
| `GlitchZone_XX` | `GlitchZoneVolume` | เฉพาะด่านที่ใช้ |

---

# 14) ลำดับการทดสอบ

ทดสอบทีละระบบจะหาต้นตอปัญหาง่ายกว่าเปิดหมดพร้อมกัน

## 14.1 Checkpoint
```
เดินผ่าน checkpoint → Console: [GameManager] Checkpoint updated: Checkpoint_01
ตาย                 → เกิดที่ checkpoint ไม่ใช่จุดเริ่มด่าน
```

## 14.2 RunInventory
```
เก็บไอเทม → [Run Inventory] +Overclock.dll (ทั้งหมด 1 ชิ้น)  + fire rate เปลี่ยนทันที
ตาย       → [Fragment Inheritance] Dropped fragment ... carrying 30 XP + 1 items
เก็บคืน   → [Fragment Inheritance] Recovered 30 XP + 1 items
หมดเวลา   → [Fragment Inheritance] Fragment expired — lost permanently this run
```

## 14.3 Glitch Zone
```
เข้าโซน   → ภาพกระสุน/ศัตรูเหลื่อมจาก hitbox จริง
ออกจากโซน → ภาพกลับมาตรงทันที ไม่ค้าง
```

## 14.4 Tutorial + Victory
```
เริ่มเกมใหม่ → เข้า Sector_Tutorial
ทำครบ       → [Sector Pool] Tutorial completed — Sector Pool unlocked
เดินออก     → [Sector Pool] Cleared: Sector_Tutorial (0/4 pool, story 0/3)
ฆ่าบอส      → [NULL.exe] Process terminated — victory condition met
              [GameManager] NULL.exe terminated — SYSTEM RESTORED
```

## 14.5 ล้าง save ตอนทดสอบ

ความคืบหน้าเก็บใน `PlayerPrefs` ทำให้ทดสอบ tutorial ซ้ำไม่ได้ ต้องล้างก่อน:

**วิธีที่ 1** — เรียกจากโค้ด/ปุ่ม:
```csharp
SectorPoolManager.Instance.ResetAllProgress();
```

**วิธีที่ 2** — เขียน editor script สั้น ๆ วางใน `Assets/Editor/`:
```csharp
using UnityEditor;
public class ClearPrefs
{
    [MenuItem("Tools/Clear All PlayerPrefs")]
    static void Clear() { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); }
}
```

**สิ่งที่ถูกล้าง:** cleared sectors, story index, tutorial flag, game completed flag,
permanent XP, skill tree ที่ปลดล็อกไว้

---

# 15) ปัญหาที่พบบ่อย

### กระสุนทะลุ ไม่ชน
เช็ค Collider2D ของ Bullet ติ๊ก **Is Trigger** และ Tag เป้าหมายถูกต้อง (`Player` / `Enemy`)

### `NullReferenceException` ตอนตาย
มักเกิดจากยังไม่ได้สร้าง manager ใน Scene หลัก — ทุกตัวใช้ `?.` กันพังไว้แล้ว
แต่ฟีเจอร์นั้นจะไม่ทำงานถ้า manager ไม่มีอยู่จริง

### `Rigidbody2D.linearVelocity` ไม่มีให้ใช้
Unity เก่ากว่าเวอร์ชัน 6 ให้เปลี่ยนเป็น `.velocity` ทุกจุด
(อยู่ใน `Bullet.cs`, `PlayerController.cs`, `EnemyBase.cs`)

### ภาพไม่เพี้ยนในโซน Glitch
1. ย้าย `SpriteRenderer` ไป child แล้วหรือยัง (ดูข้อ 11.2)
2. `GlitchVisualDisplacer` ติดที่ **root** ไม่ใช่ที่ child
3. Collider ของโซนติ๊ก **Is Trigger** แล้วหรือยัง
4. ดู Console มี warning `ไม่พบ child สำหรับภาพ` หรือไม่

### ภาพค้างเหลื่อมหลังออกจากโซน
เกิดเมื่อมี component อื่นไปแก้ `localPosition` ของ child แข่งกัน — ย้ายมาแก้ใน `LateUpdate()` แทน

### เก็บไอเทมแล้วสเตตไม่เปลี่ยน
1. มี GameObject `RunInventory` ใน Scene หลักหรือยัง
2. Console ขึ้น `[Item Pickup] ไม่พบ RunInventory ใน Scene หลัก` หรือไม่
3. ไอเทมตั้ง `type` ถูกไหม — `WeaponUpgrade` เท่านั้นที่มีผลกับ `PlayerShooter`

### เล่น tutorial ซ้ำไม่ได้
`HasCompletedTutorial` ถูกเซฟใน PlayerPrefs — ล้างตามข้อ 14.5

### โหลด Scene แล้ว error `Scene couldn't be loaded`
ยังไม่ได้เพิ่ม Scene ใน `File > Build Settings > Scenes In Build`
หรือชื่อ Scene ที่กรอกใน `SectorPoolManager` สะกดไม่ตรง (case-sensitive)

### บอสตายแล้วไม่ขึ้นหน้าจอชนะ
1. `NullExeBoss` attach อยู่บน GameObject เดียวกับ `EnemyHealth` หรือไม่
2. ดู Console ว่ามี `[NULL.exe] Process terminated` ขึ้นไหม
3. ถ้าขึ้นแต่ UI ไม่มา — อ่านหมายเหตุในข้อ 10.6 เรื่อง `Load Victory Scene On Win`

### ตายแล้วเกิดซ้ำหลายรอบ / respawn รัว
เวอร์ชันใหม่มี `IsRespawning` กันไว้แล้ว ถ้ายังเกิดอยู่แปลว่ายังใช้ `GameManager.cs` ตัวเก่า

### เดินช้าค้างถาวรหลังโดน Latency Spike
บั๊กเดิมของ `PlayerController` (StopAllCoroutines ทับ `moveSpeed`) — แก้แล้วในเวอร์ชันใหม่
ถ้ายังเป็นอยู่แปลว่ายังใช้ไฟล์เก่า

---

# 16) ภาคผนวก: API ที่เรียกใช้ได้

รวม method สาธารณะที่มักต้องเรียกจากสคริปต์ในฉาก

## GameManager
```csharp
GameManager.Instance.SetCheckpoint(Transform checkpoint);
GameManager.Instance.OnPlayerDied();
GameManager.Instance.OnSectorCleared();              // ใช้ชื่อ Scene ปัจจุบัน
GameManager.Instance.OnSectorCleared(string scene);
GameManager.Instance.OnFinalBossDefeated();          // เงื่อนไขชนะ

GameManager.Instance.IsGameWon;
GameManager.Instance.IsRespawning;

GameManager.Instance.OnPlayerDeathSequenceStarted += ...;
GameManager.Instance.OnPlayerRespawned += ...;
GameManager.Instance.OnGameWon += ...;
```

## SectorPoolManager
```csharp
SectorPoolManager.Instance.GetNextSector();          // คืนชื่อ Scene ถัดไป
SectorPoolManager.Instance.LoadSector(string scene);
SectorPoolManager.Instance.LoadVictoryScene();
SectorPoolManager.Instance.MarkSectorCleared(string scene);
SectorPoolManager.Instance.MarkTutorialComplete();
SectorPoolManager.Instance.MarkGameCompleted();
SectorPoolManager.Instance.ResetAllProgress();       // ล้าง save

SectorPoolManager.Instance.HasCompletedTutorial;
SectorPoolManager.Instance.HasCompletedGame;
SectorPoolManager.Instance.ClearedCount;   // ผ่านไปกี่ด่าน
SectorPoolManager.Instance.PoolSize;       // คลังมีกี่ด่าน
```

## RunInventory
```csharp
RunInventory.Instance.AddItem(RunItem item);
RunInventory.Instance.RemoveItem(string id);
RunInventory.Instance.Has(string id);
RunInventory.Instance.TakeAll();                     // ตอนตาย
RunInventory.Instance.RestoreAll(List<RunItem>);     // ตอนเก็บคืน
RunInventory.Instance.ClearRun();                    // Force Format

RunInventory.Instance.Items;    // IReadOnlyList<RunItem>
RunInventory.Instance.Count;
RunInventory.Instance.OnInventoryChanged += ...;

// ค่ารวมสำหรับระบบอื่น
RunInventory.Instance.GetFireRateMultiplier();
RunInventory.Instance.GetDamageMultiplier();
RunInventory.Instance.GetBonusBulletsPerShot();
RunInventory.Instance.GetBonusSpreadAngle();
RunInventory.Instance.GetMoveSpeedMultiplier();
```

## GlitchZoneVolume (static)
```csharp
GlitchZoneVolume.TryGetOffsetAt(Vector2 point, out Vector2 offset);
GlitchZoneVolume.GetIntensityAt(Vector2 point);
GlitchZoneVolume.ActiveZones;   // IReadOnlyList<GlitchZoneVolume>
```

## EnemyHealth
```csharp
enemyHealth.TakeDamage(float amount);
enemyHealth.Heal(float amount);
enemyHealth.SetDestroyOnDeath(bool value, float delay = 0f);

enemyHealth.CurrentHealth / MaxHealth / HealthPercent / IsDead;
enemyHealth.OnHealthChanged += (current, max) => ...;
enemyHealth.OnDeath += ...;
EnemyHealth.OnAnyEnemyKilled += (enemy) => ...;   // static
```

## EnemyBulletEmitter
```csharp
emitter.TryFireAt(Vector2 targetPosition);
emitter.SetPattern(BulletPatternType pattern);
emitter.ConfigurePattern(BulletPatternType pattern, float cooldown, int bulletCount);
emitter.CurrentPattern;
```

## อื่น ๆ
```csharp
tutorialController.CompleteStep(string stepId);
sectorExitTrigger.Unlock();
sectorExitTrigger.Lock();
playerShooter.RecalculateStats();
playerShooter.SetWeaponStats(fireRate, damage, bulletsPerShot, spread);
playerController.CurrentMoveSpeed;   // ความเร็วจริงหลังคูณทุกอย่าง
playerController.IsDashing;
fragmentInheritanceManager.ActiveFragment;   // fragment ล่าสุดในฉาก
dataFragment.RemainingTime;                  // เวลาเหลือก่อนหาย
nullExeBoss.OnPhaseChanged += (index, phase) => ...;
```
