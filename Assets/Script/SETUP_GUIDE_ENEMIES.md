# SETUP GUIDE ภาค 4 — ศัตรู 3 ชนิด
### SYSTEM.exe : Ghost in the Kernel

ต่อจาก `SETUP_GUIDE_WAVES.md` — เอกสารนี้คือข้อ 22–26

---

# 22) ภาพรวม

## 22.1 ทำไมต้องมี 3 ชนิดที่ต่างกันจริง

ถ้าศัตรูทุกตัวคือ "เดินเข้าหาแล้วยิง" ต่างกันแค่ sprite กับตัวเลข HP
ผู้เล่นจะใช้ทักษะเดียวกันตลอด 60 wave = เบื่อตั้งแต่ด่าน 2

3 ชนิดนี้ออกแบบให้**บังคับทักษะคนละอย่าง** เอามาผสมกันแล้วเกิดโจทย์ใหม่

| ชนิด | เคลื่อนที่ | ฆ่าด้วย | ทักษะที่บังคับ | จุดอ่อน |
|---|---|---|---|---|
| **Pop-up Swarmer** | กระโดดเป็นจังหวะ หยุด-เตือน-พุ่ง | ชนตัว | **อ่านจังหวะ** ขยับตอนเห็นสัญญาณสั่น | นิ่งสนิทตอนพัก ยิงง่าย |
| **Cursor Chaser** | ไล่ตามมีความเฉื่อย เลยเป้าได้ | ชนตัว | **หลอกล่อ** เลี้ยวกะทันหันให้มันเลยเป้า | ช่วง recover หลังพุ่ง |
| **Firewall Turret** | ไม่ขยับเลย | กระสุน | **จัดตำแหน่ง** วิ่งอ้อมไปด้านหลัง | ด้านหลังไม่มีโล่ + ตอนยิงโล่ปิด |

## 22.2 ไฟล์ใหม่

| ไฟล์ | วางที่ | หน้าที่ |
|---|---|---|
| `EnemyAIBase.cs` | `Scripts/Enemy/` | คลาสฐาน AI (หา player, หมุน, สั่งความเร็ว) |
| `PopupSwarmer.cs` | `Scripts/Enemy/` | ศัตรูกระโดด + แตกตัวตอนตาย |
| `CursorChaser.cs` | `Scripts/Enemy/` | ศัตรูไล่ตามแบบมีความเฉื่อย + พุ่งใส่ |
| `FirewallTurret.cs` | `Scripts/Enemy/` | ป้อมอยู่กับที่ + โล่กันด้านหน้า |
| `ContactDamage.cs` | `Scripts/Enemy/` | ดาเมจจากการชนตัว |
| `DirectionalShield.cs` | `Scripts/Enemy/` | โล่กันดาเมจเฉพาะด้าน |

## 22.3 ไฟล์ที่ต้องเขียนทับ

| ไฟล์ | เปลี่ยนอะไร |
|---|---|
| `BulletPatternType.cs` | + `Collision` (ตายจากการชน) — **ต่อท้าย enum ไม่แทรกกลาง** |
| `Bullet.cs` | ส่งตำแหน่งจุดชน + ชื่อเจ้าของกระสุนไปด้วย |
| `EnemyBulletEmitter.cs` | + `Source Display Name` |
| `EnemyHealth.cs` | + รองรับ `DirectionalShield` |
| `PlayerHealth.cs` | + รับชื่อตัวที่ฆ่า, กันตายซ้อน |
| `DeathLogManager.cs` | + เก็บชื่อตัวที่ฆ่า, `DescribeCause()` |
| `DeathScreenUI.cs` | + บรรทัด `Cause of death:` และเลข Process ID |

> `EnemyBase.cs` ตัวเดิม **ไม่ถูกแตะ** ยังใช้ได้กับศัตรูพื้นฐานที่แค่เดินเข้าหาแล้วยิง

## 22.4 ต้องแก้ Prefab/Scene เพิ่ม

หลังเขียนทับไฟล์แล้ว มี 2 จุดที่ Inspector มีช่องใหม่โผล่มา

1. **`EnemyBulletEmitter` ทุกตัว** → กรอก `Source Display Name`
   (เว้นไว้จะขึ้นว่า `Unknown Process` ใน Death Log)
