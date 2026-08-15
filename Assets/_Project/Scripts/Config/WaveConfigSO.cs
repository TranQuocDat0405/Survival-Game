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

        /// <summary>Một con boss xuất hiện ở đúng một wave nhất định.</summary>
        [Serializable]
        public struct BossEntry
        {
            [Tooltip("Xuất hiện ở wave số mấy. Wave đầu tiên là 1.")]
            [Min(1)] public int Wave;

            [Tooltip("Prefab boss. Phải có component EnemyActor, giống mọi loại quái khác.")]
            public EnemyActor BossPrefab;
        }

        [Header("Thành phần mỗi wave")]
        [SerializeField]
        private List<SpawnEntry> _entries = new List<SpawnEntry>();

        [Header("Boss")]
        [SerializeField, Tooltip(
            "Lịch boss: con nào ra ở wave nào. Để trống thì không có boss.\n\n" +
            "Boss KHÔNG phải một cơ chế riêng — nó chỉ là một EnemyActor với config có máu và " +
            "sát thương cao hơn. Nhờ vậy nó dùng lại nguyên bộ AI, đường tìm đường, thanh máu, " +
            "hiệu ứng chết và tính EXP đã có; thêm một con boss nữa chỉ là thêm một dòng ở đây.")]
        private List<BossEntry> _bosses = new List<BossEntry>();

        [Header("Kết thúc màn chơi")]
        [SerializeField, Min(0), Tooltip(
            "Clear xong wave này thì THẮNG. Để 0 nghĩa là chơi vô hạn, không bao giờ thắng.\n\n" +
            "Spec không nói gì về điều kiện thắng — mục 5 chỉ mô tả cách wave nối nhau. Đặt một " +
            "wave cuối là lựa chọn có chủ đích cho một bài nộp: người chấm chơi hết được một vòng " +
            "trọn vẹn có mở đầu, cao trào và kết, thay vì phải tự quyết định lúc nào thì dừng.")]
        private int _finalWave = 5;

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
            "Nửa cạnh sân đấu vuông. Điểm sinh luôn bị kéo vào trong khoảng này để quái không rơi ra ngoài tường.\n\n" +
            "Phải NHỎ HƠN nửa cạnh tường một chút. Hình dạng ở đây bắt buộc phải khớp với hình dạng " +
            "tường vô hình: sân vuông thì kẹp theo từng trục, sân tròn thì kẹp theo bán kính. " +
            "Lệch hình dạng là sinh ra quái nằm ngoài tường, hoặc chừa hẳn bốn góc sân không bao giờ có quái.")]
        [UnityEngine.Serialization.FormerlySerializedAs("_arenaRadius")]
        private float _arenaExtent = 33f;

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
        public IReadOnlyList<BossEntry> Bosses => _bosses;
        public int FinalWave => _finalWave;

        /// <summary>Wave này có phải wave cuối không. Trả về false khi cấu hình là chơi vô hạn.</summary>
        public bool IsFinalWave(int wave) => _finalWave > 0 && wave >= _finalWave;
        public LayerMask SpawnBlockMask => _spawnBlockMask;
        public float SpawnClearRadius => _spawnClearRadius;
        public float MinSpawnRadius => _minSpawnRadius;
        public float MaxSearchRadius => _maxSearchRadius;
        public float ArenaExtent => _arenaExtent;
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
