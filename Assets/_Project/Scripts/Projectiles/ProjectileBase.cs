using NFramework;
using Survival.Combat;
using UnityEngine;

namespace Survival.Projectiles
{
    /// <summary>
    /// Viên đạn / mũi tên bay thẳng, dùng chung cho cả mũi tên của player lẫn đạn độc của quái.
    ///
    /// Kế thừa <see cref="PooledObject"/> nên nó được tái sử dụng thay vì tạo/huỷ liên tục.
    ///
    /// CÁCH PHÁT HIỆN VA CHẠM — đây là chỗ dễ sinh lỗi "đạn xuyên qua quái":
    /// Nếu chỉ dùng collider thường, Unity kiểm tra va chạm ở từng vị trí RỜI RẠC mỗi bước vật lý.
    /// Đạn bay 10 unit/giây, mỗi bước vật lý 0.02 giây -> mỗi bước nhảy 0.2 unit.
    /// Con quái mỏng hơn 0.2 unit thì đạn có thể "nhảy qua" mà không chạm vào.
    /// Nên ở đây ta không để đạn tự va chạm: mỗi bước ta tự bắn một tia hình cầu
    /// (<c>SphereCast</c>) từ vị trí cũ tới vị trí mới, quét sạch khoảng giữa.
    /// Đạn bay nhanh cỡ nào cũng không thể lọt.
    /// </summary>
    public class ProjectileBase : PooledObject
    {
        [SerializeField, Tooltip("Bán kính của tia quét. To hơn một chút thì bắn trúng dễ chịu hơn.")]
        private float _radius = 0.15f;

        [SerializeField, Tooltip("Hiệu ứng sinh ra tại điểm trúng. Có thể để trống.")]
        private PooledObject _hitEffectPrefab;

        [SerializeField, Tooltip("Tự động trả về pool sau ngần này giây, phòng khi bay mãi không trúng gì.")]
        private float _maxLifeTime = 6f;

        private float _speed;
        private float _maxDistance;
        private float _travelled;
        private float _lifeTimer;
        private LayerMask _hitMask;
        private DamageInfo _damageTemplate;
        private IProjectileEffect _onHitEffect;
        private bool _isActive;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[8];

        /// <summary>
        /// Nạp thông số cho viên đạn rồi cho nó bay. Gọi ngay sau khi lấy từ pool.
        /// </summary>
        /// <param name="rawDamage">Sát thương gốc, ĐÃ nhân DamageMultiplier nếu người bắn là player.</param>
        /// <param name="onHitEffect">Hiệu ứng phụ khi trúng (ví dụ dính độc). Có thể null.</param>
        public void Launch(
            Vector3 direction,
            float speed,
            float maxDistance,
            float rawDamage,
            EDamageSource source,
            GameObject instigator,
            LayerMask hitMask,
            IProjectileEffect onHitEffect = null)
        {
            transform.forward = direction;

            _speed = speed;
            _maxDistance = maxDistance;
            _travelled = 0f;
            _lifeTimer = 0f;
            _hitMask = hitMask;
            _onHitEffect = onHitEffect;
            _damageTemplate = new DamageInfo(rawDamage, source, instigator, transform.position);
            _isActive = true;
        }

        public override void OnBeforeReturnToPool()
        {
            base.OnBeforeReturnToPool();
            _isActive = false;
            _onHitEffect = null;
        }

        private void Update()
        {
            if (!_isActive)
                return;

            float deltaTime = Time.deltaTime;

            _lifeTimer += deltaTime;
            if (_lifeTimer >= _maxLifeTime)
            {
                ReturnToPool();
                return;
            }

            float step = _speed * deltaTime;

            // Bay quá tầm cho phép thì dừng ngay tại mốc tầm, không bay lố.
            if (_travelled + step >= _maxDistance)
            {
                step = _maxDistance - _travelled;
                if (SweepAndHit(step))
                    return;

                transform.position += transform.forward * step;
                ReturnToPool();
                return;
            }

            if (SweepAndHit(step))
                return;

            transform.position += transform.forward * step;
            _travelled += step;
        }

        /// <summary>Quét đoạn đường sắp đi. Trả về true nếu đã trúng thứ gì đó và viên đạn đã kết thúc.</summary>
        private bool SweepAndHit(float step)
        {
            if (step <= 0f)
                return false;

            // SphereCastNonAlloc ghi kết quả vào mảng có sẵn thay vì cấp phát mảng mới mỗi lần gọi
            // -> bắn liên tục cũng không sinh rác cho bộ dọn rác.
            int count = Physics.SphereCastNonAlloc(
                transform.position,
                _radius,
                transform.forward,
                HitBuffer,
                step,
                _hitMask,
                QueryTriggerInteraction.Collide);

            if (count == 0)
                return false;

            // Lấy vật gần nhất trong số những vật bị quét trúng.
            int nearestIndex = -1;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (HitBuffer[i].distance >= nearestDistance)
                    continue;

                nearestDistance = HitBuffer[i].distance;
                nearestIndex = i;
            }

            if (nearestIndex < 0)
                return false;

            var hit = HitBuffer[nearestIndex];
            Vector3 hitPoint = hit.point != Vector3.zero ? hit.point : transform.position;

            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                var info = new DamageInfo(
                    _damageTemplate.RawAmount,
                    _damageTemplate.Source,
                    _damageTemplate.Instigator,
                    hitPoint);

                damageable.TakeDamage(in info);
                _onHitEffect?.ApplyTo(damageable, _damageTemplate.Instigator);
            }

            SpawnHitEffect(hitPoint);
            ReturnToPool();
            return true;
        }

        private void SpawnHitEffect(Vector3 position)
        {
            if (_hitEffectPrefab == null || Pooling.PoolService.I == null)
                return;

            Pooling.PoolService.I.Spawn(_hitEffectPrefab, position, Quaternion.identity);
        }
    }
}
