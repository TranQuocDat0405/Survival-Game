using System.Collections.Generic;
using NFramework;
using UnityEngine;

namespace Survival.Pooling
{
    /// <summary>
    /// Một cửa duy nhất để xin và trả object tái sử dụng.
    ///
    /// VÌ SAO CẦN POOL:
    /// <c>Instantiate</c> (tạo object mới) và <c>Destroy</c> (huỷ object) đều tốn kém.
    /// Object bị huỷ không biến mất ngay — nó trở thành rác, chờ bộ dọn rác GC đến thu gom.
    /// GC chạy thì toàn bộ game đứng hình một nhịp. Một trận đấu bắn hàng trăm mũi tên,
    /// nổ hàng chục hiệu ứng, sinh vài chục con quái — nếu tạo/huỷ thật thì giật liên tục.
    ///
    /// Pool giải quyết bằng cách: tạo sẵn một mớ, dùng xong thì TẮT đi chứ không huỷ,
    /// lần sau cần thì BẬT lại cái cũ. Không tạo mới, không sinh rác.
    ///
    /// Lớp này bọc quanh <see cref="NFramework.Pool"/> có sẵn trong framework, thêm vào:
    ///   - tra cứu pool theo prefab (framework gốc bắt phải tự giữ tham chiếu tới từng Pool)
    ///   - đặt vị trí / góc xoay ngay lúc lấy ra
    ///   - trả về đúng kiểu component mình cần, đỡ phải GetComponent
    /// </summary>
    public class PoolService : SingletonMono<PoolService>
    {
        [SerializeField, Tooltip("Danh sách prefab được tạo sẵn lúc mở màn.")]
        private PoolConfigSO _config;

        /// <summary>Bản đồ prefab -> pool tương ứng. Khoá là instance ID của prefab để tra cứu nhanh.</summary>
        private readonly Dictionary<int, Pool> _pools = new Dictionary<int, Pool>();

        [SerializeField, Tooltip(
            "Nơi cất object đang ngủ trong pool, đặt xa hẳn khu vực chơi.\n\n" +
            "BẮT BUỘC phải xa. Object trong pool bị tắt nhưng KHÔNG bị dời đi đâu cả — " +
            "nếu để chúng nằm ở gốc toạ độ, tức là ngay chỗ player đứng, thì khoảnh khắc " +
            "bật một con quái lên nó sẽ nằm CHỒNG LÊN player, và hệ vật lý đẩy văng player ra " +
            "trước khi kịp dời con quái tới chỗ spawn thật.")]
        private Vector3 _poolStoragePosition = new Vector3(0f, -500f, 0f);

        private Transform _root;

        /// <summary>
        /// Nơi cất object đang ngủ. Object tự dời về đây khi được trả lại pool.
        ///
        /// Framework gốc chỉ đổi cha chứ không dời vị trí, nên object trả về pool
        /// nằm lại đúng chỗ nó vừa chết. Vô hại về mặt chạy game (object đã tắt),
        /// nhưng lúc gỡ lỗi thì rất rối: nhìn Scene View thấy một đống quái nằm chồng lên player
        /// mà không biết cái nào còn sống cái nào đã chết.
        /// </summary>
        public static Vector3 StoragePosition =>
            IsSingletonAlive ? I._poolStoragePosition : new Vector3(0f, -500f, 0f);

        protected override void Awake()
        {
            base.Awake();

            _root = new GameObject("[Pools]").transform;
            _root.SetParent(transform);
            _root.position = _poolStoragePosition;

            Prewarm();
        }

        private void Prewarm()
        {
            if (_config == null)
                return;

            var entries = _config.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Prefab == null)
                    continue;

