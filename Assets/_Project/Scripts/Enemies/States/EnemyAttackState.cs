using UnityEngine;

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
    /// 4. TRONG LÚC LẤY ĐÀ, QUÁI VẪN XOAY THEO PLAYER NHƯNG CHẬM LẠI.
    ///    Đây chính là chỗ sửa lỗi "player chỉ cần đi ngang là đòn nào cũng trượt".
    ///    Đứng im hoàn toàn thì quái vung vào chỗ trống mỗi lần player nhúc nhích.
    ///    Xoay bám 100% thì không bao giờ né được, đòn đánh mất hết ý nghĩa.
    ///    Xoay với một nửa tốc độ (chỉnh được trong config) là điểm cân bằng:
    ///    đi bộ ngang thì vẫn trúng, nhưng Dash (6 unit/giây) thì thoát được.
    /// </summary>
    public class EnemyAttackState : EnemyStateBase
    {
        private float _timer;
        private bool _damageApplied;

        public EnemyAttackState(EnemyActor enemy) : base(EnemyStateIds.Attack, enemy) { }

        public override void OnEnter()
        {
            _timer = 0f;
            _damageApplied = false;

            // Quái đứng lại trong suốt đòn đánh. Đây là tín hiệu hình ảnh quan trọng:
            // người chơi thấy quái khựng lại thì biết đòn sắp tới và có cơ hội phản ứng.
            Enemy.StopMoving();
        }

        public override void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            _timer += deltaTime;

            var config = Enemy.Config;

            if (!_damageApplied)
            {
                // Quyết định 4: bám theo player trong lúc lấy đà, với tốc độ xoay giảm bớt.
                if (config.TrackTargetDuringWindup)
                    Enemy.RotateTowardsTarget(deltaTime, config.WindupTrackingFactor);

                // Quyết định 1 và 3: tới đúng thời điểm thì ra đòn,
                // và đòn đánh tự đọc vị trí player NGAY LÚC NÀY.
                if (_timer >= config.AttackWindup)
                {
                    Enemy.ExecuteAttack();
                    _damageApplied = true;
                }

                return;
            }

            // Đã ra đòn xong, chờ hết phần thu tay rồi mới sang trạng thái đứng im.
            if (_timer >= config.AttackWindup + config.AttackRecover)
                GoTo(EnemyStateIds.Idle);
        }
    }
}
