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

        [SerializeField, Tooltip("Nền tròn phía sau số charge, để số đọc được trên mọi nền.")]
        private RectTransform _chargeBadge;

        [SerializeField, Tooltip("Vòng chia đoạn hiển thị charge. Tự ẩn với skill không dùng charge.")]
        private ChargeRingView _chargeRing;

        [Header("Màu số hiển thị")]
        [SerializeField, Tooltip(
            "Màu số giây khi đang HỒI CHARGE. Cố tình đặt trùng màu vòng charge " +
            "để mắt nối ngay con số này với cái vòng, không nhầm nó với số đạn.")]
        private Color _chargeTimerColor = new Color(1f, 0.88f, 0.35f);

        [SerializeField, Tooltip("Màu số giây khi đang HỒI CHIÊU thường (bom, dash).")]
        private Color _cooldownTimerColor = Color.white;

        [SerializeField, Tooltip(
            "Làm tối icon khi đang có số giây hiện lên.\n\n" +
            "Cần thiết vì con số nằm ĐÈ LÊN icon. Icon sáng màu (tâm ngắm trắng) cộng với " +
            "chữ vàng cho ra độ tương phản gần bằng không — số vẫn hiện đúng nhưng mắt không đọc ra, " +
            "và người chơi tưởng nó biến mất.")]
        private bool _dimIconWhileTimerVisible = true;

        [SerializeField, Range(0.05f, 1f), Tooltip("Icon còn lại bao nhiêu phần độ sáng khi có số hiện lên.")]
        private float _iconDimFactor = 0.35f;

        /// <summary>Số giây đang hiển thị có khác rỗng hay không. Dùng để quyết định làm tối icon.</summary>
        private bool _timerVisible;

        private bool _lastTimerVisible;

        [SerializeField, Tooltip("Màu icon khi skill chưa sẵn sàng.")]
        private Color _disabledTint = new Color(0.45f, 0.45f, 0.45f, 1f);

        private SkillRuntime _skill;
        private System.Action _onPressed;

        [SerializeField, Tooltip("Nền tròn của nút. SkillBarView đổi màu nút chính qua đây.")]
        private Image _backgroundImage;

        [SerializeField, Tooltip("Vòng viền ngoài. Tỉ lệ so với kích thước nút.")]
        private RectTransform _chargeRingRect;

        [SerializeField, Range(1f, 2f), Tooltip("Vòng charge lớn hơn nút bao nhiêu lần.")]
        private float _chargeRingScale = 1.15f;

        private Color _defaultTint = Color.white;
        private bool _lastCanUse = true;
        private int _lastCharges = int.MinValue;
        private int _lastWholeSecondsLeft = int.MinValue;

        /// <summary>
        /// Đặt kích thước và màu nền cho nút. <see cref="SkillBarView"/> gọi hàm này
        /// để nút đánh thường to hơn hẳn các nút bổ trợ.
        ///
        /// Kích thước không được đặt cứng trong prefab vì cùng MỘT prefab được dùng cho
        /// cả nút chính lẫn nút phụ — khác nhau chỉ ở lúc dựng.
        /// </summary>
        public void ApplyStyle(float size, Color backgroundColor, float iconRatio)
        {
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(size, size);

            if (_backgroundImage != null)
                _backgroundImage.color = backgroundColor;

            if (_iconImage != null)
                ((RectTransform)_iconImage.transform).sizeDelta = Vector2.one * (size * iconRatio);

            if (_chargeRingRect != null)
                _chargeRingRect.sizeDelta = Vector2.one * (size * _chargeRingScale);

            if (_cooldownText != null)
                _cooldownText.fontSize = size * 0.34f;

            // Số đạn được đẩy HẲN RA NGOÀI đường tròn của nút.
            // Trước đây nó nằm sát mép trong nên bị chính viền nút và vòng charge che mất một phần.
            // Đặt tâm huy hiệu ở khoảng cách 0.40 lần kích thước nút theo đường chéo dưới-phải,
            // tức là nằm ngay ngoài rìa vòng charge, không thứ gì đè lên được.
            if (_chargeBadge != null)
            {
                float badgeSize = size * 0.34f;
                float offset = size * 0.40f;

                _chargeBadge.sizeDelta = Vector2.one * badgeSize;
                _chargeBadge.anchorMin = new Vector2(0.5f, 0.5f);
                _chargeBadge.anchorMax = new Vector2(0.5f, 0.5f);
                _chargeBadge.pivot = new Vector2(0.5f, 0.5f);
                _chargeBadge.anchoredPosition = new Vector2(offset, -offset);

                if (_chargeText != null)
                    _chargeText.fontSize = badgeSize * 0.72f;
            }
        }

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

            if (_chargeBadge != null)
                _chargeBadge.gameObject.SetActive(usesCharges);

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

            if (_cooldownText != null)
            {
                bool usesCharges = _skill.MaxCharges > 0;

                // Skill dùng charge thì con số có ích là "bao lâu nữa có viên tiếp theo",
                // KHÔNG phải cooldown 0.5 giây (quá ngắn, chỉ làm nháy chữ '1' vô nghĩa).
                // Skill thường thì ngược lại, con số chính là cooldown của nó.
                float seconds = usesCharges ? _skill.ChargeTimeRemaining : _skill.CooldownRemaining;

                // Skill thường có cooldown 6 và 12 giây nên luôn qua ngưỡng 1 giây;
                // ngưỡng này chỉ để chặn mấy phần lẻ cuối cùng nhấp nháy.
                float threshold = usesCharges ? 0.05f : 1f;
                int whole = seconds >= threshold ? Mathf.CeilToInt(seconds) : 0;

                if (force || whole != _lastWholeSecondsLeft)
                {
                    _lastWholeSecondsLeft = whole;
                    _timerVisible = whole > 0;
                    _cooldownText.text = _timerVisible ? whole.ToString() : string.Empty;
                    _cooldownText.color = usesCharges ? _chargeTimerColor : _cooldownTimerColor;
                }
            }

            // Icon được tô lại SAU khi biết có số hiện lên hay không, vì màu icon
            // phụ thuộc cả vào trạng thái dùng được lẫn vào việc có con số đè lên nó.
            bool canUse = _skill.CanUse;
            if (force || canUse != _lastCanUse || _timerVisible != _lastTimerVisible)
            {
                _lastCanUse = canUse;
                _lastTimerVisible = _timerVisible;

                if (_iconImage != null)
                {
                    Color tint = canUse ? _defaultTint : _disabledTint;

                    if (_dimIconWhileTimerVisible && _timerVisible)
                        tint = new Color(tint.r * _iconDimFactor, tint.g * _iconDimFactor, tint.b * _iconDimFactor, tint.a);

                    _iconImage.color = tint;
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

                // Vòng dùng ChargeProgress (đồng hồ 3 giây), KHÔNG dùng normalized (đồng hồ 0.5 giây).
                // Cập nhật mỗi khung hình để đoạn đang hồi sáng dần lên mượt;
                // bên trong nó cũng chỉ ghi màu khi màu thật sự đổi.
                if (_chargeRing != null)
                    _chargeRing.SetCharges(charges, _skill.ChargeProgress);
            }
        }
    }
}