                GetOrCreatePool(entry.Prefab, entry.PrewarmCount, entry.AutoExpand, entry.MaxPoolSize);
            }
        }

        private Pool GetOrCreatePool(PooledObject prefab, int prewarm, bool autoExpand, int maxPoolSize)
        {
            int key = prefab.GetInstanceID();
            if (_pools.TryGetValue(key, out var existing))
                return existing;

            var pool = Pool.CreatePool(
                initializeAtAwake: true,
                autoExpandPool: autoExpand,
                initPoolSize: prewarm,
                objectToPool: prefab,
                maxPoolSize: maxPoolSize);

            // Tham số false ở đây là bắt buộc, không phải tuỳ chọn.
            //
            // Pool.CreatePool đã tạo sẵn các object con NGAY LÚC ĐÓ, tại gốc toạ độ,
            // trước khi mình kịp đưa pool về kho chứa. Mà SetParent mặc định GIỮ NGUYÊN
            // vị trí trong thế giới — nên nếu gọi SetParent(_root) không kèm tham số,
            // pool sẽ nằm dưới kho về mặt cây phân cấp nhưng vẫn ĐỨNG YÊN ở gốc toạ độ,
            // tức là đúng chỗ player. Truyền false thì pool nhảy hẳn về vị trí của kho
            // và kéo theo toàn bộ object con.
            pool.transform.SetParent(_root, false);
            pool.transform.localPosition = Vector3.zero;
            pool.name = $"Pool_{prefab.name}";
            _pools[key] = pool;
            return pool;
        }

        /// <summary>
        /// Lấy một bản sao của prefab từ pool, đặt sẵn vị trí và góc xoay.
        /// Nếu prefab chưa có pool thì pool được tạo ngay tại chỗ — nên quên khai báo trong
        /// PoolConfig cũng không gây lỗi, chỉ mất một nhịp tạo pool ở lần dùng đầu tiên.
        /// </summary>
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : PooledObject
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolService] Spawn với prefab null.", this);
                return null;
            }

            var pool = GetOrCreatePool(prefab, prewarm: 0, autoExpand: true, maxPoolSize: 0);

            var instance = pool.GetPooledObject();
            if (instance == null)
                return null;

            Teleport(instance.transform, position, rotation);

            return instance as T;
        }

        /// <summary>
        /// Dời object tới chỗ mới một cách dứt khoát.
        ///
        /// Gán <c>transform.position</c> KHÔNG đủ cho object có Rigidbody:
        ///   - Thân vật lý vẫn còn ở vị trí cũ cho tới bước vật lý kế tiếp, nên nó có thể
        ///     va chạm ở chỗ cũ trước khi kịp nhận vị trí mới.
        ///   - Rigidbody đang bật nội suy (Interpolate) sẽ VẼ đường đi mượt từ chỗ cũ sang chỗ mới,
        ///     tạo ra cảnh con quái bay vụt ngang màn hình lúc vừa spawn.
        ///   - Vận tốc còn sót lại từ lần dùng trước sẽ khiến nó trôi đi ngay khi vừa hiện ra.
        /// Nên phải đặt cả vị trí của Rigidbody và xoá sạch vận tốc.
        /// </summary>
        private static void Teleport(Transform target, Vector3 position, Quaternion rotation)
        {
            target.SetPositionAndRotation(position, rotation);

            if (!target.TryGetComponent<Rigidbody>(out var body))
                return;

            body.position = position;
            body.rotation = rotation;

            if (!body.isKinematic)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        public T Spawn<T>(T prefab, Vector3 position) where T : PooledObject
            => Spawn(prefab, position, Quaternion.identity);

        /// <summary>Trả object về pool. Chỉ là lối gọi cho thuận tay — object tự biết pool của nó.</summary>
        public void Despawn(PooledObject instance)
        {
            if (instance != null)
                instance.ReturnToPool();
        }

        /// <summary>Thu hồi tất cả object đang hoạt động. Dùng khi chơi lại từ đầu.</summary>
        public void DespawnAll()
        {
            foreach (var pool in _pools.Values)
                pool.ReturnAllToPool();
        }
    }
}
