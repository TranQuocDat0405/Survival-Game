using Survival.Combat;
using Survival.Skills;
using Survival.Stats;
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
        private static readonly int LocomotionSpeedHash = Animator.StringToHash("LocomotionSpeed");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int HitHash = Animator.StringToHash("Hit");

        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerActor _actor;
        [SerializeField] private PlayerMotor _motor;

        [SerializeField, Range(1f, 30f), Tooltip("Tốc độ chuyển mượt giữa đứng yên và chạy. Cao thì đổi nhanh, thấp thì mượt hơn.")]
        private float _speedDamping = 12f;

        [SerializeField, Min(0.1f), Tooltip(
            "Nhân vật đi nhanh bao nhiêu thì clip chạy trông vừa nhịp nhất, tính bằng unit mỗi giây.\n\n" +
            "Clip chạy được tác giả làm cho một tốc độ nhất định. Nếu nhân vật đi nhanh hơn mức đó mà " +
            "clip vẫn phát với nhịp cũ thì chân bước không kịp quãng đường — nhìn ra thành trượt băng. " +
            "Chia tốc độ thật cho con số này ra được hệ số cần nhân vào tốc độ phát clip.\n\n" +
            "Chỉnh nhỏ lại nếu thấy chân bước quá chậm so với đà chạy, chỉnh lớn lên nếu chân guồng quá nhanh.")]
        private float _animationTunedForSpeed = 2f;

        [SerializeField, Range(0.2f, 3f), Tooltip("Tinh chỉnh thêm nhịp bước sau khi đã khớp tốc độ. Để 1 là đúng tỉ lệ.")]
        private float _stepRateTrim = 1f;

        private float _displayedSpeed;

        private void Awake()
        {
            if (_actor == null) _actor = GetComponentInParent<PlayerActor>();
            if (_motor == null) _motor = GetComponentInParent<PlayerMotor>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            if (_actor != null)
            {
                _actor.OnSkillUsed += HandleSkillUsed;
                _actor.OnReset += ResetToAlive;

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
            _actor.OnReset -= ResetToAlive;

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
            _animator.SetFloat(LocomotionSpeedHash, ComputeStepRate());
        }

        /// <summary>
        /// Clip chạy phải được phát nhanh chậm theo QUÃNG ĐƯỜNG THẬT SỰ đi được.
        ///
        /// Tham số Speed ở trên chỉ nói "đang đứng yên hay đang chạy" (0 tới 1) — nó không hề
        /// biết một giây nhân vật đi được mấy unit. Nên khi tốc độ di chuyển được tăng từ 2.0
        /// lên 3.2, quãng đường dài thêm 60% mà nhịp chân vẫn y nguyên, và mắt đọc ra ngay
        /// là nhân vật đang trượt băng chứ không phải đang bước.
        ///
        /// Hàm này lấy tốc độ thật chia cho tốc độ mà clip được làm cho vừa nhịp.
        /// Nhờ vậy về sau có tune tốc độ di chuyển bao nhiêu lần nữa thì bước chân vẫn tự khớp,
        /// không phải nhớ chỉnh tay ở một chỗ thứ hai.
        /// </summary>
        private float ComputeStepRate()
        {
            // Đứng yên thì phát clip đứng yên ở nhịp bình thường. Nếu để hệ số tụt về gần 0
            // thì chính animation đứng yên cũng bị đóng băng theo, nhìn như game bị treo.
            if (_displayedSpeed < 0.05f)
                return 1f;

            float moveSpeed = _actor != null && _actor.Stats != null
                ? _actor.Stats.Get(EStatType.MoveSpeed)
                : 0f;

            float travelSpeed = _displayedSpeed * moveSpeed;
            float rate = travelSpeed / _animationTunedForSpeed;

            return Mathf.Max(rate, 0.2f) * _stepRateTrim;
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
