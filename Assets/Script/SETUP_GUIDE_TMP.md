# SETUP GUIDE ภาค 5 — เปลี่ยนมาใช้ TextMeshPro
### SYSTEM.exe : Ghost in the Kernel

ข้อ 29–31 — อ่านก่อนเขียนทับไฟล์ UI

---

# 29) สรุปสิ่งที่เปลี่ยน

## 29.1 ไฟล์ที่แก้

| ไฟล์ | ช่องที่เปลี่ยนเป็น TMP |
|---|---|
| `HUDManager.cs` | `Hp Text`, `Temp XP Text` |
| `DeathScreenUI.cs` | `Message Text`, `Cause Text`, `Resistance Gained Text` |
| `VictoryScreenUI.cs` | `Title Text`, `Stats Text` |
| `WaveHUD.cs` | `Wave Text`, `Timer Text`, `Enemies Text`, `Announce Text` |
| `SkillSelectUI.cs` | หา `TMP_Text` ใน Button Prefab |
| `TutorialSectorController.cs` | `Prompt Text` |

`Slider` และ `Button` ยังเป็นของ `UnityEngine.UI` เหมือนเดิม ไม่ต้องเปลี่ยน

## 29.2 ชนิดที่ใช้คือ `TMP_Text` ไม่ใช่ `TextMeshProUGUI`

`TMP_Text` เป็นคลาสแม่ของทั้ง `TextMeshProUGUI` (ใน Canvas) และ `TextMeshPro` (ในโลก 3D)
ลากอันไหนเข้าช่องก็ได้ ยืดหยุ่นกว่าและโค้ดไม่ต้องแก้ทีหลัง

---

# 30) ติดตั้ง TextMeshPro

## 30.1 Import TMP Essentials

ครั้งแรกที่ใช้ TMP ในโปรเจกต์:

```
Window > TextMeshPro > Import TMP Essential Resources
```

กด Import แล้วรอ ถ้าไม่ทำขั้นนี้ TMP Text ทุกตัวจะขึ้นเป็นสี่เหลี่ยมชมพู

## 30.2 ⚠ ฟอนต์ไทย — ขั้นนี้สำคัญมาก

**ฟอนต์เริ่มต้นของ TMP (LiberationSans) ไม่มีตัวอักษรไทย**
ข้อความไทยทั้งหมด (คำใบ้ tutorial, ชื่อ skill) จะกลายเป็นกล่องสี่เหลี่ยม

### วิธีทำ Font Asset ภาษาไทย

1. หาไฟล์ฟอนต์ไทยที่ใช้ได้ฟรีเชิงพาณิชย์ เช่น **Noto Sans Thai**, **Sarabun**, **IBM Plex Sans Thai**
   (ฟอนต์ราชการไทยชุด 13 ฟอนต์ก็ใช้ได้ — ตรวจ license ก่อนส่งประกวด)
2. ลากไฟล์ `.ttf` เข้า `Assets/Fonts/`
3. `Window > TextMeshPro > Font Asset Creator`
4. ตั้งค่า:

| ช่อง | ค่า |
|---|---|
| `Source Font File` | ฟอนต์ไทยที่ลากเข้ามา |
| `Sampling Point Size` | `Custom Size` = `48` |
| `Padding` | `8` |
| `Atlas Resolution` | `2048 × 2048` (ไทยกินพื้นที่เยอะ) |
| `Character Set` | **`Unicode Range (Hex)`** |
| `Character Sequence` | `20-7E,E01-E5B,200B-200D` |
| `Render Mode` | `SDFAA` |

> `E01-E5B` คือช่วง Unicode ของอักษรไทยทั้งหมด
> `200B-200D` คือ zero-width space ที่ใช้ตัดคำไทย
> `20-7E` คืออังกฤษ+ตัวเลข+สัญลักษณ์

5. กด **Generate Font Atlas** → รอ → **Save as** ลงใน `Assets/Fonts/`
6. ตั้งเป็นฟอนต์เริ่มต้น: `Edit > Project Settings > TextMeshPro > Settings`
   → ช่อง `Default Font Asset` ใส่ font asset ที่เพิ่งสร้าง

### เช็คว่าใช้ได้จริง

สร้าง TMP Text แล้วพิมพ์: `ทดสอบภาษาไทย สระอำ ไม้โท เชิญ`
ถ้าสระบนสระล่างลอยผิดตำแหน่ง แปลว่าฟอนต์นั้นทำ mark positioning ไม่ดี ให้เปลี่ยนฟอนต์

