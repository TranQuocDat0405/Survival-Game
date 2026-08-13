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
    /// nút thứ tư tự hiện ra, tự nối đúng, tự hiển thị cooldown. Không sửa scene, không sửa code.
    /// </summary>
    public class SkillBarView : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab một nút skill. Được nhân bản cho mỗi skill player có.")]
        private SkillButtonView _buttonPrefab;

        [SerializeField, Tooltip("Nút được tạo dưới nút này. Nên gắn một Layout Group để tự sắp xếp.")]
        private RectTransform _container;

        [SerializeField, Tooltip("Để trống thì tự tìm player trong scene lúc khởi động.")]
        private PlayerActor _player;

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

                // Biến cục bộ là bắt buộc ở đây. Nếu dùng thẳng biến đếm 'i' trong hàm ẩn danh,
                // mọi nút sẽ cùng nhớ một biến 'i' và sau vòng lặp tất cả đều gọi skill cuối cùng.
                int index = i;
                button.Bind(skills[i], () => _player.TryUseSkill(index));

                _buttons.Add(button);
            }
        }
    }
}
