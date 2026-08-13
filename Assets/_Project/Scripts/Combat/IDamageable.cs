using UnityEngine;

namespace Survival.Combat
{
    /// <summary>
    /// Bất cứ thứ gì có thể ăn sát thương đều cài đặt interface này.
    ///
    /// Nhờ nó mà viên đạn không cần biết nó vừa trúng player, quái, hay một cái thùng gỗ —
    /// nó chỉ hỏi "mày có <see cref="IDamageable"/> không?" rồi gọi <see cref="TakeDamage"/>.
    /// Sau này thêm mục tiêu mới (trụ, chướng ngại phá được...) thì đạn không cần sửa gì.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        Transform Transform { get; }

        /// <summary>
        /// Từ khoá <c>in</c> nghĩa là truyền tham chiếu chỉ-đọc: không sao chép lại struct,
        /// đồng thời cấm hàm này sửa nội dung đòn đánh.
        /// </summary>
        void TakeDamage(in DamageInfo info);
    }
}
