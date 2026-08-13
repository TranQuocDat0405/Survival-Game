using System;
using Survival.Combat;
using Survival.Combat.StatusEffects;
using Survival.Pooling;
using Survival.Projectiles;
using UnityEngine;

namespace Survival.Enemies.Attacks
{
    /// <summary>
    /// Đòn bắn đạn của quái đánh xa. Spec mục 4.2:
    /// "Đạn độc: bay theo hướng trước mặt, tối đa 5 unit, tốc độ 10 unit / giây".
    ///
    /// Bản thân viên đạn KHÔNG gây sát thương va chạm (mặc định 0) — spec chỉ nói tới
    /// hiệu ứng độc, không nói đạn gây sát thương lúc trúng. Vẫn để thành số chỉnh được
    /// phòng khi cần cân bằng lại, nhưng mặc định giữ đúng 0 theo spec.
    /// </summary>
    [Serializable]
    public class ProjectileAttack : EnemyAttackDefinition
    {
        [SerializeField, Tooltip("Prefab viên đạn. Bắt buộc có component ProjectileBase.")]
        private ProjectileBase _projectilePrefab;

        [SerializeField, Min(0.1f), Tooltip("Tốc độ bay, unit/giây. Spec: 10.")]
        private float _projectileSpeed = 10f;

        [SerializeField, Min(0.1f), Tooltip("Quãng đường bay tối đa, unit. Spec: 5.")]
        private float _projectileMaxDistance = 5f;

        [SerializeField, Min(0f), Tooltip(
            "Sát thương GỐC lúc va chạm. Spec không nói đạn gây sát thương trực tiếp " +
            "nên mặc định 0; toàn bộ sát thương đến từ hiệu ứng độc.")]
        private float _impactDamage = 0f;

        [SerializeReference, Tooltip("Hiệu ứng bám lên mục tiêu khi trúng. Chọn PoisonEffect cho quái đánh xa.")]
        private StatusEffectDefinition _statusEffect = new PoisonEffect();

        public ProjectileBase ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileMaxDistance => _projectileMaxDistance;
        public float ImpactDamage => _impactDamage;
        public StatusEffectDefinition StatusEffect => _statusEffect;

        public override void Execute(EnemyAttackContext context)
        {
            if (_projectilePrefab == null || PoolService.I == null)
                return;

            // Hướng bắn lấy tại ĐÚNG thời điểm khai hoả, giống hệt cách player bắn.
            // Nhờ vậy nếu player né sang bên trong lúc quái đang lấy đà thì đạn bay trượt thật.
            Vector3 direction = context.Owner.forward;
            direction.y = 0f;
            direction.Normalize();

            var projectile = PoolService.I.Spawn(
                _projectilePrefab,
                context.MuzzlePosition,
                Quaternion.LookRotation(direction, Vector3.up));

            if (projectile == null)
                return;

            projectile.Launch(
                direction,
                _projectileSpeed,
                _projectileMaxDistance,
                _impactDamage,
                EDamageSource.EnemyProjectile,
                context.OwnerGameObject,
                context.TargetMask,
                _statusEffect != null ? new StatusEffectApplier(_statusEffect) : null);
        }
    }

    /// <summary>
    /// Nối một hiệu ứng trạng thái vào viên đạn.
    ///
    /// Nhờ lớp mỏng này mà <see cref="ProjectileBase"/> không cần biết gì về độc,
    /// về <see cref="StatusEffectHandler"/>, hay về bất kỳ hiệu ứng nào —
    /// nó chỉ gọi <c>ApplyTo</c> trên một interface.
    /// </summary>
    public class StatusEffectApplier : IProjectileEffect
    {
        private readonly StatusEffectDefinition _definition;

        public StatusEffectApplier(StatusEffectDefinition definition)
        {
            _definition = definition;
        }

        public void ApplyTo(IDamageable target, GameObject instigator)
        {
            var handler = StatusEffectHandler.Find(target);
            if (handler != null)
                handler.Apply(_definition, instigator);
        }
    }
}
