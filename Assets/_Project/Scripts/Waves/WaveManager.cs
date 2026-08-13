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

        private void OnDestroy()
        {
            if (EnemyRegistry.I != null)
                EnemyRegistry.I.OnAllEnemiesCleared -= HandleAllCleared;
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

            _waitingForNextWave = true;
            _countdown = _config.DelayBetweenWaves;
        }

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

            // Phần tăng độ khó (mặc định tắt) rải đều vào các loại quái đã khai báo.
            for (int e = 0; e < extra && entries.Count > 0; e++)
            {
                var entry = entries[e % entries.Count];
                if (entry.EnemyPrefab != null)
                    SpawnOne(entry.EnemyPrefab);
            }

            OnWaveStarted?.Invoke(CurrentWave);
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
        /// Chọn một điểm trên vành khuyên quanh player.
        ///
        /// Spawn quanh player chứ không phải ở mấy điểm cố định: người chơi di chuyển liên tục,
        /// điểm cố định sẽ khiến quái sinh ra ở tận đầu kia bản đồ và mất cả chục giây mới tới nơi.
        /// </summary>
        private Vector3 PickSpawnPosition()
        {
            var player = Player.PlayerActor.Current;
            Vector3 center = player != null ? player.transform.position : Vector3.zero;

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float radius = UnityEngine.Random.Range(_config.MinSpawnRadius, _config.MaxSpawnRadius);

            var offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            var position = center + offset;
            position.y = 0f;

            return position;
        }
    }
}
