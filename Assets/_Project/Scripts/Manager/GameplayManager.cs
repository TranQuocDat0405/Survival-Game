using System;
using NFramework;
using Survival.Enemies;
using Survival.Pooling;
using Survival.Progression;
using Survival.Waves;
using UnityEngine;

namespace Survival.Manager
{
    /// <summary>
    /// Trạng thái của MỘT VÁN CHƠI — không phải trạng thái của ứng dụng.
    ///
    /// ⚠️ HẬU TỐ "Gameplay" LÀ CỐ Ý, ĐỪNG RÚT GỌN THÀNH "EGameState".
    /// Ngay trong namespace này còn một enum nữa mang đúng cái tên ngắn đó, mô tả trạng thái
    /// của cả ỨNG DỤNG (đang tải / màn hình chính / trong trận). Để hai enum trùng tên là loại
    /// lỗi tệ nhất có thể tự tạo ra: file nào chỉ <c>using</c> một trong hai sẽ BIÊN DỊCH SẠCH
    /// mà so sánh nhầm enum, không một dòng cảnh báo nào.
    /// </summary>
    public enum EGameplayState
    {
        Playing = 0,
        GameOver = 1,

        /// <summary>Clear xong wave cuối. Ván chơi kết thúc THẮNG.</summary>
        Victory = 2,
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
    public class GameplayManager : SingletonMono<GameplayManager>
    {
        [SerializeField, Tooltip("Vị trí player được đặt về khi bắt đầu ván mới.")]
        private Transform _playerSpawnPoint;

        /// <summary>Bắn ra khi ván kết thúc vì player chết. Màn hình thua nghe sự kiện này.</summary>
        public event Action OnGameOver;

        /// <summary>Bắn ra khi một ván mới bắt đầu.</summary>
        public event Action OnRestarted;

        public EGameplayState State { get; private set; } = EGameplayState.Playing;

        private Player.PlayerActor _player;

        private void Start()
        {
            BindPlayer();

            _runStartTime = Time.time;

            if (WaveManager.I != null)
                WaveManager.I.OnAllWavesCleared += HandleAllWavesCleared;

            if (EnemyRegistry.I != null)
                EnemyRegistry.I.OnEnemyDied += HandleEnemyDied;
        }

        // override chứ không phải khai báo mới: SingletonMono.OnDestroy có nhiệm vụ xoá tham chiếu
        // static trỏ tới thể hiện này. Nếu khai báo đè lên, Unity chỉ gọi bản của lớp con và
        // tham chiếu static sẽ tiếp tục trỏ vào một đối tượng đã bị huỷ.
        protected override void OnDestroy()
        {
            if (_player != null && _player.Health != null)
                _player.Health.OnDied -= HandlePlayerDied;

            if (WaveManager.I != null)
                WaveManager.I.OnAllWavesCleared -= HandleAllWavesCleared;

            if (EnemyRegistry.I != null)
                EnemyRegistry.I.OnEnemyDied -= HandleEnemyDied;

            base.OnDestroy();
        }

        private void BindPlayer()
        {
            _player = Player.PlayerActor.Current;

            if (_player == null)
            {
                Debug.LogError("[GameplayManager] không tìm thấy PlayerActor.", this);
                return;
            }

            _player.Health.OnDied += HandlePlayerDied;
        }

        private void HandlePlayerDied(Combat.Health health)
        {
            if (State != EGameplayState.Playing)
                return;

            State = EGameplayState.GameOver;

            // Chốt đồng hồ. Quên dòng này là bảng tổng kết lúc THUA hiện thời gian ÂM, vì
            // ElapsedSeconds lúc đó lấy _runEndTime (còn đang bằng 0) trừ đi _runStartTime.
            // Nhánh thắng có chốt nên hiện đúng, còn nhánh thua thì không — mà người chơi
            // thua nhiều hơn thắng rất nhiều, nên đây mới là màn hình hay bị nhìn thấy nhất.
            _runEndTime = Time.time;

            // Dừng sinh quái. Nếu không, wave mới vẫn tiếp tục đổ ra sau lưng màn hình thua.
            WaveManager.I?.StopRun();

            SubmitBestRecord();

            OnGameOver?.Invoke();
            ShowResultPopup(won: false);
        }

        /// <summary>
        /// Mở bảng kết thúc ván và nói cho nó biết kết cục.
        ///
        /// CHIỀU ĐIỀU KHIỂN ĐI TỪ ĐÂY RA, không phải bảng tự nghe sự kiện rồi tự bật lên.
        /// Bảng kết thúc giờ là một prefab được nạp theo yêu cầu, nên nó KHÔNG tồn tại vào lúc
        /// ván bắt đầu và không thể tự đăng ký nghe gì được. Đổi lại, luồng đọc dễ hơn hẳn:
        /// ai mở bảng đó ra thì nhìn thấy ngay tại chỗ ván kết thúc.
        /// </summary>
        private void ShowResultPopup(bool won)
        {
            if (UIManager.I == null)
            {
                Debug.LogWarning("[GameplayManager] Không có UIManager nên không hiện được bảng kết thúc. " +
                                 "Nhiều khả năng scene Game đang chạy một mình, không có scene Main.", this);
                return;
            }

            UIManager.I.Open<UI.ResultPopup>(Define.UIName.RESULT_POPUP, p => p.Show(won));
        }

        /// <summary>
        /// Báo kết quả ván vừa xong cho bảng thành tích ở màn hình chính.
        ///
        /// Gọi cho CẢ thua lẫn thắng: người chơi thua ở wave 4 vẫn là đi xa hơn người thắng
        /// chưa từng có, và thành tích đó xứng đáng được ghi lại.
        /// </summary>
        private void SubmitBestRecord()
        {
            int wave = WaveManager.I != null ? WaveManager.I.CurrentWave : 0;
            Data.UserData.I?.Submit(wave, Kills, ElapsedSeconds);
        }

        /// <summary>Bắn ra khi clear xong wave cuối. Giao diện nghe để hiện bảng chiến thắng.</summary>
        public event Action OnVictory;

        /// <summary>Số quái đã hạ trong ván này. Dùng cho bảng tổng kết.</summary>
        public int Kills { get; private set; }

        /// <summary>Ván chơi đã kéo dài bao lâu, tính bằng giây.</summary>
        public float ElapsedSeconds => State == EGameplayState.Playing ? Time.time - _runStartTime : _runEndTime - _runStartTime;

        private float _runStartTime;
        private float _runEndTime;

        private void HandleAllWavesCleared(int finalWave)
        {
            if (State != EGameplayState.Playing)
                return;

            State = EGameplayState.Victory;
            _runEndTime = Time.time;

            // Không cần dừng WaveManager: nó đã tự dừng ngay khi phát hiện wave cuối đã clear.
            // Nhưng vẫn thu quái về pool cho chắc, phòng trường hợp còn xác đang chờ biến mất.
            SubmitBestRecord();

            OnVictory?.Invoke();
            ShowResultPopup(won: true);
        }

        private void HandleEnemyDied(Enemies.EnemyActor enemy) => Kills++;

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

            // Đặt lại số liệu tổng kết. Quên bước này thì bảng kết thúc của ván sau sẽ cộng dồn
            // số quái đã giết và thời gian của cả những ván trước.
            Kills = 0;
            _runStartTime = Time.time;

            State = EGameplayState.Playing;
            OnRestarted?.Invoke();
        }
    }
}
