using UnityEngine;

namespace Survival.Skills
{
    /// <summary>
    /// Bản mô tả một skill dưới dạng ScriptableObject — tức là một FILE tài sản trong project
    /// (đuôi .asset) mà người chỉnh game mở lên trên Inspector và sửa số trực tiếp,
    /// không cần mở Visual Studio, không cần build lại.
    ///
    /// Đây là lớp cha trừu tượng. Ba skill của đề bài là ba lớp con:
    /// ChargedShootSkillSO (bắn 3 viên), BombSkillSO (đặt bom), DashSkillSO (lướt rồi nổ).
    ///
    /// Muốn thêm skill thứ tư: viết một lớp con mới + tạo file .asset, kéo vào danh sách skill
    /// của player. UI tự sinh thêm nút. Không phải sửa một dòng nào trong code cũ.
    /// </summary>
    public abstract class SkillDefinition : ScriptableObject
    {
        [Header("Hiển thị")]
        [SerializeField, Tooltip("Tên hiện trên UI")]
        private string _displayName = "Skill";

        [SerializeField, Tooltip("Icon hiện trên nút skill")]
        private Sprite _icon;

        [Header("Thời gian")]
        [SerializeField, Min(0f), Tooltip("Thời gian hồi chiêu, tính bằng giây")]
        private float _cooldown = 1f;

        [Header("Animation")]
        [SerializeField, Tooltip("Tên trigger trong Animator sẽ được bật khi dùng skill. Để trống nếu skill không có animation riêng.")]
        private string _animationTrigger = "";

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public float Cooldown => _cooldown;
        public string AnimationTrigger => _animationTrigger;

        /// <summary>
        /// Tạo ra bản trạng thái riêng cho một nhân vật cụ thể.
        /// Đây là điểm mấu chốt khiến hệ thống mở rộng được: người gọi chỉ biết tới
        /// <see cref="SkillDefinition"/> và <see cref="SkillRuntime"/>, không cần biết
        /// đây là skill bắn hay skill bom.
        /// </summary>
        public abstract SkillRuntime CreateRuntime(SkillContext context);
    }
}
