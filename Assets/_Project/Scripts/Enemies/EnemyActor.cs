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

        [Header("Né vật cản")]
        [SerializeField, Tooltip(
            "Layer của những thứ chặn đường: cây, đá tảng, thân cây đổ, và tường vô hình quanh sân.")]
        private LayerMask _obstacleMask;

        [SerializeField, Min(0.05f), Tooltip(
            "Bán kính thân quái, dùng cho tia dò vật cản.\n" +
            "Nên hơi lớn hơn collider thật một chút để quái bắt đầu lách TRƯỚC khi chạm vào cây, " +
            "thay vì chạm rồi mới xử lý — chạm rồi mới lách thì nhìn ra thành cảnh húc vào cây rồi giật lùi.")]
        private float _avoidBodyRadius = 0.45f;

        [SerializeField, Min(0.1f), Tooltip(
            "Nhìn trước bao xa để phát hiện vật cản, tính bằng unit.\n" +
            "Ngắn quá thì phát hiện muộn và vẫn húc vào; dài quá thì quái né cả những cái cây " +
            "mà đường đi thật ra không hề đâm vào, trông như bị hoảng.")]
        private float _avoidProbeDistance = 1.6f;

        [Header("Tìm đường vòng")]
        [SerializeField, Min(0.05f), Tooltip(
            "Bao lâu tính lại đường một lần, tính bằng giây.\n\n" +
            "Tính đường là phép đắt nhất trong lớp này nên KHÔNG được tính mỗi khung hình. " +
            "Ngắn quá thì tốn máy vô ích vì đường gần như không đổi; dài quá thì quái phản ứng " +
            "chậm khi người chơi vòng sang hướng khác.")]
        private float _pathRefreshInterval = 0.25f;

        [SerializeField, Min(0.1f), Tooltip(
            "Người chơi chạy xa khỏi đích của đường cũ bao nhiêu thì tính lại ngay, không chờ hết hạn.\n" +
            "Nhờ vậy quái vẫn bám kịp lúc người chơi đổi hướng đột ngột.")]
        private float _pathRetargetDistance = 1.5f;

        [SerializeField, Min(0.05f), Tooltip(
            "Tới gần một mốc trên đường bao nhiêu thì coi như đã qua và nhắm tới mốc kế tiếp.\n" +
            "Nhỏ quá thì quái cố chạm chính xác từng mốc và đi giật cục ở các khúc cua.")]
        private float _cornerReachedDistance = 0.4f;

        /// <summary>Đường đang bám theo. Cấp phát một lần rồi dùng lại, tránh sinh rác mỗi lần tính.</summary>
        private UnityEngine.AI.NavMeshPath _path;

        /// <summary>
        /// Các mốc của đường. Mảng cố định dùng với GetCornersNonAlloc để không cấp phát mảng mới
        /// mỗi lần tính đường — sáu con quái tính lại bốn lần mỗi giây thì lượng rác đó cộng dồn rất nhanh.
        /// </summary>
        private readonly Vector3[] _pathCorners = new Vector3[24];
        private int _pathCornerCount;
        private int _pathCornerIndex;
        private float _pathTimer;
        private Vector3 _lastPathTarget;

        /// <summary>Vị trí ở khung hình trước, dùng để biết thật sự đi được bao xa.</summary>
        private Vector3 _lastPosition;

        /// <summary>Đã bị chặn liên tục bao lâu.</summary>
        private float _stuckTimer;

        /// <summary>Còn bao lâu nữa thì thôi ép đi đường vòng.</summary>
        private float _forcePathTimer;

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
            _rigidbody.constraints = BaseConstraints;
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
            _health.OnDamaged += HandleDamaged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_health != null)
            {
                _health.OnDied -= HandleDied;
                _health.OnDamaged -= HandleDamaged;
            }
        }

        /// <summary>
        /// Kêu một tiếng khi quái ăn đòn — đây là phản hồi quan trọng nhất của cả trận đánh,
        /// vì nó là thứ trả lời câu hỏi "mũi tên vừa rồi có trúng không".
        ///
        /// Không kêu khi giáp đỡ hết, để người chơi phân biệt được "đỡ được" với "ăn đủ".
        /// Không kêu theo từng tick độc: độc trừ máu mỗi giây suốt ba giây, kêu theo tick thì
        /// một con dính độc là tiếng kêu liên hồi và át hết mọi thứ khác.
        /// </summary>
        private void HandleDamaged(Health target, float appliedDamage, in Combat.DamageInfo info)
        {
            if (appliedDamage <= 0f || info.Source == Combat.EDamageSource.Poison)
                return;

            Audio.GameAudioService.PlayEnemyHurt();
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
            //
            // THỨ TỰ HAI DÒNG NÀY QUAN TRỌNG. Lúc chết, thân vật lý bị chuyển sang kinematic
            // để cái xác không bị trượt đi. Mà Unity KHÔNG cho đặt vận tốc lên một thân kinematic:
            // đặt trước khi bật lại thì mỗi lần sinh quái là một dòng cảnh báo đỏ trong console.
            // Phải trả nó về động trước, rồi mới xoá vận tốc còn sót lại từ lần chết trước.
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            // Xoá đường đi của kiếp trước. Quái lấy ra từ pool mang theo nguyên trạng thái cũ,
            // nên nếu không xoá thì nó sẽ lững thững đi theo lộ trình tính cho một vị trí
            // hoàn toàn khác trước khi kịp tính lại.
            _pathCornerCount = 0;
            _pathCornerIndex = 0;
            _pathTimer = 0f;
            _stuckTimer = 0f;
            _forcePathTimer = 0f;
            _lastPosition = _cachedTransform.position;

            // Thả ghim. Quái lấy từ pool có thể đã chết ngay giữa lúc đang ghim ở đòn đánh
            // trước, và nếu không trả ràng buộc về mặc định thì kiếp sau nó sinh ra đã bị
            // khoá cứng hai trục ngang — đứng nguyên tại chỗ sinh cho tới hết ván.
            SetAnchored(false);

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

        /// <param name="speedFactor">Nhân vào tốc độ chạy. 1 là chạy bình thường, 0 là đứng yên.</param>
        /// <param name="stopDistance">
        /// Tới gần hơn khoảng này thì dừng lại, tính bằng unit. Để 0 nghĩa là đi tới sát tận nơi.
        /// Dùng khi quái chỉ cần GIỮ KHOẢNG CÁCH chứ không cần áp sát — nếu không nó ủi thẳng
        /// vào người chơi rồi bị collider chặn, trông như đang húc đầu vào nhau.
        /// </param>
        public void MoveTowardsTarget(float speedFactor = 1f, float stopDistance = 0f)
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

            if (speedFactor <= 0f)
            {
                StopMoving();
                return;
            }

            if (stopDistance > 0f && direction.sqrMagnitude <= stopDistance * stopDistance)
            {
                StopMoving();
                return;
            }

            // Cùng lý do với StopMoving: xác đã chuyển sang kinematic thì không được ghi vận tốc.
            if (_rigidbody.isKinematic)
                return;

            direction.Normalize();
            direction = ResolveMoveDirection(target.position, direction);

            _rigidbody.velocity = direction * Stats.Get(EStatType.MoveSpeed) * speedFactor;
        }

        /// <summary>
        /// Hướng đi thật sự trong khung hình này. Đây là nơi quyết định đi thẳng hay đi vòng.
        ///
        /// BA MỨC, xét từ rẻ tới đắt:
        ///
        ///   1. NHÌN THẤY PLAYER thì đi thẳng. Đây là đa số thời gian của một trận, và nó
        ///      cho ra chuyển động thẳng thớm, phản ứng tức thì với mọi bước né của người chơi.
        ///      Đi đường vòng trong trường hợp này chỉ làm quái chạy lượn vô nghĩa.
        ///
        ///   2. BỊ CHẮN thì hỏi NavMesh đường đi vòng. Chỉ tính lại vài lần mỗi giây chứ không
        ///      phải mỗi khung hình — tính đường là phép đắt nhất trong cả lớp này.
        ///
        ///   3. KHÔNG TÍNH ĐƯỢC ĐƯỜNG (quái vừa sinh ra ngoài lưới, hoặc bị đẩy vào kẹt góc)
        ///      thì quay về cách cũ: dò tia rồi trượt dọc vật cản. Luôn phải có đường lui,
        ///      vì thà quái đi hơi ngu còn hơn đứng chết một chỗ.
        /// </summary>
        private Vector3 ResolveMoveDirection(Vector3 targetPosition, Vector3 straightDirection)
        {
            UpdateStuckDetection();

            bool mustDetour = _forcePathTimer > 0f || HasObstacleBetween(targetPosition);

            if (!mustDetour)
            {
                _pathCornerCount = 0;   // bỏ đường cũ, lần sau bị chắn sẽ tính lại từ đầu
                return straightDirection;
            }

            if (TryFollowPath(targetPosition, out Vector3 pathDirection))
                return pathDirection;

            return SteerAroundObstacles(straightDirection);
        }

        /// <summary>
        /// Phát hiện kẹt bằng QUÃNG ĐƯỜNG THẬT SỰ ĐI ĐƯỢC, không bằng tia ngắm.
        ///
        /// ĐÂY LÀ BÀI HỌC PHẢI NHỚ. Ban đầu tôi chỉ kiểm tra "giữa mình và player có vật cản
        /// không" bằng một tia hình cầu. Cách đó bỏ sót đúng trường hợp quan trọng nhất:
        /// khi quái đã ÁP SÁT vào gốc cây, tia bắt đầu từ bên trong collider, mà Unity thì
        /// KHÔNG báo va chạm cho tia xuất phát từ trong lòng một collider.
        /// Kết quả là quái húc thẳng vào cây, vận tốc vẫn đúng 3.0 nhưng không nhích được
        /// centimet nào, mà phép kiểm lại khẳng định "đường thông thoáng".
        /// (Đúng hiện tượng đã gặp khi kiểm tra mũi tên xuyên qua tảng đá.)
        ///
        /// So quãng đường đi được với quãng đường LẼ RA phải đi thì không bao giờ bị đánh lừa:
        /// bị chặn là bị chặn, bất kể hình học phía trước trông ra sao.
        /// </summary>
        private void UpdateStuckDetection()
        {
            if (_forcePathTimer > 0f)
                _forcePathTimer -= Time.deltaTime;

            float expected = Stats.Get(EStatType.MoveSpeed) * Time.deltaTime;
            float actual = Vector3.Distance(_cachedTransform.position, _lastPosition);
            _lastPosition = _cachedTransform.position;

            if (expected <= 0.0001f)
                return;

            // Đi được dưới một phần ba mức đáng lẽ phải đi thì coi như đang bị chặn.
            if (actual < expected * 0.35f)
                _stuckTimer += Time.deltaTime;
            else
                _stuckTimer = 0f;

            if (_stuckTimer < StuckThreshold)
                return;

            // Ép đi đường vòng một lúc. Phải giữ đủ lâu để quái thoát hẳn ra khỏi chỗ kẹt,
            // nếu thả ra ngay khi vừa nhúc nhích được thì nó quay đầu húc vào đúng cái cây đó.
            _stuckTimer = 0f;
            _forcePathTimer = ForcePathDuration;
            _pathCornerCount = 0;   // buộc tính lại đường ngay lập tức
        }

        /// <summary>Bị chặn liên tục bao lâu thì kết luận là kẹt, tính bằng giây.</summary>
        private const float StuckThreshold = 0.25f;

        /// <summary>Ép bám đường vòng bao lâu sau khi phát hiện kẹt, tính bằng giây.</summary>
        private const float ForcePathDuration = 2.5f;

        /// <summary>Giữa mình và mục tiêu có vật cản chắn không.</summary>
        private bool HasObstacleBetween(Vector3 targetPosition)
        {
            if (_obstacleMask.value == 0)
                return false;

            Vector3 from = _cachedTransform.position + Vector3.up * _avoidBodyRadius;
            Vector3 to = targetPosition + Vector3.up * _avoidBodyRadius;
            Vector3 delta = to - from;
            float distance = delta.magnitude;

            if (distance < 0.01f)
                return false;

            return Physics.SphereCast(from, _avoidBodyRadius, delta / distance, out _, distance,
                _obstacleMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Bám theo đường do NavMesh tính. Trả về false nếu không có đường hợp lệ.
        /// </summary>
        private bool TryFollowPath(Vector3 targetPosition, out Vector3 direction)
        {
            direction = Vector3.zero;

            _pathTimer -= Time.deltaTime;

            // Tính lại khi hết hạn, hoặc khi player đã chạy xa khỏi chỗ mà đường cũ dẫn tới.
            bool targetMovedFar = (targetPosition - _lastPathTarget).sqrMagnitude > _pathRetargetDistance * _pathRetargetDistance;
            if (_pathTimer <= 0f || targetMovedFar || _pathCornerCount == 0)
            {
                _pathTimer = _pathRefreshInterval;
                RecalculatePath(targetPosition);
            }

            if (_pathCornerCount == 0)
                return false;

            // Bỏ qua các mốc đã đi tới nơi. Dùng while chứ không phải if: một khung hình chậm
            // có thể vượt qua hai mốc liền, nếu chỉ bỏ một thì quái sẽ quay đầu đi ngược lại.
            while (_pathCornerIndex < _pathCornerCount)
            {
                Vector3 flat = _pathCorners[_pathCornerIndex] - _cachedTransform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > _cornerReachedDistance * _cornerReachedDistance)
                {
                    direction = flat.normalized;
                    return true;
                }
                _pathCornerIndex++;
            }

            return false;
        }

        private void RecalculatePath(Vector3 targetPosition)
        {
            _pathCornerCount = 0;
            _pathCornerIndex = 0;
            _lastPathTarget = targetPosition;

            if (_path == null)
                _path = new UnityEngine.AI.NavMeshPath();

            // Kéo hai đầu về đúng mặt lưới trước khi tính. Quái có thể đang đứng lệch ra ngoài
            // lưới một chút do bị xô đẩy, và khi đó phép tính đường sẽ thất bại ngay.
            if (!UnityEngine.AI.NavMesh.SamplePosition(_cachedTransform.position, out var fromHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                return;
            if (!UnityEngine.AI.NavMesh.SamplePosition(targetPosition, out var toHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                return;

            if (!UnityEngine.AI.NavMesh.CalculatePath(fromHit.position, toHit.position, UnityEngine.AI.NavMesh.AllAreas, _path))
                return;

            // Đường cụt vẫn dùng được: nó dẫn tới chỗ gần nhất có thể tới, tốt hơn là đứng im.
            _pathCornerCount = _path.GetCornersNonAlloc(_pathCorners);

            // Mốc đầu tiên luôn là chỗ đang đứng, bỏ qua để khỏi tự đi tới chính mình.
            if (_pathCornerCount > 1)
                _pathCornerIndex = 1;
        }

        /// <summary>
        /// Lách quanh vật cản trên đường tới player.
        ///
        /// VÌ SAO PHẢI CÓ: game này KHÔNG dùng NavMesh — quái chỉ đơn giản lao thẳng về phía
        /// người chơi. Chừng nào sân trống thì cách đó chạy tốt và cực rẻ. Nhưng từ khi cây và
        /// đá có va chạm thật, "lao thẳng" đồng nghĩa với húc vào gốc cây: hệ vật lý chặn lại,
        /// còn script thì khung hình nào cũng đặt lại đúng vận tốc đâm vào cây đó.
        /// Kết quả là con quái đứng rung tại chỗ mãi mãi, và người chơi chỉ cần đứng sau một cái cây
        /// là bất tử.
        ///
        /// CÁCH GIẢI: dò một tia hình cầu về phía trước. Gặp vật cản thì thay vì đi thẳng,
        /// quái đi TRƯỢT DỌC theo mặt vật cản — chiếu hướng mong muốn lên mặt phẳng của vật cản.
        /// Cách này cho ra chuyển động vòng qua chướng ngại rất tự nhiên, và quan trọng là
        /// nó không cần biết trước bản đồ, nên thêm bớt cây cối bao nhiêu cũng không phải làm gì thêm.
        ///
        /// Đây là kỹ thuật "steering behaviour" cổ điển, nhẹ hơn NavMesh rất nhiều
        /// (một phép quét hình cầu mỗi con mỗi khung hình) và đủ dùng cho sân đấu trống trải kiểu này.
        /// </summary>
        private Vector3 SteerAroundObstacles(Vector3 desired)
        {
            if (_obstacleMask.value == 0)
                return desired;

            // Bắn tia từ ngang thân chứ không từ dưới chân: từ sát mặt đất thì tia
            // quét trúng luôn mặt nền và quái sẽ tưởng lúc nào phía trước cũng có vật cản.
            Vector3 origin = _cachedTransform.position + Vector3.up * _avoidBodyRadius;

            if (!Physics.SphereCast(origin, _avoidBodyRadius, desired, out RaycastHit hit,
                    _avoidProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
                return desired;

            Vector3 normal = hit.normal;
            normal.y = 0f;

            Vector3 slide = normal.sqrMagnitude > 0.0001f
                ? Vector3.ProjectOnPlane(desired, normal.normalized)
                : Vector3.zero;
            slide.y = 0f;

            // Đâm gần như vuông góc vào vật cản thì phép chiếu ở trên cho ra vector gần bằng 0
            // — không còn hướng nào để trượt. Khi đó phải tự chọn vòng sang trái hay sang phải.
            //
            // Dùng TÂM KHỐI của vật cản chứ không dùng điểm chạm: khi thân quái đã lọt vào trong
            // collider thì Unity trả về điểm chạm là gốc toạ độ thế giới, và chọn hướng theo đó
            // sẽ cho ra một bên hoàn toàn ngẫu nhiên.
            if (slide.sqrMagnitude < 0.01f)
            {
                Vector3 side = Vector3.Cross(Vector3.up, desired);

                Vector3 toObstacle = hit.collider.bounds.center - _cachedTransform.position;
                toObstacle.y = 0f;

                // Vòng sang phía NGƯỢC LẠI với tâm vật cản, tức là đi ra chỗ trống.
                slide = Vector3.Dot(toObstacle, side) > 0f ? -side : side;
            }

            return slide.normalized;
        }

        /// <summary>
        /// Dừng hẳn chuyển động.
        ///
        /// PHẢI kiểm tra kinematic trước khi ghi vận tốc. Lúc quái chết, thân vật lý được
        /// chuyển sang kinematic để cái xác không trượt đi — mà Unity KHÔNG cho đặt vận tốc
        /// lên thân kinematic, ghi vào là một dòng cảnh báo đỏ trong console.
        ///
        /// Đặt lá chắn NGAY TRONG HÀM NÀY chứ không phải ở từng nơi gọi, vì hàm được gọi từ
        /// nhiều chỗ (trạng thái AI, lúc chết, lúc trả về pool) và chỉ cần quên một chỗ là
        /// cảnh báo quay lại. Đây đúng là cách PoolService đã làm và tôi lẽ ra phải làm theo
        /// ngay từ đầu — trước đó tôi mới chỉ vá đúng một nơi gọi trong Setup.
        /// </summary>
        public void StopMoving()
        {
            if (_rigidbody == null || _rigidbody.isKinematic)
                return;

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Ràng buộc nền của thân vật lý: luôn khoá xoay và khoá trục đứng.
        ///
        /// Khoá xoay vì việc xoay do code tự lo với đúng tốc độ trong config — để hệ vật lý
        /// xoay thì va chạm sẽ làm quái quay tít. Khoá trục đứng vì sân phẳng, không có gì
        /// để rơi, mà thả tự do thì một cú va chạm lệch cũng đủ đội quái lên khỏi mặt đất.
        /// </summary>
        private const RigidbodyConstraints BaseConstraints =
            RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        /// <summary>Ràng buộc lúc bị ghim: khoá thêm hai trục ngang nên không ai xô đi được.</summary>
        private const RigidbodyConstraints AnchoredConstraints =
            BaseConstraints | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;

        /// <summary>
        /// Ghim quái tại chỗ, hoặc thả ra.
        ///
        /// VÌ SAO CẦN: spec bắt quái "tấn công xong thì đứng im 1 giây". Nhưng đứng im theo
        /// nghĩa "không tự đi" thì chưa đủ — nó vẫn là một thân vật lý động, nên mấy con phía
        /// sau đang lao tới sẽ húc vào và ĐẨY NÓ TRƯỢT tới trước. Nhìn ra thành cảnh cả đàn
        /// xô nhau dồn cục vào người chơi, và quãng nghỉ một giây — vốn là khoảng thở duy nhất
        /// của người chơi — biến mất.
        ///
        /// Ghim từ lúc BẮT ĐẦU VUNG ĐÒN chứ không phải chỉ trong một giây nghỉ. Lý do là độ
        /// chính xác của đòn đánh: sát thương được tính bằng một hình nón xuất phát từ vị trí
        /// quái tại đúng thời điểm ra đòn. Bị xô lệch đi trong lúc lấy đà là hình nón đó xuất
        /// phát từ chỗ khác, và cú đánh trượt vì một lý do chẳng liên quan gì tới người chơi.
        ///
        /// DÙNG RÀNG BUỘC CHỨ KHÔNG CHUYỂN SANG KINEMATIC. Kinematic sẽ đổi hẳn loại thân vật lý
        /// giữa chừng, kéo theo cả một họ lỗi "không được ghi vận tốc lên thân kinematic" mà dự án
        /// này đã dính hai lần. Khoá trục thì thân vẫn là thân động bình thường: nó vẫn chặn
        /// người chơi và chặn quái khác đúng như một tảng đá, chỉ là không bị đẩy đi.
        /// </summary>
        public void SetAnchored(bool anchored)
        {
            if (_rigidbody == null)
                return;

            _rigidbody.constraints = anchored ? AnchoredConstraints : BaseConstraints;
        }

        /// <summary>
        /// Ra đòn NGAY BÂY GIỜ. Được gọi tại đúng thời điểm gây sát thương trong animation.
        /// Đòn đánh tự đọc vị trí player ở thời điểm này, nên nếu người chơi đã né kịp thì trượt thật.
        /// </summary>
        public void ExecuteAttack()
        {
            _config.Attack?.Execute(_attackContext);

            // Tiếng ra đòn phát ĐÚNG lúc gây sát thương, không phải lúc bắt đầu lấy đà.
            // Nhờ vậy nó là tín hiệu trung thực: nghe thấy tiếng nghĩa là đòn đã ra rồi,
            // né lúc này là muộn. Muốn báo sớm thì đã có động tác vung tay lo việc đó.
            if (_config.Attack is Attacks.ProjectileAttack)
                Audio.GameAudioService.PlayEnemyRangedAttack();
            else
                Audio.GameAudioService.PlayEnemyAttack();
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

            SpawnDeathVfx();
            Audio.GameAudioService.PlayEnemyDeath();

            EnemyRegistry.I?.NotifyDied(this);
            OnDied?.Invoke(this);

            // Trả về pool sau một nhịp cho animation gục xuống chạy xong.
            // KHÔNG trả ngay: quái sẽ biến mất đột ngột, người chơi không kịp thấy mình đã giết được nó.
            // KHÔNG bỏ qua bước này: xác sẽ nằm lại trong scene vĩnh viễn và pool phải tạo object mới
            // cho từng con quái của mọi wave — tức là pool mất sạch tác dụng.
            //
            // PHẢI KIỂM TRA OBJECT CÒN BẬT KHÔNG trước khi mở coroutine. Unity không cho khởi động
            // coroutine trên một object đã tắt, và nó ném ra một dòng cảnh báo đỏ.
            // Tình huống đó có thật: lúc bấm Chơi lại, GameSession gọi KillAll rồi mới DespawnAll,
            // nên một con đã được trả về pool (đã tắt) vẫn có thể nhận lệnh chết ngay sau đó.
            // Object đã tắt thì cũng chẳng cần chờ animation gục — trả thẳng về pool là đúng.
            if (_despawnDelay > 0f && isActiveAndEnabled)
                StartCoroutine(DespawnAfterDelay());
            else
                ReturnToPool();
        }

        /// <summary>
        /// Nổ hiệu ứng tại chỗ quái vừa gục.
        ///
        /// Lấy từ pool chứ không tạo mới: một ván bình thường giết vài chục con, mà mỗi hiệu ứng
        /// là cả một cụm hệ hạt — tạo rồi huỷ liên tục là nguồn rác lớn nhất trong cả trận đánh.
        ///
        /// Hiệu ứng KHÔNG gắn làm con của cái xác. Nếu gắn thì tới lúc xác được trả về pool
        /// (1.35 giây sau) hiệu ứng cũng bị kéo đi theo và biến mất giữa chừng.
        /// </summary>
        private void SpawnDeathVfx()
        {
            if (_config == null || _config.DeathVfx == null || Pooling.PoolService.I == null)
                return;

            Vector3 position = _cachedTransform.position + Vector3.up * _config.DeathVfxHeight;
            Pooling.PoolService.I.Spawn(_config.DeathVfx, position, Quaternion.identity);
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
