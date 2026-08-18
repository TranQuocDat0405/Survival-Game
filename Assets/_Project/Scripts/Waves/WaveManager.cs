using System;
using System.Collections.Generic;
using NFramework;
using Survival.Config;
using Survival.Enemies;
using Survival.Pooling;
using UnityEngine;

namespace Survival.Waves
{
    /// <summary>
    /// Sinh quái theo từng đợt. Spec mục 5:
    ///
    ///   "Mỗi wave spawn ngẫu nhiên 3–4 quái đánh gần và 1–2 quái đánh xa."
    ///   "Chỉ spawn wave tiếp theo khi đã clear toàn bộ quái wave hiện tại."
    ///
    /// Luật thứ hai được thực thi bằng SỰ KIỆN chứ không phải bằng cách đếm mỗi khung hình:
    /// <see cref="EnemyRegistry"/> bắn <c>OnAllEnemiesCleared</c> đúng vào lúc con cuối cùng chết.
    /// Nhờ vậy lớp này gần như không làm gì trong <c>Update</c> — chỉ đếm lùi thời gian chờ
    /// giữa hai wave, và cũng chỉ khi đang thực sự chờ.
    /// </summary>
    public class WaveManager : SingletonMono<WaveManager>
    {
        [SerializeField, Tooltip("File cấu hình thành phần và nhịp độ wave.")]
        private WaveConfigSO _config;

        [SerializeField, Tooltip("Tự bắt đầu wave đầu tiên khi vào màn. Tắt nếu muốn màn hình bắt đầu điều khiển.")]
        private bool _autoStart = true;

        /// <summary>Bắn ra khi một wave mới vừa spawn xong. Tham số là số thứ tự wave, bắt đầu từ 1.</summary>
        public event Action<int> OnWaveStarted;

        /// <summary>Bắn ra khi vừa clear sạch một wave.</summary>
        public event Action<int> OnWaveCleared;

        /// <summary>Danh sách tạm dùng lại giữa các lần spawn, tránh cấp phát List mới mỗi wave.</summary>
        private readonly List<EnemyActor> _spawnBuffer = new List<EnemyActor>();

        private float _countdown;
        private bool _waitingForNextWave;
        private bool _running;

        public int CurrentWave { get; private set; }

        private void Start()
        {
            if (EnemyRegistry.I != null)
                EnemyRegistry.I.OnAllEnemiesCleared += HandleAllCleared;

            if (_autoStart)
                BeginRun();
        }

        // override chứ không phải khai báo mới: SingletonMono.OnDestroy có nhiệm vụ xoá tham chiếu
        // static trỏ tới thể hiện này. Nếu khai báo đè lên, Unity chỉ gọi bản của lớp con và
        // tham chiếu static sẽ tiếp tục trỏ vào một đối tượng đã bị huỷ.
        protected override void OnDestroy()
        {
            if (EnemyRegistry.I != null)
                EnemyRegistry.I.OnAllEnemiesCleared -= HandleAllCleared;

            base.OnDestroy();
        }

        /// <summary>Bắt đầu chuỗi wave từ đầu.</summary>
        public void BeginRun()
        {
            CurrentWave = 0;
            _running = true;
            _waitingForNextWave = true;
            _countdown = _config != null ? _config.DelayBeforeFirstWave : 1f;
        }

        /// <summary>Dừng hẳn việc sinh quái. Dùng khi player chết.</summary>
        public void StopRun()
        {
            _running = false;
            _waitingForNextWave = false;
        }

        private void Update()
        {
            if (!_running || !_waitingForNextWave)
                return;

            _countdown -= Time.deltaTime;
            if (_countdown > 0f)
                return;

            _waitingForNextWave = false;
            SpawnNextWave();
        }

        private void HandleAllCleared()
        {
            if (!_running || CurrentWave == 0)
                return;

            OnWaveCleared?.Invoke(CurrentWave);

            // Clear xong wave cuối là THẮNG — dừng hẳn, không xếp lịch wave kế.
            // Phải dừng ở đây chứ không chỉ báo ra ngoài: nếu vẫn đếm ngược thì wave mới sẽ
            // sinh ra ngay sau lưng bảng chiến thắng, và người chơi thắng xong vẫn bị đánh.
            if (_config.IsFinalWave(CurrentWave))
            {
                _running = false;
                _waitingForNextWave = false;
                OnAllWavesCleared?.Invoke(CurrentWave);
                return;
            }

            _waitingForNextWave = true;
            _countdown = _config.DelayBetweenWaves;
        }

