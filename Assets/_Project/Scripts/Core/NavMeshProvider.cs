using UnityEngine;
using UnityEngine.AI;

namespace Survival.Core
{
    /// <summary>
    /// Giữ và nạp mặt lưới đi đường của sân đấu.
    ///
    /// VÌ SAO KHÔNG DÙNG CỬA SỔ NAVIGATION CÓ SẴN CỦA UNITY:
    /// Lệnh bake của cửa sổ đó đọc HÌNH HỌC CỦA MESH. Với sân này, mesh và collider lệch nhau
    /// rất xa — đo được trên 867 vật cản thì mesh rộng trung bình GẤP 7.1 LẦN collider.
    /// Một cây thông có thân chặn người trong bán kính 0.15 nhưng tán lá xoè ra 1.12:
    /// người chơi đi lọt dưới tán, còn mặt lưới thì coi cả vùng tán là tường.
    /// Hậu quả là những khoảng rừng người chơi đi vào được nhưng quái không có đường theo —
    /// tức là CHỖ TRỐN BẤT TỬ, đúng thứ nhà tuyển dụng thử một lần là ra.
    ///
    /// Nên mặt lưới ở đây được dựng từ CHÍNH CÁC COLLIDER mà vật lý dùng
    /// (<see cref="NavMeshCollectGeometry.PhysicsColliders"/>). Nhờ vậy thứ chặn người chơi
    /// và thứ chặn đường đi của quái là MỘT, không thể lệch nhau được nữa.
    /// Lợi thêm: cỏ hoa sỏi vốn không có collider nên tự động bị bỏ qua, không cần đánh dấu gì.
    ///
    /// Số đo trước và sau, tính từ giữa sân:
    ///   bake theo mesh     : sân trống 100%, rừng trong 97%, rừng ngoài 84%
    ///   bake theo collider : 100% / 100% / 100%
    ///
    /// Dữ liệu được nướng sẵn thành asset ở Editor (menu <c>Survival > Bake NavMesh</c>) rồi
    /// nạp vào lúc chạy, chứ không dựng lại mỗi lần mở game — dựng lại tốn thời gian khởi động
    /// và trên máy yếu thì thành một cú khựng ngay đầu ván.
    /// </summary>
    [ExecuteAlways]
    public class NavMeshProvider : MonoBehaviour
    {
        [Header("Dữ liệu đã nướng sẵn")]
        [SerializeField, Tooltip("Asset mặt lưới do menu Survival > Bake NavMesh sinh ra. Dựng lại bản đồ xong thì phải bake lại.")]
        private NavMeshData _bakedData;

        [Header("Thông số bake — sửa xong nhớ bake lại")]
        [SerializeField, Min(0.05f), Tooltip(
            "Bán kính thân của kẻ đi đường, tính bằng unit.\n\n" +
            "PHẢI khớp đúng bán kính collider của quái và của player (hiện là 0.32).\n" +
            "Để LỚN hơn thì mặt lưới bị khoét rộng ra khỏi gốc cây, sinh những khe mà người " +
            "chui lọt còn mặt lưới coi là tường — người chơi đứng vào đó là quái không tới được.\n" +
            "Để NHỎ hơn thì ngược lại: mặt lưới lấn vào chỗ thân không lọt, quái sẽ cà vào gốc cây.")]
        private float _agentRadius = 0.32f;

        [SerializeField, Min(0.1f), Tooltip("Chiều cao thân, dùng để loại những chỗ trần quá thấp.")]
        private float _agentHeight = 1.8f;

        [SerializeField, Range(0f, 60f), Tooltip("Dốc tối đa còn trèo được, tính bằng độ.")]
        private float _agentSlope = 45f;

        [SerializeField, Min(0f), Tooltip("Bậc cao tối đa còn bước lên được, tính bằng unit.")]
        private float _agentClimb = 0.3f;

