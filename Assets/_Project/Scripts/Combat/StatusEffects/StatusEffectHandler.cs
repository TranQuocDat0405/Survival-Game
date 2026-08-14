using System.Collections.Generic;
using UnityEngine;

namespace Survival.Combat.StatusEffects
{
    /// <summary>
    /// Giữ và chạy các hiệu ứng đang bám trên một nhân vật.
    ///
    /// Gắn cạnh <see cref="Health"/>. Mỗi loại hiệu ứng chỉ tồn tại tối đa MỘT bản
    /// trên cùng một nhân vật — tra theo <see cref="StatusEffectDefinition.Id"/>.
    /// Đây là chỗ thực thi luật "không stack sát thương" của spec: dính độc lần hai
    /// không tạo hiệu ứng thứ hai mà chỉ làm mới cái đang có.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class StatusEffectHandler : MonoBehaviour
    {
        private readonly Dictionary<string, StatusEffectRuntime> _active =
            new Dictionary<string, StatusEffectRuntime>();

        /// <summary>Danh sách phụ để duyệt mà không cấp phát enumerator của Dictionary mỗi khung hình.</summary>
        private readonly List<string> _keys = new List<string>();
        private readonly List<string> _finishedBuffer = new List<string>();

        private Health _health;

        /// <summary>Có đang dính hiệu ứng nào không. Dùng cho hiệu ứng hình ảnh (đổi màu nhân vật).</summary>
        public bool HasAnyEffect => _active.Count > 0;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _health.OnDied += HandleDied;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnDied -= HandleDied;
        }

        public void Apply(StatusEffectDefinition definition, GameObject instigator)
        {
            if (definition == null || !_health.IsAlive)
                return;

            string id = definition.Id;

            if (_active.TryGetValue(id, out var existing))
            {
                // Đã có hiệu ứng cùng loại -> làm mới nó, KHÔNG tạo thêm cái thứ hai.
                existing.OnApplied(isRefresh: true);
                return;
            }

            var context = new StatusEffectContext
            {
                Target = _health,
                Instigator = instigator,
            };

            var runtime = definition.CreateRuntime(context);
            _active[id] = runtime;
            _keys.Add(id);

            runtime.OnApplied(isRefresh: false);

            // Hiệu ứng có thể kết thúc ngay trong tick đầu (ví dụ cấu hình chỉ 1 tick).
            if (runtime.IsFinished)
                Remove(id);
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>
        /// Chạy các hiệu ứng đang bám, với bước thời gian truyền từ ngoài vào.
        ///
        /// Tách khỏi <c>Update</c> và nhận deltaTime làm tham số là có chủ đích:
        /// nếu hàm tự đọc <c>Time.deltaTime</c> bên trong thì không cách nào kiểm chứng được
        /// luật "đúng 4 tick" của spec, vì bước thời gian phụ thuộc tốc độ khung hình lúc chạy.
        /// Truyền vào từ ngoài thì viết test được: đưa đúng 1.0 giây rồi kiểm tra số máu.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_active.Count == 0)
                return;

            // Duyệt trên danh sách khoá riêng: không thể sửa Dictionary trong lúc đang duyệt nó,
            // mà tick có thể làm hiệu ứng kết thúc và cần gỡ ra.
            _finishedBuffer.Clear();

            for (int i = 0; i < _keys.Count; i++)
            {
                // Lấy khoá ra TRƯỚC khi tick, không đọc lại _keys[i] sau đó.
                //
                // Lý do: tick của nọc độc có thể trừ nốt số máu cuối cùng và GIẾT mục tiêu
                // ngay tại đây. Cái chết đó bắn sự kiện OnDied, và bộ xử lý này nghe sự kiện
                // để gọi ClearAll() — tức là danh sách _keys bị xoá sạch NGAY GIỮA VÒNG LẶP
                // đang duyệt chính nó. Đọc lại _keys[i] sau khi tick sẽ đọc vào danh sách rỗng
                // và văng ArgumentOutOfRangeException.
                //
                // Lỗi chỉ xuất hiện đúng vào lúc người chơi chết vì độc, nên rất dễ lọt qua
                // các lần thử thông thường.
                string id = _keys[i];

                if (!_active.TryGetValue(id, out var runtime))
                    continue;

                runtime.Tick(deltaTime);

                // Mục tiêu vừa chết trong tick ở trên thì mọi hiệu ứng đã bị gỡ hết rồi,
                // không còn gì để duyệt tiếp.
                if (_active.Count == 0)
                    return;

                if (runtime.IsFinished)
                    _finishedBuffer.Add(id);
            }

            for (int i = 0; i < _finishedBuffer.Count; i++)
                Remove(_finishedBuffer[i]);
        }

        private void Remove(string id)
        {
            _active.Remove(id);
            _keys.Remove(id);
        }

        /// <summary>Gỡ sạch mọi hiệu ứng. Gọi khi nhân vật chết hoặc được tái sử dụng từ pool.</summary>
        public void ClearAll()
        {
            _active.Clear();
            _keys.Clear();
        }

        private void HandleDied(Health health) => ClearAll();

        /// <summary>
        /// Tìm bộ xử lý hiệu ứng của một mục tiêu.
        /// Collider thường nằm trên GameObject con nên phải tìm ngược lên cha.
        /// </summary>
        public static StatusEffectHandler Find(IDamageable target)
        {
            if (target == null || target.Transform == null)
                return null;

            return target.Transform.GetComponentInParent<StatusEffectHandler>();
        }
    }
}
