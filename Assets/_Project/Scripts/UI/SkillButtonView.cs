using Survival.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Một nút kỹ năng trên màn hình.
    ///
    /// Nút KHÔNG biết nó đang là skill nào. Nó nhận vào một <see cref="SkillRuntime"/>
    /// rồi hiển thị bất cứ thứ gì skill đó báo ra: icon, thời gian hồi chiêu, số charge.
    /// Nhờ vậy thêm skill thứ tư không cần viết thêm nút mới — xem <see cref="SkillBarView"/>.
    ///
    /// Cách hiển thị cooldown: dùng một ảnh đặt Image Type = Filled, Fill Method = Radial 360.
    /// Ảnh đó phủ lên icon và bị "ăn" dần theo hình quạt tròn giống kim đồng hồ,
    /// nên người chơi nhìn phát biết còn bao lâu mà không cần đọc số.
    /// </summary>
    public class SkillButtonView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private Image _iconImage;

        [SerializeField, Tooltip("Ảnh phủ lên icon, Image Type = Filled, Fill Method = Radial 360.")]
        private Image _cooldownOverlay;

        [SerializeField, Tooltip("Số giây hồi chiêu còn lại.")]
        private TextMeshProUGUI _cooldownText;

        [SerializeField, Tooltip("Số charge còn lại. Tự ẩn với skill không dùng charge.")]
        private TextMeshProUGUI _chargeText;

        [SerializeField, Tooltip("Vòng chia đoạn hiển thị charge. Tự ẩn với skill không dùng charge.")]
        private ChargeRingView _chargeRing;

        [SerializeField, Tooltip("Màu icon khi skill chưa sẵn sàng.")]
        private Color _disabledTint = new Color(0.45f, 0.45f, 0.45f, 1f);

        private SkillRuntime _skill;
        private System.Action _onPressed;

        private Color _defaultTint = Color.white;
        private bool _lastCanUse = true;
        private int _lastCharges = int.MinValue;
        private int _lastWholeSecondsLeft = int.MinValue;

        public void Bind(SkillRuntime skill, System.Action onPressed)
        {
            _skill = skill;
            _onPressed = onPressed;

            if (_iconImage != null)
            {
                _iconImage.sprite = skill.Def.Icon;
                _iconImage.enabled = skill.Def.Icon != null;
                _defaultTint = Color.white;
            }

            // Skill không dùng charge trả về -1, khi đó ẩn hẳn phần hiển thị charge đi.
            bool usesCharges = skill.MaxCharges > 0;

            if (_chargeText != null)
                _chargeText.gameObject.SetActive(usesCharges);

            if (_chargeRing != null)
            {
                if (usesCharges)
                    _chargeRing.Build(skill.MaxCharges);
                else
                    _chargeRing.gameObject.SetActive(false);
            }

            Refresh(force: true);
        }

        /// <summary>
        /// Dùng <c>IPointerDownHandler</c> thay vì <c>Button.onClick</c>:
        /// onClick chỉ kích hoạt khi người chơi NHẢ tay ra, tạo cảm giác trễ rõ rệt
        /// trong một game hành động. Bấm xuống là bắn ngay thì phản hồi tức thì.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData) => _onPressed?.Invoke();

        private void Update()
        {
            if (_skill != null)
                Refresh(force: false);
        }

        /// <summary>
        /// Chỉ ghi vào thành phần UI khi giá trị THỰC SỰ đổi.
        ///
        /// Gán text hoặc màu cho một thành phần UI khiến Unity đánh dấu toàn bộ canvas là "bẩn"
        /// và phải dựng lại lưới hình học của nó. Ghi mỗi khung hình dù không có gì đổi
        /// là một trong những nguyên nhân tụt khung hình phổ biến nhất trên điện thoại.
        /// Vì thế số giây chỉ được ghi lại khi phần nguyên của nó đổi, không phải 60 lần mỗi giây.
        /// </summary>
        private void Refresh(bool force)
        {
            float normalized = _skill.CooldownNormalized;

            // fillAmount chỉ ghi vào một con số, không dựng lại lưới chữ -> ghi mỗi khung hình vẫn rẻ.
            if (_cooldownOverlay != null)
                _cooldownOverlay.fillAmount = 1f - normalized;

            bool canUse = _skill.CanUse;
            if (force || canUse != _lastCanUse)
            {
                _lastCanUse = canUse;
                if (_iconImage != null)
                    _iconImage.color = canUse ? _defaultTint : _disabledTint;
            }

            if (_cooldownText != null)
            {
                float remaining = _skill.CooldownRemaining;
                int whole = Mathf.CeilToInt(remaining);
                if (force || whole != _lastWholeSecondsLeft)
                {
                    _lastWholeSecondsLeft = whole;
                    _cooldownText.text = remaining > 0.05f ? whole.ToString() : string.Empty;
                }
            }

            if (_skill.MaxCharges > 0)
            {
                int charges = _skill.ChargeCount;

                if (_chargeText != null && (force || charges != _lastCharges))
                {
                    _lastCharges = charges;
                    _chargeText.text = charges.ToString();
                }

                // Vòng charge cập nhật mỗi khung hình vì đoạn đang hồi phải sáng dần lên mượt.
                // Bên trong nó cũng chỉ ghi màu khi màu thật sự đổi.
                if (_chargeRing != null)
                    _chargeRing.SetCharges(charges, charges >= _skill.MaxCharges ? 1f : normalized);
            }
        }
    }
}
