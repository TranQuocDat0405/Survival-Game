# DEVLOG — Survival Top-down (Wolffun Test)

> File theo dõi tiến độ. Đối chiếu trực tiếp với `Docs/README.md` (spec bắt buộc).
> **Deadline: 17/08/2026**

| | |
|---|---|
| Unity | 2022.3.62f3 (Built-in RP) |
| Repo | https://github.com/TranQuocDat0405/Survival-Game |
| Scene chính | `Assets/_Project/Scenes/Game.unity` |
| Orientation | Landscape (Left + Right) |

---

## 1. Checklist spec bắt buộc

### 2. Nhân vật (Player)

| # | Yêu cầu | Giá trị spec | Trạng thái |
|---|---|---|---|
| 2.1 | Máu (HP) khởi đầu | 500 | ⬜ |
| 2.1 | Tốc độ di chuyển | 2 unit/s | ⬜ |
| 2.1 | Tốc độ xoay | 180 °/s | ⬜ |
| 2.1 | Giáp | 0 | ⬜ |
| 2.1 | Damage Multiplier | 0 | ⬜ |
| 2.1 | Bắn/bom/dash dùng **forward hiện tại**, không theo joystick | — | ⬜ |
| 2.2 | `Sát thương nhận = Sát thương gốc − Giáp`, clamp ≥ 0 | — | ⬜ |
| 2.2 | `Sát thương gây ra = gốc × (1 + DamageMultiplier)` | — | ⬜ |
| 2.2 | Giáp áp dụng cho **cả đòn chém lẫn độc** | — | ⬜ |

### 3. Kỹ năng nhân vật

| # | Yêu cầu | Giá trị spec | Trạng thái |
|---|---|---|---|
| 3.1 | Bắn 3 viên hình nón −15° / 0° / +15° | — | ⬜ |
| 3.1 | Sát thương gốc mỗi viên | 10 | ⬜ |
| 3.1 | Tối đa 3 charge, mỗi phát tốn 1 | 3 | ⬜ |
| 3.1 | Hồi +1 charge / 3 s (chỉ khi chưa đầy) | 3 s | ⬜ |
| 3.1 | Giãn cách tối thiểu 2 phát bắn (chống spam) | 0.5 s | ⬜ |
| 3.1 | Không đủ charge → không bắn được | — | ⬜ |
| 3.2 | Bom: nổ sau 2 s, 50 dmg gốc, bán kính 5 unit | — | ⬜ |
| 3.2 | Bom cooldown | 12 s | ⬜ |
| 3.3 | Dash: 3 unit trong 0.5 s theo forward | — | ⬜ |
| 3.3 | Hết lướt nổ 15 dmg gốc, bán kính 3 unit | — | ⬜ |
| 3.3 | Dash cooldown | 6 s | ⬜ |

### 4. Kẻ địch

| # | Yêu cầu | Giá trị spec | Trạng thái |
|---|---|---|---|
| 4.1 | Quái cận chiến — Máu | 220 | ⬜ |
| 4.1 | Tốc độ di chuyển | 3 unit/s | ⬜ |
| 4.1 | Tấn công cone 50°, tầm 1.3 unit, 30 dmg gốc, 1 lần/đòn | — | ⬜ |
| 4.1 | Behavior: tiếp cận → tấn công → đứng im 1 s → lặp | — | ⬜ |
| 4.2 | Quái tầm xa — Máu | 180 | ⬜ |
| 4.2 | Tốc độ di chuyển | 2.7 unit/s | ⬜ |
| 4.2 | Tầm tiếp cận để bắn | 3 unit | ⬜ |
| 4.2 | Đạn độc bay thẳng, tối đa 5 unit, tốc độ 10 unit/s | — | ⬜ |
| 4.2 | Độc: 30 dmg gốc/s, tick ngay lúc trúng, kéo dài 3 s | — | ⬜ |
| 4.2 | Tổng **4 tick** (t=0,1,2,3) | 4 | ⬜ |
| 4.2 | Refresh: dính lại → reset thời gian, **không stack** damage | — | ⬜ |
| 4.2 | Behavior: tiếp cận 3 unit → bắn → đứng im 1 s → lặp | — | ⬜ |