        /// <summary>
        /// Bắn ra khi người chơi clear xong wave cuối cùng. Đây là tín hiệu THẮNG MÀN.
        ///
        /// WaveManager chỉ báo sự việc chứ không tự dựng giao diện — việc quyết định ván chơi
        /// kết thúc thế nào là của <c>GameplayManager</c>, cùng cách nó đang xử lý lúc thua.
        /// </summary>
        public event System.Action<int> OnAllWavesCleared;

        private void SpawnNextWave()
        {
            if (_config == null || PoolService.I == null)
                return;

            CurrentWave++;
            _spawnBuffer.Clear();

            var entries = _config.Entries;
            int extra = _config.ExtraEnemiesPerWave * (CurrentWave - 1);

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.EnemyPrefab == null)
                    continue;

                // Random.Range với số nguyên KHÔNG bao gồm giá trị max, nên phải +1
                // thì "3-4" mới thật sự bốc được cả 3 lẫn 4. Đây là chỗ rất dễ sai một đơn vị.
                int count = UnityEngine.Random.Range(entry.MinCount, entry.MaxCount + 1);

                for (int c = 0; c < count; c++)
                    SpawnOne(entry.EnemyPrefab);
            }

            // Phần tăng độ khó rải đều vào các loại quái đã khai báo.
            for (int e = 0; e < extra && entries.Count > 0; e++)
            {
                var entry = entries[e % entries.Count];
                if (entry.EnemyPrefab != null)
                    SpawnOne(entry.EnemyPrefab);
            }

            SpawnBossesForCurrentWave();

            OnWaveStarted?.Invoke(CurrentWave);
        }

        /// <summary>
        /// Thả những con boss được xếp lịch cho wave đang bắt đầu.
        ///
        /// Boss đi qua ĐÚNG đường sinh như mọi con quái khác — cùng pool, cùng cách chọn chỗ
        /// khuất camera, cùng đăng ký vào EnemyRegistry. Nhờ vậy điều kiện "clear hết wave"
        /// tự động tính cả boss mà không phải viết thêm một nhánh nào: wave chỉ kết thúc khi
        /// con cuối cùng chết, và boss thường là con cuối cùng đó.
        /// </summary>
        private void SpawnBossesForCurrentWave()
        {
            var bosses = _config.Bosses;
            if (bosses == null)
                return;

            for (int i = 0; i < bosses.Count; i++)
            {
                if (bosses[i].Wave != CurrentWave || bosses[i].BossPrefab == null)
                    continue;

                SpawnOne(bosses[i].BossPrefab);
            }
        }

        private void SpawnOne(EnemyActor prefab)
        {
            Vector3 position = PickSpawnPosition();

            var enemy = PoolService.I.Spawn(prefab, position, Quaternion.identity);
            if (enemy == null)
                return;

            // Quay mặt về phía player ngay từ lúc sinh ra, để không có cảnh quái đứng quay lưng
            // rồi mới từ từ xoay lại.
            var player = Player.PlayerActor.Current;
            if (player != null)
            {
                Vector3 toPlayer = player.transform.position - position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.0001f)
                    enemy.transform.rotation = Quaternion.LookRotation(toPlayer, Vector3.up);
            }

            _spawnBuffer.Add(enemy);
        }

        /// <summary>
        /// Chọn chỗ sinh quái quanh player, luôn nằm ngoài khung hình.
        ///
        /// Spawn quanh player chứ không phải ở mấy điểm cố định: người chơi di chuyển liên tục,
        /// điểm cố định sẽ khiến quái sinh ra ở tận đầu kia bản đồ và mất cả chục giây mới tới nơi.
        ///
        /// Camera được lấy một lần rồi nhớ lại: <c>Camera.main</c> thực chất là một phép
        /// tìm object theo tag, gọi cho từng con quái ở mỗi wave là lãng phí không cần thiết.
        /// </summary>
        private Vector3 PickSpawnPosition()
        {
            var player = Player.PlayerActor.Current;
            Vector3 center = player != null ? player.transform.position : Vector3.zero;

            if (_camera == null)
                _camera = Camera.main;

            return SpawnPointPicker.Pick(
                center,
                _config.MinSpawnRadius,
                _config.MaxSearchRadius,
                _config.ArenaExtent,
                _camera,
                _config.SpawnViewportMargin,
                blockMask: _config.SpawnBlockMask,
                clearRadius: _config.SpawnClearRadius);
        }

        private Camera _camera;
    }
}
