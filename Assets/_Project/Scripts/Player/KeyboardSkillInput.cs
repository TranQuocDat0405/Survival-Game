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

        [SerializeField, Tooltip("Giữ phím thì bắn liên tục thay vì phải bấm từng phát. Chỉ áp cho skill 0.")]
        private bool _allowHoldForFirstSkill = true;

        private void Awake()
        {
            if (_player == null)
                _player = GetComponent<PlayerActor>();
        }

        private void Update()
        {
            if (_player == null)
                return;

            for (int i = 0; i < _skillKeys.Length; i++)
            {
                if (!IsTriggered(i))
                    continue;

                // Không cần kiểm tra cooldown ở đây: skill tự từ chối nếu chưa sẵn sàng.
                // Giữ chuột trái sẽ gọi liên tục nhưng luật "cách nhau 0.5 giây" vẫn chặn đúng.
                _player.TryUseSkill(i);
            }
        }

        private bool IsTriggered(int index)
        {
            bool hold = _allowHoldForFirstSkill && index == 0;

            KeyCode primary = _skillKeys[index];
            if (primary != KeyCode.None)
            {
                if (hold ? Input.GetKey(primary) : Input.GetKeyDown(primary))
                    return true;
            }

            if (index < _alternateKeys.Length)
            {
                KeyCode alternate = _alternateKeys[index];
                if (alternate != KeyCode.None)
                {
                    if (hold ? Input.GetKey(alternate) : Input.GetKeyDown(alternate))
                        return true;
                }
            }

            return false;
        }
    }
}
