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

        private Transform _root;

        protected override void Awake()
        {
            base.Awake();

            _root = new GameObject("[Pools]").transform;
            _root.SetParent(transform);

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

            pool.transform.SetParent(_root);
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

            var t = instance.transform;
            t.SetPositionAndRotation(position, rotation);

            return instance as T;
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