        [SerializeField, Min(0f), Tooltip(
            "Mảng lưới nhỏ hơn diện tích này sẽ bị vứt bỏ, tính bằng unit vuông.\n\n" +
            "Những mảnh tí hon kẹt giữa mấy gốc cây không bao giờ là chỗ đứng hợp lệ, " +
            "giữ lại chỉ làm mặt lưới rối và làm chậm việc hỏi đường.")]
        private float _minRegionArea = 2f;

        [Header("Lấy hình học từ đâu")]
        [SerializeField, Tooltip("Chỉ collider trên các layer này được đưa vào bake. Thường là Ground, Wall và Obstacle.")]
        private LayerMask _geometryMask = 0;

        [SerializeField, Tooltip(
            "Các nhánh mà mọi collider bên dưới đều bị đánh dấu KHÔNG ĐI ĐƯỢC.\n\n" +
            "Khai báo theo nhánh chứ không theo từng vật: chỉ cần hai dòng cho cả cụm trang trí " +
            "và cụm tường, thay vì phải nhớ gắn cờ cho từng gốc cây một.")]
        private Transform[] _blockedRoots = new Transform[0];

        [SerializeField, Tooltip("Khối không gian được bake, đặt tại gốc toạ độ. Phải trùm hết sân đấu và tường bao.")]
        private Vector3 _volumeSize = new Vector3(80f, 30f, 80f);

        private NavMeshDataInstance _instance;

        /// <summary>Mã vùng "không đi được" trong bảng NavMesh Areas của Unity, luôn là 1.</summary>
        public const int NotWalkableArea = 1;

        public NavMeshData BakedData => _bakedData;
        public LayerMask GeometryMask => _geometryMask;
        public Transform[] BlockedRoots => _blockedRoots;
        public Vector3 VolumeSize => _volumeSize;

        /// <summary>Gói thông số bake lấy từ Inspector, để công cụ Editor dùng đúng một nguồn duy nhất.</summary>
        public NavMeshBuildSettings BuildSettings
        {
            get
            {
                var settings = NavMesh.GetSettingsByID(0);
                settings.agentRadius = _agentRadius;
                settings.agentHeight = _agentHeight;
                settings.agentSlope = _agentSlope;
                settings.agentClimb = _agentClimb;
                settings.minRegionArea = _minRegionArea;
                return settings;
            }
        }

        // Dùng OnEnable/OnDisable thay vì Awake/OnDestroy, kèm [ExecuteAlways]:
        // nhờ vậy mặt lưới có mặt cả trong Editor lúc chưa bấm Play. Điều đó cho phép kiểm tra
        // đường đi ngay trong Scene view, và quan trọng hơn là người mở dự án nhìn thấy ngay
        // mặt lưới xanh phủ sân thay vì tưởng dự án chưa bake gì.
        private void OnEnable() => Load();

        private void OnDisable() => Unload();

        /// <summary>Đưa mặt lưới đã nướng vào hệ thống tìm đường.</summary>
        public void Load()
        {
            Unload();

            if (_bakedData == null)
            {
                Debug.LogWarning(
                    "[NavMeshProvider] Chưa có dữ liệu mặt lưới. Chạy menu Survival > Bake NavMesh. " +
                    "Chưa có thì quái sẽ không biết đi vòng qua vật cản.", this);
                return;
            }

            _instance = NavMesh.AddNavMeshData(_bakedData);
        }

        /// <summary>Gỡ mặt lưới ra. Bắt buộc phải gọi, nếu không nạp lại scene sẽ chồng hai lớp lưới lên nhau.</summary>
        public void Unload()
        {
            if (_instance.valid)
                _instance.Remove();
        }

        /// <summary>Gắn dữ liệu vừa nướng xong. Chỉ công cụ Editor gọi.</summary>
        public void SetBakedData(NavMeshData data)
        {
            _bakedData = data;
            Load();
        }
    }
}