### 5. Wave, kinh nghiệm, lên cấp

| # | Yêu cầu | Giá trị spec | Trạng thái |
|---|---|---|---|
| 5 | Mỗi wave spawn ngẫu nhiên 3–4 quái cận chiến | 3–4 | ⬜ |
| 5 | + 1–2 quái tầm xa | 1–2 | ⬜ |
| 5 | Chỉ spawn wave kế khi **clear hết** wave hiện tại | — | ⬜ |
| 5 | Giết 1 quái → +30 EXP | 30 | ⬜ |
| 5 | Đủ 100 EXP → lên 1 cấp, **EXP dư giữ lại** | 100 | ⬜ |
| 5 | Lên cấp: +40 máu hiện tại, +40 máu tối đa | +40 / +40 | ⬜ |
| 5 | Lên cấp: +2 giáp, +0.1 Damage Multiplier | +2 / +0.1 | ⬜ |

### 6. UI bắt buộc

| # | Yêu cầu | Trạng thái |
|---|---|---|
| 6 | Overlay: thanh máu player | ⬜ |
| 6 | Overlay: số level hiện tại | ⬜ |
| 6 | Joystick ảo điều khiển di chuyển | ⬜ |
| 6 | Nút dùng kỹ năng (đánh thường, bom, dash) | ⬜ |
| 6 | Hiển thị cooldown trên nút kỹ năng | ⬜ |
| 6 | Thanh máu từng con quái, gắn trên đầu (**world space**) | ⬜ |

### 7. Tiêu chí chấm

| Tiêu chí | Tỷ trọng | Cách đáp ứng | Trạng thái |
|---|---|---|---|
| Game logic chạy đúng | 50% | Mọi damage đi qua `CombatMath`; charge/CD/AI/wave/EXP theo đúng spec | ⬜ |
| Config tách rõ, dễ tuning | 20% | Toàn bộ chỉ số nằm trong ScriptableObject (`_Project/Configs/`), 0 magic number trong code | ⬜ |
| Tối ưu code | 10% | Pool đạn/bom/quái/VFX/popup; `OverlapSphereNonAlloc` + buffer; UI event-driven | ⬜ |
| Dễ đọc, dễ maintain | 10% | Tách trách nhiệm theo folder, không God-class, tên rõ nghĩa | ⬜ |
| Dễ mở rộng | 10% | Thêm skill/quái/chỉ số = tạo asset mới, không sửa logic cũ | ⬜ |

### 8. Bonus (chỉ làm sau khi phần bắt buộc xong)

| Hạng mục | Trạng thái |
|---|---|
| Camera shake (bắn, dash, nhận damage) | ⬜ |
| VFX nổ / đạn / hit / lên cấp | ⬜ |
| Âm thanh SFX | ⬜ |

### 9. Nộp bài

| Hạng mục | Trạng thái |
|---|---|
| Repo GitHub chứa project Unity | ⬜ |
| README nộp bài (mở scene, điều khiển, đã/chưa làm, Unity version) | ⬜ |
| Video gameplay | ⬜ |
| Build Windows | ⬜ |
| Build Android APK | ⬜ |
| Scene chính Play được ngay, 0 lỗi compile | ⬜ |

### Ngoài spec (tự thêm)

| Hạng mục | Trạng thái |
|---|---|
| Damage popup số nổi (verify công thức bằng mắt) | ⬜ |
| Debug/Cheat panel (kill all, +EXP, god mode, skip wave) | ⬜ |
| Unit test EditMode (công thức, charge, poison, EXP) | ⬜ |
| Scene HomeMenu | ⬜ |
| Refactor bố cục ThirdParty | ⬜ |

---

## 2. Nhật ký