2. **`DeathScreenUI`** → สร้าง `Text` เพิ่ม 1 ตัวสำหรับบรรทัด `Cause of death:`
   แล้วลากเข้าช่อง `Cause Text`

---

# 23) Pop-up Swarmer

## 23.1 พฤติกรรม

```
  พัก 0.7s        สั่นเตือน 0.3s       พุ่ง 0.35s        พัก...
 ┌─────────┐     ┌───────────┐      ┌─────────┐
 │  นิ่ง    │ ──► │ สั่นซ้ายขวา │ ──► │ พุ่งตรง  │ ──► วนซ้ำ
 │ ยิงง่าย  │     │ ล็อกทิศแล้ว │      │ ชน=ดาเมจ│
 └─────────┘     └───────────┘      └─────────┘
                       ▲
              ทิศถูกล็อกตั้งแต่ตรงนี้
              ผู้เล่นที่ขยับทันจะรอด
```

**ทิศถูกล็อกตอนเริ่มสั่น ไม่ใช่ตอนพุ่ง** — จุดนี้คือหัวใจของการออกแบบ
ทำให้มันเป็นศัตรูที่ "อ่านออกแล้วหลบได้" ไม่ใช่ศัตรูที่ตามตลอดจนหลบไม่ได้

**ตายแล้วแตกตัว** เป็นตัวเล็ก 2 ตัว (ตามธีม pop-up ที่ปิดแล้วเด้งใหม่)
ทำให้การยิงมั่วส่งผลเสีย — ต้องคิดว่าจะยิงตัวไหนก่อน

## 23.2 โครงสร้าง Prefab

```
Swarmer                          <- Rigidbody2D (Gravity 0) + Collider2D + Tag: Enemy
├── EnemyHealth
├── ContactDamage
├── PopupSwarmer
├── GlitchVisualDisplacer
└── Visual                       <- ช่อง Visual Root ของ GlitchVisualDisplacer
     └── Shake                   <- ช่อง Rotating Part ของ PopupSwarmer
          └── SpriteRenderer
```

> **สำคัญ:** ห้ามชี้ `Visual Root` (Glitch) กับ `Rotating Part` (Shake) ไปที่ node เดียวกัน
> GlitchVisualDisplacer เขียนตำแหน่งใน `LateUpdate()` จะทับการสั่นทุกเฟรม
> ต้องซ้อนกันเป็น 2 ชั้นตามผังด้านบน

## 23.3 ค่าใน Inspector

### PopupSwarmer

| ช่อง | ค่าแนะนำ | ผลถ้าปรับ |
|---|---|---|
| `Hop Speed` | `7` | สูง = พุ่งไกล หลบยาก |
| `Hop Duration` | `0.35` | ยาว = พุ่งไกลขึ้น |
| `Rest Duration` | `0.7` | **สั้น = กดดันมาก** ปรับตัวนี้เป็นหลักเวลาเพิ่มความยาก |
| `Telegraph Duration` | `0.3` | **สั้น = ยากขึ้นชัดเจน** ต่ำกว่า 0.15 จะไม่ยุติธรรม |
| `Aim Jitter` | `12` | องศาคลาดเคลื่อน — 0 = เล็งเป๊ะ โหดมาก |
| `Shake Amount` | `0.08` | ความแรงของการสั่นเตือน |
| `Split Prefab` | prefab ตัวเอง | เว้นว่าง = ไม่แตกตัว |
| `Split Count` | `2` | |
| `Max Generation` | `1` | 1 = ตัวแม่แตกได้ ลูกแตกต่อไม่ได้ |
| `Split Scale` | `0.65` | ขนาดตัวลูก |
| `Split Health Multiplier` | `0.4` | HP ตัวลูก |
| `Split Xp Multiplier` | `0.5` | XP ตัวลูก |

### ContactDamage

| ช่อง | ค่าแนะนำ |
|---|---|
| `Damage` | `15` |
| `Damage Cooldown` | `0.8` |
| `Source Display Name` | `Pop-up Swarmer` |
| `Die On Contact` | ✗ (ติ๊ก ✓ ถ้าอยากให้เป็นตัวพลีชีพ) |

