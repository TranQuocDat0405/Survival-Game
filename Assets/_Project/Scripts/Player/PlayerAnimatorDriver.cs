using Survival.Combat;
using Survival.Skills;
using UnityEngine;

namespace Survival.Player
{
    /// <summary>
    /// Nối trạng thái của player vào Animator.
    ///
    /// Đây là lớp CHỈ ĐỌC: nó quan sát nhân vật rồi kể lại cho Animator, và không bao giờ
    /// tác động ngược. Gameplay chạy đủ và đúng kể cả khi gỡ hẳn component này ra —
    /// điều đó có nghĩa animation không bao giờ có thể làm hỏng luật chơi.
    ///
    /// Cách phân chia này quan trọng: nếu để animation quyết định thời điểm gây sát thương
    /// hay thời điểm được bắn, thì mỗi lần đổi model hoặc đổi clip là luật chơi lại lệch đi.
    /// </summary>
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int HitHash = Animator.StringToHash("Hit");

        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerActor _actor;
        [SerializeField] private PlayerMotor _motor;

        [SerializeField, Range(1f, 30f), Tooltip("Tốc độ chuyển mượt giữa đứng yên và chạy. Cao thì đổi nhanh, thấp thì mượt hơn.")]
        private float _speedDamping = 12f;

        private float _displayedSpeed;

        private void Awake()
        {
            if (_actor == null) _actor = GetComponentInParent<PlayerActor>();
            if (_motor == null) _motor = GetComponentInParent<PlayerMotor>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            if (_actor != null)
            {
                _actor.OnSkillUsed += HandleSkillUsed;

                if (_actor.Health != null)
                {
                    _actor.Health.OnDamaged += HandleDamaged;
                    _actor.Health.OnDied += HandleDied;
                }
            }
        }

        private void OnDestroy()
        {
            if (_actor == null)
                return;

            _actor.OnSkillUsed -= HandleSkillUsed;

            if (_actor.Health != null)
            {
                _actor.Health.OnDamaged -= HandleDamaged;
                _actor.Health.OnDied -= HandleDied;
            }
        }

        private void Update()
        {
            if (_animator == null || _motor == null)
                return;

            // Làm mượt giá trị đưa vào Animator thay vì gán thẳng.
            // Joystick nhả tay là tốc độ tụt từ 1 về 0 ngay lập tức; gán thẳng sẽ khiến
            // nhân vật đang chạy khựng sang đứng yên trong một khung hình, nhìn rất giật.
            _displayedSpeed = Mathf.MoveTowards(
                _displayedSpeed,
                _motor.NormalizedSpeed,
                _speedDamping * Time.deltaTime);

            _animator.SetFloat(SpeedHash, _displayedSpeed);
        }

        private void HandleSkillUsed(SkillRuntime skill)
        {
            if (_animator == null)
                return;

            string trigger = skill.Def.AnimationTrigger;
            if (string.IsNullOrEmpty(trigger))
                return;

            _animator.SetTrigger(trigger);
        }

        private void HandleDamaged(Health target, float appliedDamage, in DamageInfo info)
        {
            // Chỉ giật người khi thật sự mất máu. Nếu giáp chặn hết thì không giật,
            // để người chơi phân biệt được "đỡ được" với "ăn đủ".
            if (_animator != null && appliedDamage > 0f && target.IsAlive)
                _animator.SetTrigger(HitHash);
        }

        private void HandleDied(Health target)
        {
            if (_animator != null)
                _animator.SetBool(DeadHash, true);
        }

        /// <summary>Gọi khi chơi lại để nhân vật đứng dậy khỏi tư thế chết.</summary>
        public void ResetToAlive()
        {
            if (_animator == null)
                return;

            _animator.SetBool(DeadHash, false);
            _displayedSpeed = 0f;
            _animator.SetFloat(SpeedHash, 0f);
        }
    }
}
