using UnityEngine;

namespace Survival.Combat
{
    /// <summary>Nguồn gây sát thương. Dùng để chọn hiệu ứng hình ảnh / âm thanh và để lọc khi cần.</summary>
    public enum EDamageSource
    {
        PlayerBullet = 0,
        PlayerBomb = 1,
        PlayerDash = 2,
        EnemyMelee = 3,
        EnemyProjectile = 4,
        Poison = 5,
    }

    /// <summary>
    /// Gói dữ liệu mô tả một đòn đánh đang bay tới mục tiêu.
    ///
    /// Đây là <c>readonly struct</c> chứ không phải class: struct nằm trên stack nên
    /// không cấp phát bộ nhớ heap, tức là bắn 100 viên đạn cũng không sinh rác cho GC.
    /// (GC = Garbage Collector, bộ dọn rác của .NET; nó chạy thì game bị khựng một nhịp.)
    /// Từ khoá <c>readonly</c> đảm bảo không ai lỡ tay sửa dữ liệu đòn đánh giữa chừng.
    ///
    /// Lưu ý quan trọng: <see cref="RawAmount"/> là sát thương GỐC (chưa trừ giáp),
    /// nhưng ĐÃ nhân DamageMultiplier nếu người gây sát thương là player.
    /// Việc trừ giáp do phía nhận (<c>Health</c>) tự làm, vì chỉ nó mới biết giáp của mình.
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>Sát thương gốc, trước khi trừ giáp của mục tiêu.</summary>
        public readonly float RawAmount;

        public readonly EDamageSource Source;

        /// <summary>Ai gây ra đòn này. Có thể null (ví dụ tick độc sau khi quái bắn đã chết).</summary>
        public readonly GameObject Instigator;

        /// <summary>Điểm va chạm, dùng để đặt hiệu ứng trúng đòn và số sát thương bay lên.</summary>
        public readonly Vector3 HitPoint;

        public DamageInfo(float rawAmount, EDamageSource source, GameObject instigator, Vector3 hitPoint)
        {
            RawAmount = rawAmount;
            Source = source;
            Instigator = instigator;
            HitPoint = hitPoint;
        }
    }
}
