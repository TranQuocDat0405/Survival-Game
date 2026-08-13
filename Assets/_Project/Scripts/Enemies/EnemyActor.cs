using System;
using NFramework;
using Survival.Combat;
using Survival.Config;
using Survival.Enemies.Attacks;
using Survival.Enemies.States;
using Survival.Stats;
using UnityEngine;

namespace Survival.Enemies
{
    /// <summary>
    /// Thân xác của một con quái. Giống <c>PlayerActor</c>, lớp này CỐ TÌNH không chứa AI —
    /// nó chỉ lắp ráp và cung cấp các thao tác cơ bản (đi tới, xoay về, dừng lại, ra đòn).
    /// Phần "khi nào thì làm gì" nằm trong ba lớp trạng thái ở thư mục States.
    ///
    /// Kế thừa <see cref="PooledObject"/> nên quái được tái sử dụng qua các wave
    /// thay vì tạo mới rồi huỷ liên tục.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyActor : PooledObject
    {
        [Header("Config")]
        [SerializeField, Tooltip("File định nghĩa loại quái này. Quyết định máu, tốc độ, kiểu đòn đánh.")]
        private EnemyConfigSO _config;

        [Header("Tham chiếu trong prefab")]
        [SerializeField] private Health _health;
        [SerializeField] private Rigidbody _rigidbody;

        [SerializeField, Tooltip("Nút chứa phần hình ảnh. Model của quái nằm dưới đây.")]
        private Transform _visualRoot;

        [SerializeField, Tooltip("Điểm sinh đạn cho quái đánh xa. Để trống thì dùng gốc quái.")]
        private Transform _muzzle;

        [Header("Mục tiêu")]
        [SerializeField, Tooltip("Layer được coi là mục tiêu. Với quái thì đây là layer Player.")]
        private LayerMask _targetMask;

        [Header("Lúc chết")]
        [SerializeField, Min(0f), Tooltip(
            "Chờ bao lâu sau khi chết rồi mới trả về pool, tính bằng giây.\n" +
            "Đủ dài để animation gục xuống chạy xong, đủ ngắn để xác không nằm vướng mắt.")]
        private float _despawnDelay = 1.2f;

        [SerializeField, Tooltip("Collider của quái. Bị tắt ngay khi chết để xác không cản đường và không chặn đạn.")]
        private Collider _collider;

        public event Action<EnemyActor> OnDied;

        private EnemyAttackContext _attackContext;
        private Transform _cachedTransform;
        private bool _statesBuilt;
        private bool _setupCalled;

        /// <summary>
        /// Máy trạng thái là object C# thuần do chính lớp này sở hữu và gọi Tick,
        /// KHÔNG phải một component gắn trên GameObject. Nhờ vậy Unity không có gì để
        /// lưu xuống scene, và tham chiếu từ trạng thái tới con quái luôn còn nguyên.
        /// </summary>
        private readonly EnemyStateMachine _stateMachine = new EnemyStateMachine();

        public EnemyStateMachine StateMachine => _stateMachine;

        public StatSet Stats { get; private set; }
        public EnemyConfigSO Config => _config;
        public Health Health => _health;
        public Transform VisualRoot => _visualRoot;

        /// <summary>Mục tiêu để đuổi theo. Luôn là player, lấy qua tham chiếu tĩnh nên không phải quét scene.</summary>
        public Transform Target => Player.PlayerActor.Current != null ? Player.PlayerActor.Current.transform : null;

        public bool HasTarget
        {
            get
            {
                var player = Player.PlayerActor.Current;
                return player != null && player.Health != null && player.Health.IsAlive;
            }
        }

        private void Awake()
        {
            _cachedTransform = transform;

            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            if (_health == null) _health = GetComponent<Health>();
            if (_collider == null) _collider = GetComponent<Collider>();

            _rigidbody.useGravity = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            Stats = new StatSet();

            _attackContext = new EnemyAttackContext
            {
                Owner = _cachedTransform,
                OwnerGameObject = gameObject,
                Muzzle = _muzzle != null ? _muzzle : _cachedTransform,
                TargetMask = _targetMask,
            };

            _health.OnDied += HandleDied;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_health != null)
                _health.OnDied -= HandleDied;
        }

        /// <summary>
        /// Nạp số liệu và bật AI. Gọi ngay sau khi lấy quái ra khỏi pool.
        /// Truyền null thì dùng config đã gán sẵn trên prefab.
        /// </summary>
        /// <summary>
        /// Quái được đặt sẵn trong scene (không qua pool) thì tự khởi động ở đây.
        /// Rất tiện để đặt vài con vào scene mà thử AI, không cần dựng cả hệ thống wave.
        /// </summary>
        private void Start()
        {
            if (!_setupCalled)
                Setup();
        }

        /// <summary>Quái lấy ra từ pool thì khởi động lại toàn bộ ở đây.</summary>
        public override void OnSpawnedFromPool()
        {
            base.OnSpawnedFromPool();
            Setup();
        }

        public void Setup(EnemyConfigSO config = null)
        {
            _setupCalled = true;

            if (config != null)
                _config = config;

            Stats.SetBase(_config.BaseStats);
            _health.Initialize(Stats);

            // Bật lại mọi thứ đã bị tắt lúc chết. Bắt buộc phải làm vì đây là object
            // TÁI SỬ DỤNG từ pool — nó mang theo nguyên trạng thái của lần chết trước.
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.isKinematic = false;
            if (_collider != null)
                _collider.enabled = true;

            BuildStateMachineOnce();
            _stateMachine.IsRunning = true;
            _stateMachine.SetState(EEnemyState.Approach);

            EnemyRegistry.I?.Register(this);
        }

