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
    /// Toàn bộ lớp này KHÔNG có hàm Update. Nó đăng ký nghe sự kiện một lần lúc khởi động
    /// rồi ngồi im; máu đổi thì <c>Health</c> gọi nó, cấp đổi thì hệ thống EXP gọi nó.
    ///
    /// Vì sao quan trọng: cách làm thông thường là mỗi khung hình đọc lại máu rồi ghi vào
    /// thanh và ghi vào chữ. Ghi chữ khiến Unity phải dựng lại toàn bộ lưới hình học của
    /// canvas — làm 60 lần mỗi giây trong khi con số không hề đổi là lãng phí thuần tuý,
    /// và là nguyên nhân tụt khung hình rất hay gặp trên điện thoại.
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

        [SerializeField, Tooltip("Để trống thì tự tìm player trong scene lúc khởi động.")]
        private PlayerActor _player;

        private Health _health;

        private void Start()
        {
            if (_player == null)
                _player = PlayerActor.Current;

            if (_player == null)
            {
                Debug.LogError("[PlayerStatusView] không tìm thấy PlayerActor.", this);
                return;
            }

            _health = _player.Health;

            _health.Current.OnValueChanged += HandleHealthChanged;
            _health.OnMaxChanged += HandleMaxChanged;

            RefreshHealth();

            // Tự nối vào hệ thống EXP nếu có. HUD chỉ NGHE, không bao giờ gọi ngược lại
            // hệ thống EXP — nhờ vậy hệ thống EXP chạy được kể cả khi không có UI nào cả.
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

        private Progression.ExperienceSystem _experience;

        private void OnDestroy()
        {
            // Bắt buộc gỡ đăng ký. Nếu không, khi màn chơi được nạp lại,
            // sự kiện vẫn giữ tham chiếu tới object đã bị huỷ và Unity sẽ ném lỗi.
            if (_health != null)
            {
                _health.Current.OnValueChanged -= HandleHealthChanged;
                _health.OnMaxChanged -= HandleMaxChanged;
            }

            if (_experience != null)
            {
                _experience.Level.OnValueChanged -= HandleLevelChanged;
                _experience.CurrentExp.OnValueChanged -= HandleExpChanged;
            }
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

            if (_expText != null)
                _expText.text = $"{current} / {required}";
        }
    }
}
