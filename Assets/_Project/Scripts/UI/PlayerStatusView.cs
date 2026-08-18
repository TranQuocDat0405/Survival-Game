using Survival.Combat;
using Survival.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Thanh máu và số cấp của player trên màn hình. Yêu cầu bắt buộc ở spec mục 6.
    ///
    /// Toàn bộ lớp này KHÔNG có hàm Update. Nó đăng ký nghe sự kiện một lần lúc bật lên
    /// rồi ngồi im; máu đổi thì <c>Health</c> gọi nó, cấp đổi thì hệ thống EXP gọi nó.
    ///
    /// Vì sao quan trọng: cách làm thông thường là mỗi khung hình đọc lại máu rồi ghi vào
    /// thanh và ghi vào chữ. Ghi chữ khiến Unity phải dựng lại toàn bộ lưới hình học của
    /// canvas — làm 60 lần mỗi giây trong khi con số không hề đổi là lãng phí thuần tuý,
    /// và là nguyên nhân tụt khung hình rất hay gặp trên điện thoại.
    ///
    /// NỐI TRONG <c>OnEnable</c> CHỨ KHÔNG PHẢI <c>Start</c> — ĐÂY LÀ ĐIỂM DỄ SAI NHẤT.
    /// Màn hình này nằm trong một prefab do UIManager quản, mà UIManager KHÔNG huỷ view khi
    /// đóng: nó tắt đi rồi cất vào bộ nhớ đệm. Nghĩa là <c>Start</c> chạy đúng MỘT lần trong cả
    /// vòng đời ứng dụng. Còn player thì chết theo scene trận đấu và được tạo mới ở trận sau.
    /// Nối trong <c>Start</c> thì từ trận thứ HAI trở đi lớp này vẫn đang nghe một <c>Health</c>
    /// đã bị huỷ, và thanh máu đứng im suốt cả trận mà không có một dòng lỗi nào.
    /// Khuôn <c>OnEnable</c>/<c>OnDisable</c> dưới đây lấy đúng từ <see cref="HurtFlashView"/>.
    /// </summary>
    public class PlayerStatusView : MonoBehaviour
    {
        [Header("Máu")]
        [SerializeField, Tooltip("Ảnh thanh máu, Image Type = Filled, Fill Method = Horizontal.")]
        private Image _healthFill;

        [SerializeField, Tooltip("Chữ dạng 'máu hiện tại / máu tối đa'. Có thể để trống.")]
        private TextMeshProUGUI _healthText;

        [Header("Cấp độ")]
        [SerializeField] private TextMeshProUGUI _levelText;

        [SerializeField, Tooltip("Thanh kinh nghiệm, Image Type = Filled. Có thể để trống.")]
        private Image _expFill;

        [SerializeField, Tooltip("Chữ dạng 'exp hiện tại / exp cần'. Có thể để trống.")]
        private TextMeshProUGUI _expText;

        [SerializeField, Tooltip(
            "ĐỂ TRỐNG. Player sống ở scene trận đấu còn màn hình này nằm trong prefab ở scene Main, " +
            "nên không kéo dây sang được. Nó tự tìm PlayerActor.Current mỗi lần bật lên.")]
        private PlayerActor _player;

        private Health _health;
        private Progression.ExperienceSystem _experience;

        private void OnEnable() => TryBind();

        private void OnDisable() => Unbind();

        /// <summary>
        /// Thử nối vào player và hệ thống EXP. Bỏ qua trong im lặng nếu chúng chưa tồn tại —
        /// <see cref="Update"/> sẽ thử lại ở khung hình sau.
        /// </summary>
        private void TryBind()
        {
            if (_health != null)
                return;

            if (_player == null)
                _player = PlayerActor.Current;

            if (_player == null || _player.Health == null)
                return;

            _health = _player.Health;
            _health.Current.OnValueChanged += HandleHealthChanged;
            _health.OnMaxChanged += HandleMaxChanged;

            RefreshHealth();

            // Tự nối vào hệ thống EXP nếu có. HUD chỉ NGHE, không bao giờ gọi ngược lại
            // hệ thống EXP — nhờ vậy hệ thống EXP chạy được kể cả khi không có UI nào cả.
            //
            // Hệ thống EXP cũng chết theo scene trận đấu y như player, nên nó phải được nối lại
            // ở đây chứ không chỉ nối một lần lúc khởi động.
            _experience = Progression.ExperienceSystem.I;
            if (_experience != null)
            {
                _experience.Level.OnValueChanged += HandleLevelChanged;
                _experience.CurrentExp.OnValueChanged += HandleExpChanged;

                SetLevel(_experience.Level.Value);
                SetExp(_experience.CurrentExp.Value, _experience.ExpPerLevel);
            }
            else
            {
                SetLevel(1);
                SetExp(0, 100);
            }
        }

        /// <summary>
        /// Gỡ sạch mọi đăng ký và QUÊN luôn tham chiếu.
        ///
        /// Đặt về null là phần bắt buộc, không phải dọn dẹp cho đẹp: nó chính là thứ cho phép
        /// <see cref="TryBind"/> nối lại vào player của trận sau. Giữ lại tham chiếu cũ thì chốt
        /// "đã nối rồi thì thôi" ở đầu TryBind sẽ chặn luôn lần nối mới.
        /// </summary>
        private void Unbind()
        {
            if (_health != null)
            {
                _health.Current.OnValueChanged -= HandleHealthChanged;
                _health.OnMaxChanged -= HandleMaxChanged;
                _health = null;
            }

            if (_experience != null)
            {
                _experience.Level.OnValueChanged -= HandleLevelChanged;
                _experience.CurrentExp.OnValueChanged -= HandleExpChanged;
                _experience = null;
            }

            _player = null;
        }

        /// <summary>
        /// Player có thể chưa kịp tồn tại đúng khung hình mà màn hình này bật lên, nên thử lại
        /// cho tới khi nối được. Một phép so null mỗi khung hình rẻ hơn nhiều so với một thanh
        /// máu đứng im mà không ai hiểu vì sao.
        /// </summary>
        private void Update()
        {
            if (_health == null)
                TryBind();
        }

        private void HandleLevelChanged(int level)
        {
            SetLevel(level);
            // Lên cấp thì EXP đã bị trừ đi, phải vẽ lại thanh EXP cho khớp.
            if (_experience != null)
                SetExp(_experience.CurrentExp.Value, _experience.ExpPerLevel);
        }

        private void HandleExpChanged(int exp)
        {
            if (_experience != null)
                SetExp(exp, _experience.ExpPerLevel);
        }

        private void HandleHealthChanged(float _) => RefreshHealth();
        private void HandleMaxChanged(Health _) => RefreshHealth();

        private void RefreshHealth()
        {
            if (_health == null)
                return;

            if (_healthFill != null)
                _healthFill.fillAmount = _health.Normalized;

            if (_healthText != null)
                _healthText.text = $"{Mathf.CeilToInt(_health.Current.Value)} / {Mathf.CeilToInt(_health.Max)}";
        }

        /// <summary>Hệ thống EXP gọi hàm này. Nó không tự đi tìm hệ thống đó.</summary>
        public void SetLevel(int level)
        {
            if (_levelText != null)
                _levelText.text = $"Lv.{level}";
        }

        public void SetExp(int current, int required)
        {
            if (_expFill != null)
                _expFill.fillAmount = required > 0 ? Mathf.Clamp01((float)current / required) : 0f;

            // Ghi rõ chữ "EXP" chứ không chỉ để hai con số. Một thanh màu xanh với "20 / 100"
            // bên trong không tự nói được nó là kinh nghiệm hay là thứ gì khác —
            // người chơi lần đầu nhìn vào sẽ không biết nó dùng để làm gì.
            if (_expText != null)
                _expText.text = $"EXP  {current} / {required}";
        }
    }
}