### 13/08/2026 — Ngày 0: Setup

- Khảo sát project: Unity 2022.3.62f3, Built-in RP, compile sạch, Android + Windows Build Support đã cài.
- Xác định tài sản sẵn có: `nframework` (Pool, StateMachine, ObservableValue, UIManager, SafeArea, SoundSO, DOTween, attributes), `Joystick Pack`, `JMO Assets / Cartoon FX Remaster`.
- Chốt kiến trúc và lịch thi công 4 ngày.
- Cài **Cinemachine 2.9.7** (follow có damping + Impulse dùng cho camera shake ở phần bonus).
- **Player Settings:** orientation Landscape Left/Right (khoá portrait), Color Space **Linear**, Android Vulkan + OpenGLES3, minSdk 24, IL2CPP ARM64, target 60 FPS.
- **Layers:** `Player`(8) `Enemy`(9) `PlayerProjectile`(10) `EnemyProjectile`(11) `Ground`(12) `Wall`(13). Collision Matrix chỉ bật đúng các cặp cần thiết (`PlayerProjectile↔Enemy`, `EnemyProjectile↔Player`, `Player↔Enemy`, `Enemy↔Enemy`, và va chạm với Wall/Ground).
- Tạo cây thư mục `Assets/_Project/`, import assets CC0 (~89 MB) vào `_Project/Art` + `_Project/Audio`.
- Tạo scene chính `Assets/_Project/Scenes/Game.unity` (arena 60×60 + 4 tường, player capsule có mũi tên chỉ forward, Cinemachine vcam offset `(0, 11, −7)` pitch 57.5° FOV 45 damping 0.6). Đặt làm scene 0 trong Build Settings. Xoá `SampleScene` cũ.

#### Ghi chú kỹ thuật ngày 0

**1. Không dùng Assembly Definition (asmdef).**
`nframework` và DOTween không có asmdef → chúng nằm trong `Assembly-CSharp`. Unity **không cho phép** một asmdef tham chiếu tới `Assembly-CSharp`, nên nếu tạo asmdef cho code gameplay thì sẽ không dùng được `Pool`, `StateMachine`, `ObservableValue`, `UIManager`… của nframework. Tạo asmdef cho cả nframework + DOTween sẽ kéo theo một chuỗi phụ thuộc dễ vỡ, không đáng đánh đổi với deadline 4 ngày.
→ **Giải pháp:** dùng **namespace** (`Survival.Combat`, `Survival.Enemies`, …) + cấu trúc thư mục để tổ chức code. Unit test EditMode sẽ đặt trong thư mục `Editor` để nằm trong `Assembly-CSharp-Editor` — assembly này *được phép* thấy `Assembly-CSharp`.

**2. Rig & animation — đã xử lý một cái bẫy.**
Bộ *"KayKit Character Animations 1.2"* dùng rig **PrototypePete** (6 xương, nhân vật không chân), **không tương thích** với KayKit Adventurers. Import theo bộ này ra **0 animation clip**.
Animation đúng nằm trong chính pack Adventurers/Skeletons: `Animations/fbx/Rig_Medium/Rig_Medium_General.fbx` + `Rig_Medium_MovementBasic.fbx`.
Đã verify: `Rogue_Hooded` và `Skeleton_Warrior` đều cho **Avatar Humanoid hợp lệ** → dùng chung một bộ clip qua retargeting.

Clip hiện có: `Idle_A/B` `Walking_A/B/C` `Running_A/B` `Hit_A/B` `Death_A/B` `Throw` `Spawn_Air/Ground` `Interact` `PickUp` `Use_Item` `Jump_*`.
**Còn thiếu: animation tấn công / bắn / né.** Cần tải thêm pack **KayKit – Character Animations** (bản mới, cho `Rig_Medium`): https://kaylousberg.itch.io/kaykit-character-animations — CC0, có Melee combat + Ranged combat (shooting/spellcasting) + Movement (dodge).
