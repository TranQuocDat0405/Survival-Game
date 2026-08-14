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
            "Khoảng cách gần nhất được phép sinh quái, tính từ player.\n" +
            "Quái không bao giờ sinh gần hơn khoảng này, kể cả khi chỗ đó đã khuất camera.")]
        private float _minSpawnRadius = 10f;

        [SerializeField, Min(1f), Tooltip(
            "Dò ra xa nhất tới đây để tìm chỗ khuất camera. Cần lớn hơn tầm nhìn xa nhất " +
            "của camera về phía trước, nếu không thì hướng phía trước sẽ không bao giờ tìm được chỗ hợp lệ.")]
        private float _maxSearchRadius = 34f;

        [SerializeField, Min(1f), Tooltip(
            "Bán kính sân đấu. Điểm sinh luôn bị kéo vào trong khoảng này để quái không rơi ra ngoài tường.\n\n" +
            "Phải NHỎ HƠN bán kính tường một chút. Sân là hình TRÒN nên chỗ này cũng phải là bán kính; " +
            "nếu kẹp theo hình vuông thì bốn góc hình vuông nằm xa hơn tường và quái sẽ sinh ra bên ngoài.")]
        [UnityEngine.Serialization.FormerlySerializedAs("_arenaHalfExtent")]
        private float _arenaRadius = 21f;

        [SerializeField, Range(0f, 0.5f), Tooltip(
            "Phải ra ngoài mép màn hình thêm bao nhiêu phần thì mới tính là khuất.\n" +
            "0.12 nghĩa là cách mép 12% chiều màn hình — để quái không hiện ra sát rìa " +
            "rồi lập tức trôi vào tầm nhìn ngay khung hình sau.")]
        private float _spawnViewportMargin = 0.12f;

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

        [SerializeField, Tooltip(
            "Layer của những thứ mà quái không được sinh ra bên trong: cây, đá tảng, tường.\n\n" +
            "Không có bước kiểm tra này thì thỉnh thoảng sẽ có con quái xuất hiện nằm lọt trong " +
            "gốc cây. Hệ vật lý sẽ đẩy nó bật ra một cách rất kỳ quặc, hoặc tệ hơn là nó kẹt luôn " +
            "trong đó và wave không bao giờ kết thúc vì người chơi không giết được nó.")]
        private LayerMask _spawnBlockMask;

        [SerializeField, Min(0.1f), Tooltip("Khoảng trống tối thiểu quanh điểm sinh, tính bằng unit. Nên bằng bán kính thân quái.")]
        private float _spawnClearRadius = 0.6f;

        public IReadOnlyList<SpawnEntry> Entries => _entries;
        public LayerMask SpawnBlockMask => _spawnBlockMask;
        public float SpawnClearRadius => _spawnClearRadius;
        public float MinSpawnRadius => _minSpawnRadius;
        public float MaxSearchRadius => _maxSearchRadius;
        public float ArenaRadius => _arenaRadius;
        public float SpawnViewportMargin => _spawnViewportMargin;
        public float DelayBetweenWaves => _delayBetweenWaves;
        public float DelayBeforeFirstWave => _delayBeforeFirstWave;
        public int ExtraEnemiesPerWave => _extraEnemiesPerWave;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_maxSearchRadius < _minSpawnRadius)
                _maxSearchRadius = _minSpawnRadius;

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