        /// <summary>
        /// AI chạy ở đây thay vì trong một component riêng, nên thứ tự cập nhật rõ ràng
        /// và deltaTime được truyền thẳng xuống trạng thái (giúp viết test được).
        /// </summary>
        private void Update()
        {
            _stateMachine.Tick(Time.deltaTime);
        }

        /// <summary>
        /// Ba trạng thái chỉ được dựng MỘT lần cho mỗi bản sao trong pool.
        /// Dựng lại mỗi lần sinh ra sẽ cấp phát object mới liên tục —
        /// đúng thứ mà pool sinh ra để tránh.
        /// </summary>
        private void BuildStateMachineOnce()
        {
            if (_statesBuilt)
                return;

            _statesBuilt = true;

            _stateMachine.Register(EEnemyState.Approach, new EnemyApproachState(this));
            _stateMachine.Register(EEnemyState.Attack,   new EnemyAttackState(this));
            _stateMachine.Register(EEnemyState.Idle,     new EnemyIdleState(this));
        }

        // ---------------------------------------------------------------- thao tác cho các trạng thái

        /// <summary>Bình phương khoảng cách tới mục tiêu, bỏ qua chiều cao.</summary>
        public float SqrDistanceToTarget()
        {
            var target = Target;
            if (target == null)
                return float.MaxValue;

            Vector3 delta = target.position - _cachedTransform.position;
            delta.y = 0f;
            return delta.sqrMagnitude;   // dùng bình phương để khỏi phải tính căn bậc hai mỗi khung hình
        }

        public void RotateTowardsTarget(float deltaTime, float speedFactor = 1f)
        {
            var target = Target;
            if (target == null)
                return;

            Vector3 direction = target.position - _cachedTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            float rotationSpeed = Stats.Get(EStatType.RotationSpeed) * speedFactor;
            Quaternion desired = Quaternion.LookRotation(direction, Vector3.up);
            _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation, desired, rotationSpeed * deltaTime));
        }

        public void MoveTowardsTarget()
        {
            var target = Target;
            if (target == null)
            {
                StopMoving();
                return;
            }

            Vector3 direction = target.position - _cachedTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                StopMoving();
                return;
            }

            _rigidbody.velocity = direction.normalized * Stats.Get(EStatType.MoveSpeed);
        }

        public void StopMoving() => _rigidbody.velocity = Vector3.zero;

        /// <summary>
        /// Ra đòn NGAY BÂY GIỜ. Được gọi tại đúng thời điểm gây sát thương trong animation.
        /// Đòn đánh tự đọc vị trí player ở thời điểm này, nên nếu người chơi đã né kịp thì trượt thật.
        /// </summary>
        public void ExecuteAttack()
        {
            _config.Attack?.Execute(_attackContext);
        }

        public void Kill() => _health.Kill();

        private void HandleDied(Health health)
        {
            StopMoving();
            _stateMachine.IsRunning = false;

            // Tắt va chạm NGAY LẬP TỨC. Nếu để nguyên, cái xác vẫn chặn đường player
            // và vẫn hứng mũi tên: tia quét của đạn dừng lại ở collider gần nhất,
            // không gây sát thương (vì đã chết) nhưng cũng không bay tiếp tới con còn sống phía sau.
            if (_collider != null)
                _collider.enabled = false;

            // Kinematic để hệ vật lý không đẩy cái xác trượt đi trong lúc chờ biến mất.
            _rigidbody.isKinematic = true;

            EnemyRegistry.I?.NotifyDied(this);
            OnDied?.Invoke(this);

            // Trả về pool sau một nhịp cho animation gục xuống chạy xong.
            // KHÔNG trả ngay: quái sẽ biến mất đột ngột, người chơi không kịp thấy mình đã giết được nó.
            // KHÔNG bỏ qua bước này: xác sẽ nằm lại trong scene vĩnh viễn và pool phải tạo object mới
            // cho từng con quái của mọi wave — tức là pool mất sạch tác dụng.
            if (_despawnDelay > 0f)
                StartCoroutine(DespawnAfterDelay());
            else
                ReturnToPool();
        }

        private System.Collections.IEnumerator DespawnAfterDelay()
        {
            yield return new WaitForSeconds(_despawnDelay);
            ReturnToPool();
        }

        public override void OnBeforeReturnToPool()
        {
            base.OnBeforeReturnToPool();
            _stateMachine.IsRunning = false;
            StopMoving();
            EnemyRegistry.I?.Unregister(this);

            // Dời hẳn về kho chứa. Object đã tắt nên không va chạm được nữa,
            // nhưng để nó nằm lại giữa sân khiến Scene View đầy xác chồng lên nhau,
            // rất khó nhìn khi cần gỡ lỗi.
            transform.position = Pooling.PoolService.StoragePosition;
            _rigidbody.position = transform.position;
        }

#if UNITY_EDITOR
        /// <summary>Vẽ tầm đánh và hình nón lên Scene View để kiểm tra bằng mắt có đúng số spec không.</summary>
        private void OnDrawGizmosSelected()
        {
            if (_config == null)
                return;

            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, _config.AttackRange);

            if (_config.Attack is ConeMeleeAttack cone)
            {
                float half = cone.ConeAngle * 0.5f;
                Vector3 left  = Quaternion.AngleAxis(-half, Vector3.up) * transform.forward;
                Vector3 right = Quaternion.AngleAxis( half, Vector3.up) * transform.forward;
                Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.9f);
                Gizmos.DrawRay(transform.position, left  * cone.Range);
                Gizmos.DrawRay(transform.position, right * cone.Range);
            }
        }
#endif
    }
}
