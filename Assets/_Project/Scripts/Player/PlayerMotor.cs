using Survival.Stats;
using UnityEngine;

namespace Survival.Player
{
    /// <summary>
    /// Di chuyển và xoay thân nhân vật.
    ///
    /// Hai luật quan trọng của spec nằm trọn trong file này:
    ///
    /// 1. TỐC ĐỘ XOAY 180 ĐỘ/GIÂY.
    ///    Spec: "nhân vật đang chạy sang phải, joystick kéo sang trái (đổi hướng 180°)
    ///    thì cần khoảng 1 giây để xoay xong". 180 độ ÷ 180 độ/giây = đúng 1 giây.
    ///    Cài đặt bằng <c>Quaternion.RotateTowards</c> với giới hạn góc quay mỗi khung hình
    ///    là (tốc độ xoay × thời gian khung hình) — bảo đảm đúng con số, không nhanh hơn.
    ///    KHÔNG dùng <c>Slerp</c>: Slerp xoay theo tỉ lệ phần trăm nên lúc đầu rất nhanh
    ///    rồi chậm dần, không bao giờ ra đúng 180 độ/giây.
    ///
    /// 2. HƯỚNG BẮN LÀ FORWARD HIỆN TẠI, KHÔNG PHẢI HƯỚNG JOYSTICK.
    ///    File này chỉ xoay thân. Các skill đọc <c>transform.forward</c> tại đúng thời điểm
    ///    khai hoả. Nên nếu người chơi vừa bẻ joystick 180 độ rồi bắn ngay,
    ///    mũi tên vẫn bay theo hướng cũ cho tới khi thân xoay xong — đúng như spec mô tả.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Tooltip(
            "BẬT = kiểu xe tăng, chỉ đi theo hướng đang quay mặt.\n" +
            "TẮT = đi ngay theo hướng joystick, thân xoay dần theo sau (mặc định).")]
        private bool _moveAlongForwardOnly;

        [SerializeField, Range(0f, 1f), Tooltip("Dưới ngưỡng này coi như joystick ở giữa.")]
        private float _deadZone = 0.1f;

        private Rigidbody _rigidbody;
        private IStatProvider _stats;

        /// <summary>Hướng người chơi muốn đi, đã đổi sang toạ độ thế giới. Do bên ngoài ghi vào mỗi khung hình.</summary>
        private Vector3 _desiredDirection;

        private float _inputMagnitude;

        /// <summary>Bị khoá khi đang lướt dash — lúc đó dash tự lo việc di chuyển.</summary>
        public bool ControlLocked { get; set; }

        /// <summary>Tốc độ thực tế đang di chuyển, chia cho tốc độ tối đa. Animator dùng để pha giữa Idle và Run.</summary>
        public float NormalizedSpeed { get; private set; }

        public bool IsMoving => _inputMagnitude > _deadZone;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // Cấu hình bắt buộc để nhân vật đi trên mặt phẳng mà không bị vật lý làm loạn:
            //   - không trọng lực: sàn phẳng, không cần rơi
            //   - khoá xoay: chỉ script này được quyền xoay, va chạm không được đẩy nhân vật quay mòng mòng
            //   - khoá trục Y: không bị hất lên khi húc vào quái
            //   - nội suy: hình ảnh mượt vì vật lý chạy 50 lần/giây còn màn hình vẽ 60 lần/giây
            _rigidbody.useGravity = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Player nặng hơn quái rất nhiều để một đám 5-6 con vây quanh không xô đẩy được.
            // Không khoá hẳn va chạm, vì vẫn cần quái bị chặn lại chứ không đi xuyên qua người;
            // chỉ làm cho lực đẩy của chúng gần như không tác dụng lên player.
            _rigidbody.mass = 100f;
        }

        public void Initialize(IStatProvider stats, bool moveAlongForwardOnly, float deadZone)
        {
            _stats = stats;
            _moveAlongForwardOnly = moveAlongForwardOnly;
            _deadZone = deadZone;
        }

        /// <summary>
        /// Nhận hướng từ joystick (hệ trục màn hình) và đổi sang hệ trục thế giới.
        /// Camera của game nhìn từ trên xuống và KHÔNG bao giờ xoay quanh trục đứng,
        /// nên phép đổi là trực tiếp: joystick lên = thế giới +Z, joystick phải = thế giới +X.
        /// </summary>
        public void SetMoveInput(Vector2 screenInput)
        {
            _inputMagnitude = Mathf.Clamp01(screenInput.magnitude);

            if (_inputMagnitude <= _deadZone)
            {
                _desiredDirection = Vector3.zero;
                _inputMagnitude = 0f;
                return;
            }

            _desiredDirection = new Vector3(screenInput.x, 0f, screenInput.y).normalized;
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            if (ControlLocked)
            {
                NormalizedSpeed = 0f;
                return;
            }

            RotateTowardsDesired(deltaTime);
            MoveStep(deltaTime);
        }

        private void RotateTowardsDesired(float deltaTime)
        {
            if (_desiredDirection.sqrMagnitude < 0.0001f)
                return;

            float rotationSpeed = _stats != null ? _stats.Get(EStatType.RotationSpeed) : 180f;

            Quaternion target = Quaternion.LookRotation(_desiredDirection, Vector3.up);
            Quaternion next = Quaternion.RotateTowards(_rigidbody.rotation, target, rotationSpeed * deltaTime);

            _rigidbody.MoveRotation(next);
        }

        private void MoveStep(float deltaTime)
        {
            float moveSpeed = _stats != null ? _stats.Get(EStatType.MoveSpeed) : 0f;

            if (_desiredDirection.sqrMagnitude < 0.0001f || moveSpeed <= 0f)
            {
                _rigidbody.velocity = Vector3.zero;
                NormalizedSpeed = 0f;
                return;
            }

            // Kiểu xe tăng thì đi theo hướng đang quay mặt; kiểu mặc định thì đi thẳng theo joystick.
            Vector3 direction = _moveAlongForwardOnly ? transform.forward : _desiredDirection;

            Vector3 velocity = direction * (moveSpeed * _inputMagnitude);
            velocity.y = 0f;

            // Đặt thẳng vận tốc thay vì MovePosition: vận tốc để hệ vật lý tự lo va chạm,
            // nhân vật trượt dọc theo tường thay vì đâm xuyên qua hoặc bị kẹt cứng.
            _rigidbody.velocity = velocity;

            NormalizedSpeed = _inputMagnitude;
        }

        /// <summary>Đặt lại vị trí và dừng hẳn. Dùng khi chơi lại từ đầu.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            _desiredDirection = Vector3.zero;
            _inputMagnitude = 0f;
        }
    }
}
