using System;
using NFramework;
using Survival.Stats;
using UnityEngine;

namespace Survival.Combat
{
    /// <summary>
    /// Máu của một nhân vật. Dùng chung cho cả player lẫn quái — không có hai bản riêng.
    ///
    /// Đây là chỗ duy nhất trừ máu, và nó luôn trừ qua <see cref="CombatMath.ComputeIncoming"/>,
    /// nên giáp chắc chắn được áp dụng cho mọi nguồn sát thương (đòn chém, đạn, và từng tick độc).
    /// </summary>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// Delegate riêng thay vì <c>Action</c> để truyền <see cref="DamageInfo"/> bằng <c>in</c>,
        /// tránh sao chép struct mỗi lần bắn sự kiện.
        /// </summary>
        public delegate void DamageAppliedHandler(Health target, float appliedDamage, in DamageInfo info);

        [SerializeField, Tooltip("Máu tối đa dùng khi component này chưa được gắn StatSet (chủ yếu để test nhanh trong scene).")]
        private float _fallbackMaxHealth = 100f;

        private IStatProvider _stats;

        /// <summary>
        /// Máu hiện tại. Dùng <c>ObservableValue</c> của nframework: mỗi lần giá trị đổi nó tự bắn sự kiện,
        /// nên thanh máu trên UI chỉ cần đăng ký nghe một lần rồi ngồi im —
        /// không phải đọc lại máu mỗi khung hình trong Update.
        /// </summary>
        public readonly ObservableValue<float> Current = new ObservableValue<float>();

        /// <summary>Bắn ra sau khi đã trừ máu. Tham số là sát thương THỰC SỰ trừ được (đã trừ giáp).</summary>
        public event DamageAppliedHandler OnDamaged;

        /// <summary>Bắn ra đúng một lần tại thời điểm máu chạm 0.</summary>
        public event Action<Health> OnDied;

        /// <summary>Bắn ra khi được hồi máu. Dùng cho hiệu ứng lên cấp.</summary>
        public event Action<Health, float> OnHealed;

        /// <summary>
        /// Bắn ra khi máu TỐI ĐA đổi (lên cấp +40 máu tối đa).
        /// Thanh máu phải nghe cả sự kiện này, không chỉ nghe máu hiện tại,
        /// nếu không tỉ lệ vẽ ra sẽ sai cho tới lần trúng đòn kế tiếp.
        /// </summary>
        public event Action<Health> OnMaxChanged;

        public bool IsAlive { get; private set; }

        public Transform Transform => transform;

        public float Max => _stats != null ? _stats.Get(EStatType.MaxHealth) : _fallbackMaxHealth;

        public float Normalized
        {
            get
            {
                float max = Max;
                return max > 0f ? Mathf.Clamp01(Current.Value / max) : 0f;
            }
        }

        /// <summary>
        /// Gắn nguồn chỉ số rồi hồi đầy máu. Gọi mỗi khi nhân vật được sinh ra
        /// hoặc được lấy lại từ pool (pool = tái sử dụng object cũ thay vì tạo mới, xem PoolService).
        /// </summary>
        public void Initialize(IStatProvider stats)
        {
            if (_stats != null)
                _stats.OnStatChanged -= HandleStatChanged;

            _stats = stats;

            if (_stats != null)
                _stats.OnStatChanged += HandleStatChanged;

            ResetToFull();
        }

        private void OnDestroy()
        {
            if (_stats != null)
                _stats.OnStatChanged -= HandleStatChanged;
        }

        private void HandleStatChanged(EStatType type, float value)
        {
            if (type != EStatType.MaxHealth)
                return;

            // Máu hiện tại không được vượt quá trần mới (phòng khi trần bị hạ xuống).
            if (Current.Value > value)
                Current.Value = value;

            OnMaxChanged?.Invoke(this);
        }

        public void ResetToFull()
        {
            IsAlive = true;
            Current.ForceSet(Max);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive)
                return;

            float armor = _stats != null ? _stats.Get(EStatType.Armor) : 0f;
            float applied = CombatMath.ComputeIncoming(info.RawAmount, armor);

            // Sát thương bị giáp chặn hết vẫn tính là một lần trúng đòn hợp lệ:
            // vẫn báo sự kiện để hiện số "0" và hiệu ứng, nhưng không đụng vào máu.
            if (applied > 0f)
                Current.Value = Mathf.Max(0f, Current.Value - applied);

            OnDamaged?.Invoke(this, applied, in info);

            if (Current.Value <= 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            float before = Current.Value;
            Current.Value = Mathf.Min(Max, before + amount);

            float healed = Current.Value - before;
            if (healed > 0f)
                OnHealed?.Invoke(this, healed);
        }

        /// <summary>
        /// Cộng thẳng vào máu hiện tại mà KHÔNG bị chặn bởi máu tối đa.
        /// Cần cho luật lên cấp của đề bài: "+40 máu hiện tại VÀ +40 máu tối đa" —
        /// máu tối đa phải được nâng trước, rồi mới cộng máu hiện tại, nếu không sẽ bị mất phần dư.
        /// </summary>
        public void GrantCurrentHealth(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            Current.Value = Mathf.Min(Max, Current.Value + amount);
            OnHealed?.Invoke(this, amount);
        }

        public void Kill()
        {
            if (!IsAlive)
                return;

            Current.Value = 0f;
            Die();
        }

        private void Die()
        {
            IsAlive = false;
            OnDied?.Invoke(this);
        }
    }
}