## 30.3 ฟอนต์ธีมเกม

เกมนี้ธีม Windows 95/98 อยากได้ฟีล pixel/terminal
แนะนำใช้ **2 font asset**:

| ใช้กับ | ฟอนต์แนะนำ |
|---|---|
| ข้อความอังกฤษ/ระบบ (`WAVE 3 / 10`, `Process GP-001`) | ฟอนต์ pixel หรือ monospace เช่น Perfect DOS VGA, Px437 |
| ข้อความไทย (คำใบ้ tutorial) | Noto Sans Thai / Sarabun |

ตั้งฟอนต์ไทยเป็น **Fallback** ของฟอนต์ pixel:
`เลือก font asset pixel > Fallback Font Assets > +` แล้วใส่ font asset ไทย
TMP จะหยิบตัวไทยจาก fallback ให้อัตโนมัติเวลาเจอตัวอักษรที่ไม่มีในฟอนต์หลัก

---

# 31) แปลง UI ที่ทำไว้แล้ว

ถ้าใน Scene มี `Text` แบบเก่าอยู่แล้ว **ช่องใน Inspector จะว่างเปล่าหลังเขียนทับสคริปต์**
(ชนิดไม่ตรงกัน Unity จะทิ้ง reference)

## 31.1 วิธีแปลงทีละตัว

1. เลือก GameObject ที่มี `Text` เก่า
2. จดข้อความ/ขนาด/สี/ตำแหน่งไว้
3. ลบ component `Text` ทิ้ง
4. `Add Component > TextMeshPro - Text (UI)`
5. ใส่ค่ากลับตามที่จดไว้

## 31.2 หรือสร้างใหม่เลย (เร็วกว่าถ้ามีไม่กี่ตัว)

```
คลิกขวาใน Hierarchy > UI > Text - TextMeshPro
```

**อย่าเลือก `UI > Legacy > Text`** — นั่นคือของเก่า

## 31.3 ช่องที่ต้องลากใหม่หลังแปลง

| สคริปต์ | ช่องที่ต้องลากใหม่ |
|---|---|
| `HUDManager` | Hp Text, Temp XP Text |
| `DeathScreenUI` | Message Text, Cause Text, Resistance Gained Text |
| `VictoryScreenUI` | Title Text, Stats Text |
| `WaveHUD` | Wave Text, Timer Text, Enemies Text, Announce Text |
| `TutorialSectorController` | Prompt Text |
| `SkillSelectUI` | ไม่มีช่อง Text — แต่ต้องแก้ **Button Prefab** ให้ child เป็น TMP |

## 31.4 เช็คลิสต์

- [ ] Import TMP Essential Resources แล้ว
- [ ] สร้าง Font Asset ภาษาไทยแล้ว (ทดสอบพิมพ์ไทยแล้วไม่เป็นกล่อง)
- [ ] ตั้ง Default Font Asset ใน Project Settings
- [ ] แปลง Text เก่าทุกตัวใน Scene เป็น TMP
- [ ] ลาก reference กลับเข้าช่องครบทุกสคริปต์ตามตาราง 31.3
- [ ] Button Prefab ของ Skill Select ใช้ TMP child
- [ ] Console ไม่มี error `The type or namespace name 'TMPro' could not be found`
      (ถ้ามี = ยังไม่ได้ import TMP หรือ package หาย)

---

## เกร็ดที่มีประโยชน์กับเกมนี้

**Rich text** — TMP รองรับแท็กในสตริง ใช้ทำ HUD สไตล์ระบบได้เลย

```csharp
waveText.text = "<color=#FF3B30>WAVE 9</color> / 10";
announceText.text = "<b>! BOSS PROCESS SPAWNED !</b>";
causeText.text = "Cause of death: <color=#FFD60A>Pop-up Swarmer</color> collision";
```

**ตัวเลขไม่กระตุก** — เลขที่นับถอยหลัง (timer, HP) ควรเปิด monospace
เพื่อไม่ให้ตัวเลขขยับซ้ายขวาเวลาเปลี่ยนหลัก

```csharp
timerText.text = $"<mspace=0.6em>{m:00}:{s:00}</mspace>";
```

**Auto Size** — เปิดในหน้าจอตายเผื่อข้อความยาวเกินกรอบ
(ชื่อศัตรูบางตัวยาว เช่น `Cause of death: Firewall Turret spread cone`)
ที่ Inspector ของ TMP Text ติ๊ก `Auto Size` แล้วตั้ง Min/Max