> **ระวัง `Max Generation`** ตั้ง 2 ขึ้นไปแล้วจำนวนศัตรูจะระเบิดแบบเลขชี้กำลัง
> 1 ตัว → 2 → 4 → 8 ถ้า wave มี 12 ตัวจะกลายเป็น 96 ตัว เฟรมตกแน่นอน

---

# 24) Cursor Chaser

## 24.1 พฤติกรรม

```
  ไล่ตาม (มีความเฉื่อย)      เข้าใกล้ 3.5 หน่วย
 ┌──────────────────┐       ┌────────────┐      ┌────────┐      ┌─────────┐
 │ เร่งเข้าหาเรื่อย ๆ  │ ────► │ หยุดเล็ง    │ ───► │ พุ่งตรง │ ───► │ เหนื่อย  │
 │ เลี้ยวไม่ทัน เลยเป้า│       │ 0.4s สีแดง  │      │ 0.28s  │      │ 0.6s    │
 └──────────────────┘       └────────────┘      └────────┘      └─────────┘
          ▲                                                          │
          └──────────────────────────────────────────────────────────┘
                                                           ช่องให้สวนกลับ
```

**ต่างจาก Swarmer ตรงไหน:** Swarmer หยุดสนิทแล้วพุ่งเป็นจังหวะตายตัว
แต่ Chaser ตามตลอดเวลาแบบลื่นไถล ผู้เล่นต้องใช้การ**เลี้ยวกะทันหัน**ให้มันเลยเป้า
ไม่ใช่แค่ขยับหลบตามจังหวะ

## 24.2 โครงสร้าง Prefab

```
Chaser                           <- Rigidbody2D (Gravity 0) + Collider2D + Tag: Enemy
├── EnemyHealth
├── ContactDamage                <- Source Display Name = "Cursor Chaser"
├── CursorChaser
├── GlitchVisualDisplacer
└── Visual                       <- Visual Root (Glitch) + Rotating Part (Chaser)
     └── SpriteRenderer          <- รูปลูกศรเคอร์เซอร์
```

Chaser ใช้ `Rotating Part` แค่หมุน (ไม่ขยับตำแหน่ง) จึงชี้ไป node เดียวกับ Glitch ได้

## 24.3 ค่าใน Inspector

| ช่อง | ค่าแนะนำ | ผลถ้าปรับ |
|---|---|---|
| `Acceleration` | `22` | **ต่ำ = เลี้ยวไม่ทัน เลยเป้าบ่อย** (ตัวนี้คือบุคลิกของมัน) |
| `Max Chase Speed` | `4.5` | ควรต่ำกว่าความเร็วผู้เล่น (6) ไม่งั้นหนีไม่ออก |
| `Drag` | `1.5` | ต่ำ = ลื่นไถลมาก |
| `Lunge Range` | `3.5` | ระยะที่เริ่มเล็ง |
| `Aim Duration` | `0.4` | **สั้น = ยากขึ้น** ต่ำกว่า 0.2 หลบไม่ทัน |
| `Lunge Speed` | `16` | |
| `Lunge Duration` | `0.28` | |
| `Recover Duration` | `0.6` | **ยาว = ให้โอกาสสวนกลับ** ตัดเหลือ 0.2 จะโหดมาก |
| `Lunge Cooldown` | `1.5` | |
| `Aim Flash Target` | SpriteRenderer | เปลี่ยนเป็นสีแดงตอนเล็ง |

**ตั้ง `Max Chase Speed` ให้ต่ำกว่าความเร็วผู้เล่นเสมอ** ไม่งั้นมันจะไล่ทันตลอด
ผู้เล่นจะรู้สึกว่าหนีไม่ได้และเกมไม่ยุติธรรม

---

# 25) Firewall Turret

## 25.1 พฤติกรรม

