using System.Collections.Generic;
using Survival.Stats;
using UnityEngine;

namespace Survival.Config
{
    /// <summary>
    /// Luật kinh nghiệm và lên cấp. Spec mục 5:
    ///
    ///   "Giết 1 quái: +30 EXP. Đủ 100 EXP thì lên 1 cấp. EXP dư được giữ cho cấp sau."
    ///   "Mỗi lần lên cấp: +40 máu hiện tại và +40 máu tối đa, +2 giáp, +0.1 Damage Multiplier."
    ///
    /// Phần thưởng mỗi cấp được viết thành DANH SÁCH chỉ số, không viết cứng ba dòng cộng.
    /// Muốn cấp sau cộng thêm tốc độ di chuyển thì thêm một dòng ở đây — không đụng code.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Config/Progression Config", fileName = "ProgressionConfig")]
    public class ProgressionConfigSO : ScriptableObject
    {
        [Header("Kinh nghiệm")]
        [SerializeField, Min(1), Tooltip("EXP cần để lên một cấp. Spec: 100.")]
        private int _expPerLevel = 100;

        [Header("Phần thưởng mỗi lần lên cấp")]
        [SerializeField, Tooltip("Các chỉ số được cộng thêm. Spec: +40 máu tối đa, +2 giáp, +0.1 Damage Multiplier.")]
        private List<StatModifier> _perLevelGains = new List<StatModifier>
        {
            new StatModifier(EStatType.MaxHealth,        40f),
            new StatModifier(EStatType.Armor,             2f),
            new StatModifier(EStatType.DamageMultiplier,  0.1f),
        };

        [SerializeField, Min(0f), Tooltip(
            "Máu HIỆN TẠI được cộng thêm mỗi lần lên cấp. Spec: 40.\n\n" +
            "Đây là khoản TÁCH RIÊNG khỏi '+40 máu tối đa' ở trên. Máu tối đa là một chỉ số, " +
            "còn máu hiện tại thì không — nên phải nâng trần trước rồi mới đổ máu vào, " +
            "nếu làm ngược lại thì phần máu vượt trần cũ sẽ bị cắt mất.")]
        private float _currentHealthGain = 40f;

        public int ExpPerLevel => _expPerLevel;
        public IReadOnlyList<StatModifier> PerLevelGains => _perLevelGains;
        public float CurrentHealthGain => _currentHealthGain;
    }
}
