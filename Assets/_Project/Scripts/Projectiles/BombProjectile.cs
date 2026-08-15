using NFramework;
using Survival.Combat;
using Survival.Pooling;
using UnityEngine;

namespace Survival.Projectiles
{
    /// <summary>
    /// Quả bom nằm tại chỗ, đếm ngược rồi nổ.
    /// Spec mục 3.2: nổ sau 2 giây, 50 sát thương gốc, bán kính 5 unit.
    ///
    /// Bom KHÔNG tự biết mấy con số đó — chúng được truyền vào từ skill khi đặt bom.
    /// Nhờ vậy cùng một prefab bom có thể dùng cho nhiều skill khác nhau
    /// (ví dụ sau này thêm "bom lớn" bán kính 8) mà không cần prefab mới.
    /// </summary>
    public class BombProjectile : PooledObject
    {
        [SerializeField, Tooltip("Hiệu ứng vụ nổ. Có thể để trống.")]
        private PooledObject _explosionEffectPrefab;

        [SerializeField, Tooltip("Nhân kích thước hiệu ứng nổ cho khớp bán kính thật. 1 nghĩa là hiệu ứng gốc bán kính 1 unit.")]
        private float _effectScalePerUnitRadius = 1f;

        private float _fuseTimer;
        private float _damage;
        private float _radius;
        private LayerMask _targetMask;
        private GameObject _instigator;
        private EDamageSource _source;
        private bool _armed;

        /// <summary>Bắt đầu đếm ngược. Gọi ngay sau khi lấy bom ra khỏi pool.</summary>
        public void Arm(float fuseSeconds, float rawDamage, float radius, EDamageSource source, GameObject instigator, LayerMask targetMask)
        {
            _fuseTimer = fuseSeconds;
            _damage = rawDamage;
            _radius = radius;
            _source = source;
            _instigator = instigator;
            _targetMask = targetMask;
            _armed = true;
        }

        public override void OnBeforeReturnToPool()
        {
            base.OnBeforeReturnToPool();
            _armed = false;
        }

        private void Update()
        {
            if (!_armed)
                return;

            _fuseTimer -= Time.deltaTime;
            if (_fuseTimer > 0f)
                return;

            _armed = false;
            Explode();
        }

        private void Explode()
        {
            AreaDamage.Explode(transform.position, _radius, _damage, _source, _instigator, _targetMask);

            if (_explosionEffectPrefab != null && PoolService.I != null)
            {
                var effect = PoolService.I.Spawn(_explosionEffectPrefab, transform.position, Quaternion.identity);
                if (effect != null)
                    effect.transform.localScale = Vector3.one * (_radius * _effectScalePerUnitRadius);
            }

            // Bom là cú mạnh nhất trong game nên đây là một trong hai chỗ duy nhất được rung camera.
            // Độ rung nằm trong CameraShakeService chứ không ở đây, để mọi mức rung của game
            // đọc được cạnh nhau trên cùng một Inspector thay vì rải khắp các prefab.
            CameraRig.CameraShakeService.I?.ShakeOnBombExplosion();
            Audio.GameAudioService.PlayBombExplode();

            ReturnToPool();
        }

#if UNITY_EDITOR
        /// <summary>Vẽ bán kính nổ lên Scene View để kiểm tra bằng mắt có đúng 5 unit không.</summary>
        private void OnDrawGizmos()
        {
            if (!_armed)
                return;

            Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
#endif
    }
}
