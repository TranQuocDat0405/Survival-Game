using Survival.Combat;
using Survival.Enemies.States;
using UnityEngine;

namespace Survival.Enemies
{
    /// <summary>
    /// Nối trạng thái của quái vào Animator.
    ///
    /// ĐIỂM QUAN TRỌNG NHẤT: animation tấn công được CO GIÃN TỐC ĐỘ cho khớp với con số
    /// trong file config, chứ không phải ngược lại.
    ///
    /// Clip chém của KayKit dài 1.00 giây, còn config đặt lấy đà 0.35 + thu tay 0.25 = 0.60 giây.
    /// Nếu để clip chạy tốc độ gốc, quái sẽ gây sát thương ở giây thứ 0.35 trong khi tay
    /// mới vung được một phần ba — người chơi ăn đòn trước khi nhìn thấy đòn đánh.
    /// Nên ta phát clip nhanh hơn 1.67 lần để lúc "chém tới" đúng bằng lúc gây sát thương.
    ///
    /// Nhờ vậy khi tune lại windup trên Inspector thì hình ảnh TỰ khớp theo,
    /// không phải đi sửa animation hay gắn lại Animation Event.
    /// </summary>
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int AttackSpeedHash = Animator.StringToHash("AttackSpeed");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int HitHash = Animator.StringToHash("Hit");

        [SerializeField] private Animator _animator;
        [SerializeField] private EnemyActor _enemy;

        [SerializeField, Min(0.01f), Tooltip(
            "Độ dài GỐC của clip tấn công, tính bằng giây. Dùng để tính hệ số co giãn.\n" +
            "Đọc được ở Inspector của file animation.")]
        private float _attackClipLength = 1f;

        [SerializeField, Range(1f, 30f)]
        private float _speedDamping = 14f;

        private EEnemyState _lastState = (EEnemyState)(-1);
        private float _displayedSpeed;

        private void Awake()
        {
            if (_enemy == null) _enemy = GetComponentInParent<EnemyActor>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            if (_enemy != null && _enemy.Health != null)
            {
                _enemy.Health.OnDamaged += HandleDamaged;
                _enemy.Health.OnDied += HandleDied;
            }
        }

        private void OnDestroy()
        {
            if (_enemy == null || _enemy.Health == null)
                return;

            _enemy.Health.OnDamaged -= HandleDamaged;
            _enemy.Health.OnDied -= HandleDied;
        }

        /// <summary>Quái được tái sử dụng từ pool nên phải xoá tư thế chết của lần trước.</summary>
        private void OnEnable()
        {
            _lastState = (EEnemyState)(-1);
            _displayedSpeed = 0f;

            if (_animator == null)
                return;

            _animator.SetBool(DeadHash, false);
            _animator.SetFloat(SpeedHash, 0f);
            _animator.Rebind();
            _animator.Update(0f);
        }

        private void Update()
        {
            if (_animator == null || _enemy == null)
                return;

            var machine = _enemy.StateMachine;
            var state = machine.CurrentId;

            // Chỉ trạng thái Approach mới thật sự di chuyển; hai trạng thái kia đứng im.
            float targetSpeed = state == EEnemyState.Approach && machine.IsRunning ? 1f : 0f;
            _displayedSpeed = Mathf.MoveTowards(_displayedSpeed, targetSpeed, _speedDamping * Time.deltaTime);
            _animator.SetFloat(SpeedHash, _displayedSpeed);

            // Bắn trigger đúng MỘT lần tại thời điểm vừa bước vào trạng thái tấn công.
            if (state != _lastState)
            {
                _lastState = state;

                if (state == EEnemyState.Attack)
                {
                    var config = _enemy.Config;
                    float configuredDuration = Mathf.Max(0.01f, config.AttackWindup + config.AttackRecover);

                    // Đây chính là chỗ ép animation khớp theo config.
                    _animator.SetFloat(AttackSpeedHash, _attackClipLength / configuredDuration);
                    _animator.SetTrigger(AttackHash);
                }
            }
        }

        private void HandleDamaged(Health target, float appliedDamage, in DamageInfo info)
        {
            if (_animator != null && appliedDamage > 0f && target.IsAlive)
                _animator.SetTrigger(HitHash);
        }

        private void HandleDied(Health target)
        {
            if (_animator != null)
                _animator.SetBool(DeadHash, true);
        }
    }
}
