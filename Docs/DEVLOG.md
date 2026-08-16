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
| 2.1 | Máu (HP) khởi đầu | 500 | ✅ |
| 2.1 | Tốc độ di chuyển | 2 unit/s | ⚠️ **đang để 3.2** — cố ý tune, xem ghi chú dưới |
| 2.1 | Tốc độ xoay | 180 °/s | ⚠️ **đang để 360** — đo được đổi hướng 180° mất 0.50 s |
| 2.1 | Giáp | 0 | ✅ |
| 2.1 | Damage Multiplier | 0 | ✅ |
| 2.1 | Bắn/bom/dash dùng **forward hiện tại**, không theo joystick | — | ✅ |
| 2.2 | `Sát thương nhận = Sát thương gốc − Giáp`, clamp ≥ 0 | — | ✅ |
| 2.2 | `Sát thương gây ra = gốc × (1 + DamageMultiplier)` | — | ✅ |
| 2.2 | Giáp áp dụng cho **cả đòn chém lẫn độc** | — | ✅ |

> ⚠️ **Ba chỗ cố ý lệch spec, đã được nhà tuyển dụng cho phép tune và người chơi chốt giữ nguyên
> ngày 15/08/2026.** Ghi ra đây để không ai hiểu nhầm là làm sai, và **phải nhắc lại trong README
> nộp bài**. (Chỗ thứ ba là vùng nổ của Dash, xem mục 3.3 bên dưới.)
>
> Lý do: spec cho player 2 unit/s trong khi quái cận chiến chạy 3 unit/s — chênh lệch đó khiến
> người chơi **không bao giờ thoát khỏi quái bằng cách chạy**, mọi tình huống đều phải dùng Dash,
> và ván chơi mất hẳn phần di chuyển né tránh. Nâng lên 3.2 thì chạy vẫn không nhanh hơn quái
> nhiều, nhưng đủ để kéo giãn khoảng cách trong lúc quái đứng nghỉ 1 giây.
> Tốc độ xoay nâng theo cho tương xứng, vì chạy nhanh hơn mà xoay vẫn chậm thì điều khiển bị ì.
>
> Cả hai đều nằm trong `PlayerConfig.asset`, **đổi lại về đúng spec chỉ mất hai ô Inspector**,
> không phải sửa một dòng code nào.

### 3. Kỹ năng nhân vật

| # | Yêu cầu | Giá trị spec | Trạng thái |
|---|---|---|---|
| 3.1 | Bắn 3 viên hình nón −15° / 0° / +15° | — | ✅ |
| 3.1 | Sát thương gốc mỗi viên | 10 | ✅ |
| 3.1 | Tối đa 3 charge, mỗi phát tốn 1 | 3 | ✅ |
| 3.1 | Hồi +1 charge / 3 s (chỉ khi chưa đầy) | 3 s | ✅ |
| 3.1 | Giãn cách tối thiểu 2 phát bắn (chống spam) | 0.5 s | ✅ |
| 3.1 | Không đủ charge → không bắn được | — | ✅ |
| 3.2 | Bom: nổ sau 2 s, 50 dmg gốc, bán kính 5 unit | — | ✅ |
| 3.2 | Bom cooldown | 12 s | ✅ |
| 3.3 | Dash: 3 unit trong 0.5 s theo forward | — | ✅ |
| 3.3 | Hết lướt nổ 15 dmg gốc, bán kính 3 unit | — | ⚠️ **15 dmg và bán kính 3 giữ nguyên**, nhưng nổ ở **4 điểm dọc đường lướt** thay vì 1 điểm ở cuối. Mỗi con vẫn chỉ ăn **đúng một lần**. Xem ghi chú dưới |
| 3.3 | Dash cooldown | 6 s | ✅ |

> ⚠️ **Vùng nổ của Dash rải dọc đường lướt thay vì dồn vào điểm cuối.**
>
> Sát thương **không đổi**: vẫn 15 gốc, bán kính 3, và **mỗi kẻ địch chỉ ăn đúng một lần** —
> có danh sách chống trùng trong `AreaDamage.ExplodeMultiPoint` để bảo đảm điều đó, vì bốn vùng
> nổ đặt cách nhau khoảng 1 unit mà bán kính mỗi vùng là 3 nên chúng chồng lên nhau rất nhiều.
>
> Lý do đổi là **hình học của chính kỹ năng này**, không phải để làm nó mạnh hơn. Dash tồn tại
> để CHẠY KHỎI đám quái, nên tới lúc nổ ở điểm cuối thì mấy con đang bám sau lưng đã ra ngoài tầm.
> Đo được trong Play mode:
>
> | Tình huống | Trước | Sau |
> |---|---|---|
> | Quái áp sát 1.3 unit, player lướt 3 unit, quái phía sau kết thúc ở 4.3 unit | dính **2/6** | dính **4/4** |
> | Con nào ăn quá một lần | — | **0** |
> | Sát thương mỗi con | 15 | **15** |
>
> Đặt `_trailBombCount = 0` trên `Skill_Dash.asset` là quay về **đúng spec gốc** — một vụ nổ duy
> nhất ở điểm kết thúc — mà không phải sửa một dòng code nào.

### 4. Kẻ địch

| # | Yêu cầu | Giá trị spec | Trạng thái |
|---|---|---|---|
| 4.1 | Quái cận chiến — Máu | 220 | ✅ |
| 4.1 | Tốc độ di chuyển | 3 unit/s | ✅ |
| 4.1 | Tấn công cone 50°, tầm 1.3 unit, 30 dmg gốc, 1 lần/đòn | — | ✅ |
| 4.1 | Behavior: tiếp cận → tấn công → đứng im 1 s → lặp | — | ✅ |
| 4.2 | Quái tầm xa — Máu | 180 | ✅ |
| 4.2 | Tốc độ di chuyển | 2.7 unit/s | ✅ |
| 4.2 | Tầm tiếp cận để bắn | 3 unit | ✅ |
| 4.2 | Đạn độc bay thẳng, tối đa 5 unit, tốc độ 10 unit/s | — | ✅ |
| 4.2 | Độc: 30 dmg gốc/s, tick ngay lúc trúng, kéo dài 3 s | — | ✅ |
| 4.2 | Tổng **4 tick** (t=0,1,2,3) | 4 | ✅ |
| 4.2 | Refresh: dính lại → reset thời gian, **không stack** damage | — | ✅ |
| 4.2 | Behavior: tiếp cận 3 unit → bắn → đứng im 1 s → lặp | — | ✅ |

