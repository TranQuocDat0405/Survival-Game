using Survival.Combat;
using Survival.Pooling;
using Survival.Projectiles;
using Survival.Stats;
using UnityEngine;

namespace Survival.Skills
{
    /// <summary>
    /// Đánh thường — bắn nhiều viên theo hình nón, có hệ thống charge.
    /// Cài đặt đúng mục 3.1 của spec.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Skills/Charged Shoot", fileName = "Skill_ChargedShoot")]
    public class ChargedShootSkillSO : SkillDefinition
    {
        [Header("Đạn")]
        [SerializeField, Tooltip("Prefab mũi tên. Bắt buộc có component ProjectileBase.")]
        private ProjectileBase _projectilePrefab;

        [SerializeField, Tooltip(
            "Góc lệch của từng viên so với hướng nhìn, tính bằng độ.\n" +
            "Spec yêu cầu 3 viên hình nón: -15, 0, +15.\n" +
            "Muốn bắn 5 viên chỉ cần thêm 2 dòng vào đây, không phải sửa code.")]
        private float[] _spreadAngles = { -15f, 0f, 15f };

        [SerializeField, Min(0f), Tooltip("Sát thương GỐC của MỖI viên. Spec: 10.")]
        private float _damagePerProjectile = 10f;

        [SerializeField, Min(0.1f), Tooltip("Tốc độ bay của mũi tên, unit/giây.")]
        private float _projectileSpeed = 18f;

        [SerializeField, Min(0.1f), Tooltip("Quãng đường tối đa mũi tên bay được, unit.")]
        private float _projectileRange = 12f;

        [Header("Charge")]
        [SerializeField, Min(1), Tooltip("Số charge tối đa. Spec: 3.")]
        private int _maxCharges = 3;

        [SerializeField, Min(0.01f), Tooltip("Cứ ngần này giây thì hồi 1 charge, chỉ khi chưa đầy. Spec: 3 giây.")]
        private float _chargeRegenTime = 3f;

        public ProjectileBase ProjectilePrefab => _projectilePrefab;
        public float[] SpreadAngles => _spreadAngles;
        public float DamagePerProjectile => _damagePerProjectile;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileRange => _projectileRange;
        public int MaxCharges => _maxCharges;
        public float ChargeRegenTime => _chargeRegenTime;

        public override SkillRuntime CreateRuntime(SkillContext context)
            => new ChargedShootRuntime(this, context);

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Cooldown của skill này CHÍNH LÀ luật "cách nhau tối thiểu 0.5 giây" của spec.
            if (Cooldown <= 0f)
                Debug.LogWarning($"[{name}] Cooldown đang bằng 0. Spec yêu cầu 2 lần bắn cách nhau tối thiểu 0.5 giây.", this);

            if (_projectilePrefab == null)
                Debug.LogWarning($"[{name}] chưa gán prefab đạn.", this);

