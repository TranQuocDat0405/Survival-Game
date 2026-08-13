using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Vòng tròn chia đoạn quanh nút bắn, mỗi đoạn là một charge.
    ///
    /// VÌ SAO KHÔNG CHỈ HIỆN MỘT CON SỐ:
    /// Charge là tài nguyên người chơi phải quyết định trong lúc đang bị quái vây.
    /// Đọc một chữ số nhỏ đòi hỏi phải nhìn tập trung vào nút — đúng lúc không được rời mắt
    /// khỏi nhân vật. Ba vạch sáng thì nhận ra được bằng thị giác ngoại biên,
    /// liếc một cái là biết còn mấy phát.
    ///
    /// Đoạn đang hồi được vẽ mờ dần theo tiến độ, nên người chơi còn biết
    /// sắp có thêm charge hay còn lâu.
    ///
    /// Các đoạn được sinh bằng code lúc khởi động thay vì đặt tay trong scene,
    /// để đổi số charge tối đa trong file config là vòng tròn tự chia lại cho khớp.
    /// </summary>
    public class ChargeRingView : MonoBehaviour
    {
        [SerializeField, Tooltip("Ảnh hình vành khuyên dùng cho mỗi đoạn. Sinh bằng menu Survival > Generate UI Ring Sprites.")]
        private Sprite _ringSprite;

        [SerializeField, Tooltip("Màu đoạn đã đầy.")]
        private Color _filledColor = new Color(1f, 0.88f, 0.35f);

        [SerializeField, Tooltip("Màu đoạn đang trống.")]
        private Color _emptyColor = new Color(0.16f, 0.16f, 0.18f, 0.85f);

        [SerializeField, Tooltip("Màu đoạn đang hồi, chuyển dần sang màu đầy.")]
        private Color _rechargingColor = new Color(0.75f, 0.62f, 0.25f);

        [SerializeField, Range(0f, 0.2f), Tooltip("Khe hở giữa hai đoạn, tính theo phần của vòng tròn.")]
        private float _gap = 0.035f;

        private readonly List<Image> _segments = new List<Image>();
        private int _builtCount = -1;

        /// <summary>
        /// Dựng lại vòng cho đúng số charge tối đa. Gọi một lần khi nút được nối với skill.
        /// </summary>
        public void Build(int maxCharges)
        {
            if (maxCharges <= 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (_builtCount == maxCharges)
                return;

            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                if (_segments[i] != null)
                    Destroy(_segments[i].gameObject);
            }
            _segments.Clear();

            float slice = 1f / maxCharges;

            for (int i = 0; i < maxCharges; i++)
            {
                var go = new GameObject($"Segment_{i}", typeof(RectTransform));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                // Mỗi đoạn là một bản sao của cả vòng, nhưng chỉ tô một lát bằng 1/N,
                // rồi xoay cho về đúng vị trí của nó trên vòng tròn.
                rect.localRotation = Quaternion.Euler(0f, 0f, -i * slice * 360f);

                var image = go.AddComponent<Image>();
                image.sprite = _ringSprite;
                image.raycastTarget = false;
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Radial360;
                image.fillOrigin = (int)Image.Origin360.Top;
                image.fillClockwise = true;
                image.fillAmount = Mathf.Max(0f, slice - _gap);

                _segments.Add(image);
            }

            _builtCount = maxCharges;
        }

        /// <summary>
        /// Cập nhật màu các đoạn.
        /// </summary>
        /// <param name="charges">Số charge đang có.</param>
        /// <param name="rechargeProgress">Tiến độ hồi charge kế tiếp, từ 0 tới 1.</param>
        public void SetCharges(int charges, float rechargeProgress)
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                Color color;

                if (i < charges)
                    color = _filledColor;
                else if (i == charges)
                    // Đoạn kế tiếp sáng dần lên theo tiến độ hồi.
                    color = Color.Lerp(_emptyColor, _rechargingColor, Mathf.Clamp01(rechargeProgress));
                else
                    color = _emptyColor;

                if (_segments[i].color != color)
                    _segments[i].color = color;
            }
        }
    }
}
