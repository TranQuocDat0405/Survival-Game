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

        /// <summary>
        /// Hiệu ứng hình ảnh đang bám trên người, tra theo cùng khoá với hiệu ứng gây ra nó.
        ///
        /// Giữ riêng khỏi <see cref="_active"/> vì hai thứ có vòng đời khác nhau về nguyên tắc:
        /// một cái là LUẬT CHƠI (trừ máu mỗi giây), cái kia là TRANG TRÍ. Gỡ hẳn phần hình ảnh
        /// ra thì độc vẫn trừ máu đúng như cũ.
        /// </summary>
        private readonly Dictionary<string, NFramework.PooledObject> _activeVfx =
            new Dictionary<string, NFramework.PooledObject>();

        [SerializeField, Min(0f), Tooltip(
            "Nâng thêm hiệu ứng bám người lên bao nhiêu unit, cộng vào con số khai báo trong " +
            "hiệu ứng. Dùng khi cùng một hiệu ứng độc bám lên player và lên quái cao thấp khác nhau.")]
        private float _vfxHeightOffset;

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

            SpawnActiveVfx(id, definition);

            runtime.OnApplied(isRefresh: false);

            // Hiệu ứng có thể kết thúc ngay trong tick đầu (ví dụ cấu hình chỉ 1 tick).
            if (runtime.IsFinished)
                Remove(id);
        }

        /// <summary>
        /// Bật hiệu ứng hình ảnh bám trên người, và GẮN NÓ LÀM CON của nhân vật.
        ///
        /// Gắn làm con là toàn bộ điểm mấu chốt: trước đây làn khói độc nổ ra tại chỗ viên đạn
        /// chạm rồi NẰM LẠI ĐÓ, nên người chơi chạy đi vài bước là khói ở lại phía sau và trông
        /// như độc đã hết trong khi máu vẫn đang tụt. Làm con thì khói đi theo người, và người
        /// chơi đọc được đúng thứ cần đọc: "tôi đang dính độc".
        /// </summary>
        private void SpawnActiveVfx(string id, StatusEffectDefinition definition)
        {
            if (definition.ActiveVfx == null || Pooling.PoolService.I == null)
                return;

            if (_activeVfx.ContainsKey(id))
                return;

            Vector3 position = transform.position + Vector3.up * (definition.ActiveVfxHeight + _vfxHeightOffset);
            var effect = Pooling.PoolService.I.Spawn(definition.ActiveVfx, position, Quaternion.identity);
            if (effect == null)
                return;

            effect.transform.SetParent(transform, worldPositionStays: true);
            _activeVfx[id] = effect;
        }

        /// <summary>
        /// Tắt hiệu ứng hình ảnh và trả về pool.
        ///
        /// PHẢI gỡ khỏi cha trước khi trả về. Nếu không, hiệu ứng vẫn là con của nhân vật, và
        /// khi nhân vật là quái được trả về pool thì hiệu ứng bị kéo theo — pool của hiệu ứng
        /// mất dấu object của mình, và lần sau nó nằm nhầm chỗ trong cây phân cấp.
        /// </summary>
        private void DespawnActiveVfx(string id)
        {
            if (!_activeVfx.TryGetValue(id, out var effect))
                return;

            _activeVfx.Remove(id);

            if (effect == null)
                return;

            // Hào quang độc bám trên người tới 3 giây, và trong quãng đó người chơi có thể bấm
            // Chơi lại — ván mới thu sạch object về pool, kể cả hào quang này. Trả thêm lần nữa
            // sẽ báo lỗi "isn't in activeObjects", nên để hàm dùng chung tự kiểm tra giúp.
            effect.transform.SetParent(null, worldPositionStays: true);
            Pooling.PoolService.ReturnIfActive(effect);
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
            DespawnActiveVfx(id);
        }

        /// <summary>Gỡ sạch mọi hiệu ứng. Gọi khi nhân vật chết hoặc được tái sử dụng từ pool.</summary>
        public void ClearAll()
        {
            _active.Clear();
            _keys.Clear();

            // Dọn cả phần hình ảnh. Bỏ sót chỗ này thì làn khói độc sẽ ở lại trên cái xác,
            // rồi theo cái xác về pool và xuất hiện lại trên con quái tiếp theo dù nó chưa
            // hề dính độc — một lỗi rất khó lần ra vì nó chỉ lộ ở wave sau.
            if (_activeVfx.Count == 0)
                return;

            _vfxKeyBuffer.Clear();
            foreach (var key in _activeVfx.Keys)
                _vfxKeyBuffer.Add(key);

            for (int i = 0; i < _vfxKeyBuffer.Count; i++)
                DespawnActiveVfx(_vfxKeyBuffer[i]);
        }

        /// <summary>Danh sách phụ để gỡ hiệu ứng hình ảnh mà không sửa Dictionary trong lúc đang duyệt nó.</summary>
        private readonly List<string> _vfxKeyBuffer = new List<string>();

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
