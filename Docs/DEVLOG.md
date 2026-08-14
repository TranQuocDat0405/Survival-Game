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
| 2.1 | Tốc độ di chuyển | 2 unit/s | ✅ |
| 2.1 | Tốc độ xoay | 180 °/s | ✅ |
| 2.1 | Giáp | 0 | ✅ |
| 2.1 | Damage Multiplier | 0 | ✅ |
| 2.1 | Bắn/bom/dash dùng **forward hiện tại**, không theo joystick | — | ✅ |
| 2.2 | `Sát thương nhận = Sát thương gốc − Giáp`, clamp ≥ 0 | — | ✅ |
| 2.2 | `Sát thương gây ra = gốc × (1 + DamageMultiplier)` | — | ✅ |
| 2.2 | Giáp áp dụng cho **cả đòn chém lẫn độc** | — | ✅ |

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
| 3.3 | Hết lướt nổ 15 dmg gốc, bán kính 3 unit | — | ✅ |
| 3.3 | Dash cooldown | 6 s | ✅ |

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

## 4. Việc còn lại (chốt cuối ngày 15/08/2026)

Thứ tự này do người chơi chốt, không phải tôi tự sắp.

| # | Việc | Ghi chú |
|---|---|---|
| 1 | Quái đang nghỉ 1 giây sau khi đánh **không được bị con sau xô đẩy** | Đã chốt cách làm: **ghim cứng** con đang nghỉ, biến nó thành vật cản không đẩy được; con phía sau tự vòng qua như vòng qua gốc cây — tận dụng luôn phần tìm đường vừa làm |
| 2 | **Bonus mục 8 README** — camera shake, VFX, SFX | Ưu tiên trước hai việc dưới vì đây là mục **có điểm cộng thật**. Cinemachine Impulse; Cartoon FX Remaster đã import sẵn; SFX Kenney |
| 3 | Hiệu ứng báo dính độc trên người player và cạnh thanh máu | Ý thêm, **ngoài spec** |
| 4 | Bình hồi máu spawn định kỳ quanh player (10 s, tối đa 3, hồi 75) | Ý thêm, **ngoài spec** |
| 5 | Cấu hình sẵn Build Windows + Android, viết hướng dẫn | **Không tự build** — người chơi tự bấm. Làm cuối cùng |
| 6 | Scene HomeMenu · refactor thư mục ThirdParty · README nộp bài · video gameplay | Chỉ làm sau khi mọi yêu cầu bắt buộc đã đủ và đúng |
