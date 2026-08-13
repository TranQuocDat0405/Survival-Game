using System.Collections.Generic;
using Survival.Player;
using UnityEngine;

namespace Survival.UI
{
    /// <summary>
    /// Cụm nút kỹ năng. Tự sinh ra đúng số nút bằng số skill mà player đang có.
    ///
    /// ĐÂY LÀ ĐIỂM ĂN TIÊU CHÍ "DỄ MỞ RỘNG" Ở PHÍA GIAO DIỆN.
    /// Không có nút nào được đặt sẵn bằng tay trong scene, không có dòng nào kiểu
    /// "nút số 2 là bom". Thêm skill thứ tư = kéo thêm một file .asset vào PlayerConfig,
    /// nút thứ tư tự hiện ra đúng vị trí trên cung, tự nối đúng, tự hiển thị cooldown.
    ///
    /// BỐ CỤC: skill đầu tiên (đánh thường) là một nút LỚN đặt sát góc, các skill bổ trợ
    /// là những nút NHỎ HƠN xếp thành cung tròn quanh nó — giống bố cục trong video tham khảo.
    /// Lý do không phải chỉ để giống: đánh thường là nút bấm nhiều nhất trong cả trận,
    /// nên nó phải to nhất và nằm đúng chỗ ngón cái nghỉ tự nhiên. Các skill bổ trợ dùng
    /// thưa hơn nên nhỏ hơn và nằm xa hơn một chút, đổi lại là khó bấm nhầm.
    /// </summary>
    public class SkillBarView : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab một nút skill. Được nhân bản cho mọi skill, khác nhau ở kích thước lúc dựng.")]
        private SkillButtonView _buttonPrefab;

        [SerializeField, Tooltip("Nút được tạo dưới nút này.")]
        private RectTransform _container;

        [SerializeField, Tooltip("Để trống thì tự tìm player trong scene lúc khởi động.")]
        private PlayerActor _player;

        [Header("Nút chính (đánh thường)")]
        [SerializeField] private float _primarySize = 230f;
        [SerializeField] private Vector2 _primaryPosition = new Vector2(-170f, 165f);
        [SerializeField] private Color _primaryColor = new Color(0.62f, 0.48f, 0.13f, 0.92f);
        [SerializeField, Range(0.3f, 0.9f)] private float _primaryIconRatio = 0.52f;

        [Header("Nút bổ trợ (xếp thành cung quanh nút chính)")]
        [SerializeField] private float _secondarySize = 130f;
        [SerializeField] private Color _secondaryColor = new Color(0.13f, 0.15f, 0.22f, 0.90f);
        [SerializeField, Range(0.3f, 0.9f)] private float _secondaryIconRatio = 0.56f;

        [SerializeField, Tooltip("Khoảng cách từ tâm nút chính tới tâm các nút bổ trợ.")]
        private float _arcRadius = 235f;

        [SerializeField, Tooltip("Góc của nút bổ trợ đầu tiên, tính bằng độ. 90 là thẳng lên, 180 là sang trái.")]
        private float _arcStartAngle = 96f;

        [SerializeField, Tooltip("Góc cách nhau giữa hai nút bổ trợ liền kề, tính bằng độ.")]
        private float _arcStepAngle = 42f;

        private readonly List<SkillButtonView> _buttons = new List<SkillButtonView>();

        private void Start()
        {
            if (_player == null)
                _player = PlayerActor.Current;

            if (_player == null)
            {
                Debug.LogError("[SkillBarView] không tìm thấy PlayerActor, không dựng được nút skill.", this);
                return;
            }

            Build();
        }

        private void Build()
        {
            var skills = _player.Skills;

            for (int i = 0; i < skills.Count; i++)
            {
                var button = Instantiate(_buttonPrefab, _container);
                button.name = $"SkillButton_{i}_{skills[i].Def.DisplayName}";

                var rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                if (i == 0)
                {
                    rect.anchoredPosition = _primaryPosition;
                    button.ApplyStyle(_primarySize, _primaryColor, _primaryIconRatio);
                }
                else
                {
                    float angle = (_arcStartAngle + (i - 1) * _arcStepAngle) * Mathf.Deg2Rad;
                    var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _arcRadius;
                    rect.anchoredPosition = _primaryPosition + offset;
                    button.ApplyStyle(_secondarySize, _secondaryColor, _secondaryIconRatio);
                }

                // Biến cục bộ là bắt buộc ở đây. Nếu dùng thẳng biến đếm 'i' trong hàm ẩn danh,
                // mọi nút sẽ cùng nhớ một biến 'i' và sau vòng lặp tất cả đều gọi skill cuối cùng.
                int index = i;
                button.Bind(skills[i], () => _player.TryUseSkill(index));

                _buttons.Add(button);
            }
        }
    }
}
