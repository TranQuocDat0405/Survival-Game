using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Tấm màn che phủ toàn màn hình lúc chuyển scene.
    ///
    /// Nó không tự biết khi nào nên hiện hay tắt — <see cref="Core.SceneFlow"/> ra lệnh.
    /// Lớp này chỉ lo phần nhìn: mờ dần vào, quay cái vòng chờ, mờ dần ra.
    ///
    /// TẤT CẢ ĐỒNG HỒ Ở ĐÂY DÙNG THỜI GIAN KHÔNG BỊ CO GIÃN (unscaled).
    /// Lý do: người chơi có thể bấm "Về Home" từ bảng tạm dừng, mà lúc tạm dừng thì
    /// <c>Time.timeScale</c> đang bằng 0. Nếu dùng <c>Time.deltaTime</c> thường thì màn hình
    /// đứng im vĩnh viễn và game treo cứng ngay tại đó.
    /// </summary>
    public class LoadingScreenView : MonoBehaviour
    {
        [SerializeField, Tooltip("Nhóm điều khiển độ mờ của cả tấm màn.")]
        private CanvasGroup _canvasGroup;

        [SerializeField, Tooltip("Vòng tròn quay tròn cho biết game đang chạy chứ không phải bị treo.")]
        private RectTransform _spinner;

        [SerializeField, Tooltip("Dòng chữ báo đang tải.")]
        private TextMeshProUGUI _label;

        [SerializeField, Min(0f), Tooltip("Thời gian mờ dần vào và mờ dần ra, tính bằng giây.")]
        private float _fadeDuration = 0.25f;

        [SerializeField, Tooltip("Tốc độ quay của vòng chờ, độ mỗi giây.")]
        private float _spinnerSpeed = 220f;

        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            SetVisible(false);
        }

        private void Update()
        {
            // Quay vòng chờ. Chỉ quay khi tấm màn đang hiện, để lúc ẩn không tốn công vô ích.
            if (_spinner != null && _canvasGroup != null && _canvasGroup.alpha > 0.01f)
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
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            yield return Fade(_canvasGroup.alpha, 1f);
        }

        /// <summary>Mờ dần ra rồi tắt hẳn.</summary>
        public IEnumerator FadeOut()
        {
            yield return Fade(_canvasGroup.alpha, 0f);

            SetVisible(false);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_fadeDuration <= 0f)
            {
                _canvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = to;
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
            }

            gameObject.SetActive(visible);
        }
    }
}
