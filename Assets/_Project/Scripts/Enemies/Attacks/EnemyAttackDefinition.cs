using System;
using UnityEngine;

namespace Survival.Enemies.Attacks
{
    /// <summary>
    /// Cách một con quái ra đòn.
    ///
    /// Đây là lớp cha trừu tượng, KHÔNG phải ScriptableObject mà là class thường có
    /// <c>[Serializable]</c>. Lý do: nó được nhúng thẳng vào <c>EnemyConfigSO</c> bằng
    /// <c>[SerializeReference]</c> — một tính năng của Unity cho phép một ô trên Inspector
    /// chứa được nhiều KIỂU khác nhau, chọn qua danh sách xổ xuống.
    ///
    /// Nhờ vậy quái đánh gần và quái đánh xa dùng CHUNG một file config và CHUNG một script AI;
    /// khác biệt duy nhất là ô "Attack" của con này chọn <c>ConeMeleeAttack</c>,
    /// con kia chọn <c>ProjectileAttack</c>.
    ///
    /// Thêm kiểu đòn thứ ba (ví dụ nhảy bổ gây sát thương vùng) = viết thêm một lớp con.
    /// Script AI, config, và mọi thứ khác không phải sửa gì.
    /// </summary>
    [Serializable]
    public abstract class EnemyAttackDefinition
    {
        [SerializeField, Min(0f), Tooltip(
            "Tầm mà quái cần vào tới để bắt đầu ra đòn, tính bằng unit.\n" +
            "Spec: quái đánh gần 1.3, quái đánh xa 3.")]
        protected float _range = 1.3f;

        public float Range => _range;

        /// <summary>
        /// Thực thi đòn đánh. Được gọi tại ĐÚNG thời điểm gây sát thương trong animation,
        /// không phải lúc bắt đầu vung tay — xem <c>EnemyAttackState</c> để hiểu vì sao.
        /// </summary>
        public abstract void Execute(EnemyAttackContext context);
    }

    /// <summary>Mọi thứ một đòn đánh cần biết để thực thi.</summary>
    public class EnemyAttackContext
    {
        /// <summary>Transform của con quái đang ra đòn.</summary>
        public Transform Owner;

        public GameObject OwnerGameObject;

        /// <summary>Điểm sinh đạn, đặt ở tay hoặc miệng quái. Có thể trùng Owner.</summary>
        public Transform Muzzle;

        /// <summary>Layer được tính là mục tiêu hợp lệ (thường chỉ có layer Player).</summary>
        public LayerMask TargetMask;

        public Vector3 MuzzlePosition => Muzzle != null ? Muzzle.position : Owner.position;
    }
}
