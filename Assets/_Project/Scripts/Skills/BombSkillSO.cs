using Survival.Combat;
using Survival.Pooling;
using Survival.Projectiles;
using Survival.Stats;
using UnityEngine;

namespace Survival.Skills
{
    /// <summary>
    /// Kỹ năng bổ trợ 1 — Đặt bom. Spec mục 3.2:
    /// triệu hồi bom tại vị trí hiện tại của player, sau 2 giây nổ,
    /// gây 50 sát thương gốc cho mọi kẻ địch trong bán kính 5 unit. Cooldown 12 giây.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Skills/Bomb", fileName = "Skill_Bomb")]
    public class BombSkillSO : SkillDefinition
    {
        [Header("Bom")]
        [SerializeField, Tooltip("Prefab quả bom. Bắt buộc có component BombProjectile.")]
        private BombProjectile _bombPrefab;

        [SerializeField, Min(0f), Tooltip("Thời gian từ lúc đặt tới lúc nổ, tính bằng giây. Spec: 2.")]
        private float _fuseSeconds = 2f;

        [SerializeField, Min(0f), Tooltip("Sát thương GỐC của vụ nổ, trước khi nhân DamageMultiplier. Spec: 50.")]
        private float _damage = 50f;

        [SerializeField, Min(0f), Tooltip("Bán kính vụ nổ, tính bằng unit. Spec: 5.")]
        private float _radius = 5f;

        public BombProjectile BombPrefab => _bombPrefab;
        public float FuseSeconds => _fuseSeconds;
        public float Damage => _damage;
        public float Radius => _radius;

        public override SkillRuntime CreateRuntime(SkillContext context) => new BombRuntime(this, context);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_bombPrefab == null)
                Debug.LogWarning($"[{name}] chưa gán prefab bom.", this);

            if (!Mathf.Approximately(Cooldown, 12f))
                Debug.Log($"[{name}] cooldown đang là {Cooldown}s. Spec yêu cầu 12s.", this);
        }
#endif
    }

    /// <summary>
    /// Điểm đáng chú ý: DamageMultiplier được nhân NGAY LÚC ĐẶT BOM, không phải lúc bom nổ.
    ///
    /// Sự khác biệt là có thật: nếu người chơi đặt bom rồi lên cấp trong 2 giây chờ nổ,
    /// bom sẽ nổ với sức mạnh tại thời điểm ĐẶT. Đây là lựa chọn có chủ đích —
    /// nó khớp với cách người chơi hiểu ("lúc tôi bấm nút, tôi mạnh chừng này"),
    /// và giúp quả bom không cần giữ tham chiếu ngược về người đặt (người đặt có thể đã chết).
    /// </summary>
    public class BombRuntime : SkillRuntime
    {
        private readonly BombSkillSO _def;

        public BombRuntime(BombSkillSO definition, SkillContext context) : base(definition, context)
        {
            _def = definition;
        }

        protected override void Execute()
        {
            if (_def.BombPrefab == null || PoolService.I == null)
                return;

            float multiplier = Context.Stats != null ? Context.Stats.Get(EStatType.DamageMultiplier) : 0f;
            float damage = CombatMath.ComputeOutgoing(_def.Damage, multiplier);

            // Spec nói rõ: bom đặt tại VỊ TRÍ HIỆN TẠI CỦA PLAYER (dưới chân),
            // không phải ở đầu nòng như mũi tên.
            Vector3 position = Context.Owner.position;

            var bomb = PoolService.I.Spawn(_def.BombPrefab, position, Quaternion.identity);
            if (bomb == null)
                return;

            bomb.Arm(
                _def.FuseSeconds,
                damage,
                _def.Radius,
                EDamageSource.PlayerBomb,
                Context.OwnerGameObject,
                Context.TargetMask);
        }
    }
}
