using NFramework;
using UnityEngine;

namespace Survival.Vfx
{
    /// <summary>
    /// Bọc một hiệu ứng hạt để nó tự trả mình về pool sau khi diễn xong.
    ///
    /// VÌ SAO PHẢI CÓ LỚP NÀY:
    /// Bộ Cartoon FX đi kèm script <c>CFXR_Effect</c>, và mặc định nó đặt
    /// <c>clearBehavior = Destroy</c> — tức là DIỄN XONG THÌ TỰ HUỶ GAMEOBJECT.
    /// Với một object lấy từ pool thì đó là thảm hoạ: pool tưởng mình vẫn còn giữ object,
    /// nhưng object đã bị huỷ mất. Lần sau lấy ra là một tham chiếu chết, và pool cạn dần
    /// cho tới khi mỗi vụ nổ lại phải cấp phát mới — đúng thứ mà pool sinh ra để tránh.
    ///
    /// Nên component này làm hai việc, và chỉ hai việc:
    ///   1. Ép mọi <c>CFXR_Effect</c> bên trong về <c>Disable</c> thay vì <c>Destroy</c>.
    ///   2. Tự đếm giờ rồi trả về pool khi hạt đã tắt hết.
    ///
    /// VÌ SAO ĐẾM GIỜ CHỨ KHÔNG DÙNG <c>OnParticleSystemStopped</c>:
    /// Sự kiện đó chỉ bắn khi <c>stopAction</c> của particle được đặt là Callback, và phải
    /// đặt tay cho TỪNG hệ hạt trong TỪNG prefab. Một hiệu ứng Cartoon FX thường có bốn tới
    /// mười hệ hạt con, nên chỉ cần quên một cái là hiệu ứng đó nằm lại ngoài pool vĩnh viễn.
    /// Đo sẵn thời lượng dài nhất trong lúc khởi tạo thì đúng một lần cho tất cả, không có
    /// đường nào lọt.
    /// </summary>
    public class PooledVfx : PooledObject
    {
        [SerializeField, Min(0f), Tooltip(
            "Sống bao lâu rồi tự về pool, tính bằng giây.\n\n" +
            "Để 0 thì tự đo lấy từ các hệ hạt bên trong — nên để 0 trong hầu hết trường hợp. " +
            "Chỉ điền tay khi hiệu ứng có phần lặp vô hạn và cần cắt ngắn chủ động.")]
        private float _lifetime;

        [SerializeField, Min(0f), Tooltip(
            "Cộng thêm vào thời lượng đo được, tính bằng giây.\n\n" +
            "Hạt cuối cùng thường vẫn đang mờ dần đúng lúc hệ hạt báo là đã dừng. " +
            "Thu về đúng khoảnh khắc đó thì người chơi thấy hiệu ứng bị cắt cụt.")]
        private float _extraTail = 0.25f;

        private ParticleSystem[] _particles;
        private float _resolvedLifetime;
        private float _timer;

        private void Awake()
        {
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            ForceCartoonFxToDisableInsteadOfDestroy();
            _resolvedLifetime = _lifetime > 0f ? _lifetime : MeasureLongestDuration() + _extraTail;
        }

        /// <summary>
        /// Bảo mọi hiệu ứng Cartoon FX bên trong là hãy TẮT chứ đừng tự huỷ.
        ///
        /// Tìm theo tên kiểu thay vì tham chiếu thẳng tới <c>CFXR_Effect</c>, để dự án vẫn
        /// biên dịch được nếu sau này gỡ bộ Cartoon FX ra. Đây là code chạy đúng một lần
        /// lúc khởi tạo mỗi hiệu ứng nên chi phí phản chiếu không đáng kể.
        /// </summary>
        private void ForceCartoonFxToDisableInsteadOfDestroy()
        {
            var components = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null || component.GetType().Name != "CFXR_Effect")
                    continue;

                var field = component.GetType().GetField("clearBehavior");
                if (field == null || !field.FieldType.IsEnum)
                    continue;

                // Enum ClearBehavior của Cartoon FX là { None = 0, Disable = 1, Destroy = 2 }.
                field.SetValue(component, System.Enum.ToObject(field.FieldType, 1));
            }
        }

        /// <summary>Hệ hạt nào tắt muộn nhất thì đó là lúc cả hiệu ứng coi như xong.</summary>
        private float MeasureLongestDuration()
        {
            float longest = 0f;

            for (int i = 0; i < _particles.Length; i++)
            {
                var main = _particles[i].main;

                // Cộng cả thời gian chờ trước khi phát, vì một hệ hạt hoãn 0.5 giây rồi mới
                // bắt đầu thì nó kết thúc muộn hơn đúng ngần ấy.
                float duration = main.duration
                                 + main.startDelayMultiplier
                                 + main.startLifetimeMultiplier;

                if (duration > longest)
                    longest = duration;
            }

            return longest;
        }

        public override void OnSpawnedFromPool()
        {
            base.OnSpawnedFromPool();

            _timer = 0f;

            // Phát lại từ đầu. Object lấy từ pool mang theo nguyên trạng thái của lần dùng
            // trước, nên không gọi Play thì lần thứ hai trở đi hiệu ứng không hiện gì cả.
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i].Clear(true);
                _particles[i].Play(true);
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= _resolvedLifetime)
                ReturnToPool();
        }
    }
}
