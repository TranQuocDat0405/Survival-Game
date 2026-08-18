using System.Collections;
using NFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Tấm màn che phủ toàn màn hình lúc chuyển cảnh.
    ///
    /// Nó không tự biết khi nào nên hiện hay tắt — <c>GameManager</c> ra lệnh.
    /// Lớp này chỉ lo phần nhìn: mờ dần vào, quay cái vòng chờ, mờ dần ra.
    ///
    /// NẰM Ở LAYER <c>AlwaysOnTop</c>, KHÔNG PHẢI <c>Popup</c> — và đó là chủ ý.
    /// Trong lúc chuyển cảnh, <c>GameManager</c> gọi <c>CloseAllInLayer(Popup)</c> để dọn sạch
    /// bảng tạm dừng và bảng kết thúc còn đang mở. Nếu tấm màn cũng nằm ở layer Popup thì chính
    /// nó bị đóng mất giữa chừng, để lộ nguyên cảnh đang đổi scene.
    ///
    /// TẤT CẢ ĐỒNG HỒ Ở ĐÂY DÙNG THỜI GIAN KHÔNG BỊ CO GIÃN (unscaled).
    /// Lý do: người chơi có thể bấm "Về Home" từ bảng tạm dừng, mà lúc tạm dừng thì
    /// <c>Time.timeScale</c> đang bằng 0. Nếu dùng <c>Time.deltaTime</c> thường thì màn hình
    /// đứng im vĩnh viễn và game treo cứng ngay tại đó.
    /// </summary>
    public class LoadingPopup : BaseUIView
    {
        // KHÔNG khai báo lại một field CanvasGroup ở đây. BaseUIView đã có sẵn một field tên
        // _canvasGroup, và Unity KHÔNG cho phép lớp con lẫn lớp cha cùng serialize một tên field —
        // nó báo "The same field name is serialized multiple times" và bỏ qua cả component.
        // Dùng property CanvasGroup kế thừa: nó tự tìm component trên chính object này, tức đúng
        // cái mà ô kéo thả cũ đang trỏ tới, nên không mất gì cả.

        [SerializeField, Tooltip("Vòng tròn quay tròn cho biết game đang chạy chứ không phải bị treo.")]
        private RectTransform _spinner;

        [SerializeField, Tooltip("Dòng chữ báo đang tải.")]
        private TextMeshProUGUI _label;

        [SerializeField, Min(0f), Tooltip("Thời gian mờ dần vào và mờ dần ra, tính bằng giây.")]
        private float _fadeDuration = 0.25f;

        [SerializeField, Tooltip("Tốc độ quay của vòng chờ, độ mỗi giây.")]
        private float _spinnerSpeed = 220f;

        private void Awake() => SetVisible(false);

        /// <summary>
        /// Mỗi lần UIManager mở lại tấm màn thì đưa nó về đúng vạch xuất phát.
        ///
        /// BẮT BUỘC vì UIManager KHÔNG huỷ view khi đóng — nó tắt đi rồi cất vào bộ nhớ đệm.
        /// Nghĩa là <c>Awake</c> chỉ chạy đúng một lần trong cả vòng đời ứng dụng. Lần chuyển
        /// cảnh thứ hai trở đi, nếu không đặt lại alpha ở đây thì tấm màn mở ra đã ở độ mờ còn
        /// sót từ lần trước — thường là 0, tức là nó "hiện" mà nhìn không thấy gì, và người chơi
        /// thấy nguyên cảnh scene đang nạp dở.
        /// </summary>
        public override void OnOpen()
        {
            base.OnOpen();

            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;
        }

        private void Update()
        {
            // Quay vòng chờ. Chỉ quay khi tấm màn đang hiện, để lúc ẩn không tốn công vô ích.
            if (_spinner != null && CanvasGroup.alpha > 0.01f)
                _spinner.Rotate(0f, 0f, -_spinnerSpeed * Time.unscaledDeltaTime);
        }

        public void SetLabel(string text)
        {
            if (_label != null)
                _label.text = text;
        }

        /// <summary>Hiện tấm màn, mờ dần cho tới khi che kín.</summary>
        public IEnumerator FadeIn()
        {
            gameObject.SetActive(true);

            // Chặn thao tác NGAY LẬP TỨC, không đợi mờ xong. Nếu không, trong một phần tư giây
            // đầu người chơi vẫn bấm được xuyên qua tấm màn đang trong suốt — bấm trúng nút
            // "Chơi lại" một lần nữa là chạy hai lần chuyển scene chồng lên nhau.
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;

            yield return Fade(CanvasGroup.alpha, 1f);
        }

        /// <summary>Mờ dần ra rồi tắt hẳn.</summary>
        public IEnumerator FadeOut()
        {
            yield return Fade(CanvasGroup.alpha, 0f);

            SetVisible(false);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_fadeDuration <= 0f)
            {
                CanvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                CanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                yield return null;
            }

            CanvasGroup.alpha = to;
        }

        private void SetVisible(bool visible)
        {
            CanvasGroup.alpha = visible ? 1f : 0f;
            CanvasGroup.blocksRaycasts = visible;
            CanvasGroup.interactable = visible;

            gameObject.SetActive(visible);
        }
    }
}
