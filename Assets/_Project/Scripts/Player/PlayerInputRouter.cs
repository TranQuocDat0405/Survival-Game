using UnityEngine;

namespace Survival.Player
{
    /// <summary>
    /// Gom mọi nguồn điều khiển di chuyển về một chỗ.
    ///
    /// Nhờ có lớp trung gian này, <see cref="PlayerMotor"/> không hề biết joystick là cái gì.
    /// Nó chỉ hỏi "hướng di chuyển hiện tại là gì?". Hệ quả:
    ///   - Test trong Editor bằng bàn phím WASD cho nhanh, không cần rê chuột lên joystick.
    ///   - Sau này muốn thêm tay cầm, hoặc muốn AI điều khiển player, chỉ sửa đúng file này.
    /// </summary>
    public class PlayerInputRouter : MonoBehaviour
    {
        [SerializeField, Tooltip("Joystick ảo trên UI. Kéo prefab joystick trong scene vào đây.")]
        private Joystick _joystick;

        [SerializeField, Tooltip(
            "Cho phép điều khiển bằng WASD / phím mũi tên. " +
            "Rất tiện khi test nhanh trong Editor và khi người chấm chơi bản build Windows.")]
        private bool _enableKeyboardFallback = true;

        /// <summary>
        /// Hướng di chuyển mong muốn, trong hệ trục của MÀN HÌNH:
        /// x = trái/phải, y = lên/xuống. Độ dài từ 0 tới 1.
        /// Việc đổi sang toạ độ thế giới là việc của <see cref="PlayerMotor"/>.
        /// </summary>
        public Vector2 MoveInput { get; private set; }

        public void SetJoystick(Joystick joystick) => _joystick = joystick;

        private void Update()
        {
            Vector2 input = _joystick != null ? _joystick.Direction : Vector2.zero;

            // Bàn phím chỉ được dùng khi joystick đang không bị chạm vào,
            // để hai nguồn không tranh nhau ghi đè lên nhau.
            if (_enableKeyboardFallback && input.sqrMagnitude < 0.0001f)
            {
                input = new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical"));
            }

            // Chặn độ dài lớn hơn 1: đi chéo bằng bàn phím cho ra vector dài 1.41,
            // nếu không chặn thì đi chéo sẽ nhanh hơn đi thẳng khoảng 41%.
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            MoveInput = input;
        }
    }
}