            if (_spreadAngles == null || _spreadAngles.Length == 0)
                Debug.LogWarning($"[{name}] danh sách góc bắn đang trống, sẽ không bắn ra viên nào.", this);
        }
#endif
    }

    /// <summary>
    /// Trạng thái lúc chạy của skill bắn. Đây là nơi 6 luật của spec mục 3.1 được thực thi:
    ///
    ///   1. Mỗi lần bắn ra nhiều viên cùng lúc theo hình nón −15° / 0° / +15°  -> Execute()
    ///   2. Sát thương mỗi viên là 10 gốc, rồi nhân DamageMultiplier            -> Execute()
    ///   3. Tối đa 3 charge                                                     -> _charges, ChargeCount
    ///   4. Mỗi phát bắn tốn 1 charge                                           -> Execute()
    ///   5. Hồi +1 charge mỗi 3 giây, CHỈ KHI chưa đầy                          -> Tick()
    ///   6. Hai lần bắn cách nhau tối thiểu 0.5 giây, không phụ thuộc charge     -> CanUse + cooldown lớp cha
    /// </summary>
    public class ChargedShootRuntime : SkillRuntime
    {
        private readonly ChargedShootSkillSO _def;

        private int _charges;
        private float _regenTimer;

        public ChargedShootRuntime(ChargedShootSkillSO definition, SkillContext context)
            : base(definition, context)
        {
            _def = definition;
            _charges = definition.MaxCharges;   // bắt đầu màn với charge đầy
        }

        public override int ChargeCount => _charges;
        public override int MaxCharges => _def.MaxCharges;

        /// <summary>
        /// Luật 6 (0.5 giây giữa 2 phát) VÀ luật "không đủ charge thì không bắn được"
        /// là hai điều kiện ĐỘC LẬP, phải thoả cả hai.
        /// <c>base.CanUse</c> lo phần 0.5 giây; phần charge kiểm tra ở đây.
        /// </summary>
        public override bool CanUse => base.CanUse && _charges > 0;

        /// <summary>
        /// Nút bắn hiển thị tiến độ hồi charge chứ không phải cooldown 0.5 giây —
        /// vì 0.5 giây trôi qua quá nhanh để mắt kịp thấy, còn cái người chơi thực sự
        /// cần biết là "bao giờ mới có thêm charge".
        /// Khi charge đã đầy thì nút hiện đầy luôn.
        /// </summary>
        public override float CooldownNormalized
        {
            get
            {
                if (_charges >= _def.MaxCharges)
                    return 1f;
                return Mathf.Clamp01(_regenTimer / _def.ChargeRegenTime);
            }
        }

        /// <summary>Số giây còn lại tới charge kế tiếp. Bằng 0 khi đã đầy charge.</summary>
        public override float CooldownRemaining
        {
            get
            {
                if (_charges >= _def.MaxCharges)
                    return 0f;
                return Mathf.Max(0f, _def.ChargeRegenTime - _regenTimer);
            }
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);   // đếm lùi khoảng cách 0.5 giây giữa 2 phát bắn

            // Luật 5: chỉ hồi khi CHƯA đầy. Khi đã đầy thì đồng hồ đứng yên ở 0,
            // nên vừa bắn một phát là bắt đầu đếm lại từ đầu, không được cộng dồn sẵn.
            if (_charges >= _def.MaxCharges)
            {
                _regenTimer = 0f;
                return;
            }

            _regenTimer += deltaTime;

            // Dùng while thay vì if: nếu một khung hình bị kéo dài bất thường (máy khựng),
            // ta vẫn hồi đủ số charge đáng lẽ phải hồi, thay vì mất phần dư.
            while (_regenTimer >= _def.ChargeRegenTime && _charges < _def.MaxCharges)
            {
                _regenTimer -= _def.ChargeRegenTime;
                _charges++;
            }

            if (_charges >= _def.MaxCharges)
                _regenTimer = 0f;
        }

        protected override void Execute()
        {
            _charges--;   // luật 4

            var prefab = _def.ProjectilePrefab;
            var angles = _def.SpreadAngles;
            if (prefab == null || angles == null || angles.Length == 0 || PoolService.I == null)
                return;

            // Luật 2: nhân hệ số sát thương ĐÚNG MỘT LẦN cho cả loạt bắn,
            // rồi mới chia cho từng viên. Mỗi viên nhận cùng con số đã tính sẵn.
            float multiplier = Context.Stats != null ? Context.Stats.Get(EStatType.DamageMultiplier) : 0f;
            float damagePerProjectile = CombatMath.ComputeOutgoing(_def.DamagePerProjectile, multiplier);

            // Hướng bắn lấy tại ĐÚNG THỜI ĐIỂM NÀY, không phải hướng joystick.
            // Nếu người chơi vừa bẻ joystick mà thân chưa xoay xong thì đạn bay theo hướng cũ,
            // đúng như spec mô tả ở mục 2.1.
            Vector3 forward = Context.Forward;
            Vector3 origin = Context.SpawnPosition;

            for (int i = 0; i < angles.Length; i++)
            {
                // Xoay quanh trục đứng để tạo hình nón. Quay hướng forward đi angles[i] độ.
                Vector3 direction = Quaternion.AngleAxis(angles[i], Vector3.up) * forward;

                var projectile = PoolService.I.Spawn(prefab, origin, Quaternion.LookRotation(direction, Vector3.up));
                if (projectile == null)
                    continue;

                projectile.Launch(
                    direction,
                    _def.ProjectileSpeed,
                    _def.ProjectileRange,
                    damagePerProjectile,
                    EDamageSource.PlayerBullet,
                    Context.OwnerGameObject,
                    Context.TargetMask);
            }
        }
    }
}
