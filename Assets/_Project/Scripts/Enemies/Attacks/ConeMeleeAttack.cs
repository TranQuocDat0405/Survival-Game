using System;
using Survival.Combat;
using UnityEngine;

namespace Survival.Enemies.Attacks
{
    /// <summary>
    /// Đòn chém hình nón của quái đánh gần.
    /// Spec mục 4.1: hình nón 50 độ, tầm 1.3 unit, 30 sát thương gốc, mỗi mục tiêu trúng 1 lần.
    ///
    /// CÁCH KIỂM TRA HÌNH NÓN:
    /// Unity không có sẵn phép kiểm tra hình nón, nên làm hai bước:
    ///   Bước 1 - lấy mọi thứ nằm trong hình CẦU bán kính 1.3 quanh quái (nhanh, do vật lý lo).
    ///   Bước 2 - với từng thứ lấy được, tính GÓC giữa hướng quái đang nhìn và hướng tới mục tiêu.
    ///            Góc nhỏ hơn 25 độ (một nửa của 50) thì nằm trong nón.
    /// Chia đôi vì "nón 50 độ" nghĩa là mở 25 độ về mỗi bên so với hướng nhìn.
    /// </summary>
    [Serializable]
    public class ConeMeleeAttack : EnemyAttackDefinition
    {
        [SerializeField, Range(1f, 360f), Tooltip("Góc mở của hình nón, tính bằng độ. Spec: 50.")]
        private float _coneAngle = 50f;

        [SerializeField, Min(0f), Tooltip("Sát thương GỐC. Giáp của player được trừ ở phía nhận. Spec: 30.")]
        private float _damage = 30f;

        [SerializeField, Tooltip(
            "Nâng điểm gốc của hình cầu kiểm tra lên ngang thân người, tính bằng unit.\n" +
            "Nếu để 0, gốc nằm dưới chân quái và có thể trượt mục tiêu đứng sát ngay trước mặt.")]
        private float _heightOffset = 0.8f;

        /// <summary>
        /// Bộ đệm dùng lại cho mọi lần kiểm tra, cấp phát MỘT lần duy nhất cho toàn bộ vòng đời game.
        ///
        /// <c>Physics.OverlapSphere</c> (không có "NonAlloc") tạo một mảng MỚI mỗi lần gọi.
        /// 6 con quái đánh liên tục = hàng trăm mảng rác mỗi phút, và bộ dọn rác GC sẽ phải
        /// dừng game lại để dọn. Bản "NonAlloc" ghi vào mảng có sẵn nên không sinh rác.
        ///
        /// Dùng <c>static</c> được vì đòn đánh xảy ra tức thời trong một khung hình,
        /// không có hai đòn nào dùng bộ đệm cùng lúc.
        /// </summary>
        private static readonly Collider[] OverlapBuffer = new Collider[16];

        public float ConeAngle => _coneAngle;
        public float Damage => _damage;

        public override void Execute(EnemyAttackContext context)
        {
            Vector3 origin = context.Owner.position + Vector3.up * _heightOffset;
            Vector3 forward = context.Owner.forward;

            int count = Physics.OverlapSphereNonAlloc(
                origin, _range, OverlapBuffer, context.TargetMask, QueryTriggerInteraction.Collide);

            float halfAngle = _coneAngle * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var collider = OverlapBuffer[i];
                if (collider == null)
                    continue;

                var damageable = collider.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                    continue;

                // Bỏ chiều cao khi tính góc: quái và player đứng trên cùng mặt phẳng,
                // nếu tính cả trục Y thì mục tiêu cao hơn hoặc thấp hơn sẽ bị tính là ngoài nón.
                Vector3 toTarget = damageable.Transform.position - context.Owner.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude < 0.0001f)
                {
                    // Đứng chồng lên nhau: không có hướng để tính góc, coi như trúng.
                    ApplyDamage(damageable, context, damageable.Transform.position);
                    continue;
                }

                if (Vector3.Angle(forward, toTarget) > halfAngle)
                    continue;   // ngoài hình nón -> trượt

                ApplyDamage(damageable, context, damageable.Transform.position);
            }

            // Mỗi mục tiêu chỉ xuất hiện một lần trong kết quả OverlapSphere,
            // nên "1 lần mỗi đòn" theo spec được bảo đảm tự nhiên, không cần danh sách chống trùng.
        }

        private void ApplyDamage(IDamageable target, EnemyAttackContext context, Vector3 hitPoint)
        {
            // Sát thương của quái KHÔNG nhân DamageMultiplier — theo spec, hệ số đó chỉ thuộc về player.
            // Việc trừ giáp do phía nhận tự làm trong Health.
            var info = new DamageInfo(_damage, EDamageSource.EnemyMelee, context.OwnerGameObject, hitPoint);
            target.TakeDamage(in info);
        }
    }
}
