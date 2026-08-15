using NFramework;
using UnityEngine;

namespace Survival.Config
{
    /// <summary>
    /// Mọi con số của bình hồi máu rơi trên sân.
    ///
    /// ĐÂY LÀ PHẦN NGOÀI SPEC. `Docs/README.md` không hề nhắc tới vật phẩm hồi máu — nó được
    /// thêm vào vì trong một ván dài, người chơi mất máu dần mà cách duy nhất hồi lại là lên cấp,
    /// nên càng về sau càng chỉ có một chiều đi xuống. Ghi rõ ra đây để người chấm biết đâu là
    /// yêu cầu của đề và đâu là phần tự thêm.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Pickup Config", fileName = "PickupConfig")]
    public class PickupConfigSO : ScriptableObject
    {
        [Header("Vật phẩm")]
        [SerializeField, Tooltip("Prefab bình hồi máu. Để trống thì không sinh gì cả.")]
        private PooledObject _healthPickupPrefab;

        [SerializeField, Min(0f), Tooltip("Nhặt một cái thì hồi bao nhiêu máu. 75 là 15% của 500 máu khởi đầu.")]
        private float _healAmount = 75f;

        [Header("Nhịp sinh")]
        [SerializeField, Min(0.5f), Tooltip("Bao lâu sinh một cái, tính bằng giây.")]
        private float _spawnInterval = 10f;

        [SerializeField, Min(1), Tooltip(
            "Trên sân cùng lúc nhiều nhất bao nhiêu cái.\n\n" +
            "Có giới hạn này thì không cần cho vật phẩm tự biến mất: người chơi bỏ qua vài cái " +
            "cũng không bao giờ tới mức sân đầy rác, mà cũng không bị mất phần thưởng oan " +
            "chỉ vì lúc đó đang bị vây không thoát ra được.")]
        private int _maxAlive = 3;

        [Header("Sinh ở đâu")]
        [SerializeField, Min(0.5f), Tooltip(
            "Không sinh gần người chơi hơn khoảng này.\n" +
            "Gần quá thì vật phẩm tự dính vào người, mất hẳn phần phải chủ động đi nhặt.")]
        private float _minSpawnRadius = 4f;

        [SerializeField, Min(1f), Tooltip("Xa nhất tới đây. Nên để trong tầm nhìn camera để người chơi thấy mà đi nhặt.")]
        private float _maxSpawnRadius = 9f;

        [SerializeField, Tooltip(
            "BẬT = chỉ sinh ở chỗ ĐANG NHÌN THẤY trên màn hình.\n\n" +
            "Đây là điểm khác hẳn với chỗ sinh quái, vốn bắt buộc phải KHUẤT camera. " +
            "Vật phẩm mà rơi ngoài khung hình thì người chơi không biết nó tồn tại, và nó chỉ " +
            "làm nền chứ không tạo ra quyết định nào.")]
        private bool _requireVisible = true;

        [SerializeField, Range(0f, 0.4f), Tooltip(
            "Phải nằm sâu vào trong mép màn hình bao nhiêu, tính theo tỉ lệ khung hình.\n" +
            "0.15 nghĩa là cách mép ít nhất 15% — để vật phẩm không bị dính sát rìa rồi trôi ra " +
            "ngoài ngay khi người chơi nhúc nhích.")]
        private float _viewportMargin = 0.15f;

        [SerializeField, Tooltip("Layer của vật cản. Chỗ sinh nằm đè lên cây đá sẽ bị loại.")]
        private LayerMask _blockMask;

        [SerializeField, Min(0.1f), Tooltip("Bán kính khoảng trống cần có quanh chỗ sinh.")]
        private float _clearRadius = 0.6f;

        [SerializeField, Min(1), Tooltip("Thử bao nhiêu chỗ trước khi bỏ qua lượt sinh này.")]
        private int _placementAttempts = 24;

        [Header("Điều kiện")]
        [SerializeField, Tooltip(
            "BẬT = chỉ sinh khi người chơi đang thiếu máu.\n\n" +
            "Tắt thì đầu ván, lúc còn đủ máu, sân đã có sẵn mấy bình vô dụng nằm chờ.")]
        private bool _onlyWhenHurt = true;

        [SerializeField, Min(0.1f), Tooltip("Khoảng cách đủ gần để tự nhặt, tính bằng unit.")]
        private float _pickupRadius = 0.9f;

        public PooledObject HealthPickupPrefab => _healthPickupPrefab;
        public float HealAmount => _healAmount;
        public float SpawnInterval => _spawnInterval;
        public int MaxAlive => _maxAlive;
        public float MinSpawnRadius => _minSpawnRadius;
        public float MaxSpawnRadius => _maxSpawnRadius;
        public bool RequireVisible => _requireVisible;
        public float ViewportMargin => _viewportMargin;
        public LayerMask BlockMask => _blockMask;
        public float ClearRadius => _clearRadius;
        public int PlacementAttempts => _placementAttempts;
        public bool OnlyWhenHurt => _onlyWhenHurt;
        public float PickupRadius => _pickupRadius;
    }
}
