using DG.Tweening;
using NFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Lớp nền của mọi popup: nút đóng, tiếng bấm, và hiệu ứng bung ra khi mở.
    ///
    /// Có lớp này thì mọi popup trong game cảm giác giống hệt nhau mà không popup nào phải
    /// tự viết lại phần đó — và sửa nhịp bung một lần là cả game đổi theo.
    ///
    /// MỌI TWEEN Ở ĐÂY DÙNG <c>SetUpdate(true)</c>, TỨC THỜI GIAN KHÔNG BỊ CO GIÃN.
    /// Bắt buộc: popup của game này mở ra đúng vào lúc <c>Time.timeScale</c> đang bằng 0
    /// (bảng tạm dừng, bảng kết thúc ván). Thiếu nó thì tween đứng hình vĩnh viễn và người chơi
    /// nhìn thấy một popup mở ra ở kích thước 0 — tức là một màn hình trống không bấm được gì.
    /// </summary>
    public class Popup : BaseUIView
    {
        [SerializeField, Tooltip("Khung giữa màn hình, phần được phóng to ra khi mở. Để trống thì bỏ qua hiệu ứng.")]
        protected Transform _root;

        [SerializeField, Tooltip("Nút đóng ở góc. Để trống nếu popup này bắt buộc phải chọn một lựa chọn.")]
        protected Button _closeButton;

        [SerializeField, Tooltip("Kiểu nảy của hiệu ứng mở.")]
        private Ease _ease = Ease.OutBack;

        [SerializeField, Min(0f), Tooltip("Hiệu ứng mở kéo dài bao lâu, tính bằng giây.")]
        private float _openDuration = 0.35f;

        protected virtual void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleClosePressed);
        }

        protected virtual void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleClosePressed);
        }

        private void HandleClosePressed()
        {
            Audio.GameAudioService.PlayUiClick();
            CloseSelf();
        }

        public override void OnOpen()
        {
            base.OnOpen();

            CanvasGroup.DOKill();
            CanvasGroup.alpha = 0f;
            CanvasGroup.DOFade(1f, _openDuration).SetEase(Ease.OutCirc).SetUpdate(true);

            if (_root == null)
                return;

            _root.DOKill();
            _root.localScale = Vector3.one * 0.7f;
            _root.DOScale(1f, _openDuration).SetEase(_ease).SetUpdate(true);
        }

        public override void OnClose()
        {
            base.OnClose();

            // Dừng tween đang chạy dở. Không dừng thì tween tiếp tục ghi vào một object đã bị tắt,
            // và lần mở sau nó ghi đè lên hiệu ứng mở mới.
            if (_root != null)
                _root.DOKill();

            CanvasGroup.DOKill();

            // Trả kích thước về 1. Bỏ dòng này thì popup nào bị đóng giữa lúc đang bung ra sẽ
            // nằm lại ở tỉ lệ dở dang, và lần mở sau nó nhảy từ đúng chỗ đó.
            if (_root != null)
                _root.localScale = Vector3.one;
        }

        /// <summary>
        /// Phím Back trên Android / phím Esc trên máy tính đóng popup đang ở trên cùng.
        ///
        /// Hàm này KHÔNG tự đọc bàn phím. <c>GameManager</c> là nơi duy nhất đọc phím rồi gọi
        /// xuống màn hình trên cùng — nhờ vậy hai popup cùng mở không thể cùng phản ứng với
        /// một lần bấm, và popup nào muốn chặn phím này chỉ cần override thành rỗng.
        /// </summary>
        public override void HandleOnKeyBack() => CloseSelf();
    }
}