```
        ผู้เล่นยิงจากด้านหน้า            ผู้เล่นอ้อมไปด้านหลัง
              ✗ โดนกัน                        ✓ เข้าเต็ม

              ╱‾‾‾╲  โล่ 140°
        ●───►│ ▓▓▓ │                      │ ▓▓▓ │◄─── ●
              ╲___╱                        ╲___╱

   วนลูป:  พัก 1.8s ──► ชาร์จ 1s (สีแดงขึ้น) ──► ยิงรัว 2s ──► พัก...
                                                    ▲
                                        ตอนนี้โล่ปิด + หยุดหมุน
                                        = ช่วงที่เข้าไปยิงได้
```

**บังคับการจัดตำแหน่ง** — ยืนยิงหน้าตรงจะไม่มีอะไรเกิดขึ้นเลย
ต้องวิ่งอ้อมให้เร็วกว่าที่ป้อมหมุนตาม (`Rotation Speed` 90°/วินาที)

## 25.2 โครงสร้าง Prefab

```
Turret                           <- Rigidbody2D (Kinematic) + Collider2D + Tag: Enemy
├── EnemyHealth                  <- ช่อง Shield ปล่อยว่าง (หาเองได้)
├── EnemyBulletEmitter           <- Source Display Name = "Firewall Turret"
├── DirectionalShield            <- ช่อง Facing = "Barrel"
├── FirewallTurret               <- ช่อง Rotating Part = "Barrel"
├── Base                         <- SpriteRenderer ฐานป้อม (ไม่หมุน)
└── Barrel                       <- ส่วนที่หมุน
     ├── SpriteRenderer          <- ปากกระบอก + รูปโล่
     └── FirePoint               <- ช่อง Fire Point ของ EnemyBulletEmitter
```

**Body Type ต้องเป็น Kinematic** ไม่งั้นป้อมจะถูกกระสุน/ศัตรูดันจนเลื่อน
(สคริปต์ตั้งให้เองใน `Awake()` แล้ว แต่ตั้งใน Inspector ด้วยจะเห็นชัดกว่า)

## 25.3 ค่าใน Inspector

### FirewallTurret

| ช่อง | ค่าแนะนำ | ผลถ้าปรับ |
|---|---|---|
| `Rotation Speed` | `90` | **ต่ำ = อ้อมหลังง่าย** ตัวนี้คือความยากหลัก |
| `Activation Range` | `12` | ไกลกว่านี้ป้อมจะนิ่งเฉย |
| `Charge Duration` | `1` | **ช่วงเตือน** สั้นกว่า 0.5 จะหาที่หลบไม่ทัน |
| `Burst Duration` | `2` | ยิงรัวนานเท่าไหร่ |
| `Rest Duration` | `1.8` | **ช่วงเข้าไปทำดาเมจ** สั้น = ยากขึ้น |
| `Lock Rotation While Firing` | ✓ | หยุดหมุนตอนยิง ให้ผู้เล่นวิ่งหนีลำกระสุนได้ |
| `Drop Shield While Firing` | ✓ | เปิดช่องสวนกลับตอนมันยุ่ง |
| `Use Format Warning` | ✗ | ติ๊ก ✓ เฉพาะป้อมตัวใหญ่/บอสย่อย |

### DirectionalShield

| ช่อง | ค่าแนะนำ | ความหมาย |
|---|---|---|
| `Facing` | `Barrel` | ทิศที่โล่หัน (ใช้แกน right) |
| `Arc Angle` | `140` | มุมที่โล่ครอบ — 360 = กันรอบตัว (อย่าใช้) |
| `Blocked Damage Multiplier` | `0` | 0 = กันหมด, 0.25 = เข้าแค่ 25% |
| `Shield Visual` | SpriteRenderer | กะพริบตอนกันได้ |
| `Block Effect Prefab` | particle | ประกายตอนกระสุนโดนโล่ |

เลือกป้อมใน Scene แล้วจะเห็น Gizmo เส้นสีส้มกางเป็นพัด บอกมุมที่โล่ครอบ

### EnemyBulletEmitter (สำหรับป้อม)

| ช่อง | ค่าแนะนำ |
|---|---|
| `Source Display Name` | `Firewall Turret` |
| `Pattern Type` | `SpreadCone` หรือ `Aimed` |
| `Fire Cooldown` | `0.25` (คุมจังหวะห่างของแต่ละนัดในชุด) |
| `Bullet Count` | `3` |
| `Spread Angle` | `25` |

