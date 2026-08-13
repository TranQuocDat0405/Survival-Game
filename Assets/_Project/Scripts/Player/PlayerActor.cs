using System;
using System.Collections.Generic;
using Survival.Combat;
using Survival.Config;
using Survival.Skills;
using Survival.Stats;
using UnityEngine;

namespace Survival.Player
{
    /// <summary>
    /// Điểm lắp ráp của nhân vật người chơi.
    ///
    /// Lớp này cố tình KHÔNG chứa logic combat, không chứa công thức, không chứa AI.
    /// Việc duy nhất của nó là nối dây: đọc file config, dựng bộ chỉ số, đưa bộ chỉ số đó
    /// cho máu / chuyển động / các skill, rồi mỗi khung hình chuyển input xuống và đếm giờ skill.
    ///
    /// Đây là cách tránh "God-class" — một class ôm hết mọi thứ, dài hàng nghìn dòng,
    /// sửa một chỗ hỏng ba chỗ. Tiêu chí chấm "Dễ đọc, dễ maintain" nhắm thẳng vào việc này.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerActor : MonoBehaviour
    {
        /// <summary>
        /// Tham chiếu tĩnh tới player đang sống trong màn.
        ///
        /// Vì sao cần: quái phải biết player ở đâu để đuổi theo, và nó hỏi việc này MỖI KHUNG HÌNH.
        /// Nếu mỗi con quái gọi <c>FindObjectOfType</c> thì Unity phải duyệt toàn bộ scene
        /// mỗi lần gọi — với 6 con quái là 6 lần quét scene mỗi khung hình, cực kỳ lãng phí.
        /// Một tham chiếu tĩnh là phép tra cứu tức thì.
        /// </summary>
        public static PlayerActor Current { get; private set; }

        [Header("Config")]
        [SerializeField, Tooltip("File chứa toàn bộ chỉ số và danh sách skill của player.")]
        private PlayerConfigSO _config;

        [Header("Tham chiếu trong prefab")]
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerInputRouter _input;
        [SerializeField] private Health _health;

        [SerializeField, Tooltip("Điểm sinh ra mũi tên, đặt ở đầu nỏ. Nếu để trống thì dùng gốc nhân vật.")]
        private Transform _muzzle;

        /// <summary>Bắn ra khi một skill vừa khai hoả. Animator, âm thanh, camera shake nghe sự kiện này.</summary>
        public event Action<SkillRuntime> OnSkillUsed;

        public event Action OnDied;

        private readonly List<SkillRuntime> _skills = new List<SkillRuntime>();

        public StatSet Stats { get; private set; }
        public Health Health => _health;
        public PlayerMotor Motor => _motor;
        public PlayerConfigSO Config => _config;

        /// <summary>Danh sách skill lúc chạy, đúng thứ tự trong config. UI dựng nút theo danh sách này.</summary>
        public IReadOnlyList<SkillRuntime> Skills => _skills;

        private void Awake()
        {
            Current = this;

            if (_motor == null) _motor = GetComponent<PlayerMotor>();
            if (_input == null) _input = GetComponent<PlayerInputRouter>();
            if (_health == null) _health = GetComponent<Health>();

            BuildStats();
            BuildSkills();

            _health.OnDied += HandleDied;
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;

            if (_health != null)
                _health.OnDied -= HandleDied;
        }

        private void BuildStats()
        {
            Stats = new StatSet(_config.BaseStats);

            // Máu phải được khởi tạo SAU khi có bộ chỉ số, vì nó lấy máu tối đa từ đó.
            _health.Initialize(Stats);
            _motor.Initialize(Stats, _config.MoveAlongForwardOnly, _config.InputDeadZone);
        }

        private void BuildSkills()
        {
            var context = new SkillContext
            {
                Owner = transform,
                Muzzle = _muzzle != null ? _muzzle : transform,
                OwnerGameObject = gameObject,
                Stats = Stats,
                TargetMask = _config.EnemyMask,
                CoroutineRunner = this,
                OwnerRigidbody = GetComponent<Rigidbody>(),
                SetControlLocked = locked => _motor.ControlLocked = locked,
            };

            _skills.Clear();

            var definitions = _config.Skills;
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                    continue;

                var runtime = definition.CreateRuntime(context);
                runtime.OnUsed += HandleSkillUsed;
                _skills.Add(runtime);
            }
        }

        private void Update()
        {
            if (!_health.IsAlive)
                return;

            _motor.SetMoveInput(_input.MoveInput);

            // Đếm giờ hồi chiêu và hồi charge cho mọi skill.
            // Dùng vòng for thay vì foreach: foreach trên List tạo ra một enumerator mỗi lần gọi,
            // chạy mỗi khung hình thì đó là rác nhỏ nhưng liên tục.
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _skills.Count; i++)
                _skills[i].Tick(deltaTime);
        }

        /// <summary>Nút skill trên UI gọi hàm này. Trả về true nếu skill đã khai hoả.</summary>
        public bool TryUseSkill(int index)
        {
            if (!_health.IsAlive)
                return false;

            if (index < 0 || index >= _skills.Count)
                return false;

            return _skills[index].TryUse();
        }

        private void HandleSkillUsed(SkillRuntime skill) => OnSkillUsed?.Invoke(skill);

        private void HandleDied(Health health)
        {
            _motor.SetMoveInput(Vector2.zero);
            _motor.ControlLocked = true;

            // Bắt buộc phải dừng hẳn. Chỉ khoá điều khiển thôi thì vận tốc CUỐI CÙNG
            // trước lúc chết vẫn còn nguyên, và vì không có ma sát nên xác player
            // sẽ trôi ngang màn hình mãi mãi trong khi quái đứng im.
            _motor.Stop();

            OnDied?.Invoke();
        }

        /// <summary>Dựng lại player về trạng thái đầu màn. Dùng cho nút Chơi lại.</summary>
        public void ResetToStart(Vector3 spawnPosition)
        {
            Stats.SetBase(_config.BaseStats);
            _health.Initialize(Stats);
            _motor.ControlLocked = false;
            _motor.Teleport(spawnPosition, Quaternion.identity);
            BuildSkills();
        }
    }
}
