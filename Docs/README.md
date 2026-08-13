# Survival Top-down

> **Bắt buộc hoàn thành toàn bộ gameplay trước khi làm Bonus.**

Xây dựng một prototype game survival góc nhìn từ trên xuống (top-down), một nhân vật di chuyển trên mặt phẳng ngang, camera follow. Toàn bộ yêu cầu gameplay bên dưới là bắt buộc. Phần Bonus chỉ làm sau khi phần bắt buộc đã chạy đúng.

**Ghi chú:** [`videos/gameplay-ref.mp4`](videos/gameplay-ref.mp4) là video tham khảo gameplay (feel, nhịp combat, camera). Video **không phải** mục tiêu implement — spec trong README này mới là nguồn bắt buộc. Art, VFX, layout UI và chi tiết không ghi trong spec không cần bắt chước video.

---

## 1. Mục tiêu & phạm vi

Bài test đánh giá khả năng implement combat, AI quái đơn giản, hệ thống wave, cấu hình chỉ số để tuning, và tổ chức code có thể maintain. Không yêu cầu art production; hình khối / sprite tạm được chấp nhận nếu gameplay rõ.

| | |
|---|---|
| **Góc nhìn** | Top-down, camera follow nhân vật |
| **Không gian** | 1 nhân vật di chuyển trên bề mặt ngang (3D hoặc 2.5D) |
| **Input** | Joystick ảo trên UI + nút kỹ năng |
| **Nền tảng** | Unity (bản LTS gần nhất bạn đang dùng). Nộp project chạy được trên Editor |

---

## 2. Nhân vật (Player)

### 2.1. Chỉ số khởi đầu

| Chỉ số | Giá trị ban đầu | Ghi chú |
|---|---|---|
| Máu (HP) | 500 | Máu tối đa khởi đầu = 500 |
| Tốc độ di chuyển | 2 unit / giây | |
| Tốc độ xoay | 180 độ / giây | Xem ví dụ bên dưới |
| Giáp | 0 | Công thức nhận sát thương |
| Damage Multiplier | 0 | Công thức gây sát thương |

**Xoay người:** tốc độ quay 180°/s. Ví dụ nhân vật đang chạy sang phải, joystick kéo sang trái (đổi hướng 180°) thì cần khoảng 1 giây để xoay xong sang trái. Hướng bắn, bom dash dùng hướng **forward hiện tại** của nhân vật (không bắn theo hướng joystick nếu chưa xoay xong).

### 2.2. Công thức sát thương

Sát thương nhân vật nhận từ quái (đã trừ giáp):

```
Sát thương nhận = Sát thương gốc − Giáp
```

Sát thương nhân vật gây ra cho kẻ địch (đạn, bom, nổ dash):

```
Sát thương gây ra = Sát thương gốc × (1 + Damage Multiplier)
```

Giáp áp dụng cho sát thương từ quái (gồm đòn chém và độc). Nếu kết quả nhận sát thương `< 0`, tính bằng `0`.

---

## 3. Kỹ năng nhân vật

### 3.1. Đánh thường — bắn 3 viên (charge)

- Mỗi lần bắn xuất **3 viên đạn** cùng lúc theo hướng forward, hình nón: **−15°, 0°, +15°**.
- Sát thương mỗi viên (gốc): **10**, sau đó nhân Damage Multiplier.
- Hệ thống charge: tối đa **3 charge**. Mỗi phát bắn tốn **1 charge**.
- Hồi charge: **+1 charge mỗi 3 giây**, chỉ khi đang chưa đủ 3 charge.
- Khoảng cách tối thiểu giữa 2 lần bắn: **0.5 giây**, không phụ thuộc số charge còn lại (chống spam).
- Không đủ charge thì không bắn được.

### 3.2. Kỹ năng bổ trợ 1 — Đặt bom

- Triệu hồi 1 quả bom tại vị trí hiện tại của player.
- Sau **2 giây** bom nổ, gây **50 sát thương gốc** cho mọi kẻ địch trong bán kính **5 unit**.
- Cooldown: **12 giây**.

### 3.3. Kỹ năng bổ trợ 2 — Dash rồi nổ

- Đẩy player theo hướng forward một quãng **3 unit** trong **0.5 giây**.
- Hết lướt: kích nổ gây **15 sát thương gốc** cho mọi kẻ địch trong bán kính **3 unit**.
- Cooldown: **6 giây**.

---

## 4. Kẻ địch

