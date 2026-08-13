using System;
using NFramework;
using Survival.Config;
using Survival.Enemies;
using UnityEngine;

namespace Survival.Progression
{
    /// <summary>
    /// Cộng kinh nghiệm khi giết quái và xử lý lên cấp. Spec mục 5.
    ///
    /// Lớp này không có <c>Update</c>. Nó chỉ ngồi nghe sự kiện "một con quái vừa chết"
    /// từ <see cref="EnemyRegistry"/>. Không có gì phải kiểm tra mỗi khung hình.
    /// </summary>
    public class ExperienceSystem : SingletonMono<ExperienceSystem>
    {
        [SerializeField] private ProgressionConfigSO _config;

        /// <summary>Cấp hiện tại, bắt đầu từ 1. UI nghe sự kiện đổi giá trị của nó.</summary>
        public readonly ObservableValue<int> Level = new ObservableValue<int>(1);

        /// <summary>EXP đang tích trong cấp hiện tại (đã trừ phần đã dùng để lên cấp).</summary>
        public readonly ObservableValue<int> CurrentExp = new ObservableValue<int>(0);

        /// <summary>Bắn ra mỗi lần lên một cấp. Dùng cho hiệu ứng và âm thanh.</summary>
        public event Action<int> OnLeveledUp;

        public int ExpPerLevel => _config != null ? _config.ExpPerLevel : 100;

        private void Start()
        {
            if (EnemyRegistry.I != null)
                EnemyRegistry.I.OnEnemyDied += HandleEnemyDied;
        }

        private void OnDestroy()
        {
            if (EnemyRegistry.I != null)
                EnemyRegistry.I.OnEnemyDied -= HandleEnemyDied;
        }

        private void HandleEnemyDied(EnemyActor enemy)
        {
            if (enemy == null || enemy.Config == null)
                return;

            // Lượng EXP nằm trong config của TỪNG loại quái, không viết cứng số 30 ở đây.
            // Nhờ vậy sau này cho quái to thưởng nhiều EXP hơn là chuyện sửa file config.
            AddExp(enemy.Config.ExpReward);
        }

        public void AddExp(int amount)
        {
            if (amount <= 0)
                return;

            CurrentExp.Value += amount;

            int required = ExpPerLevel;

            // Dùng while chứ không phải if. Hai lý do:
            //   1. Giết một con quái thưởng nhiều EXP có thể lên liền hai cấp.
            //   2. Phép trừ (thay vì gán về 0) chính là cách giữ lại EXP DƯ cho cấp sau,
            //      đúng như spec yêu cầu.
            while (CurrentExp.Value >= required)
            {
                CurrentExp.Value -= required;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level.Value++;

            var player = Player.PlayerActor.Current;
            if (player == null || _config == null)
                return;

            // THỨ TỰ Ở ĐÂY LÀ BẮT BUỘC.
            // Nâng máu tối đa TRƯỚC, rồi mới cộng máu hiện tại.
            // Làm ngược lại thì phần máu cộng vào sẽ bị cắt cụt ở trần cũ:
            // player đầy 500/500 mà cộng 40 máu trước sẽ vẫn là 500, mất trắng 40 điểm.
            player.Stats.ApplyAll(_config.PerLevelGains);
            player.Health.GrantCurrentHealth(_config.CurrentHealthGain);

            OnLeveledUp?.Invoke(Level.Value);
        }

        /// <summary>Đưa về cấp 1 và 0 EXP. Dùng cho nút Chơi lại.</summary>
        public void ResetProgress()
        {
            Level.ForceSet(1);
            CurrentExp.ForceSet(0);
        }
    }
}
