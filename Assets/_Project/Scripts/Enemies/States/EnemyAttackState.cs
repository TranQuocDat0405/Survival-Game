namespace Survival.Enemies.States
{
    /// <summary>
    /// Giai đoạn 2 của chu kỳ trong spec: "khi vào tầm thì tấn công".
    ///
    /// ĐÂY LÀ FILE QUAN TRỌNG NHẤT VỀ CẢM GIÁC CHƠI, và là nơi xử lý đúng vấn đề
    /// "quái vung tay nhưng player đã chạy đi mất nên không gây được sát thương".
    ///
    /// Một đòn đánh được chia làm hai đoạn, cả hai đều là số trong file config:
    ///
    ///   [--- lấy đà (windup) ---]►GÂY SÁT THƯƠNG[--- thu tay (recover) ---]  rồi sang Idle
    ///
    /// BỐN QUYẾT ĐỊNH THIẾT KẾ Ở ĐÂY:
    ///
    /// 1. SÁT THƯƠNG TÍNH BẰNG ĐỒNG HỒ, KHÔNG DÙNG ANIMATION EVENT.
    ///    Animation Event là mốc đánh dấu gắn trực tiếp vào file animation. Nghe thì tiện,
    ///    nhưng: nó biến mất mỗi khi import lại file FBX, nó không sửa được trên Inspector,
    ///    và nó nằm rải rác trong hàng chục file animation nên rất khó tune.
    ///    Dùng đồng hồ đọc từ config thì thời điểm gây sát thương là MỘT con số duy nhất,
    ///    sửa được ngay trên Inspector, và không bao giờ mất.
    ///
    /// 2. ANIMATION CHẠY KHỚP THEO CONFIG, KHÔNG PHẢI NGƯỢC LẠI.
    ///    Con số trong config là chuẩn; tốc độ phát animation được co giãn cho vừa.
    ///    Nên khi tune "windup 0.35 -> 0.5 giây" thì hình ảnh tự chậm lại theo, luôn khớp.
    ///
    /// 3. VỊ TRÍ PLAYER ĐƯỢC ĐỌC TẠI ĐÚNG THỜI ĐIỂM GÂY SÁT THƯƠNG.
    ///    KHÔNG ghi nhớ vị trí player từ lúc bắt đầu vung tay. Nếu ghi nhớ thì quái sẽ
    ///    đánh trúng cả khi player đã chạy ra xa — ăn gian và cảm giác rất tệ.
    ///    Đọc tại thời điểm ra đòn thì né được là né thật, công bằng cho cả hai phía.
    ///
    /// 4. TRONG LÚC LẤY ĐÀ, QUÁI VỪA XOAY VỪA ĐI THEO PLAYER.
    ///    Đây là chỗ sửa lỗi "player chỉ cần chạy là đòn nào cũng trượt", và nó là lỗi SỐ HỌC
    ///    chứ không phải lỗi AI: lấy đà mất 0.5 giây, mà player chạy 3.2 unit/giây thì trong
    ///    0.5 giây đó đã đi được 1.6 unit — xa gấp rưỡi tầm đánh 1.3. Quái đứng chôn chân thì
    ///    chỉ cần người chơi còn chạy là MỌI ĐÒN ĐỀU HỤT.
    ///    Đo được: player đứng yên ăn 3 đòn / 5 giây, player chạy chỉ ăn 1.
    ///
    ///    Cho quái bám đủ tốc độ 3.0 thì khoảng cách chỉ nới ra 0.1 unit, đòn vẫn trúng.
    ///    Nhưng Dash (6 unit/giây) vẫn nới ra được 1.5 unit nên vẫn né được.
    ///    Đó chính là điểm cân bằng cần giữ: ĐI BỘ KHÔNG THOÁT, DASH THÌ THOÁT.
    ///
    ///    Quái chỉ bám tới một khoảng cách giữ nhất định rồi dừng, chứ không ủi thẳng vào
    ///    người chơi — nếu không hai collider húc vào nhau trông rất kỳ.
    ///
    /// 5. GHIM CỨNG TỪ KHOẢNH KHẮC RA ĐÒN CHO TỚI HẾT QUÃNG NGHỈ.
    ///    Trước đó thì không, vì lúc lấy đà quái còn phải bám theo player. Nhưng từ lúc đòn
    ///    đã ra, spec bảo nó đứng im — và đứng im phải là đứng im thật, không con nào đẩy
    ///    nó trượt đi được. Chi tiết ở <c>EnemyActor.SetAnchored</c>.
    /// </summary>
    public class EnemyAttackState : EnemyState
    {
        private float _timer;
        private bool _damageApplied;

        public EnemyAttackState(EnemyActor enemy) : base(enemy) { }

        public override void OnEnter()
        {
            _timer = 0f;
            _damageApplied = false;

            // Quái khựng lại một nhịp. Đây là tín hiệu hình ảnh quan trọng:
            // người chơi thấy quái dừng vung tay thì biết đòn sắp tới và có cơ hội phản ứng.
            Enemy.StopMoving();

            // CHƯA ghim vội. Trong lúc lấy đà quái còn phải bám theo player (xem OnUpdate),
            // nên nó cần thân vật lý tự do. Ghim ngay từ đây sẽ khoá luôn cả việc bám đó.
            // Ngoại lệ: nếu config tắt hẳn việc bám thì quái đứng yên suốt đòn đánh — mà đã
            // đứng yên thì ghim luôn, không có lý do gì để bị con khác xô lệch khỏi hướng đã ngắm.
            Enemy.SetAnchored(!Enemy.Config.TrackTargetDuringWindup);
        }

        public override void OnUpdate(float deltaTime)
        {
            _timer += deltaTime;

            var config = Enemy.Config;

            if (!_damageApplied)
            {
                // Quyết định 4: bám theo player trong lúc lấy đà — xoay chậm lại, và ĐI THEO.
                if (config.TrackTargetDuringWindup)
                {
                    Enemy.RotateTowardsTarget(deltaTime, config.WindupTrackingFactor);
                    Enemy.MoveTowardsTarget(config.WindupChaseFactor, config.WindupHoldDistance);
                }

                // Quyết định 1 và 3: tới đúng thời điểm thì ra đòn,
                // và đòn đánh tự đọc vị trí player NGAY LÚC NÀY.
                if (_timer >= config.AttackWindup)
                {
                    Enemy.ExecuteAttack();
                    _damageApplied = true;

                    // Đòn đã ra. TỪ ĐÂY quái mới thật sự đứng im — hết phần thu tay rồi tới
                    // trọn một giây nghỉ — và trong suốt quãng đó không con nào xô nó đi được.
                    Enemy.StopMoving();
                    Enemy.SetAnchored(true);
                }

                return;
            }

            // Đã ra đòn xong, chờ hết phần thu tay rồi mới sang trạng thái đứng im.
            if (_timer >= config.AttackWindup + config.AttackRecover)
                GoTo(EEnemyState.Idle);
        }
    }
}