### 5. Wave, kinh nghiệm, lên cấp

| # | Yêu cầu | Giá trị spec | Trạng thái |
|---|---|---|---|
| 5 | Mỗi wave spawn ngẫu nhiên 3–4 quái cận chiến | 3–4 | ✅ |
| 5 | + 1–2 quái tầm xa | 1–2 | ✅ |
| 5 | Chỉ spawn wave kế khi **clear hết** wave hiện tại | — | ✅ |
| 5 | Giết 1 quái → +30 EXP | 30 | ✅ |
| 5 | Đủ 100 EXP → lên 1 cấp, **EXP dư giữ lại** | 100 | ✅ |
| 5 | Lên cấp: +40 máu hiện tại, +40 máu tối đa | +40 / +40 | ✅ |
| 5 | Lên cấp: +2 giáp, +0.1 Damage Multiplier | +2 / +0.1 | ✅ |

### 6. UI bắt buộc

| # | Yêu cầu | Trạng thái |
|---|---|---|
| 6 | Overlay: thanh máu player | ✅ |
| 6 | Overlay: số level hiện tại | ✅ |
| 6 | Joystick ảo điều khiển di chuyển | ✅ |
| 6 | Nút dùng kỹ năng (đánh thường, bom, dash) | ✅ |
| 6 | Hiển thị cooldown trên nút kỹ năng | ✅ |
| 6 | Thanh máu từng con quái, gắn trên đầu (**world space**) | ✅ |

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
| Camera shake | ✅ Cinemachine Impulse. **Cố ý chỉ rung ở bom nổ và ăn đòn**, không rung khi bắn và dash — đánh thường bắn mỗi 0.5 s, rung theo từng phát thì màn hình không lúc nào đứng yên. Cả hai vẫn có sẵn dây nối, bật lại chỉ cần đổi một số |
| VFX nổ / đạn / hit / lên cấp | ✅ 8 hiệu ứng, tất cả qua pool. Nổ 20 cái liền: pool tự nở 12→20, sau 5 giây cả 20 tự trả về |
| Âm thanh SFX | ✅ 11 tiếng, gom trong một asset `GameAudio.asset` |

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
| **Bình hồi máu rơi trên sân** (10 s · tối đa 3 · hồi 75) | ✅ Sinh trong tầm nhìn camera, chữ thập đỏ phát sáng |
| Debug/Cheat panel (kill all, +EXP, god mode, skip wave) | ⬜ |
| Unit test EditMode (công thức, charge, poison, EXP) | ⬜ |
| **Animation chết cho player và quái** | ⬜ Ngày 3 (art) |
| **Shader tan biến cho quái sau khi animation chết xong** | ⬜ Ngày 3–4 |
| **Dựng bản đồ bằng công cụ Editor** (`ArenaDresser`, `GroundTextureGenerator`) | ✅ 6304 vật, 867 vật cản có collider |
| **Quái biết đi vòng qua cây và đá** (NavMesh chỉ dùng để hỏi đường) | ✅ 0/360 điểm sinh bị kẹt |
| Scene HomeMenu | ⬜ |
| Refactor bố cục ThirdParty | ⬜ |

---

## 3. Bài học quy trình (rút ra ngày 13/8)

**Mọi lỗi người chơi phát hiện đều nằm ở khe hở giữa "dữ liệu đúng" và "hình ảnh đúng".**

| Lỗi | Đã kiểm chứng | Bỏ sót |
|---|---|---|
| Mũi tên dài 7.5 mm | sát thương = 10 ✓ | nhìn không thấy |
| Nút bắn hiện sai đồng hồ | logic đúng spec ✓ | UI nói ngược lại luật chơi |
| Số charge bị icon che | đếm 3→2→1 ✓ | chữ vàng trên icon trắng |
| Thanh máu không tụt | `fillAmount = 0.820` ✓ | `Image` thiếu sprite nên bỏ qua fillAmount |
| Quái spawn đè lên player | 200/200 điểm spawn ngoài camera ✓ | object trong pool nằm ở gốc toạ độ |

**Nguyên nhân sâu xa:** ảnh chụp qua công cụ không lấy được canvas ở chế độ `Screen Space - Overlay`, nên không tự nhìn thấy UI để kiểm tra.

**Đã khắc phục:** chuyển `HUDCanvas` sang `Screen Space - Camera`. Từ đó ảnh chụp lấy được toàn bộ UI, và lỗi hình ảnh được phát hiện ngay trong lúc làm thay vì phải chờ người chơi báo.

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

**1. Không dùng Assembly Definition (asmdef) cho code gameplay.**

> _Đính chính:_ ghi chú đầu tiên của tôi nói "nframework không có asmdef" — **sai**. nframework thực tế có `NFramework.Runtime.asmdef` (với `autoReferenced: true`, nên `Assembly-CSharp` dùng được nó bình thường). Kết luận không đổi, nhưng lý do thì khác:

**DOTween không có asmdef** → nó nằm trong `Assembly-CSharp`. Unity **không cho phép** một asmdef tham chiếu ngược tới `Assembly-CSharp`. Nên nếu tạo asmdef cho code gameplay thì mất quyền dùng DOTween (dự định dùng cho damage popup, tween UI, feedback lên cấp).
→ **Giải pháp:** dùng **namespace** (`Survival.Combat`, `Survival.Enemies`, …) + cấu trúc thư mục để tổ chức code. Unit test EditMode đặt trong thư mục `Editor` để nằm trong `Assembly-CSharp-Editor` — assembly này *được phép* thấy `Assembly-CSharp`.
→ Nếu tới Ngày 4 việc này gây vướng, DOTween có sẵn nút tạo asmdef (Tools > Demigiant > DOTween Utility Panel) để chuyển sang phương án asmdef đầy đủ.

**2. Rig & animation — đã xử lý một cái bẫy.**
Bộ *"KayKit Character Animations 1.2"* dùng rig **PrototypePete** (6 xương, nhân vật không chân), **không tương thích** với KayKit Adventurers. Import theo bộ này ra **0 animation clip**.
Animation đúng nằm trong chính pack Adventurers/Skeletons: `Animations/fbx/Rig_Medium/Rig_Medium_General.fbx` + `Rig_Medium_MovementBasic.fbx`.
Đã verify: `Rogue_Hooded` và `Skeleton_Warrior` đều cho **Avatar Humanoid hợp lệ** → dùng chung một bộ clip qua retargeting.

