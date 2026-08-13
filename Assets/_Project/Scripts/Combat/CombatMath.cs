using UnityEngine;

namespace Survival.Combat
{
    /// <summary>
    /// Nơi duy nhất trong toàn bộ project chứa công thức sát thương của đề bài.
    ///
    /// Mọi nguồn sát thương — đạn, bom, nổ dash, đòn chém của quái, mỗi tick độc —
    /// đều bắt buộc đi qua hai hàm dưới đây. Không có chỗ nào tự cộng trừ máu tay.
    /// Nhờ vậy khi cần kiểm chứng "combat có đi qua công thức đã cho hay không"
    /// thì chỉ cần đọc đúng file này.
    ///
    /// Trích nguyên văn spec (Docs/README.md mục 2.2):
    ///     Sát thương nhận  = Sát thương gốc − Giáp                (nhỏ hơn 0 thì tính bằng 0)
    ///     Sát thương gây ra = Sát thương gốc × (1 + DamageMultiplier)
    /// </summary>
    public static class CombatMath
    {
        /// <summary>
        /// Sát thương mà phe tấn công gây ra, sau khi nhân hệ số DamageMultiplier.
        /// Ví dụ: đạn gốc 10, DamageMultiplier 0.2 (player cấp 3) -> 10 × 1.2 = 12.
        /// </summary>
        public static float ComputeOutgoing(float rawDamage, float damageMultiplier)
        {
            return rawDamage * (1f + damageMultiplier);
        }

        /// <summary>
        /// Sát thương thực sự bị trừ vào máu, sau khi trừ giáp. Không bao giờ âm.
        /// Ví dụ: quái đánh 30, player có 4 giáp -> nhận 26. Quái đánh 2, player có 10 giáp -> nhận 0.
        /// </summary>
        public static float ComputeIncoming(float rawDamage, float armor)
        {
            return Mathf.Max(0f, rawDamage - armor);
        }
    }
}
