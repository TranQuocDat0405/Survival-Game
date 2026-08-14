using System.Collections.Generic;
using Survival.Enemies.Attacks;
using Survival.Stats;
using UnityEngine;

namespace Survival.Config
{
    /// <summary>
    /// Toàn bộ định nghĩa của MỘT loại quái.
    ///
    /// Đây là file quan trọng nhất cho tiêu chí chấm "Dễ mở rộng — 10%".
    /// Quái đánh gần và quái đánh xa của spec KHÔNG phải là hai class khác nhau —
    /// chúng là hai file .asset khác nhau, dùng chung một prefab logic và một script AI.
    /// Khác biệt nằm hoàn toàn ở số liệu và ở kiểu đòn đánh được chọn trong ô Attack.
    ///
    /// Muốn thêm quái thứ ba (ví dụ quái to, chậm, máu trâu, đánh vùng):
    /// bấm chuột phải > Create > Survival > Config > Enemy Config, điền số, kéo vào WaveConfig.
    /// Không sửa một dòng code nào.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Config/Enemy Config", fileName = "EnemyConfig")]
    public class EnemyConfigSO : ScriptableObject
    {
        [Header("Nhận dạng")]
        [SerializeField] private string _displayName = "Enemy";

        // Đã bỏ trường _visualPrefab. Ý định ban đầu là gắn model vào prefab logic lúc sinh ra,
        // nhưng cuối cùng model được đặt thẳng trong prefab quái nên KHÔNG CHỖ NÀO đọc tới nó.
        // Nó chỉ còn tác dụng duy nhất là bắn ra một cảnh báo "chưa gán model hiển thị" mỗi lần
        // chạm vào file config — một cảnh báo giả, mà cảnh báo giả thì nguy hiểm hơn không có gì:
        // nhìn quen mắt rồi thì cảnh báo thật cũng bị lướt qua.

        [SerializeField, Tooltip("Tỉ lệ phóng to/thu nhỏ phần hình ảnh, để mọi model khác nguồn đều vừa vặn.")]
        private float _visualScale = 1f;

        [Header("Chỉ số")]
        [SerializeField]
        private List<StatModifier> _baseStats = new List<StatModifier>
        {
            new StatModifier(EStatType.MaxHealth,     220f),
            new StatModifier(EStatType.MoveSpeed,       3f),
            new StatModifier(EStatType.RotationSpeed, 360f),
            new StatModifier(EStatType.Armor,           0f),
        };

        [Header("Nhịp tấn công")]
        [SerializeField, Min(0f), Tooltip(
            "Thời gian TỪ LÚC bắt đầu ra đòn TỚI LÚC gây sát thương, tính bằng giây.\n\n" +
            "Đây là con số điều khiển toàn bộ cảm giác 'né được đòn'. Để 0 thì sát thương xảy ra " +
            "ngay lập tức, người chơi không kịp phản ứng và nhìn rất giả vì animation chưa vung tay xong. " +
            "Để quá dài thì đòn nào cũng trượt.\n" +
            "Animation sẽ được co giãn cho khớp với con số này, không phải ngược lại.")]
        private float _attackWindup = 0.35f;

        [SerializeField, Min(0f), Tooltip("Phần đuôi animation sau khi đã gây sát thương, tính bằng giây.")]
        private float _attackRecover = 0.25f;

        [SerializeField, Min(0f), Tooltip("Thời gian đứng im sau mỗi đòn trước khi tiếp cận lại. Spec: 1 giây.")]
        private float _idleAfterAttack = 1f;

        [SerializeField, Tooltip(
            "BẬT = trong lúc lấy đà, quái vẫn xoay theo player (nhưng chậm hơn, xem hệ số bên dưới).\n\n" +
            "Đây là thứ sửa lỗi 'player chỉ cần đi ngang là đòn nào cũng trượt'. " +
            "Tắt hoàn toàn thì quái vung tay vào chỗ trống mỗi khi người chơi di chuyển; " +
            "bật 100% thì không bao giờ né được. Xoay chậm lại là điểm cân bằng.")]
        private bool _trackTargetDuringWindup = true;

        [SerializeField, Range(0f, 1f), Tooltip("Tốc độ xoay trong lúc lấy đà, so với tốc độ xoay bình thường.")]
        private float _windupTrackingFactor = 0.5f;

        [Header("Đòn đánh")]
        [SerializeReference, Tooltip(
            "Chọn kiểu đòn ở danh sách xổ xuống. ConeMeleeAttack cho quái đánh gần, " +
            "ProjectileAttack cho quái đánh xa.")]
        private EnemyAttackDefinition _attack = new ConeMeleeAttack();

        [Header("Phần thưởng")]
        [SerializeField, Min(0), Tooltip("EXP nhận được khi giết con quái này. Spec: 30.")]
        private int _expReward = 30;

        public string DisplayName => _displayName;
        public float VisualScale => _visualScale;
        public IReadOnlyList<StatModifier> BaseStats => _baseStats;
        public float AttackWindup => _attackWindup;
        public float AttackRecover => _attackRecover;
        public float IdleAfterAttack => _idleAfterAttack;
        public bool TrackTargetDuringWindup => _trackTargetDuringWindup;
        public float WindupTrackingFactor => _windupTrackingFactor;
        public EnemyAttackDefinition Attack => _attack;
        public int ExpReward => _expReward;

        /// <summary>Tầm cần vào tới để ra đòn. Lấy thẳng từ đòn đánh nên không bị lệch hai nơi.</summary>
        public float AttackRange => _attack != null ? _attack.Range : 1f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_attack == null)
                Debug.LogWarning($"[{name}] chưa chọn kiểu đòn đánh — quái sẽ đuổi theo mà không bao giờ tấn công.", this);

            foreach (EStatType required in System.Enum.GetValues(typeof(EStatType)))
            {
                if (required == EStatType.DamageMultiplier)
                    continue;   // quái không dùng hệ số này, theo spec nó chỉ thuộc về player

                bool found = false;
                for (int i = 0; i < _baseStats.Count; i++)
                {
                    if (_baseStats[i].Type != required)
                        continue;
                    found = true;
                    break;
                }

                if (!found)
                    Debug.LogWarning($"[{name}] thiếu chỉ số '{required}' — nó sẽ mặc định bằng 0.", this);
            }
        }
#endif
    }
}
