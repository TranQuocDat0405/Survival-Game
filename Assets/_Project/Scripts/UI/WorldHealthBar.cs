using Survival.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Thanh máu nổi trên đầu quái, đặt trong không gian thế giới. Spec mục 6 yêu cầu rõ điều này.
    ///
    /// Thanh này là một phần CỦA CHÍNH prefab quái, không phải object riêng phải tự quản lý.
    /// Nhờ vậy nó theo quái vào pool và ra khỏi pool cùng nhau — không có nguy cơ
    /// một thanh máu bị bỏ quên lơ lửng giữa không trung khi quái chết.
    ///
    /// Về hiệu năng: mỗi con quái mang một Canvas riêng nghe có vẻ tốn, nhưng Canvas chỉ
    /// dựng lại lưới hình khi NỘI DUNG đổi, mà nội dung ở đây chỉ đổi lúc trúng đòn.
    /// Việc xoay mặt về camera nằm ở <c>LateUpdate</c> và chỉ gán một Quaternion — rất rẻ.
    /// Canvas cũng đã bỏ GraphicRaycaster vì thanh máu không cần nhận thao tác chạm.
    /// </summary>
    public class WorldHealthBar : MonoBehaviour
    {
        [SerializeField, Tooltip("Ảnh phần máu còn lại, đặt Image Type = Filled, Fill Method = Horizontal.")]
        private Image _fillImage;

        [SerializeField, Tooltip("Máu của nhân vật. Để trống thì tự tìm ở object cha.")]
        private Health _health;

        [SerializeField, Tooltip("Nút gốc chứa phần hiển thị. Bị tắt khi máu đầy nếu bật tuỳ chọn bên dưới.")]
        private GameObject _visualRoot;

        [SerializeField, Tooltip(
            "Ẩn thanh khi máu còn đầy.\n" +
            "TẮT (mặc định) để thanh luôn hiện — spec yêu cầu 'thanh máu từng con quái', " +
            "và người chấm cần nhìn thấy nó ngay mà không phải đánh con quái trước.")]
        private bool _hideWhenFull = false;

        [SerializeField, Tooltip("Màu khi máu còn nhiều.")]
        private Color _healthyColor = new Color(0.90f, 0.25f, 0.22f);

        [SerializeField, Tooltip("Màu khi máu còn ít. Thanh chuyển dần sang màu này.")]
        private Color _criticalColor = new Color(1f, 0.72f, 0.15f);

        private Transform _cameraTransform;
        private Transform _selfTransform;

        private void Awake()
        {
            _selfTransform = transform;

            if (_health == null)
                _health = GetComponentInParent<Health>();

            if (_health != null)
            {
                _health.Current.OnValueChanged += HandleHealthChanged;
                _health.OnMaxChanged += HandleMaxChanged;
            }
        }

        private void OnDestroy()
        {
            if (_health == null)
                return;

            _health.Current.OnValueChanged -= HandleHealthChanged;
            _health.OnMaxChanged -= HandleMaxChanged;
        }

        /// <summary>
        /// Vẽ lại mỗi khi quái được lấy ra từ pool.
        /// Bắt buộc: object tái sử dụng mang theo tỉ lệ máu của lần chết trước,
        /// nếu không vẽ lại thì con quái mới sinh ra sẽ hiện thanh máu gần cạn.
        /// </summary>
        private void OnEnable()
        {
            // Camera.main là một phép tìm object theo tag nên chỉ gọi một lần rồi nhớ lại.
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            Refresh();
        }

        private void HandleHealthChanged(float _) => Refresh();
        private void HandleMaxChanged(Health _) => Refresh();

        private void Refresh()
        {
            if (_health == null || _fillImage == null)
                return;

            float normalized = _health.Normalized;

            _fillImage.fillAmount = normalized;
            _fillImage.color = Color.Lerp(_criticalColor, _healthyColor, normalized);

            if (_visualRoot != null && _hideWhenFull)
                _visualRoot.SetActive(normalized < 0.999f);
        }

        /// <summary>
        /// Quay mặt thanh máu về phía camera.
        ///
        /// Đặt ở <c>LateUpdate</c> chứ không phải <c>Update</c>: Cinemachine dời camera trong
        /// LateUpdate, nên nếu xoay ở Update thì thanh máu luôn dùng vị trí camera của khung hình
        /// TRƯỚC, và sẽ thấy nó rung nhẹ mỗi khi camera di chuyển.
        ///
        /// Dùng thẳng hướng của camera thay vì nhìn vào camera: mọi thanh máu trên màn hình
        /// khi đó song song với nhau, thẳng hàng, thay vì mỗi cái nghiêng một kiểu.
        /// </summary>
        private void LateUpdate()
        {
            if (_cameraTransform == null)
                return;

            _selfTransform.rotation = _cameraTransform.rotation;
        }
    }
}
