using UnityEngine;

namespace Survival.UI
{
    /// <summary>
    /// Kéo một mảng UI trải kín TOÀN BỘ màn hình, kể cả khi cha của nó đã bị co vào vùng an toàn.
    ///
    /// VÌ SAO CẦN:
    /// Lớp phủ tối phía sau chữ nằm chung cây với các nút bấm, mà cụm đó lại được bọc trong
    /// <see cref="SafeAreaFitter"/> để tránh tai thỏ. Hệ quả trên điện thoại có khuyết màn hình:
    /// lớp phủ co theo vùng an toàn và chỉ che được phần giữa, để lộ một viền sáng nguyên quanh
    /// mép — nhìn ra thành một ô tối lơ lửng giữa màn hình, y như lỗi hiển thị.
    ///
    /// Tài liệu của chính SafeArea cũng dặn đúng điều này: ảnh nền trải toàn màn hình phải nằm
    /// NGOÀI vùng an toàn, chỉ chữ và nút bấm mới đặt bên trong.
    ///
    /// VÌ SAO KHÔNG ĐƠN GIẢN CHUYỂN NÓ RA NGOÀI CÂY:
    /// Vì nó phải bật/tắt cùng lúc với bảng chứa nó. Tách ra ngoài thì mọi nơi mở bảng đều phải
    /// nhớ bật thêm một object nữa — kiểu ràng buộc rất dễ quên khi thêm bảng mới. Giữ nguyên
    /// chỗ trong cây rồi tự nới rộng ra thì không ai phải nhớ gì cả.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class FullScreenOverlay : MonoBehaviour
    {
        private static readonly Vector3[] CanvasCorners = new Vector3[4];

        private RectTransform _rectTransform;
        private RectTransform _canvasRect;

        private void Awake() => _rectTransform = GetComponent<RectTransform>();

        private void OnEnable() => Apply();

        /// <summary>
        /// Tính lại mỗi khung hình. Người chơi có thể xoay máy hoặc bật chia đôi màn hình giữa
        /// chừng, và vùng an toàn đổi theo. Phép tính chỉ là vài phép trừ nên không đáng kể.
        /// </summary>
        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            var parent = _rectTransform.parent as RectTransform;
            if (parent == null)
                return;

            if (_canvasRect == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                    return;

                _canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
                if (_canvasRect == null)
                    return;
            }

            // Lấy bốn góc của cả canvas rồi quy về hệ toạ độ của cha.
            // Làm qua toạ độ thế giới để không phải quan tâm cha bị co bao nhiêu, hay có bao
            // nhiêu tầng cha ở giữa.
            _canvasRect.GetWorldCorners(CanvasCorners);
            Vector2 min = parent.InverseTransformPoint(CanvasCorners[0]);
            Vector2 max = parent.InverseTransformPoint(CanvasCorners[2]);

            Rect parentRect = parent.rect;

            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.offsetMin = min - parentRect.min;
            _rectTransform.offsetMax = max - parentRect.max;
        }
    }
}
