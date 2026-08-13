using System;
using NFramework;
using Survival.Enemies;
using Survival.Pooling;
using Survival.Progression;
using Survival.Waves;
using UnityEngine;

namespace Survival.Core
{
    public enum EGameState
    {
        Playing = 0,
        GameOver = 1,
    }

    /// <summary>
    /// Điều phối vòng đời một ván chơi: đang chơi, thua, chơi lại.
    ///
    /// VÌ SAO CẦN LỚP NÀY:
    /// Trước khi có nó, player chết là mọi thứ đứng yên — không đi được, không bắn được,
    /// quái đứng im vì mất mục tiêu — và người chơi tưởng game bị treo.
    /// Thực ra game vẫn chạy bình thường, chỉ là KHÔNG AI BÁO rằng ván đã kết thúc.
    ///
    /// Lớp này cũng là chỗ duy nhất biết cách dọn dẹp để chơi lại: thu hồi quái về pool,
    /// đưa chỉ số player về ban đầu, đặt lại cấp, và khởi động lại chuỗi wave.
    /// Gom vào một nơi để không có chỗ nào bị quên khi bấm Chơi lại.
    /// </summary>
    public class GameSession : SingletonMono<GameSession>
    {
        [SerializeField, Tooltip("Vị trí player được đặt về khi bắt đầu ván mới.")]
        private Transform _playerSpawnPoint;

        /// <summary>Bắn ra khi ván kết thúc vì player chết. Màn hình thua nghe sự kiện này.</summary>
        public event Action OnGameOver;

        /// <summary>Bắn ra khi một ván mới bắt đầu.</summary>
        public event Action OnRestarted;

        public EGameState State { get; private set; } = EGameState.Playing;

        private Player.PlayerActor _player;

        private void Start()
        {
            BindPlayer();
        }

        private void OnDestroy()
        {
            if (_player != null && _player.Health != null)
                _player.Health.OnDied -= HandlePlayerDied;
        }

        private void BindPlayer()
        {
            _player = Player.PlayerActor.Current;

            if (_player == null)
            {
                Debug.LogError("[GameSession] không tìm thấy PlayerActor.", this);
                return;
            }

            _player.Health.OnDied += HandlePlayerDied;
        }

        private void HandlePlayerDied(Combat.Health health)
        {
            if (State == EGameState.GameOver)
                return;

            State = EGameState.GameOver;

            // Dừng sinh quái. Nếu không, wave mới vẫn tiếp tục đổ ra sau lưng màn hình thua.
            WaveManager.I?.StopRun();

            OnGameOver?.Invoke();
        }

        /// <summary>
        /// Bắt đầu lại từ đầu. Nút Chơi lại trên màn hình thua gọi hàm này.
        ///
        /// Cố tình KHÔNG nạp lại scene: nạp lại scene sẽ tạo lại toàn bộ pool, toàn bộ UI,
        /// và tốn một nhịp khựng hình. Đặt lại trạng thái trực tiếp thì tức thì,
        /// và nó cũng buộc mình phải viết đúng phần dọn dẹp — thứ mà dù sao cũng cần
        /// cho việc tái sử dụng object.
        /// </summary>
        public void Restart()
        {
            // Thu hồi mọi quái đang sống về pool. Phải làm TRƯỚC khi khởi động lại wave,
            // nếu không quái của ván cũ sẽ còn nguyên trên sân.
            EnemyRegistry.I?.KillAll();
            PoolService.I?.DespawnAll();

            ExperienceSystem.I?.ResetProgress();

            Vector3 spawn = _playerSpawnPoint != null ? _playerSpawnPoint.position : Vector3.zero;
            _player?.ResetToStart(spawn);

            WaveManager.I?.BeginRun();

            State = EGameState.Playing;
            OnRestarted?.Invoke();
        }
    }
}
