using System.Collections.Generic;
using Survival.Skills;
using Survival.Stats;
using UnityEngine;

namespace Survival.Config
{
    /// <summary>
    /// Toàn bộ số liệu của player nằm ở đây, trong một file .asset duy nhất.
    /// Không có con số nào của player bị viết cứng rải rác trong code.
    ///
    /// Giá trị theo spec (Docs/README.md mục 2.1):
    ///   Máu 500 · Tốc độ di chuyển 2 unit/giây · Tốc độ xoay 180 độ/giây · Giáp 0 · DamageMultiplier 0
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Config/Player Config", fileName = "PlayerConfig")]
    public class PlayerConfigSO : ScriptableObject
    {
        [Header("Chỉ số khởi đầu")]
        [SerializeField]
        private List<StatModifier> _baseStats = new List<StatModifier>
        {
            new StatModifier(EStatType.MaxHealth,        500f),
            new StatModifier(EStatType.MoveSpeed,          2f),
            new StatModifier(EStatType.RotationSpeed,    180f),
            new StatModifier(EStatType.Armor,              0f),
            new StatModifier(EStatType.DamageMultiplier,   0f),
        };

        [Header("Cách di chuyển")]
        [SerializeField, Tooltip(
            "BẬT  = kiểu xe tăng: nhân vật chỉ đi theo hướng đang quay mặt, phải xoay xong mới đi đúng hướng.\n" +
            "TẮT  = nhân vật đi ngay theo hướng joystick, thân xoay dần theo sau (mặc định, giống video tham khảo).\n" +
            "Dù chọn kiểu nào thì đạn / bom / dash vẫn luôn dùng hướng forward hiện tại theo đúng spec.")]
        private bool _moveAlongForwardOnly = false;

        [SerializeField, Range(0f, 1f), Tooltip("Dưới ngưỡng này coi như joystick đang ở giữa (chống rung tay).")]
        private float _inputDeadZone = 0.1f;

        [Header("Kỹ năng")]
        [SerializeField, Tooltip(
            "Danh sách skill của player, theo đúng thứ tự nút sẽ hiện trên UI.\n" +
            "Thêm skill mới = kéo thêm một file .asset vào đây, nút trên UI tự sinh ra.")]
        private List<SkillDefinition> _skills = new List<SkillDefinition>();

        [Header("Vật lý")]
        [SerializeField, Tooltip("Những layer mà đạn và vùng nổ của player được phép trúng.")]
        private LayerMask _enemyMask;

        public IReadOnlyList<StatModifier> BaseStats => _baseStats;
        public IReadOnlyList<SkillDefinition> Skills => _skills;
        public bool MoveAlongForwardOnly => _moveAlongForwardOnly;
        public float InputDeadZone => _inputDeadZone;
        public LayerMask EnemyMask => _enemyMask;
    }
}
