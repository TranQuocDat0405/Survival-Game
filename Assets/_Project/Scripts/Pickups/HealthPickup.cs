using NFramework;
using Survival.Player;
using UnityEngine;

namespace Survival.Pickups
{
    /// <summary>
    /// Bình hồi máu nằm trên sân, đi đè lên là tự nhặt.
    ///
    /// PHÁT HIỆN BẰNG KHOẢNG CÁCH CHỨ KHÔNG BẰNG TRIGGER COLLIDER.
    /// Trigger nghe có vẻ đúng bài hơn, nhưng nó kéo theo một chuỗi thứ phải khớp cho đúng:
    /// vật phẩm phải nằm trên một layer, layer đó phải được bật trong bảng va chạm với layer
    /// Player, và cả hai bên phải có Rigidbody. Sai một mắt xích thì vật phẩm im lặng không
    /// nhặt được, mà không có lỗi nào báo ra — đúng kiểu lỗi tốn cả buổi để lần.
    /// Sân chỉ có tối đa ba bình cùng lúc, nên so khoảng cách mỗi khung hình là ba phép trừ —
    /// rẻ hơn rất nhiều so với rủi ro đó.
    ///
    /// XOAY VÀ NHẤP NHÔ là phần bắt buộc chứ không phải trang trí: nền sân là cỏ xanh có hoa
    /// lá lấm tấm, một vật đứng yên rất dễ chìm vào đó. Chuyển động là thứ mắt người bắt được
    /// ngay cả khi đang bận nhìn chỗ khác.
    /// </summary>
    public class HealthPickup : PooledObject
    {
        [SerializeField, Tooltip("Phần hình ảnh xoay và nhấp nhô. Để trống thì lấy chính object này.")]
        private Transform _visual;

        [SerializeField, Tooltip("Tốc độ xoay quanh trục đứng, độ mỗi giây.")]
        private float _spinSpeed = 90f;

        [SerializeField, Min(0f), Tooltip("Biên độ nhấp nhô lên xuống, tính bằng unit.")]
        private float _bobHeight = 0.18f;

        [SerializeField, Min(0.1f), Tooltip("Một nhịp nhấp nhô mất bao lâu, tính bằng giây.")]
        private float _bobPeriod = 1.6f;

        private float _healAmount;
        private float _pickupRadius = 1f;
        private Vector3 _visualBaseLocalPosition;
        private float _bobTimer;
        private bool _collected;

        private void Awake()
        {
            if (_visual == null)
                _visual = transform;

            _visualBaseLocalPosition = _visual == transform ? Vector3.zero : _visual.localPosition;
        }

        /// <summary>Nạp số liệu ngay sau khi lấy ra khỏi pool.</summary>
        public void Setup(float healAmount, float pickupRadius)
        {
            _healAmount = healAmount;
            _pickupRadius = pickupRadius;
            _collected = false;

            // Bốc một pha ngẫu nhiên cho nhịp nhấp nhô. Không làm vậy thì mấy bình trên sân
            // nhấp nhô đồng loạt như một, nhìn ra ngay là máy sinh chứ không phải rơi tự nhiên.
            _bobTimer = Random.Range(0f, _bobPeriod);
        }

        public override void OnSpawnedFromPool()
        {
            base.OnSpawnedFromPool();
            _collected = false;
        }

        private void Update()
        {
            if (_collected)
                return;

            AnimateVisual();
            TryCollect();
        }

        private void AnimateVisual()
        {
            if (_visual == null)
                return;

            _visual.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.World);

            _bobTimer += Time.deltaTime;
            float phase = Mathf.Sin(_bobTimer / _bobPeriod * Mathf.PI * 2f);

            if (_visual == transform)
                return;   // không nhấp nhô được nếu hình ảnh chính là gốc, vì gốc phải đứng yên để đo khoảng cách

            _visual.localPosition = _visualBaseLocalPosition + Vector3.up * (phase * _bobHeight);
        }

        private void TryCollect()
        {
            var player = PlayerActor.Current;
            if (player == null || player.Health == null || !player.Health.IsAlive)
                return;

            // So bình phương khoảng cách để khỏi phải tính căn bậc hai mỗi khung hình.
            // Bỏ qua chênh lệch chiều cao: vật phẩm nằm sát đất còn tâm người chơi ở ngang bụng,
            // tính cả trục đứng thì phải đi đè chính xác hơn thực tế cần.
            Vector3 delta = player.transform.position - transform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude > _pickupRadius * _pickupRadius)
                return;

            Collect(player);
        }

        private void Collect(PlayerActor player)
        {
            // Khoá lại NGAY. Nếu không, hai khung hình liên tiếp đều thấy đủ gần và vật phẩm
            // hồi máu hai lần trước khi kịp về pool.
            _collected = true;

            player.Health.Heal(_healAmount);

            Audio.GameAudioService.PlayPickup();

            ReturnToPool();
        }
    }
}
