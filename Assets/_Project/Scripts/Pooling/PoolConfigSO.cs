using System;
using System.Collections.Generic;
using NFramework;
using UnityEngine;

namespace Survival.Pooling
{
    /// <summary>
    /// Khai báo trước những prefab nào cần được tạo sẵn và tạo sẵn bao nhiêu cái.
    ///
    /// "Tạo sẵn" (prewarm) nghĩa là lúc màn chơi vừa mở, ta tạo luôn một mớ đạn / hiệu ứng
    /// rồi tắt chúng đi. Khi bắn thì chỉ việc bật cái có sẵn lên. Nếu không làm vậy,
    /// viên đạn ĐẦU TIÊN sẽ phải chờ Unity tạo object mới ngay giữa lúc chiến đấu —
    /// đó là lúc dễ thấy khựng hình nhất.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Config/Pool Config", fileName = "PoolConfig")]
    public class PoolConfigSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("Prefab cần tạo sẵn. Prefab bắt buộc có component PooledObject.")]
            public PooledObject Prefab;

            [Min(0), Tooltip("Số lượng tạo sẵn lúc mở màn.")]
            public int PrewarmCount;

            [Tooltip("Cho phép tự tạo thêm khi dùng hết. Nên bật để không bao giờ thiếu đạn.")]
            public bool AutoExpand;

            [Tooltip("Trần số lượng giữ lại trong pool. 0 hoặc nhỏ hơn = không giới hạn.")]
            public int MaxPoolSize;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Prefab == null)
                    Debug.LogWarning($"[{name}] dòng {i} chưa gán prefab.", this);
            }
        }
#endif
    }
}
