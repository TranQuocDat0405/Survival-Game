# Survival Top-down

Game sinh tồn góc nhìn từ trên xuống, làm bằng **Unity 2022.3.62f3**. Bạn điều khiển một tay nỏ
đơn độc giữa rừng, chống lại từng đợt quái kéo tới, lên cấp sau mỗi lần hạ gục, và cố sống sót
qua **5 đợt** — trong đó đợt 3 và đợt 5 có boss.

Đây là bài test kỹ thuật cho vị trí thực tập tại **Wolffun Games**. Đề bài gốc nằm ở
[`Docs/README.md`](Docs/README.md); nhật ký toàn bộ quá trình làm nằm ở [`Docs/DEVLOG.md`](Docs/DEVLOG.md).

| | |
|---|---|
| **Unity** | 2022.3.62f3 (Built-in Render Pipeline, Linear color space) |
| **Nền tảng** | Windows (Mono) và Android (IL2CPP, ARM64, minSdk 24) |
| **Hướng màn hình** | Ngang (Landscape) — cả hai chiều |
| **Scene chính** | `Assets/_Project/Scenes/Main.unity` — nhưng **bấm Play ở scene nào cũng được**, project tự khởi động từ `Main` |

---

## Mục lục

- [Chơi thử trong 30 giây](#chơi-thử-trong-30-giây)
- [Cách chơi](#cách-chơi)
  - [Trên máy tính](#trên-máy-tính-editor-hoặc-bản-build-windows)
  - [Trên điện thoại](#trên-điện-thoại-android)
  - [Vài mẹo để sống lâu hơn](#vài-mẹo-để-sống-lâu-hơn)
- [Nội dung game](#nội-dung-game)
- [Đối chiếu với đề bài](#đối-chiếu-với-đề-bài)
- [Những chỗ khác đề bài, và vì sao](#những-chỗ-khác-đề-bài-và-vì-sao)
- [Những thứ thêm ngoài đề bài, và vì sao](#những-thứ-thêm-ngoài-đề-bài-và-vì-sao)
- [Kiến trúc code](#kiến-trúc-code)
- [Chỉnh số liệu ở đâu](#chỉnh-số-liệu-ở-đâu)
- [Cách build](#cách-build)
- [Video và bản build](#video-và-bản-build)
- [Phần chưa làm](#phần-chưa-làm)
- [Nguồn tài nguyên](#nguồn-tài-nguyên)

---

## Chơi thử trong 30 giây

1. Mở project bằng **Unity 2022.3.62f3** (bản khác có thể vẫn chạy nhưng chưa được kiểm chứng).
2. Mở scene `Assets/_Project/Scenes/Main.unity`.
3. Bấm **Play**, rồi bấm **CHƠI**.

Đợt quái đầu tiên xuất hiện 2 giây sau khi vào trận.

> **Mở nhầm scene cũng không sao.** Một script chỉ chạy trong Editor
> ([`PlayModeStartScene.cs`](Assets/_Project/Scripts/Editor/PlayModeStartScene.cs)) đặt `Main` làm
> scene khởi động cho mọi lần bấm Play. Lý do: sau khi tách kiến trúc, mọi manager sống suốt vòng
> đời ứng dụng đều nằm trong `Main`, nên mở thẳng `Game.unity` rồi bấm Play sẽ ra một game trông như
> bị hỏng — trong khi thực ra chỉ là vào sai cửa. Tắt được qua menu `Survival > Luôn Play từ scene Main`.

> Nếu chỉ muốn xem chứ không muốn cài Unity, xem mục [Video và bản build](#video-và-bản-build).

---

## Cách chơi

**Mục tiêu:** sống sót qua 5 đợt quái. Hạ hết quái của đợt hiện tại thì đợt sau mới tới. Clear
xong đợt 5 là thắng.

### Trên máy tính (Editor hoặc bản build Windows)

| Thao tác | Phím |
|---|---|
| Di chuyển | **W A S D** hoặc **phím mũi tên** |
| Ngắm | **Rê chuột** — nhân vật xoay người về phía con trỏ |
| Bắn (3 mũi tên) | **Chuột trái** |
| Đặt bom | **1** hoặc **E** |
| Lướt (dash) | **2** hoặc **Space** |

Vài điều nên biết:

- **Giữ chuột trái không bắn liên tục.** Phải bấm từng phát. Đây là chủ ý — đề bài đặt khoảng cách
  tối thiểu 0.5 giây giữa hai phát là để chống spam, mà cho giữ chuột bắn tự động thì chính là spam.
- **Nhân vật xoay dần chứ không xoay tức thì.** Bẻ chuột ra sau lưng thì mất khoảng nửa giây thân
  mới quay xong. Mũi tên luôn bay theo hướng thân **tại đúng lúc bấm**, nên bấm quá sớm là bắn hụt.
- Bom rơi **ngay dưới chân**, không ném đi xa. Đặt bom rồi phải tự chạy ra.

### Trên điện thoại (Android)

| Thao tác | Cách làm |
|---|---|
| Di chuyển | **Joystick ảo** ở góc dưới bên trái |
| Xoay người | Nhân vật **tự xoay theo hướng joystick** |
| Ngắm riêng một hướng | **Giữ nút bắn rồi kéo** về hướng muốn ngắm |
| Bắn | **Nhả tay** khỏi nút bắn |
| Đặt bom | Chạm **nút bom** |
| Lướt (dash) | Chạm **nút dash** |

Điểm khác biệt lớn nhất so với máy tính là **cách ngắm**:

- Bình thường nhân vật quay theo hướng bạn đang đi. Chạy sang trái thì bắn sang trái.
- Muốn bắn về hướng khác hướng đang chạy thì **giữ nút bắn và kéo** — lúc đó hướng kéo được ưu
  tiên hơn joystick, nên vừa chạy lùi vừa bắn về phía trước được. Nhả tay ra là quay lại xoay
  theo joystick.
- Vì dash đi theo hướng thân đang quay, nên **đổi hướng joystick trước rồi mới dash**.

Giao diện đã bọc **Safe Area** nên tai thỏ và thanh điều hướng của máy không che mất nút.

### Vài mẹo để sống lâu hơn

- **Đừng đứng yên bắn.** Quái cận chiến chạy 3.0 còn bạn chạy 3.2 — bạn nhanh hơn rất ít, đứng lại
  một nhịp là bị vây.
- **Ba viên đạn toè ra hình nón ±15°.** Càng đứng gần mục tiêu thì càng dễ trúng cả ba viên; bắn xa
  thì thường chỉ viên giữa trúng. Đánh boss nên áp sát.
- **Charge là tài nguyên, không phải đạn vô hạn.** Tối đa 3 lần bắn, hồi lại 1 lần mỗi 3 giây. Bắn
  hết sạch rồi bị vây là không còn gì để tự vệ.
- **Dash vừa để thoát vừa để gây sát thương.** Nó nổ dọc theo cả đường lướt, nên lướt xuyên qua đám
  quái sẽ ăn hơn là lướt ra chỗ trống.
- **Nhặt bình máu.** Bình máu đỏ phát sáng chỉ xuất hiện khi bạn đang thiếu máu, tối đa 3 bình trên
  sân, mỗi bình hồi 75 máu. Chúng luôn rơi trong tầm nhìn nên đừng bỏ qua.
- **Lên cấp là hồi máu.** Mỗi cấp cho +40 máu hiện tại, nên đôi khi cố hạ thêm một con quái lại an
  toàn hơn là bỏ chạy.

---

## Nội dung game

### Nhân vật

| Chỉ số | Giá trị đầu | Mỗi lần lên cấp |
|---|---|---|
| Máu tối đa | 500 | +40 |
| Máu hiện tại | 500 | +40 |
| Tốc độ di chuyển | 3.2 unit/giây | — |
| Tốc độ xoay | 360 độ/giây | — |
| Giáp | 0 | +2 |
| Damage Multiplier | 0 | +0.1 |

**Công thức sát thương** (đúng nguyên văn đề bài, nằm gọn trong một file duy nhất là
[`CombatMath.cs`](Assets/_Project/Scripts/Combat/CombatMath.cs)):

```
Sát thương nhận  = Sát thương gốc − Giáp          (nhỏ hơn 0 thì tính bằng 0)
Sát thương gây ra = Sát thương gốc × (1 + Damage Multiplier)
```

Giáp trừ vào **cả đòn chém lẫn từng nhịp trúng độc**.

### Ba kỹ năng

| | Bắn | Bom | Dash |
|---|---|---|---|
| Sát thương gốc | 10 mỗi mũi × 3 mũi | 50 | 15 |
| Tầm / bán kính | bay tối đa 8 unit | nổ bán kính 5 | nổ bán kính 3 |
| Nhịp dùng | tối đa 3 charge, hồi 1 charge mỗi 3 giây, cách nhau tối thiểu 0.5 giây | hồi chiêu 12 giây | hồi chiêu 6 giây |
| Đặc điểm | 3 mũi toè hình nón −15° / 0° / +15° | chờ 2 giây mới nổ, rơi dưới chân | lướt 3 unit trong 0.5 giây rồi nổ |

### Hai loại quái

| | Quái cận chiến | Quái tầm xa |
|---|---|---|
| Máu | 220 | 180 |
| Tốc độ | 3.0 | 2.7 |
| Tầm đánh | 1.3 unit, hình nón 50° | 3.0 unit |
| Sát thương | 30 mỗi đòn | đạn độc, không sát thương lúc trúng |
| Hiệu ứng | — | độc 30 sát thương mỗi nhịp, **4 nhịp** (trúng ngay + mỗi giây trong 3 giây) |
| EXP | 30 | 30 |

Cả hai chạy cùng một vòng hành vi: **tiếp cận → vào tầm thì đánh → đứng im 1 giây → lặp lại**.

Dính độc khi đang dính độc thì **đặt lại đồng hồ chứ không cộng dồn sát thương**, đúng như đề bài
yêu cầu.

### Đợt quái và lên cấp

- Mỗi đợt sinh ngẫu nhiên **3–4 quái cận chiến** và **1–2 quái tầm xa**, cộng thêm **1 con mỗi đợt**
  kể từ đợt 2 (xem [mục lệch đề bài](#những-chỗ-khác-đề-bài-và-vì-sao)).
- Quái sinh ra **ngoài khung hình**, ở chỗ có đường đi tới được người chơi.
- Hạ hết quái đợt hiện tại thì 2 giây sau đợt kế bắt đầu.
- Giết 1 con được **30 EXP**, đủ **100 EXP** lên 1 cấp, **EXP dư được giữ lại**. Một lần nhận nhiều
  EXP có thể lên liền mấy cấp.

Thành phần thực tế của cả 5 đợt (đo được khi chạy thật):

| Đợt | Cận chiến | Tầm xa | Boss | Tổng |
|---|---|---|---|---|
| 1 | 3 | 1 | — | 4 |
| 2 | 5 | 1 | — | 6 |
| 3 | 5 | 2 | **Orc** | 8 |
| 4 | 6 | 3 | — | 9 |
| 5 | 6 | 3 | **Demon** | 10 |

### Hai con boss

| | Orc (đợt 3) | Demon (đợt 5) |
|---|---|---|
| Máu | 600 | 1200 |
| Tốc độ | 3.3 | 3.45 |
| Sát thương | 100 | 175 |
| Tầm đánh | 1.9 | 2.3 |
| EXP | 150 | 300 |

Cả hai đều **nhanh hơn người chơi**, nên không thể cứ chạy vòng vòng mà thắng.

---

## Đối chiếu với đề bài

| Mục đề bài | Trạng thái | Ghi chú |
|---|---|---|
| **2.1** Chỉ số khởi đầu | ✅ Đã làm | Tốc độ chạy và xoay có chỉnh, xem mục dưới |
| **2.2** Công thức sát thương | ✅ Đã làm | Gom trong `CombatMath.cs`, giáp áp cả cho độc |
| **3.1** Bắn 3 viên có charge | ✅ Đã làm | 3 mũi ±15°, 3 charge, hồi 3 giây, giãn cách 0.5 giây |
| **3.2** Đặt bom | ✅ Đã làm | Chờ 2 giây, 50 sát thương, bán kính 5, hồi chiêu 12 giây |
| **3.3** Dash rồi nổ | ✅ Đã làm | 3 unit / 0.5 giây, 15 sát thương, bán kính 3, hồi chiêu 6 giây |
| **4.1** Quái đánh gần | ✅ Đã làm | 220 máu, 3.0 tốc độ, nón 50° tầm 1.3, 30 sát thương |
| **4.2** Quái đánh xa + độc | ✅ Đã làm | 180 máu, 2.7 tốc độ, đạn 10 u/s tầm 5, độc 4 nhịp có refresh |
| **5** Đợt quái, EXP, lên cấp | ✅ Đã làm | Có thêm 1 quái mỗi đợt, xem mục dưới |
| **6** UI bắt buộc | ✅ Đã làm | Đủ 6/6: thanh máu, cấp độ, joystick, 3 nút chiêu, hồi chiêu, thanh máu quái |
| **8** Bonus: camera rung | ✅ Đã làm | Qua Cinemachine Impulse |
| **8** Bonus: VFX | ✅ Đã làm | Nổ, trúng đòn, chết, lên cấp, pháo hoa khi thắng |
| **8** Bonus: âm thanh | ✅ Đã làm | Bắn, nổ, trúng đòn, chết, giao diện |
| **9** Scene Play được ngay | ✅ Đã làm | Không lỗi biên dịch, không cảnh báo |
| **9** README | ✅ Đã làm | Chính là file này |
| **9** Bản build Windows + Android | ⏳ Chờ | Xem mục [Video và bản build](#video-và-bản-build) |
| **9** Video gameplay | ⏳ Chờ | Xem mục [Video và bản build](#video-và-bản-build) |

Toàn bộ số liệu trên đã được **kiểm chứng bằng cách chạy thật trong Play mode** chứ không chỉ đọc
file cấu hình — chi tiết từng phép đo nằm trong [`Docs/DEVLOG.md`](Docs/DEVLOG.md).

---

## Những chỗ khác đề bài, và vì sao

Bốn chỗ dưới đây là **quyết định thiết kế có chủ ý của tôi**, không phải làm sai đề. Mỗi chỗ đều
chỉ là một con số trên Inspector, muốn trả về đúng đề bài thì sửa lại là xong, không phải đụng code.

### 1. Tốc độ di chuyển 3.2 thay vì 2.0

**Lý do:** đề bài cho quái cận chiến chạy **3.0**, nhanh hơn người chơi ở mức **2.0**. Nghĩa là một
khi bị bám thì không bao giờ cắt đuôi được bằng cách chạy, kể cả chạy hoàn hảo. Dash chỉ hồi 6 giây
một lần nên không đủ bù. Kết quả là người chơi mất hoàn toàn quyền định vị — thứ vốn là phần thú vị
nhất của thể loại này.

Đặt 3.2 thì người chơi nhanh hơn quái thường một chút, đủ để giữ khoảng cách nếu chạy khéo, nhưng
vẫn bị vây nếu đứng lại quá lâu.

**Sửa về đúng đề bài:** `Assets/_Project/Configs/Player/PlayerConfig.asset` → `Move Speed` = 2.

### 2. Tốc độ xoay 360 độ/giây thay vì 180

**Lý do:** ở 180 độ/giây, quay người 180 độ mất **trọn 1 giây**. Trong 1 giây đó quái cận chiến đi
được 3 unit — tức là mỗi lần muốn bắn ra sau lưng, người chơi phải trả giá bằng việc để quái áp sát
thêm 3 unit. Trên điện thoại, nơi ngắm bằng cách kéo nút bắn, độ trễ này nặng hơn nữa.

Ở 360 độ/giây, quay ngược mất nửa giây — vẫn thấy rõ nhân vật xoay dần chứ không xoay tức thì, đúng
tinh thần "không bắn theo hướng joystick nếu chưa xoay xong" của đề bài, nhưng không còn cảm giác
điều khiển bị nặng.

**Sửa về đúng đề bài:** `PlayerConfig.asset` → `Rotation Speed` = 180.

### 3. Dash nổ dọc theo đường lướt, không chỉ nổ ở điểm cuối

**Lý do:** đề bài nói "hết lướt thì kích nổ". Làm đúng vậy thì vụ nổ chỉ phủ chỗ *kết thúc* cú lướt,
trong khi hình ảnh người chơi nhìn thấy là nhân vật **xuyên qua** một đám quái. Quái bị lướt xuyên
qua giữa đường không hề hấn gì — nhìn như game bị lỗi.

Nên vụ nổ được rải thành **4 tâm dọc theo đường vừa lướt**: 3 tâm nhỏ bán kính 1.5 và 1 tâm chính
bán kính 3 ở cuối.

**Quan trọng — sát thương KHÔNG đổi:** mỗi con quái vẫn chỉ ăn **đúng 15 sát thương gốc, đúng một
lần**, dù nằm trong mấy vùng nổ đi nữa. Có một danh sách chống trùng đảm bảo điều đó
([`AreaDamage.cs`](Assets/_Project/Scripts/Combat/AreaDamage.cs)). Thay đổi ở đây là **vùng phủ**,
không phải sức mạnh.

**Sửa về đúng đề bài:** `Assets/_Project/Configs/Skills/Skill_Dash.asset` → `Trail Bomb Count` = 0.

### 4. Thêm 1 quái mỗi đợt

**Lý do:** đề bài cố định 3–4 quái cận chiến và 1–2 quái tầm xa cho mọi đợt, trong khi người chơi
thì mạnh lên sau mỗi đợt. Đợt 4 dễ hơn đợt 1 rất nhiều — độ khó đi ngược.

Đáng nói hơn, tôi tính ra được là **giáp cộng dồn khiến người chơi bất tử về sau**: giáp tăng +2 mỗi
cấp, mà quái thường chỉ đánh 30, nên tới **cấp 16** là giáp đạt đúng 30 và mọi đòn của quái thường
đều bị trừ về 0. Đây chính là lý do phải có boss đánh mạnh hơn — thêm bao nhiêu quái thường cũng
không làm game khó lên được.

**Sửa về đúng đề bài:** `Assets/_Project/Configs/Waves/WaveConfig.asset` → `Extra Enemies Per Wave` = 0.

### Một cách hiểu cần nói rõ: "bán kính 5 unit" đo tới đâu

Vụ nổ dùng `Physics.OverlapSphere`, tức là xét **thân quái có chạm vào vùng nổ hay không**, chứ
không xét toạ độ tâm của nó. Đo thực tế với vụ nổ bán kính 5: con quái xa nhất còn trúng nằm ở
**5.3 unit**, con gần nhất bị hụt ở **5.4** — đúng bằng `5.0 + 0.32` với 0.32 là bán kính thân quái.

Giữ cách này vì nó tự nhiên hơn với người chơi (thân quái chạm vùng nổ thì phải ăn đòn) và cần thiết
với boss vốn có thân rất to.

---

## Những thứ thêm ngoài đề bài, và vì sao

### Hai con boss ở đợt 3 và đợt 5

**Vì sao:** như đã nói ở trên, giáp cộng dồn làm quái thường mất hết ý nghĩa về sau. Cần một loại kẻ
địch mà sát thương đủ lớn để giáp không nuốt hết, nếu không thì nửa sau của ván chơi không còn rủi ro.

Boss đi qua **đúng đường sinh và đúng bộ máy trạng thái như mọi con quái khác** — cùng pool, cùng
cách chọn chỗ sinh, cùng đăng ký vào sổ theo dõi. Nhờ vậy điều kiện "clear hết đợt" tự động tính cả
boss mà không phải viết thêm một nhánh nào. Về mặt code, boss chỉ là **một file cấu hình khác**.

Boss có một vùng trúng đòn riêng to hơn cho khớp với thân hình, tách khỏi vùng va chạm dùng để đi
lại — vùng đi lại phải giữ nhỏ để boss lọt được giữa rừng cây.

### Bình máu hồi phục

**Vì sao:** không có cách hồi máu nào ngoài lên cấp thì một sai lầm ở đợt 2 sẽ đeo bám tới hết ván,
và người chơi không có cách nào gỡ lại. Bình máu tạo ra một quyết định thật sự: *có đáng rời chỗ an
toàn để chạy ra nhặt không?*

Bình chỉ xuất hiện khi người chơi **đang thiếu máu**, tối đa 3 bình, và luôn rơi **trong tầm nhìn** —
ngược hẳn với quái vốn phải sinh ngoài khung hình.

### Màn hình kết thúc và bảng tổng kết

**Vì sao:** trước khi có nó, người chơi chết là mọi thứ đứng yên — không đi được, không bắn được,
quái đứng im — và trông y hệt game bị treo. Thực ra game vẫn chạy, chỉ là không ai báo rằng ván đã
kết thúc.

Dùng chung một bảng cho cả thua lẫn thắng, chỉ khác chữ và màu, kèm bảng tổng kết đợt / cấp / số
quái đã hạ / thời gian.

### Ván chơi hữu hạn 5 đợt và màn hình chiến thắng

**Vì sao:** đề bài không nói ván chơi kết thúc thế nào. Để chạy vô hạn thì vì lý do giáp nói trên,
sau đợt 10 người chơi gần như bất tử và ván chơi không bao giờ kết thúc — người chấm sẽ phải tự tắt
game. Một chiến dịch 5 đợt có mở đầu, cao trào và kết thúc thì chơi trọn được trong vài phút.

**Đổi số đợt:** `WaveConfig.asset` → `Final Wave`. Đặt 0 là chạy vô hạn trở lại.

### Tìm đường bằng NavMesh

**Vì sao:** bản đồ có rừng cây rậm. Nếu quái chỉ lao thẳng về phía người chơi thì chúng kẹt vào gốc
cây và đứng im — người chơi chỉ cần núp sau một cái cây là an toàn tuyệt đối.

NavMesh được **bake từ collider chứ không phải từ mesh hiển thị**, vì mesh của cây rộng gấp 7 lần
collider của nó, bake theo mesh thì mặt đường đi bị thủng lỗ chỗ khắp rừng.

### Điều khiển bằng chuột và bàn phím

**Vì sao:** đề bài mục 9 nói rõ "Scene chính Play được ngay", tức người chấm sẽ bấm Play trên Editor.
Bắt họ rê chuột xuống góc màn hình bấm từng nút tròn thì rất khó đánh giá cảm giác combat. Có phím
tắt thì chơi được ngay như game PC. Trên điện thoại phần này tự vô hiệu vì không có bàn phím.

### Phần đánh bóng khác

- **Camera rung** khi nổ bom và khi trúng đòn (qua Cinemachine Impulse).
- **Viền đỏ loé lên** ở mép màn hình khi mất máu, để biết mình vừa ăn đòn mà không phải liếc lên thanh máu.
- **Hiệu ứng và âm thanh** cho bắn, nổ, trúng đòn, chết, lên cấp, nhặt đồ.
- **60 khung hình/giây trên điện thoại** — Unity mặc định khoá 30 trên Android, mà lỗi này không bao
  giờ lộ ra trong Editor.

---

## Kiến trúc code

Code chia theo **ba tầng vòng đời**, và đây là thứ đáng nhìn trước tiên:

| Tầng | Ai điều phối | Sống ở đâu |
|---|---|---|
| **Ứng dụng** | `GameManager` — máy trạng thái `LOADING → HOME → INGAME` | `Main.unity`, không bao giờ unload |
| **Trận đấu** | `GameplayManager` — đang chơi / thua / thắng, và dọn dẹp để chơi lại | `Game.unity`, nạp additive khi vào trận |
| **Giao diện** | `UIManager` của nframework — mỗi màn hình là một prefab `BaseUIView` | `Resources/UI/`, nạp theo yêu cầu |

Muốn biết "bấm Play xong thì chuyện gì xảy ra theo thứ tự nào" thì đọc đúng một hàm:
`GameManager.HandleGameStateChanged`. Console cũng in ra đường đi thật lúc chạy —
`GameState: LOADING → HOME → INGAME`.

```
Assets/_Project/Scripts/
  Manager/       GameManager (FSM ứng dụng), GameplayManager (vòng đời ván chơi)
  Data/          UserData — thành tích tốt nhất, lưu xuống đĩa
  Utils/         Define — nơi duy nhất chứa tên scene và tên prefab UI
  Config/        Các ScriptableObject: player, quái, đợt quái, tiến trình, pool, vật phẩm, flow
  Core/          NavMeshProvider
  Stats/         EStatType, StatSet, StatModifier — chỉ số theo kiểu bảng tra
  Combat/        CombatMath, DamageInfo, Health, AreaDamage, StatusEffects/
  Player/        PlayerActor, PlayerMotor, PlayerInputRouter, KeyboardSkillInput
  Skills/        SkillDefinition + SkillRuntime, ChargedShoot / Bomb / Dash
  Enemies/       EnemyActor, States/ (Approach, Attack, Idle), Attacks/ (ConeMelee, Projectile)
  Projectiles/   ProjectileBase, mũi tên, đạn độc, bom
  Waves/         WaveManager, SpawnPointPicker
  Progression/   ExperienceSystem
  Pooling/       PoolService
  CameraRig/     CameraShakeService
  UI/
    Menu/        HomeMenu, GamePlayMenu               ← màn hình do UIManager quản
    Popup/       Popup (nền chung), LoadingPopup, SettingsPopup, PausePopup, ResultPopup
    (gốc)        PlayerStatusView, SkillBarView, HurtFlashView, VolumeSettingsView — widget con
                 SkillButtonView, ChargeRingView, WorldHealthBar — không phải view của UIManager
                 SafeAreaFitter, FullScreenOverlay — helper bố cục, đi kèm prefab
  Vfx/ Audio/    PooledVfx, GameAudioService
```

```
Assets/_Project/Resources/UI/     ← chỉ chứa thứ được nạp động theo tên
  HomeMenu · GamePlayMenu · LoadingPopup · SettingsPopup · PausePopup · ResultPopup
```

Năm quyết định đáng nói nhất:

**Chỉ số là bảng tra, không phải field cứng.** `StatSet` lưu theo `EStatType` nên thêm một chỉ số
mới chỉ tốn một dòng enum và một dòng trên Inspector, không phải sửa logic nào. Level-up chỉ là
"cộng một danh sách `StatModifier` vào `StatSet`".

**Công thức sát thương nằm đúng một chỗ.** Mọi nguồn sát thương — mũi tên, bom, dash, đòn chém của
quái, từng nhịp trúng độc — đều bắt buộc đi qua `CombatMath`. Không có chỗ nào tự cộng trừ máu.
Muốn kiểm chứng "combat có đi qua công thức đã cho không" thì chỉ cần đọc một file.

**Kỹ năng và đòn đánh của quái dùng `[SerializeReference]`.** Nhờ vậy `EnemyConfigSO` giữ được một
đòn đánh *đa hình* chọn từ dropdown ngay trên Inspector. **Thêm loại quái thứ ba = tạo một file
`.asset`, kéo vào WaveConfig, không sửa một dòng code.** Thanh kỹ năng cũng tự sinh nút từ danh sách
skill, nên thêm skill thứ tư là kéo một asset vào danh sách, nút tự xuất hiện.

**Thời điểm gây sát thương tính bằng đồng hồ trong config, không bằng Animation Event.**
`AttackWindup` là một con số trên Inspector và là nguồn sự thật duy nhất; animation được **co giãn
tốc độ cho khớp với con số đó**. Đổi windup trên Inspector thì hình ảnh tự khớp theo, không phải mở
file animation ra sửa. Việc kiểm tra trúng/hụt chạy tại **đúng thời điểm gây sát thương và đọc vị trí
người chơi lúc đó**, nên né kịp là hụt thật.

**Scene `Main` không bao giờ unload.** Trước đây `SaveManager`, `SoundManager`, `GameAudioService`,
`BestRecord`, `SaveBootstrap` và `SceneMusicPlayer` được đặt sẵn ở **cả hai** scene, nên mỗi lần đổi
scene là chúng bị huỷ rồi tạo lại. Hệ quả đo được: chỉnh âm lượng 0.22 ở màn hình chính, vào màn chơi
còn **0.157** — vì `SoundManager` mới nạp lại bản cũ trên đĩa. Bản vá lúc đó là ghi đĩa ngay trước khi
rời scene; nó chữa đúng triệu chứng nhưng bệnh là *manager không nên chết theo scene*. Giờ chúng sống
trong `Main`, còn màn chơi được nạp **additive** chồng lên. Không còn gì để vá, và cũng không cần
`DontDestroyOnLoad`: mọi manager nhìn thấy được ngay trong Hierarchy, không có object "mồ côi" lơ lửng.

Cùng lý do đó, giao diện chỉnh âm lượng chỉ còn **một** bản duy nhất. Trước đây nó bị dựng hai lần —
một trong màn hình chính, một trong bảng tạm dừng — nên sửa bố cục phải nhớ sửa cả hai chỗ. Giờ cả
hai nơi mở đúng cùng một `SettingsPopup`.

**Về tối ưu:** mọi thứ sinh ra nhiều lần đều đi qua pool — mũi tên, đạn độc, bom, quái, mọi hiệu ứng
hạt, bình máu. Các phép quét vùng dùng bản `NonAlloc` với bộ đệm cấp phát sẵn nên không sinh rác cho
bộ dọn rác. So khoảng cách bằng bình phương để khỏi phải khai căn. Giao diện cập nhật theo **sự kiện**
chứ không đọc lại mỗi khung hình.

---

## Chỉnh số liệu ở đâu

Không có con số cân bằng nào nằm trong code. Tất cả ở `Assets/_Project/Configs/`:

| File | Chứa gì |
|---|---|
| `Player/PlayerConfig.asset` | Máu, tốc độ chạy, tốc độ xoay, giáp, damage multiplier, danh sách skill |
| `Player/ProgressionConfig.asset` | EXP mỗi cấp, thưởng mỗi lần lên cấp |
| `Skills/Skill_ChargedShoot.asset` | Số mũi tên, góc toè, sát thương, số charge, thời gian hồi |
| `Skills/Skill_Bomb.asset` | Thời gian chờ nổ, sát thương, bán kính, hồi chiêu |
| `Skills/Skill_Dash.asset` | Quãng lướt, thời gian, sát thương, bán kính, hồi chiêu |
| `Enemies/Enemy_*.asset` | Máu, tốc độ, tầm, sát thương, nhịp đánh, EXP thưởng, kiểu đòn đánh |
| `Waves/WaveConfig.asset` | Thành phần mỗi đợt, đợt cuối, lịch boss, bán kính sinh quái |
| `Pickups/PickupConfig.asset` | Nhịp rơi bình máu, số bình tối đa, lượng máu hồi |
| `Pools/PoolConfig.asset` | Số lượng khởi tạo sẵn cho từng pool |
| `Audio/GameAudio.asset` | Toàn bộ âm thanh, âm lượng từng loại, và hai bản nhạc nền |
| `GameConfig.asset` | Thời gian tối thiểu của màn chuyển cảnh, số khung hình mục tiêu |

---

## Cách build

Build Settings đã cấu hình sẵn, chỉ cần chọn nền tảng và bấm Build.

**Đã cấu hình sẵn:**

| | |
|---|---|
| Scene trong build | `Main.unity` ở vị trí **0**, `Game.unity` ở vị trí **1** |
| Hướng màn hình | Chỉ Landscape trái và phải (đã tắt màn hình dọc) |
| Color space | Linear |
| Phiên bản | 1.0 |

**Windows:**

| | |
|---|---|
| Scripting backend | Mono |
| Độ phân giải mặc định | 1920 × 1080, toàn màn hình dạng cửa sổ |

`File > Build Settings > Windows` → `Build`. Thoát game bằng **Alt + F4**.

**Android:**

| | |
|---|---|
| Scripting backend | IL2CPP |
| Kiến trúc | ARM64 |
| Min SDK | 24 (Android 7.0) |
| Package name | `com.TranQuocDat.SurvivalTopdown` |
| Định dạng | APK (đã tắt App Bundle để cài trực tiếp được) |

`File > Build Settings > Android` → `Switch Platform` → `Build`. Lần đầu chuyển nền tảng sẽ mất khá
lâu vì Unity phải nén lại toàn bộ texture.

---

## Video và bản build

> **File build không được commit vào repo** vì mỗi bản nặng vài trăm MB, sẽ làm việc clone repo trở
> nên rất chậm.

| | Đường dẫn |
|---|---|
| Video gameplay | _(dán link Google Drive vào đây)_ |
| Bản build Windows | _(dán link Google Drive vào đây)_ |
| Bản build Android (APK) | _(dán link Google Drive vào đây)_ |

Để cài file APK trên điện thoại Android cần bật **"Cài đặt ứng dụng không rõ nguồn gốc"** cho ứng
dụng quản lý file hoặc trình duyệt đang dùng để mở file.

---

## Phần chưa làm

Ghi ra cho đầy đủ, không giấu:

- **Unit test EditMode** cho công thức sát thương, charge, độc và tiến trình lên cấp. Các phần này
  hiện đã được kiểm chứng bằng cách chạy thật trong Play mode (chi tiết trong `Docs/DEVLOG.md`)
  nhưng chưa được viết thành bộ test tự động chạy lại được.
- **Sắp xếp lại thư mục `ThirdParty`** và tách assembly definition cho code của project. Hiện toàn bộ
  code nằm chung trong `Assembly-CSharp`, nên mỗi lần sửa một file là biên dịch lại tất cả.

---

## Nguồn tài nguyên

Toàn bộ tài nguyên đều là hàng miễn phí dùng được cho mục đích thương mại.

| Loại | Nguồn |
|---|---|
| Nhân vật, quái xương | KayKit Adventurers 2.0 FREE, KayKit Skeletons 1.1 FREE — Kay Lousberg |
| Animation nhân vật | KayKit Character Animations 1.2 — Kay Lousberg |
| Boss (Orc, Demon) | Cute Animated Monsters — Quaternius |
| Cây cối, đá, môi trường | Stylized Nature MegaKit, Ultimate Nature Pack — Quaternius |
| Hiệu ứng hạt | Cartoon FX Remaster Free — Jean Moreno |
| Giao diện, icon | Kenney UI Pack, Kenney Game Icons |
| Âm thanh | Kenney Impact / Interface / RPG Audio / Sci-fi Sounds; tiếng nỏ từ Freesound |
| Font | Baloo 2 và Luckiest Guy — Google Fonts (SIL Open Font License) |
| Joystick ảo | Joystick Pack — Fenerax Studios (Unity Asset Store, miễn phí) |
| Framework nền | nframework (pool, máy trạng thái, observable, âm thanh, safe area) |
| Tween | DOTween — Demigiant |
| Camera | Cinemachine 2.9.7 — Unity |

---

<sub>Trần Quốc Đạt · bài test kỹ thuật Wolffun Games · tháng 8/2026</sub>