Đã bổ sung pack **KayKit – Character Animations 1.1** (https://kaylousberg.itch.io/kaykit-character-animations, CC0). Bộ clip cuối cùng dùng cho game:

| Vai trò | Clip | Độ dài |
|---|---|---|
| Player đứng yên | `Idle_A` | 1.07s (loop) |
| Player chạy | `Running_HoldingBow` | 0.80s (loop) |
| Player bắn | `Ranged_1H_Shoot` | 1.07s |
| Player dash | `Dodge_Forward` | 0.40s |
| Player ném bom | `Throw` | 1.37s |
| Quái cận chiến chém | `Melee_1H_Attack_Slice_Diagonal` | 1.00s |
| Quái tầm xa niệm chú | `Ranged_Magic_Shoot` | 0.93s |
| Trúng đòn / chết | `Hit_A` / `Death_A` | 0.67s / 0.80s |

---

### 13/08/2026 — Ngày 1: Nền móng + Player + Quái cận chiến

**Đã viết** (tất cả nằm trong `Assets/_Project/Scripts/`):

| Nhóm | File | Vai trò |
|---|---|---|
| Stats | `EStatType`, `StatModifier`, `StatSet`, `IStatProvider` | Chỉ số lưu trong mảng float đánh chỉ số bằng enum (tránh boxing của Dictionary) |
| Combat | `CombatMath`, `DamageInfo`, `IDamageable`, `Health` | Một cửa duy nhất trừ máu, luôn qua công thức spec |
| Pooling | `PoolService`, `PoolConfigSO` | Bọc `NFramework.Pool`, tra cứu theo prefab, prewarm |
| Player | `PlayerActor`, `PlayerMotor`, `PlayerInputRouter`, `KeyboardSkillInput` | Xoay đúng 180°/s bằng `Quaternion.RotateTowards` |
| Skills | `SkillDefinition`, `SkillRuntime`, `SkillContext`, `ChargedShootSkillSO` | Đủ 6 luật mục 3.1 |
| Projectiles | `ProjectileBase`, `IProjectileEffect` | `SphereCastNonAlloc` quét giữa 2 khung hình, chống đạn xuyên |
| Enemies | `EnemyActor`, `EnemyRegistry`, `EnemyConfigSO`, `ConeMeleeAttack`, 3 state | AI dùng `NFramework.StateMachine` |

**Kết quả đo được khi chạy thật (Play mode):**

| Kiểm chứng | Kết quả |
|---|---|
| Chỉ số player nạp từ config | 500 / 2 / 180 / 0 / 0 — đúng spec |
| Chỉ số quái nạp từ config | 220 / 3 / 360 / 0 — đúng spec |
| Một loạt bắn ra mấy viên | **3 viên**, góc đo được **−15.0° / 0.0° / +15.0°** |
| Charge sau 1 phát | 3 → 2 |
| Bắn phát 2 ngay lập tức | **Bị chặn**, và **không bị trừ oan charge** |
| Mũi tên trúng bia | trừ đúng **10** máu (10 × (1+0) − 0 giáp) |
| Quái đánh player 9 đòn liên tiếp | mất đúng **270** máu = 9 × 30 |
| Số mũi tên để giết quái | **22** viên = 220 ÷ 10 |
| Sự kiện `OnEnemyDied` / `OnAllEnemiesCleared` | Đều bắn đúng → hệ thống wave nối vào được |

**Lỗi tìm được và đã sửa trong ngày:**
1. `SkillRuntime.TryUse` không tự bắt đầu cooldown → skill con viết thiếu một dòng sẽ chạy bình thường nhưng **không hồi chiêu**, không crash, không cảnh báo. Đã chuyển lên lớp cha.
2. Thanh máu sẽ vẽ sai sau khi lên cấp vì máu tối đa đổi mà máu hiện tại thì không → thêm `IStatProvider.OnStatChanged` và `Health.OnMaxChanged`.
3. `PlayerConfigSO` / `EnemyConfigSO` chưa kiểm tra thiếu chỉ số → xoá nhầm một dòng làm `MoveSpeed` về 0 (nhân vật đứng im) mà không báo gì. Đã thêm `OnValidate`.
4. `Health` sinh ra với 0 máu và `IsAlive = false` nếu không ai gọi `Initialize` → mọi đòn đánh vào nó bị bỏ qua âm thầm. Đã thêm tự khởi tạo trong `Awake`.

---

### 14/08/2026 — Dựng bản đồ

Người chơi báo map trống trải, "hoang sơ quá, không giống một map game". Đi xem lại
video tham chiếu và cách các game cùng thể loại dựng cảnh, rút ra ba khác biệt:
nền đất có **mảng màu lớn** chứ không phải một sắc xanh đều; cỏ mọc **thành đám**
chứ không rải đều; và có **mốc thị giác** (đá to, lối mòn) để mắt bám vào khi di chuyển.

**Hai công cụ Editor** (`Assets/_Project/Scripts/Editor/`) — dựng bằng code chứ không
kéo thả tay, để chạy lại lúc nào cũng ra kết quả như nhau:

| Công cụ | Menu | Việc |
|---|---|---|
| `GroundTextureGenerator` | `Survival > Generate Ground Texture` | Sinh texture nền 1024² lát liền mạch |
| `ArenaDresser` | `Survival > Dress Arena` | Rải 3150 vật trang trí theo 3 lớp |

**Texture nền.** `Grass.png` đi kèm bộ Stylized Nature không phải texture mặt đất mà là
một *bảng màu dạng dải* để model low-poly tra màu qua UV — lát ra làm nền thì thành sọc
ngang màn hình. Thay bằng nhiễu Perlin tự sinh, lấy mẫu **quanh một vòng tròn** thay vì
theo đường thẳng nên mép trái nối khít mép phải, lát không thấy đường ghép. Ba tầng nhiễu:
tần số 1.6 quyết định mảng cỏ lớn, 5 phá biên cho đỡ tròn trịa, 26 tạo lấm tấm; cộng một
tầng vệt đất **độc lập** — nếu dùng chung tầng thì đất luôn rơi đúng chỗ cỏ sẫm nhất,
thành quy luật đều đặn mà mắt nhận ra ngay là máy sinh.

**Ba luật giữ cho sân vẫn đánh nhau được** (đây mới là phần quan trọng, không phải phần đẹp):

1. **Gỡ sạch collider** của mọi vật trang trí. Quái đi thẳng tới player chứ không qua
   NavMesh, nên chỉ một cục đá có collider là chúng kẹt cứng tại chỗ.
2. **Không vật nào trong vùng chơi cao quá 0.65 unit**, so với nhân vật cao 1.4 —
   cây cỏ không bao giờ che mất quái đang lao tới.
3. **Cây to chỉ mọc từ bán kính 21 trở ra.** Vùng chiến đấu để trống hoàn toàn.

Nới mặt đất từ 60×60 lên 110×110 để mép vuông của plane không bao giờ lọt vào khung hình.

**Kết quả đo được:** 3150 vật · **0 collider** · 620 cây xanh / 62 cây lá đỏ (chỉ 10%, làm
điểm nhấn) / 55 cây khô · **16.0 ms mỗi khung hình (~62 FPS)** · không vật nào lọt ra ngoài
mặt đất.

**Lỗi tìm được trong lúc dựng:**
1. Bộ lọc tên dùng tiền tố `"PineTree_"` nhưng file thật tên `Pine_` → **toàn bộ cây thông
   bị bỏ qua âm thầm**, không có lỗi nào báo ra. Rừng thiếu hẳn một loại cây mà không biết.
2. Cảnh ngả sang đỏ như mùa thu. Đọc thẳng pixel `Leaves_TwistedTree_C.png` ra RGB(167,23,23):
   `TwistedTree_` là cây **lá đỏ**, không phải cây xanh. Tách thành danh sách điểm nhấn riêng, dùng 10%.
3. Cỏ và đá cao 2–2.9 unit, **cao hơn cả nhân vật 1.4 unit**. Đã chỉnh tỉ lệ theo luật số 2 ở trên.

**Bài học tự rút ra:** tôi nhiều lần kết luận nền đất "bị rửa trôi, nhạt màu" khi nhìn ảnh
chụp màn hình, nhưng giải mã file PNG ra thì pixel thật là RGB(29,49,13) — xanh đậm và bão hoà.
**Ảnh chụp hiển thị lại không trung thực về màu.** Từ đây mọi quyết định về màu đều đo pixel
chứ không tin mắt nhìn qua ảnh.

---

### 15/08/2026 — Quái biết đi vòng qua vật cản (NavMesh)

Người chơi yêu cầu **không ai được đi xuyên cây**, và sân được mở rộng thành rừng vào được.
Hai yêu cầu đó cộng lại làm hỏng luật cũ ở mục trên: khi cây có collider thật, cách "quái
đi thẳng tới player" không còn dùng được nữa — con quái nào có gốc cây chắn giữa là ép mặt
vào thân cây tới hết ván.

**Cách chọn: NavMesh chỉ để HỎI ĐƯỜNG, không để DI CHUYỂN.**
Không dùng `NavMeshAgent`. Agent tự quản luôn cả tốc độ, gia tốc, xoay và né nhau, nên
những con số spec bắt buộc (tốc độ 3.0, xoay 360°/s) sẽ bị nó ghi đè và không còn tune được
trên Inspector nữa — mất điểm đúng vào tiêu chí "config dễ tuning" chiếm 20% barem.
Ở đây chỉ gọi `NavMesh.CalculatePath` để xin danh sách khúc quanh, còn việc đẩy thân vẫn do
`Rigidbody` trong code cũ lo. Spec giữ nguyên, mà quái vẫn biết đường vòng.

**Ba mức quyết định mỗi khung hình**, xếp từ rẻ tới đắt để không tốn phép tính vô ích:

| Tình huống | Cách xử lý | Chi phí |
|---|---|---|
| Đường thẳng trống | đi thẳng | gần như 0 |
| Có vật chắn hoặc đang kẹt | bám các khúc quanh của NavMesh | 1 lần hỏi đường mỗi 0.25 s |
| NavMesh cũng không ra đường | lái tránh theo cảm biến như cũ | phương án chót |

Đo trong phiên chơi thật: chỉ **6% số khung hình** cần tới mức thứ hai.

**Hai lỗi phải lần ra mới chạy được:**

1. **Tia dò bắt đầu từ BÊN TRONG một collider thì báo là không chạm gì cả.** Con quái đang
   ép sát gốc cây, tia dò xuất phát từ trong thân nó nên trả về "phía trước trống trơn" —
   nó kết luận đi thẳng được và đứng ì tại chỗ. Sửa bằng cách nhận biết kẹt qua **quãng
   đường thực sự đi được**: muốn đi mà 0.25 s không nhúc nhích quá một ngưỡng thì ép sang
   chế độ đi vòng trong 2.5 s. Đây là lần thứ hai đúng cái bẫy này xuất hiện trong dự án
   (lần đầu là mũi tên bắn ra từ trong người player), nên đã ghi lại thành luật.

2. **`isStatic = true` bật LUÔN cả cờ Navigation Static.** Mà lệnh bake NavMesh của Unity
   đọc **hình học của MeshRenderer chứ không đọc collider** — nên toàn bộ 5437 nhánh cỏ,
   cánh hoa, viên sỏi lát đường đều bị đưa vào bake, mỗi thứ đội mặt lưới lên vài centimet
   và băm nó thành hàng nghìn mảnh vụn rời rạc. Quái đứng trên một mảnh cô lập thì không
   đời nào tính ra đường đi tới người chơi.

   Sửa cờ chữa được mảnh vụn (rừng từ 0% lên 97%), **nhưng chưa phải gốc rễ** — xem dưới.

**Lỗi thứ ba, tìm ra nhờ đo chứ không nhờ nhìn:** sân đã thông nhưng quái vẫn có con đứng
ngoài rìa cả ván. Nguyên nhân nằm ở chỗ khác hẳn — `SpawnPointPicker` chỉ kiểm tra hình học
("chỗ này có trống không") nên vẫn thả quái vào những **hốc không có lối ra**: trống trải,
khuất camera, đủ xa, qua hết mọi bài kiểm tra, mà từ đó không đi tới ai được. Nay điểm sinh
phải qua thêm một câu hỏi cuối: **từ đây có đường đi thông tới người chơi không.**

#### Gốc rễ thật, và vì sao phải bỏ hẳn cửa sổ Navigation của Unity

Sửa cờ tĩnh xong, rừng ngoài vẫn chỉ 84% tới được. Tôi định kết luận "chỗ đó cây vây kín,
người chơi cũng không vào được nên không sao" — nhưng **đo thử thì sai hẳn**: lấy 200 điểm
không tới được, thân người chơi đặt vừa ở **199 điểm**. Tức là người chơi đi vào được mà quái
thì không — **chỗ trốn bất tử**, đúng thứ nhà tuyển dụng thử một lần là ra.

Đo tiếp thì ra nguyên nhân, và nó không nằm ở cờ tĩnh nữa:

> Trên 867 vật cản, **mesh rộng trung bình gấp 7.1 lần collider**.
> Một cây thông thân chặn người trong bán kính 0.15 nhưng tán lá xoè ra 1.12.

Bake của Unity đọc mesh, nên nó cấm cả vùng tán lá mà người chơi đi lọt bên dưới. Đây là
**lệch lạc không thể chữa bằng cách chỉnh cờ** — hai hệ thống đang đọc hai nguồn hình học
khác nhau thì sớm muộn cũng phải lệch.

Nên chuyển sang **nướng mặt lưới từ chính collider** (`NavMeshBuilder.CollectSources` với
`NavMeshCollectGeometry.PhysicsColliders`) — đây là API lõi của Unity, không cần cài thêm gói:

- `Assets/_Project/Scripts/Core/NavMeshProvider.cs` — giữ asset lưới, nạp lúc chạy, và
  chứa toàn bộ thông số bake dưới dạng field trên Inspector.
- `Assets/_Project/Scripts/Editor/NavMeshBaker.cs` — menu `Survival > Bake NavMesh`.

Ba cái lợi, theo thứ tự quan trọng:
1. **Thứ chặn người chơi và thứ chặn đường đi của quái nay là MỘT.** Không còn khả năng lệch.
2. Cỏ hoa sỏi vốn không có collider nên **tự động bị bỏ qua** — không cần đánh dấu gì cả,
   và `ArenaDresser` gỡ bỏ được hẳn phần xử lý cờ Navigation Static.
3. Bake đọc **872 collider thay vì 6304 mesh**, nhanh hơn hẳn.

Cũng chỉnh **bán kính thân dùng để bake từ 0.40 xuống 0.32** cho khớp đúng collider thật của
player và của quái. Để lớn hơn là tự tay tạo ra những khe "người chui lọt mà lưới coi là tường".

**Số đo qua ba lần sửa:**

| Phép đo | Ban đầu | Sửa cờ tĩnh | Bake từ collider |
|---|---|---|---|
| Sân trống tới được từ giữa sân | 60% | 100% | **100%** |
| Rừng trong tới được | 0% | 97% | **100%** |
| Rừng ngoài tới được | 0% | 84% | **100%** |
| Chỗ trốn bất tử (trên 4000 chỗ player đứng được) | — | 199/200 mẫu | **0** |
| Điểm sinh quái mà quái không tới được player | 35% | 0/360 | **0/360** |
| — riêng khi player đứng sâu trong rừng | 78% | 0% | **0%** |
| Nguồn hình học đưa vào bake | 6304 mesh | 6304 mesh | **872 collider** |

**Hai bài chạy thật trong Play mode:**
- Player và quái hai bên một gốc đá, đường thẳng bị chặn sau 1.43/5.30 unit → quái đi vòng
  (toạ độ z lệch khỏi đường thẳng) và vào tầm đánh sau **3.0 giây**.
- Quái thả cách 21.96 unit xuyên rừng, đường vòng dài 22.25 unit → vào tầm đánh sau
  **7.3 giây**, trong khi lý thuyết ở tốc độ 3.0 là 7.4 giây. Tức là **không mất một giây nào
  vào việc kẹt**.

**Sửa kèm — ba cảnh báo `CS0114` hoá ra là bug thật.** `GameSession`, `WaveManager`,
`ExperienceSystem` đều khai báo `private void OnDestroy()` che mất `OnDestroy` của
`SingletonMono`. Unity chỉ gọi bản của lớp con, nên **biến static trỏ tới thể hiện singleton
không bao giờ được xoá** — chơi lại là nó trỏ vào một đối tượng đã bị huỷ. Đổi thành
`protected override` và gọi `base.OnDestroy()`. Bài học: cảnh báo trình biên dịch không phải
tiếng ồn, ba cái này im lặng suốt mấy ngày mà bên dưới là một lỗi vòng đời thật.

> ⚠️ **Dựng lại bản đồ thì phải bake lại mặt lưới.**
> Chạy `Survival > Dress Arena` xong thì chạy tiếp `Survival > Bake NavMesh`.
> Bỏ bước hai thì mặt lưới vẫn là của bản đồ cũ và quái sẽ đi vòng qua những cái cây
> không còn tồn tại.

---

---

### 15/08/2026 — Quái nghỉ sau đòn đánh không còn bị xô đẩy

Spec bắt quái "tấn công xong thì đứng im 1 giây". Nhưng đứng im theo nghĩa *không tự đi* thì
chưa đủ: nó vẫn là một thân vật lý động, nên mấy con phía sau đang lao tới húc vào và **đẩy nó
trượt tới trước**. Quãng nghỉ một giây — vốn là khoảng thở duy nhất của người chơi, vì quái
chạy 3.0 còn player chỉ 2.0 theo spec — biến mất, và cả đàn nhìn như dồn cục.

**Luật chọn: ghim khi KHÔNG ở trạng thái tiếp cận.** `Approach` là trạng thái duy nhất có di
chuyển, nên nó cũng là chỗ duy nhất cần thân vật lý tự do. Đặt `SetAnchored(false)` ở
`Approach.OnEnter` và `SetAnchored(true)` ở `Attack.OnEnter` + `Idle.OnEnter` — luật ở hai đầu
nên không có đường nào lọt, dù quái vào tiếp cận từ lúc mới sinh, sau một đòn, hay sau khi mất
mục tiêu.

**Ghim từ lúc bắt đầu vung đòn chứ không chỉ trong một giây nghỉ.** Lý do là độ chính xác của
đòn đánh: sát thương là một hình nón xuất phát từ vị trí quái *tại đúng thời điểm ra đòn*. Bị xô
lệch trong 0.5 giây lấy đà là hình nón đó xuất phát từ chỗ khác, và cú đánh trượt vì một lý do
chẳng liên quan gì tới người chơi.

**Dùng ràng buộc trục chứ không chuyển sang kinematic.** Kinematic đổi hẳn loại thân vật lý giữa
chừng, kéo theo cả họ lỗi "không được ghi vận tốc lên thân kinematic" mà dự án này đã dính hai
lần. Khoá trục thì thân vẫn động bình thường: vẫn chặn người chơi và chặn quái khác đúng như một
tảng đá, chỉ là không bị đẩy đi.

**Cái bẫy phải chặn trước:** quái có thể **chết ngay giữa lúc đang bị ghim**, rồi được lấy lại
từ pool cho wave sau. Không trả ràng buộc về mặc định trong `Setup` thì kiếp sau nó sinh ra đã
bị khoá cứng hai trục ngang — đứng chôn chân tại chỗ sinh cho tới hết ván. Đây đúng là loại lỗi
mà `Setup` đã có sẵn cả một khối chú thích cảnh báo.

**Đo được:** trôi đi trong lúc đang đánh/nghỉ **0.340 unit** cộng dồn trên **60.6 giây** đứng
yên của cả 6 con, tức khoảng 0.006 unit mỗi giây — nhiễu số học, coi như bằng không.
Tác dụng phụ đáng chú ý: player đứng yên giữa 6 con giờ ăn **1260 sát thương / 15 giây** thay vì
900. Không phải game bị làm khó thêm, mà là các đòn đánh nay **trúng đúng như spec** thay vì bị
xô văng ra khỏi tầm giữa chừng.

---

### 15/08/2026 — Quái không còn đánh hụt khi người chơi đang chạy

Người chơi báo quái hay vung vào chỗ trống. Tôi kiểm git trước khi kết luận, vì tôi vừa thêm
phần ghim và nghi chính mình gây ra: nhưng bản trước đó (`95d3cc6`) đã có `StopMoving()` ngay ở
`OnEnter` và chỉ gọi `RotateTowardsTarget`, **chưa bao giờ có `MoveTowardsTarget`**. Vậy quái
đứng chôn chân suốt đòn đánh là hành vi có từ commit đầu tiên, không phải do phần ghim.

**Gốc rễ là số học chứ không phải AI:**

> Lấy đà mất **0.5 giây**. Player chạy **3.2 unit/giây** → đi được **1.6 unit** trong lúc quái
> đứng yên. Tầm đánh chỉ **1.3**. Nghĩa là từ khoảnh khắc quái quyết định vung đòn, chỉ cần
> người chơi còn chạy là đòn đó đã thua về khoảng cách.

Đây chính là hệ quả kéo theo của việc nâng tốc độ player lên 3.2 (spec cho 2.0, quái 3.0):
người chơi giờ **nhanh hơn quái**, nên quái vừa hụt vừa không đuổi lại được.

**Cách sửa: cho quái ĐI THEO trong lúc lấy đà**, chứ không chỉ xoay theo. Hai số mới trong
`EnemyConfigSO`, đều chỉnh được trên Inspector:

| Trường | Quái cận chiến | Quái đánh xa | Ý nghĩa |
|---|---|---|---|
| `_windupChaseFactor` | 1.0 | **0** | Bám theo với bao nhiêu phần tốc độ chạy |
| `_windupHoldRangeFactor` | 0.7 | 0.95 | Dừng lại ở bao nhiêu phần tầm đánh |

Quái đánh xa cố ý để **0**: spec bảo nó đứng ở khoảng cách 3 unit mà bắn, và đạn đã bay ra thì
người chơi né được là chuyện công bằng.

Khoảng giữ 0.7 × 1.3 = **0.91 unit** là để quái không ủi thẳng vào người chơi rồi bị collider
chặn lại — trông như hai bên húc đầu vào nhau.

**Đo bằng khoảng cách tại đúng khoảnh khắc ra đòn**, chứ không đếm số đòn trúng — vì đếm đòn bị
nhiễu bởi thời gian quái chạy lại gần giữa hai chu kỳ:

| Hệ số bám | Khoảng cách lúc ra đòn | Tỉ lệ trúng |
|---|---|---|
| **0** (hành vi cũ) | 1.24 – 2.19 | **60%** |
| **1.0** (đã sửa) | 0.85 – 0.87 | **100%** |

**Counterplay vẫn còn nguyên** — đây là điều kiện bắt buộc, không phải điểm cộng. Dash đi 3 unit
trong 0.5 giây tức 6 unit/giây, gấp đôi tốc độ bám của quái. Test bằng cách tự động Dash ngay khi
quái bắt đầu vung đòn: hai đòn rơi vào lúc đang Dash đo được **1.61 và 2.03 unit** → trượt cả
hai, các đòn còn lại 0.87 → trúng. Đúng điểm cân bằng cần giữ: **đi bộ không thoát, Dash thì
thoát.**

**Một chi tiết dễ hiểu nhầm:** trong phiên chơi mà người chơi đứng yên, quãng đường quái đi trong
lúc lấy đà đo được là **0.00 unit**. Đó không phải lỗi — player đứng yên thì quái đã nằm trong
khoảng giữ 0.91 nên không cần nhích. Nó chỉ đuổi khi người chơi thật sự bỏ chạy, nhờ vậy vẫn giữ
được nhịp "quái khựng lại rồi mới đánh" mà người chơi đọc được.

**Phạm vi ghim đổi theo:** thả trong lúc lấy đà (vì lúc đó nó phải bám), ghim từ **đúng khoảnh
khắc ra đòn** cho tới hết một giây nghỉ. Kiểm chứng 45 giây chơi tự nhiên: **0 vi phạm trên 3267
lần kiểm tra**, trôi **0.000 unit** trong 33 giây đứng nghỉ cộng dồn.

---

## 4. Việc còn lại (chốt cuối ngày 15/08/2026)

Thứ tự này do người chơi chốt, không phải tôi tự sắp.

| # | Việc | Ghi chú |
|---|---|---|
| ~~1~~ | ~~Quái đang nghỉ không bị xô đẩy~~ | ✅ Xong — xem mục nhật ký ngay trên |
| 2 | **Bonus mục 8 README** — camera shake, VFX, SFX | Ưu tiên trước hai việc dưới vì đây là mục **có điểm cộng thật**. Cinemachine Impulse; Cartoon FX Remaster đã import sẵn; SFX Kenney |
| 3 | Hiệu ứng báo dính độc trên người player và cạnh thanh máu | Ý thêm, **ngoài spec** |
| 4 | Bình hồi máu spawn định kỳ quanh player (10 s, tối đa 3, hồi 75) | Ý thêm, **ngoài spec** |
| 5 | Cấu hình sẵn Build Windows + Android, viết hướng dẫn | **Không tự build** — người chơi tự bấm. Làm cuối cùng |
| 6 | Scene HomeMenu · refactor thư mục ThirdParty · README nộp bài · video gameplay | Chỉ làm sau khi mọi yêu cầu bắt buộc đã đủ và đúng |

---

### 15/08/2026 — Âm thanh (nốt phần cuối của bonus mục 8)

**Gom cả 11 tiếng vào MỘT asset `Configs/Audio/GameAudio.asset`**, không rải vào từng config.
Lý do: âm thanh không thuộc riêng hệ thống nào, và điều quan trọng nhất khi chỉnh âm là **nghe
chúng cạnh nhau** — tiếng nổ to hay nhỏ chỉ có nghĩa khi so với tiếng bắn. Rải mỗi tiếng vào
config của hệ thống sinh ra nó thì phải mở sáu bảy file mới cân được, và gần như chắc chắn lệch.
Cùng lý lẽ đã dùng khi gom mọi mức rung camera vào một nơi.

**`GameSound` — mỗi tiếng mang theo một khoảng nghỉ tối thiểu.** Đây là phần quan trọng nhất.
Nhiều sự kiện trong game xảy ra thành **chùm** chứ không lẻ tẻ: đánh thường bắn ba viên cùng lúc,
một quả bom nổ trúng năm con, cú lướt nổ bốn điểm liền nhau. Phát đủ từng tiếng thì chúng chồng
lên nhau, biên độ cộng dồn gây vỡ tiếng, và tai nghe ra một tiếng "bụp" méo mó chứ không phải năm
cú đánh. Ngưỡng để riêng từng tiếng vì nhịp tự nhiên khác nhau: bắn 0.08 s cho nhịp mượt, nổ
0.25 s vì không bao giờ nên chồng.

Đo được: gọi `PlayEnemyHurt` **20 lần trong một khung hình → đúng 1 tiếng kêu**; `PlayBombExplode`
8 lần → 1 tiếng.

**`GameAudioService` là cửa duy nhất để phát tiếng.** Mọi nơi gọi một dòng tĩnh, không chỗ nào
trong gameplay giữ tham chiếu tới file âm thanh. Xoá hẳn service đi thì game vẫn chạy đủ luật,
chỉ là im lặng — cùng nguyên tắc "trang trí không nắm quyền quyết định gì của luật chơi" đã theo
từ `PlayerAnimatorDriver`.

**Chọn tiếng:**

| Sự kiện | Clip | Ghi chú |
|---|---|---|
| Bắn nỏ | `metalClick` (RPG Audio) | Tiếng lẫy nỏ. Kenney không có tiếng cung nào sát hơn |
| Nổ bom | `lowFrequency_explosion` | Tiếng to nhất game — bom 50 sát thương, bán kính 5 |
| Nổ dash | `explosionCrunch` | Nhỏ và gọn hơn bom |
| Quái xa bắn | `slime` (Sci-fi) | Nghe đúng chất nhớt độc |
| Quái trúng đòn | `impactSoft_medium` | Thứ trả lời câu hỏi "mũi tên vừa rồi có trúng không" |
| Player ăn đòn | `impactPunch_heavy` | Đi kèm viền đỏ và rung camera |

Tiếng ra đòn của quái phát **đúng lúc gây sát thương**, không phải lúc bắt đầu lấy đà — nhờ vậy
nó là tín hiệu trung thực: nghe thấy tiếng nghĩa là đòn đã ra, né lúc này là muộn. Việc báo sớm
đã có động tác vung tay lo.

Không kêu khi giáp đỡ hết (để phân biệt "đỡ được" với "ăn đủ"), và **không kêu theo từng tick
độc** — độc trừ máu mỗi giây suốt ba giây, kêu theo tick thì một con dính độc là tiếng liên hồi
át hết mọi thứ.

**Một lỗi bắt được nhờ soi console chứ không nhờ nghe:** `UnassignedReferenceException: the
variable _audioMixer of SoundManager has not been assigned`. Âm thanh **vẫn kêu bình thường** nên
nghe thì không phát hiện ra, nhưng console thì đỏ mỗi lần chỉnh âm lượng. Đã gán AudioMixer sẵn
có của nframework cùng hai nhóm Music/SFX — 11/11 kênh nay đều đi qua mixer.

**Bài học lặp lại nhiều lần trong phiên này:** ba lần liên tiếp tôi tưởng code hỏng, cả ba đều là
**phép đo của chính tôi sai** — đọc kênh âm thanh khi 10 kênh đã bận hết; gọi hai bài kiểm tra
trong cùng một khung hình nên ngưỡng chống chồng chặn cả hai; và `player đã chết` nên `TryUseSkill`
trả về false, mà nguyên nhân là **63.9 giây trôi qua giữa hai lệnh MCP** để player đứng yên cho
quái đánh. Con số vô lý thì phải nghi phép đo trước khi nghi code.

---

### 15/08/2026 — Bình hồi máu rơi trên sân (ngoài spec)

`Docs/README.md` không hề nhắc tới vật phẩm hồi máu. Thêm vào vì trong một ván dài, người chơi
mất máu dần mà cách duy nhất hồi lại là lên cấp — càng về sau càng chỉ có một chiều đi xuống.

**Chỗ sinh phải NHÌN THẤY ĐƯỢC — ngược hoàn toàn với chỗ sinh quái.** Quái bắt buộc phải sinh
ngoài khung hình, nếu không người chơi thấy chúng mọc ra từ hư không. Vật phẩm thì rơi ngoài
khung hình nghĩa là người chơi không biết nó tồn tại, và nó không tạo ra quyết định nào — trong
khi cả điểm hay của vật phẩm là buộc người chơi cân nhắc "có đáng rời chỗ an toàn để chạy ra
nhặt không". Hai lớp làm hai việc ngược nhau nên cố tình **không dùng chung** một hàm chọn điểm.

**Nhặt bằng khoảng cách chứ không bằng trigger collider.** Trigger nghe đúng bài hơn nhưng kéo
theo một chuỗi thứ phải khớp: layer của vật phẩm, ô tương ứng trong bảng va chạm, và Rigidbody
ở cả hai bên. Sai một mắt xích thì vật phẩm im lặng không nhặt được mà **không có lỗi nào báo ra**.
Sân chỉ có tối đa ba bình nên so khoảng cách mỗi khung hình là ba phép trừ — rẻ hơn rất nhiều
so với rủi ro đó.

**Màu sắc — người chơi cảnh báo trước là cỏ xanh sẽ nuốt mất vật phẩm, và cảnh báo đó đúng.**
Đo màu nền cỏ ra RGB(73, 128, 44), tức hue 99°; màu đối lập chính xác là tím magenta 279°.
Nhưng map đã có sẵn hoa tím **và** hoa đỏ lấm tấm khắp nơi, nên chọn theo hue thôi không đủ.
Thứ phân biệt tuyệt đối là **PHÁT SÁNG**: trong cả cảnh không có vật nào phát sáng, nên vật liệu
emissive nổi lên bất kể quanh đó có hoa màu gì.

Bản đầu vẫn chưa đủ: chụp hình ra thì chữ thập bằng đúng cỡ mấy bông hoa đỏ, liếc nhanh dễ lẫn.
Đã phóng to 1.6 lần, nâng cao từ 0.55 lên 0.85 để lộ khỏi thảm cỏ, và thêm một **đĩa đỏ dẹt dưới
chân** làm mốc — nhờ nó vẫn tìm ra được ngay cả khi thân chữ thập bị cỏ che.

Xoay chậm và nhấp nhô là **bắt buộc chứ không phải trang trí**: nền là cỏ có hoa lá lấm tấm, một
vật đứng yên rất dễ chìm vào đó, còn chuyển động thì mắt bắt được ngay cả khi đang bận nhìn chỗ khác.
Mỗi bình bốc một pha nhấp nhô ngẫu nhiên, nếu không cả ba nhấp nhô đồng loạt như một.

**Đo được (50 giây không can thiệp):**

| | |
|---|---|
| Số bình trên sân | **3 — đúng giới hạn** |
| Pool | 3 tổng / 3 đang dùng, không rò rỉ |
| Khoảng cách tới player | 4.2 · 6.3 · 7.8 unit (config 4–9) |
| Toạ độ màn hình | x 0.42–0.54 · y 0.65–0.79 — cả ba nằm gọn trong khung hình |
| Hồi máu | **đúng +75** (200 → 275) |

Chỉ sinh khi người chơi **đang thiếu máu** — tắt điều kiện này thì đầu ván, lúc còn đủ máu, sân
đã có sẵn mấy bình vô dụng nằm chờ. Không cho vật phẩm tự biến mất: giới hạn 3 cái đã đủ chống
sân bị rác, mà người chơi cũng không mất phần thưởng oan chỉ vì lúc đó đang bị vây không thoát ra kịp.

---

## Thanh máu & thanh EXP — bo tròn phần tô đầy cho khớp nền

**Triệu chứng người chơi nhìn thấy:** ảnh nền của thanh có bo góc ở hai đầu, nhưng phần tô đầy
lại vuông góc. Lúc máu đầy, bốn góc vuông của phần tô đầy thò hẳn ra ngoài đường bo của nền —
nhìn không giống một lựa chọn thẩm mỹ mà giống một lỗi hiển thị.

**Thuật ngữ để gọi các thành phần này** (ghi lại vì sẽ cần khi trình bày):

| Thành phần | Tên gọi | Trong project |
|---|---|---|
| Ảnh nền tối phía sau | *background* / *track* | `HealthBar_BG`, `ExpBar_BG` — `Sprite_Panel` |
| Phần màu chạy theo chỉ số | *fill* | `HealthBar_Fill`, `ExpBar_Fill` |
| Góc bo tròn | *rounded corner* / *corner radius* | bán kính = nửa chiều cao → hình *viên nang* (**capsule** / **pill**) |
| Cách kéo ảnh mà góc không méo | *9-slice* / *sliced sprite*, viền gọi là *border* | `Sprite_Panel` border 18 |

**Vì sao trước đây cố tình để vuông.** Ảnh nền dùng được 9-slice nên kéo dài bao nhiêu góc vẫn
tròn đều. Phần tô đầy thì không: nó đặt `Image.Type = Filled` để cắt theo tỉ lệ máu, mà
**`Filled` không hỗ trợ 9-slice**. Nên không thể vừa cắt theo phần trăm vừa có góc bo không méo
từ cùng một cơ chế. Bản trước chọn bỏ góc bo.

**Đánh đổi đã chốt lại.** Cái dở lúc máu đầy (luôn nhìn thấy, ngay từ giây đầu ván) nặng hơn cái
dở lúc máu vơi. Nên đổi sang: **đầy máu thì hai đầu bo khít nền, vơi máu thì mép phải là một
đường thẳng đứng** — đúng kiểu thanh máu phổ biến nhất trong game.

**Cách làm.** Thêm `WriteCapsuleFill` vào `RingSpriteGenerator`, vẽ chữ nhật bo tròn hai đầu bằng
hàm khoảng cách có dấu (*signed distance field*) để mép mượt, không răng cưa. Điểm mấu chốt:
vì `Filled` kéo thẳng ảnh cho vừa ô, **mỗi thanh phải có ảnh sinh đúng tỉ lệ dài/cao của nó**,
nếu không hình tròn hai đầu bị kéo bẹt thành bầu dục.

| Ảnh sinh ra | Kích thước | Dùng cho | Ô đích |
|---|---|---|---|
| `Sprite_FillRound_Health` | 462 × 38 | `HealthBar_Fill` | 462 × 38 |
| `Sprite_FillRound_Exp` | 464 × 28 | `ExpBar_Fill` | 464 × 28 |
| `Sprite_FillRound_Enemy` | 324 × 32 | `Fill` của 4 prefab quái | 81 × 8 (cùng tỉ lệ 10.1) |

Ảnh đặt `Uncompressed`: thanh chỉ cao vài chục pixel, nén khối sẽ làm mép bo lởm chởm mà chẳng
tiết kiệm được bao nhiêu bộ nhớ.

Sửa cả thanh máu world-space trên đầu quái (`Enemy_Melee`, `Enemy_Ranged`, `Enemy_BossOrc`,
`Enemy_BossDemon`) để đồng bộ — cùng một lỗi hình học thì sửa hết một lượt, đừng để sót một chỗ
rồi lần sau lại phải quay lại.

**Đã kiểm chứng trong Play mode bằng ảnh chụp phóng to**, cả ba thanh, ở hai trạng thái:
máu đầy 500/500 → hai đầu bo khít nền; máu vơi 275/500 và EXP 37/100 → đầu trái bo, mép phải
cắt thẳng. Console 0 lỗi / 0 cảnh báo.
