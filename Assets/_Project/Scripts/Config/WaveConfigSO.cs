using System;
using System.Collections.Generic;
using Survival.Enemies;
using UnityEngine;

namespace Survival.Config
{
    /// <summary>
    /// Thành phần một wave. Spec mục 5:
    /// "Mỗi wave spawn ngẫu nhiên 3–4 quái đánh gần và 1–2 quái đánh xa."
    ///
    /// Được viết dưới dạng DANH SÁCH các loại quái kèm khoảng số lượng, chứ không viết cứng
    /// "3-4 con loại A, 1-2 con loại B". Nhờ vậy thêm loại quái thứ ba vào wave
    /// chỉ là thêm một dòng trên Inspector — không phải sửa code sinh quái.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Config/Wave Config", fileName = "WaveConfig")]
    public class WaveConfigSO : ScriptableObject
    {
        [Serializable]
        public struct SpawnEntry
        {
            [Tooltip("Prefab quái. Phải có component EnemyActor.")]
            public EnemyActor EnemyPrefab;

            [Tooltip("Số lượng tối thiểu spawn mỗi wave.")]
            [Min(0)] public int MinCount;

            [Tooltip("Số lượng tối đa spawn mỗi wave. Số thực tế được bốc ngẫu nhiên trong khoảng này.")]
            [Min(0)] public int MaxCount;
        }

        [Header("Thành phần mỗi wave")]
        [SerializeField]
        private List<SpawnEntry> _entries = new List<SpawnEntry>();

        [Header("Vị trí spawn")]
        [SerializeField, Min(1f), Tooltip(
            "Bán kính tối thiểu tính từ player. Phải đủ lớn để quái không hiện ra ngay trước mặt " +
            "người chơi — vừa gây bất ngờ khó chịu, vừa khiến người chơi ăn đòn mà không kịp phản ứng.")]
        private float _minSpawnRadius = 12f;

        [SerializeField, Min(1f), Tooltip("Bán kính tối đa tính từ player.")]
        private float _maxSpawnRadius = 16f;

        [Header("Nhịp độ")]
        [SerializeField, Min(0f), Tooltip("Chờ bao lâu sau khi clear sạch wave rồi mới spawn wave kế.")]
        private float _delayBetweenWaves = 2f;

        [SerializeField, Min(0f), Tooltip("Chờ bao lâu khi vừa vào màn rồi mới spawn wave đầu tiên.")]
        private float _delayBeforeFirstWave = 1.5f;

        [Header("Tăng độ khó theo wave (tuỳ chọn)")]
        [SerializeField, Min(0), Tooltip(
            "Mỗi wave thêm bao nhiêu quái vào TỔNG số. Để 0 thì mọi wave đều theo đúng spec.\n" +
            "Spec không yêu cầu tăng độ khó, nên mặc định là 0.")]
        private int _extraEnemiesPerWave = 0;

        public IReadOnlyList<SpawnEntry> Entries => _entries;
        public float MinSpawnRadius => _minSpawnRadius;
        public float MaxSpawnRadius => _maxSpawnRadius;
        public float DelayBetweenWaves => _delayBetweenWaves;
        public float DelayBeforeFirstWave => _delayBeforeFirstWave;
        public int ExtraEnemiesPerWave => _extraEnemiesPerWave;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_maxSpawnRadius < _minSpawnRadius)
                _maxSpawnRadius = _minSpawnRadius;

            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.EnemyPrefab == null)
                    Debug.LogWarning($"[{name}] dòng {i} chưa gán prefab quái.", this);

                if (e.MaxCount < e.MinCount)
                {
                    e.MaxCount = e.MinCount;
                    _entries[i] = e;
                }
            }
        }
#endif
    }
}
