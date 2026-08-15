using System.Collections;
using Survival.Combat;
using Survival.Pooling;
using Survival.Stats;
using UnityEngine;
using NFramework;

namespace Survival.Skills
{
    /// <summary>
    /// Kỹ năng bổ trợ 2 — Dash rồi nổ. Spec mục 3.3:
    /// đẩy player theo hướng forward 3 unit trong 0.5 giây;
    /// hết lướt thì nổ gây 15 sát thương gốc trong bán kính 3 unit. Cooldown 6 giây.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Skills/Dash", fileName = "Skill_Dash")]
    public class DashSkillSO : SkillDefinition
    {
        [Header("Lướt")]
        [SerializeField, Min(0f), Tooltip("Quãng đường lướt, tính bằng unit. Spec: 3.")]
        private float _distance = 3f;

        [SerializeField, Min(0.01f), Tooltip("Thời gian lướt, tính bằng giây. Spec: 0.5.")]
        private float _duration = 0.5f;

        [Header("Vụ nổ khi kết thúc")]
        [SerializeField, Min(0f), Tooltip("Sát thương GỐC. Spec: 15.")]
        private float _damage = 15f;

        [SerializeField, Min(0f), Tooltip("Bán kính vụ nổ, tính bằng unit. Spec: 3.")]
        private float _radius = 3f;

        [SerializeField, Tooltip("Hiệu ứng nổ cuối đường lướt. Có thể để trống.")]
        private PooledObject _explosionEffectPrefab;

        [SerializeField, Tooltip("Nhân kích thước hiệu ứng nổ cho khớp bán kính thật.")]
        private float _effectScalePerUnitRadius = 1f;

        /// <summary>Tốc độ lướt suy ra từ quãng đường và thời gian. Spec 3 unit / 0.5 giây = 6 unit/giây.</summary>
        public float DashSpeed => _distance / _duration;

        public float Distance => _distance;
        public float Duration => _duration;
        public float Damage => _damage;
        public float Radius => _radius;
        public PooledObject ExplosionEffectPrefab => _explosionEffectPrefab;
        public float EffectScalePerUnitRadius => _effectScalePerUnitRadius;

        public override SkillRuntime CreateRuntime(SkillContext context) => new DashRuntime(this, context);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Mathf.Approximately(Cooldown, 6f))
                Debug.Log($"[{name}] cooldown đang là {Cooldown}s. Spec yêu cầu 6s.", this);
        }
#endif
    }

    public class DashRuntime : SkillRuntime
    {
        /// <summary>
        /// Dùng lại một đối tượng chờ duy nhất cho mọi lần dash.
        /// Viết <c>yield return new WaitForFixedUpdate()</c> trong vòng lặp sẽ cấp phát
        /// một đối tượng mới ở MỖI bước — 25 bước mỗi lần dash, và dash dùng liên tục cả trận.
        /// Đối tượng này không giữ trạng thái gì nên dùng chung hoàn toàn an toàn.
        /// </summary>
        private static readonly WaitForFixedUpdate WaitForFixedStep = new WaitForFixedUpdate();

        private readonly DashSkillSO _def;
        private bool _isDashing;

        public DashRuntime(DashSkillSO definition, SkillContext context) : base(definition, context)
        {
            _def = definition;
        }

        /// <summary>Không cho dash chồng lên dash. Cooldown 6 giây đã chặn rồi, đây là lớp bảo vệ thứ hai.</summary>
        public override bool CanUse => base.CanUse && !_isDashing;

        protected override void Execute()
        {
            if (Context.CoroutineRunner == null)
                return;

            Context.CoroutineRunner.StartCoroutine(DashRoutine());
        }

        private IEnumerator DashRoutine()
        {
            _isDashing = true;
            Context.SetControlLocked?.Invoke(true);

            // Hướng lướt được CHỐT MỘT LẦN tại thời điểm bấm nút, đúng theo spec
            // "dùng hướng forward hiện tại của nhân vật". Nếu đọc lại forward mỗi khung hình
            // thì đường lướt sẽ bị bẻ cong khi người chơi ngoáy joystick giữa chừng.
            Vector3 direction = Context.Owner.forward;
            direction.y = 0f;
            direction.Normalize();

            var rigidbody = Context.OwnerRigidbody;
            float speed = _def.DashSpeed;
            float elapsed = 0f;

            // Đẩy bằng vận tốc của Rigidbody chứ không dịch thẳng transform:
            // nhờ vậy nếu lướt vào tường thì hệ vật lý chặn lại, không xuyên qua tường.
            // Hệ quả có chủ đích: lướt vào tường thì đi được ngắn hơn 3 unit — đúng và hợp lý.
            //
            // Vòng lặp đồng bộ theo NHỊP VẬT LÝ (FixedUpdate) chứ không theo nhịp khung hình.
            // Vận tốc chỉ được hệ vật lý đọc ở mỗi bước vật lý; nếu đếm giờ theo khung hình
            // (60, 144, hay 30 khung/giây tuỳ máy) thì số bước vật lý thực sự chạy sẽ lệch,
            // và quãng đường lướt sẽ không còn đúng 3 unit trên mọi máy.
            // Đếm theo bước vật lý thì 0.5 giây luôn là đúng 25 bước x 0.02 giây,
            // cho ra 25 x 0.02 x 6 = 3.00 unit, giống nhau ở mọi cấu hình.
            while (elapsed < _def.Duration)
            {
                if (rigidbody != null)
                    rigidbody.velocity = direction * speed;

                yield return WaitForFixedStep;
                elapsed += Time.fixedDeltaTime;
            }

            if (rigidbody != null)
                rigidbody.velocity = Vector3.zero;

            Context.SetControlLocked?.Invoke(false);
            _isDashing = false;

            Explode();
        }

        private void Explode()
        {
            float multiplier = Context.Stats != null ? Context.Stats.Get(EStatType.DamageMultiplier) : 0f;
            float damage = CombatMath.ComputeOutgoing(_def.Damage, multiplier);

            Vector3 center = Context.Owner.position;

            AreaDamage.Explode(
                center, _def.Radius, damage,
                EDamageSource.PlayerDash,
                Context.OwnerGameObject,
                Context.TargetMask);

            if (_def.ExplosionEffectPrefab != null && PoolService.I != null)
            {
                var effect = PoolService.I.Spawn(_def.ExplosionEffectPrefab, center, Quaternion.identity);
                if (effect != null)
                    effect.transform.localScale = Vector3.one * (_def.Radius * _def.EffectScalePerUnitRadius);
            }

            // Mặc định độ rung của cú này đang để 0, tức không rung — vì dash được dùng rất
            // thường xuyên để né đòn. Vẫn gọi ở đây để ai muốn bật lại thì chỉ cần đổi một số
            // trên Inspector, không phải sửa code.
            Survival.CameraRig.CameraShakeService.I?.ShakeOnDashExplosion();
        }
    }
}