### 4.1. Quái đánh gần

| Chỉ số / kỹ năng | Giá trị |
|---|---|
| Máu | 220 |
| Tốc độ di chuyển | 3 unit / giây |
| Tấn công | Hình nón 50°, tầm 1.3 unit, 30 sát thương gốc (1 lần mỗi đòn) |

**Behavior:** di chuyển tiếp cận player → khi vào tầm thì tấn công → đứng im 1 giây → lặp lại (tiếp cận tiếp).

### 4.2. Quái đánh xa (đạn độc)

| Chỉ số / kỹ năng | Giá trị |
|---|---|
| Máu | 180 |
| Tốc độ di chuyển | 2.7 unit / giây |
| Tầm tiếp cận để bắn | 3 unit |
| Đạn độc | Bay theo hướng trước mặt, tối đa 5 unit, tốc độ 10 unit / giây |
| Hiệu ứng độc | 30 sát thương gốc / giây; tick ngay lúc trúng; kéo dài 3 giây |
| Số tick | Tổng 4 tick (lúc trúng + mỗi giây trong 3 giây) |
| Refresh | Dính độc khi đang độc: reset thời gian độc, không stack sát thương |

**Behavior:** tiếp cận player tới khoảng cách 3 unit → tấn công (bắn đạn độc) → đứng im 1 giây → lặp lại (tiếp cận tiếp).

---

## 5. Wave, kinh nghiệm, lên cấp

- Mỗi wave spawn ngẫu nhiên **3–4 quái đánh gần** và **1–2 quái đánh xa**.
- Chỉ spawn wave tiếp theo khi đã **clear toàn bộ quái wave hiện tại**.
- Giết 1 quái: **+30 EXP**. Đủ **100 EXP** thì lên 1 cấp. EXP dư được giữ cho cấp sau.
- Mỗi lần lên cấp: **+40 máu hiện tại** và **+40 máu tối đa**, **+2 giáp**, **+0.1 Damage Multiplier**.

---

## 6. UI bắt buộc

- Overlay: thanh máu player, số level hiện tại.
- Joystick ảo điều khiển di chuyển.
- Nút chọn / dùng kỹ năng (đánh thường, bom, dash).
- Hiển thị cooldown trên nút kỹ năng.
- Thanh máu từng con quái, gắn trên đầu (world space).

---

## 7. Tiêu chí chấm (phần bắt buộc — 100%)

Toàn bộ gameplay mục 2–6 phải được implement. Điểm phần bắt buộc phân bổ như sau:

| Tiêu chí | Tỷ trọng | Kỳ vọng |
|---|---|---|
| Game logic chạy đúng | 50% | Đúng công thức, charge, CD, AI, wave, EXP/level, UI đủ như mô tả |
| Config tách rõ, dễ tuning | 20% | Chỉ số (máu, dmg, CD, tầm, speed…) không hard-code rải rác; chỉnh trên Inspector / file config |
| Tối ưu code | 10% | Không tạo/hủy object bừa bãi khi có thể pool; tránh Update nặng không cần thiết |
| Dễ đọc, dễ maintain | 10% | Tách trách nhiệm, tên rõ, cấu trúc folder hợp lý, hạn chế God-class |
| Dễ mở rộng | 10% | Thêm skill / loại quái / chỉ số mới không phải sửa đẫm logic cũ |

Bài không Play được trên Editor, hoặc combat không đi qua công thức đã cho, sẽ không được chấm các tiêu chí còn lại.

---

## 8. Bonus (điểm cộng — không bắt buộc)

Chỉ thực hiện khi bạn tự tin phần bắt buộc đã hoàn thành và chơi được.

| Hạng mục | Gợi ý |
|---|---|
| Camera shake | Khi bắn đạn, khi dash, khi player nhận sát thương |
| Polish visual | VFX nổ/đạn/hit, feedback rõ lúc lên cấp, đọc combat dễ hơn |
| Âm thanh | SFX bắn, nổ, hit, dash, UI — không bắt buộc nhạc nền |

---

## 9. Nộp bài

- Gửi Github Repository có chứa project Unity.
- Kèm file README: cách mở scene, phím/nút điều khiển, danh sách phần đã làm / chưa làm, Unity version.
- Kèm theo video quay lại gameplay và bản build game (chấp nhận build windows và android)
- Scene chính Play được ngay, không lỗi compile.

Thời hạn nộp bài và cách gửi theo hướng dẫn trong thư mời.
