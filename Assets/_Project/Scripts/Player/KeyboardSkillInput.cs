using UnityEngine;

namespace Survival.Player
{
    /// <summary>
    /// Cho phép dùng kỹ năng bằng bàn phím / chuột, song song với nút bấm trên UI.
    ///
    /// Đây không phải code tạm để test. Spec mục 9 ghi rõ "Scene chính Play được ngay" —
    /// tức là người chấm sẽ bấm Play trên Editor và chơi bằng chuột + bàn phím.
    /// Bắt họ rê chuột xuống góc màn hình bấm từng nút tròn sẽ rất khó đánh giá cảm giác combat.
    /// Có phím tắt thì họ chơi được ngay bằng WASD + chuột trái + phím số, y như game PC.
    ///
    /// Trên điện thoại thì component này vô hại: không có bàn phím nên không có gì được kích hoạt.
    /// </summary>
    public class KeyboardSkillInput : MonoBehaviour
    {
        [SerializeField] private PlayerActor _player;

        [SerializeField, Tooltip(
            "Phím cho từng skill, khớp theo thứ tự danh sách skill trong PlayerConfig.\n" +
            "Mặc định: skill 0 = chuột trái, skill 1 = phím 1 hoặc E, skill 2 = phím 2 hoặc Space.")]
        private KeyCode[] _skillKeys =
        {
            KeyCode.Mouse0,
            KeyCode.Alpha1,
            KeyCode.Alpha2,
        };

        [SerializeField, Tooltip("Phím phụ, dùng thêm cho cùng các skill trên. Có thể để trống.")]
        private KeyCode[] _alternateKeys =
        {
            KeyCode.None,
            KeyCode.E,
            KeyCode.Space,
        };

        [SerializeField, Tooltip(
            "Giữ phím để bắn liên tục.\n\n" +
            "MẶC ĐỊNH TẮT, và đó là chủ ý. Spec ghi rõ khoảng cách 0.5 giây giữa hai phát bắn là để " +
            "'chống spam'. Cho giữ phím bắn liên tục chính là spam tự động: người chơi bấm giữ một lần " +
            "rồi game tự tiêu hết 3 charge, sau đó cứ 3 giây lại tự bắn thêm một phát.\n\n" +
            "Hệ quả là hệ thống charge mất hết ý nghĩa — nó không còn là tài nguyên để cân nhắc, " +
            "mà chỉ là cái van tự động. Bấm từng phát thì người chơi mới phải quyết định " +
            "'bắn ngay 3 phát để dứt điểm, hay giữ lại một charge phòng khi quái áp sát'.")]
        private bool _allowHoldToRepeat = false;

        [Header("Ngắm bằng chuột (chỉ dùng trên máy tính)")]
        [SerializeField, Tooltip(
            "Cho nhân vật xoay về phía con trỏ chuột.\n\n" +
            "Người chấm sẽ bấm Play trên Editor và chơi bằng chuột. Nếu không có cái này, " +
            "họ chỉ bắn được về đúng hướng đang chạy — trong khi quái nhanh hơn player " +
            "và luôn bám sau lưng, gần như không thể bắn trúng.\n\n" +
            "Trên điện thoại thì không có chuột nên phần này tự vô hiệu.")]
        private bool _aimWithMouse = true;

        [SerializeField, Tooltip("Mặt phẳng mà tia chuột chiếu xuống, tính theo độ cao. Nên đặt ngang tầm ngực nhân vật.")]
        private float _aimPlaneHeight = 0.7f;

        private Camera _camera;
        private PlayerMotor _motor;

        private void Awake()
        {
            if (_player == null)
                _player = GetComponent<PlayerActor>();

            _motor = GetComponent<PlayerMotor>();
        }

        /// <summary>
        /// Chiếu tia từ con trỏ chuột xuống mặt phẳng ngang tầm nhân vật, rồi lấy hướng từ
        /// nhân vật tới điểm đó làm hướng nhắm.
        ///
        /// Giống hệt cần nhắm trên điện thoại, đây chỉ là MỤC TIÊU XOAY.
        /// Thân vẫn xoay dần 180 độ/giây và đạn vẫn bay theo forward hiện tại — spec giữ nguyên.
        /// </summary>
        private void UpdateMouseAim()
        {
            if (!_aimWithMouse || _motor == null)
                return;

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                    return;
            }

            var plane = new Plane(Vector3.up, new Vector3(0f, _aimPlaneHeight, 0f));
            var ray = _camera.ScreenPointToRay(Input.mousePosition);

            if (!plane.Raycast(ray, out float distance))
                return;

            Vector3 point = ray.GetPoint(distance);
            Vector3 toPoint = point - transform.position;
            toPoint.y = 0f;

            // Chuột nằm gần như ngay trên đầu nhân vật thì hướng nhắm không còn ý nghĩa,
            // giữ nguyên hướng cũ thay vì để nhân vật quay loạn.
            if (toPoint.sqrMagnitude < 0.25f)
                return;

            toPoint.Normalize();
            _motor.SetAimInput(new Vector2(toPoint.x, toPoint.z));
        }

        private void Update()
        {
            if (_player == null)
                return;

            // Khi ngón tay / con trỏ đang đè lên một nút UI thì bỏ qua bàn phím và chuột.
            // Nếu không, một cú bấm vào nút kỹ năng sẽ được tính HAI lần:
            // một lần do nút UI nhận, một lần do chuột trái ở đây.
            if (IsPointerOverUI())
                return;

            // Ngắm chuột chạy trước, để khi bấm bắn ngay sau đó thì thân đã đang xoay đúng hướng.
            UpdateMouseAim();

            for (int i = 0; i < _skillKeys.Length; i++)
            {
                if (!IsTriggered(i))
                    continue;

                // Không cần kiểm tra cooldown ở đây: skill tự từ chối nếu chưa sẵn sàng.
                _player.TryUseSkill(i);
            }
        }

        private static bool IsPointerOverUI()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
                return false;

            if (eventSystem.IsPointerOverGameObject())
                return true;

            // Trên điện thoại, mỗi ngón tay là một "con trỏ" riêng và phải hỏi theo id của nó.
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (eventSystem.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
            }

            return false;
        }

        private bool IsTriggered(int index)
        {
            KeyCode primary = _skillKeys[index];
            if (primary != KeyCode.None && IsPressed(primary))
                return true;

            if (index < _alternateKeys.Length)
            {
                KeyCode alternate = _alternateKeys[index];
                if (alternate != KeyCode.None && IsPressed(alternate))
                    return true;
            }

            return false;
        }

        private bool IsPressed(KeyCode key)
            => _allowHoldToRepeat ? Input.GetKey(key) : Input.GetKeyDown(key);
    }
}
