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
            "Trên điện thoại phần này bị chặn hẳn bằng Application.isMobilePlatform: Unity ánh xạ " +
            "chạm thành chuột, nên nếu không chặn thì nhân vật sẽ xoay về phía ngón tay đang giữ " +
            "joystick chứ không xoay theo hướng di chuyển.")]
        private bool _aimWithMouse = true;

        [SerializeField, Tooltip("Mặt phẳng mà tia chuột chiếu xuống, tính theo độ cao. Nên đặt ngang tầm ngực nhân vật.")]
        private float _aimPlaneHeight = 0.7f;

        private Camera _camera;
        private PlayerMotor _motor;

        /// <summary>
        /// Mỗi phím phải được nhìn thấy ở trạng thái NHẢ RA ít nhất một lần thì mới được phép kích hoạt.
        ///
        /// Đây là bản vá cho lỗi "vừa bấm Play là nhân vật tự bắn một phát".
        /// Nguyên nhân: người chấm bấm nút Play trên thanh công cụ Unity bằng CHUỘT TRÁI,
        /// mà chuột trái cũng chính là phím bắn. Cú nhấn đó chưa được nhả ra khi play mode bắt đầu,
        /// nên ở khung hình đầu tiên <c>Input.GetKeyDown(Mouse0)</c> báo true và skill khai hoả
        /// dù người chơi chưa hề bấm gì trong game.
        ///
        /// Hậu quả không chỉ là một phát đạn thừa: nó tiêu mất một charge và mở luôn
        /// khoảng chờ 0.5 giây, nên ván nào cũng bắt đầu ở trạng thái thiếu tài nguyên.
        ///
        /// Cách sửa là bỏ qua mọi phím đang bị giữ sẵn từ TRƯỚC khi vào game.
        /// Trên bản build thì không có phím nào bị giữ lúc màn chơi mở ra,
        /// nên mọi phím đều sẵn sàng ngay từ khung hình đầu — không mất gì cả.
        /// </summary>
        private bool[] _primaryArmed;
        private bool[] _alternateArmed;

        private void Awake()
        {
            if (_player == null)
                _player = GetComponent<PlayerActor>();

            _motor = GetComponent<PlayerMotor>();

            _primaryArmed = new bool[_skillKeys.Length];
            _alternateArmed = new bool[_alternateKeys.Length];
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

            // TUYỆT ĐỐI KHÔNG chạy nhánh này trên điện thoại.
            //
            // Unity ÁNH XẠ CHẠM THÀNH CHUỘT: trên Android, <c>Input.mousePosition</c> trả về vị trí
            // ngón tay chạm gần nhất chứ không phải một con trỏ chuột thật. Hệ quả trên máy thật:
            //   - Người chơi giữ joystick ở góc dưới trái, nên "chuột" luôn nằm ở góc dưới trái,
            //     và nhân vật xoay về phía đó — kéo joystick đi hướng nào cũng vẫn quay sang trái.
            //   - Chạm một nút kỹ năng bên phải là "chuột" nhảy sang đó, nhân vật đột ngột xoay theo.
            //
            // Lỗi này KHÔNG BAO GIỜ lộ ra trong Editor vì ở đó có chuột thật. Chỉ người cầm bản
            // build trên điện thoại mới thấy.
            //
            // Trên điện thoại, việc xoay người do joystick lo (xoay theo hướng chạy) và do thao tác
            // kéo trên nút bắn lo (khi cần ngắm riêng một hướng) — không cần tới nhánh này.
            if (Application.isMobilePlatform)
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

            // TẮT TOÀN BỘ trên điện thoại. Ở đó mọi thao tác đã có joystick và các nút trên màn
            // hình lo, và Unity ánh xạ chạm thành cả chuột lẫn phím chuột nên lớp này gây ra
            // hai lỗi cùng lúc nếu để chạy:
            //
            //   1. Xoay sai hướng. Nhả tay ra rồi thì không còn touch nào, nên hàm chặn
            //      IsPointerOverUI bên dưới trả về false — nhưng Input.mousePosition vẫn GIỮ
            //      NGUYÊN vị trí chạm cuối cùng. Nhân vật xoay mãi về phía ngón tay vừa rời đi,
            //      thường là góc dưới trái nơi đặt joystick.
            //   2. Tự bắn. Phím của kỹ năng đầu tiên là KeyCode.Mouse0, mà Unity coi mỗi cú chạm
            //      là một lần nhấn chuột trái. Chạm vào chỗ trống trên màn hình là bắn một phát.
            //
            // Cả hai đều KHÔNG lộ ra trong Editor vì ở đó chuột và cảm ứng là hai thứ tách bạch.
            if (Application.isMobilePlatform)
                return;

            // Cập nhật trạng thái "đã nhả ra chưa" TRƯỚC mọi lần thoát sớm bên dưới.
            // Nếu để sau, một ngón tay đang đè lên nút UI sẽ khiến hàm thoát sớm mãi
            // và phím không bao giờ được ghi nhận là đã nhả — bấm cách mấy cũng không bắn được.
            UpdateArming();

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

        /// <summary>
        /// Ghi nhận phím nào đã được nhả ra. Phím chỉ "sẵn sàng" sau khi thấy nó ở trạng thái nhả.
        /// </summary>
        private void UpdateArming()
        {
            for (int i = 0; i < _skillKeys.Length; i++)
                if (_skillKeys[i] != KeyCode.None && !Input.GetKey(_skillKeys[i]))
                    _primaryArmed[i] = true;

            for (int i = 0; i < _alternateKeys.Length; i++)
                if (_alternateKeys[i] != KeyCode.None && !Input.GetKey(_alternateKeys[i]))
                    _alternateArmed[i] = true;
        }

        private bool IsTriggered(int index)
        {
            KeyCode primary = _skillKeys[index];
            if (primary != KeyCode.None && _primaryArmed[index] && IsPressed(primary))
                return true;

            if (index < _alternateKeys.Length)
            {
                KeyCode alternate = _alternateKeys[index];
                if (alternate != KeyCode.None && _alternateArmed[index] && IsPressed(alternate))
                    return true;
            }

            return false;
        }

        private bool IsPressed(KeyCode key)
            => _allowHoldToRepeat ? Input.GetKey(key) : Input.GetKeyDown(key);
    }
}