> `Burst Duration` 2 วินาที ÷ `Fire Cooldown` 0.25 = ยิง 8 ชุดต่อรอบ

---

# 26) เอาไปใช้ใน Wave

## 26.1 ผสมยังไงให้เกิดโจทย์

ความสนุกไม่ได้มาจากศัตรูตัวเดียว แต่มาจากการที่ 2 ชนิดขัดกันเอง

| ส่วนผสม | โจทย์ที่เกิด |
|---|---|
| Swarmer อย่างเดียว | ฝึกอ่านจังหวะ — ใช้ใน wave ต้น ๆ |
| Chaser อย่างเดียว | ฝึกหลอกล่อ |
| Turret อย่างเดียว | ฝึกจัดตำแหน่ง |
| **Turret + Swarmer** | ต้องอ้อมไปหลังป้อม แต่มี Swarmer พุ่งขวางทาง |
| **Turret + Chaser** | จะอ้อมป้อมก็ถูก Chaser ไล่ ต้องเลือกว่าจะจัดการใครก่อน |
| **Chaser + Swarmer** | ทิศมาจากคนละแบบ — ตัวหนึ่งพุ่งเป็นจังหวะ ตัวหนึ่งตามตลอด |
| ทั้ง 3 ชนิด | ใช้ใน wave 7–9 เท่านั้น ก่อนหน้านั้นจะรกเกินไป |

## 26.2 ตารางไล่ระดับ 10 wave (ฉบับใช้ศัตรู 3 ชนิด)

| # | Wave Name | Groups | หมายเหตุ |
|---|---|---|---|
| 1 | `Boot Sequence` | Swarmer ×4, interval 1.5 | สอนอ่านจังหวะ |
| 2 | `Background Task` | Swarmer ×7, interval 1.0 | เพิ่มจำนวน |
| 3 | `Cursor Trace` | Chaser ×3 | แนะนำชนิดใหม่ตัวเดียว |
| 4 | `Mixed Signal` | Swarmer ×5 + Chaser ×2 | ผสมครั้งแรก |
| 5 | `Firewall Probe` | Turret ×2 (จุด `sides`) + Swarmer ×4 | แนะนำป้อม |
| 6 | `Cross Fire` | Turret ×3 + Chaser ×3 | บีบให้เลือกเป้าหมาย |
| 7 | `Memory Leak` | Swarmer ×10 (แตกตัว ✓) + Chaser ×3 | จอเริ่มรก |
| 8 | `Stack Overflow` | Turret ×4 + Swarmer ×8, interval 0.4 | ⚠ Format Warning |
| 9 | `Kernel Panic` | ทั้ง 3 ชนิดเต็มที่ | ⚠ Format Warning |
| 10 | `SECTOR GUARDIAN` | บอสประจำด่าน ×1 | ✓ Is Boss Wave |

**หลักการ:** แนะนำชนิดใหม่ทีละตัวใน wave ที่มีแต่ชนิดนั้น
ให้ผู้เล่นเรียนรู้พฤติกรรมก่อน แล้วค่อยเอาไปผสม

## 26.3 ใช้ prefab ชุดเดียวข้าม 6 ด่าน

ไม่ต้องทำ prefab แยกต่อด่าน — `WaveManager` มี `Enemy Health Multiplier`
กับ `Enemy Xp Multiplier` ให้ปรับต่อด่านอยู่แล้ว (ดูตารางข้อ 18.1)

ถ้าอยากให้แต่ละ Sector มีบุคลิกต่างกันจริง ๆ ค่อยทำ prefab variant:
- **Firewall Sector** → Turret เยอะ, `Rotation Speed` สูงขึ้น
- **RAM Sector** → Chaser เยอะ, `Drag` ต่ำ (ลื่นกว่าปกติ ตามธีมข้อมูลไหล)
- **Registry Sector** → Swarmer เยอะ, `Max Generation` = 2 (เขาวงกตที่แตกตัว)

---

# 27) ทดสอบ

## 27.1 Pop-up Swarmer
```
1. Play → Swarmer หยุดนิ่ง → สั่น → พุ่งตรงเข้าหา
2. ขยับหลบตอนเห็นมันสั่น → ต้องรอด (ทิศล็อกแล้ว)
3. ปล่อยให้ชน → HUD เลือดลด
4. ตาย → Console: [Pop-up Swarmer] แตกตัวเป็น 2 ตัว (รุ่น 1/1)
5. ตัวลูกเล็กลงและ HP น้อยลง
6. ฆ่าตัวลูก → wave count ต้องลดถูกต้อง (ไม่ค้าง)
```

## 27.2 Cursor Chaser
```
1. วิ่งเป็นเส้นตรง → มันตามหลัง
2. เลี้ยว 90 องศากะทันหัน → มันต้องเลยเป้าไปก่อนแล้วค่อยเลี้ยวตาม
3. ยืนนิ่งให้มันเข้าใกล้ → มันหยุด เปลี่ยนสีแดง แล้วพุ่ง
4. หลบตอนมันเปลี่ยนสี → ต้องรอด
5. หลังพุ่งจบ มันช้าลงชั่วครู่ → ช่วงนี้เข้าไปยิงได้
```

## 27.3 Firewall Turret
```
1. ยืนยิงด้านหน้า → เลือดไม่ลด + เห็นประกายที่โล่
2. อ้อมไปด้านหลัง → เลือดลดปกติ
3. ป้อมหมุนตาม แต่ช้ากว่าที่วิ่งได้
4. ตอนมันชาร์จ สีเปลี่ยนเป็นแดงเข้ม
5. ตอนยิง มันหยุดหมุน + โล่ปิด → ยิงจากหน้าก็เข้าแล้ว
```

## 27.4 Death Log
```
ตายจาก Swarmer → หน้าจอตายขึ้น:
   Process GP-001 has been terminated
   Cause of death: Pop-up Swarmer collision

ตายจากป้อม →
   Cause of death: Firewall Turret spread cone

ตายซ้ำ → เลข Process ID เพิ่มเป็น GP-002, GP-003 ...
```

## 27.5 เช็คลิสต์
- [ ] `Source Display Name` กรอกครบทุก `ContactDamage` และ `EnemyBulletEmitter`
- [ ] `DeathScreenUI` ลาก `Cause Text` เข้าช่องแล้ว
- [ ] Swarmer: `Visual` กับ `Shake` เป็นคนละ node
- [ ] Chaser: `Max Chase Speed` < ความเร็วผู้เล่น
- [ ] Turret: Rigidbody2D = Kinematic, `Facing` ชี้ไป Barrel
- [ ] ทุก prefab Tag = `Enemy`
- [ ] `Max Generation` ไม่เกิน 1 (ยกเว้นตั้งใจ)

---

# 28) ภาคผนวก: API

```csharp
// คลาสฐาน — สืบทอดเพื่อทำศัตรูชนิดใหม่
public class MyEnemy : EnemyAIBase
{
    protected override void Tick()
    {
        // ของที่ใช้ได้: HasPlayer, ToPlayer, DistanceToPlayer, DirectionToPlayer
        SetVelocity(DirectionToPlayer * 3f);
        RotateTowards(DirectionToPlayer, 180f);
        SnapRotation(DirectionToPlayer);
        bulletEmitter?.TryFireAt(player.position);
    }

    protected override void OnSpawned() { /* ตั้งค่าตอนเกิด */ }
}

// โล่
shield.IsActive = false;                    // ปิดโล่ชั่วคราว
shield.GetDamageMultiplier(hitPoint);       // 1 = ผ่าน, 0 = กันหมด

// EnemyHealth ที่เพิ่มมา
enemyHealth.TakeDamage(damage, hitPoint);   // ผ่านโล่
enemyHealth.OnDamageBlocked += (point) => ...;

// Death Log
DeathLogManager.Instance.LogDeath(cause, position, "Pop-up Swarmer");
DeathLogManager.Instance.DescribeCause(record);   // "Pop-up Swarmer collision"
DeathLogManager.Instance.GetDeathMessage(record); // ข้อความ Task Manager

// PlayerHealth
playerHealth.TakeDamage(amount, cause, sourceName);
playerHealth.OnDamaged += (amount, cause) => ...;  // ให้เอฟเฟกต์สั่นจอไปฟัง
```
