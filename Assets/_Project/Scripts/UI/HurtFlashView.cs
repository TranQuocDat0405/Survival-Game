using Survival.CameraRig;
using Survival.Combat;
using Survival.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Loé một viền đỏ ở rìa màn hình mỗi khi người chơi ăn đòn, và rung camera cùng lúc.
    ///
    /// VÌ SAO CHỌN VIỀN MÀN HÌNH CHỨ KHÔNG NỔ HIỆU ỨNG TRÊN NGƯỜI:
    /// Trong game này người chơi thường xuyên bị bốn tới năm con vây đánh liên tục. Nổ một cụm
    /// hạt đỏ ngay trên nhân vật mỗi lần trúng đòn sẽ che mất chính cái cần nhìn rõ nhất —
    /// vị trí nhân vật và hướng nó đang quay. Viền nằm ở RÌA màn hình nên báo được là
    /// "vừa mất máu" mà không che một pixel nào ở giữa.
    ///
    /// Đây cũng là cách các game cùng thể loại làm, và tài liệu thiết kế phản hồi đều thống nhất
    /// hai điểm: loé sáng nên rất ngắn (khoảng 50-100 mili giây), và loé nên đi kèm rung camera
    /// chứ không đứng một mình.
    ///
    /// KHÔNG loé khi trúng độc. Độc trừ máu mỗi giây suốt ba giây; loé theo từng tick thì màn
    /// hình nhấp nháy liên tục và người chơi hết đọc được trận đánh. Việc báo dính độc là việc
    /// của một hiệu ứng riêng, không phải của lớp này.
    /// </summary>
    public class HurtFlashView : MonoBehaviour
    {
        [SerializeField, Tooltip("Ảnh viền phủ toàn màn hình. Nên là ảnh mờ dần từ rìa vào giữa, giữa trong suốt hoàn toàn.")]
        private Image _vignette;

        [SerializeField, Range(0f, 1f), Tooltip("Độ đậm nhất mà viền đạt tới ngay lúc trúng đòn.")]
        private float _peakAlpha = 0.45f;

        [SerializeField, Min(0.01f), Tooltip("Bao lâu thì mờ hẳn trở lại, tính bằng giây. Ngắn thôi, dài quá là che tầm nhìn.")]
        private float _fadeDuration = 0.28f;

        [SerializeField, Tooltip("Màu viền. Đỏ sẫm đọc rõ hơn đỏ tươi trên nền rừng xanh.")]
        private Color _tint = new Color(0.78f, 0.05f, 0.05f);

        private PlayerActor _player;
        private Health _health;
        private float _alpha;

        private void Awake()
        {
            if (_vignette != null)
            {
                _vignette.raycastTarget = false;   // không được chặn thao tác chạm lên joystick
                SetAlpha(0f);
            }
        }

        private void OnEnable() => TryBind();

        private void OnDisable() => Unbind();

        /// <summary>
        /// Player có thể chưa tồn tại lúc UI bật lên, nên thử nối lại mỗi khung hình cho tới khi được.
        /// Rẻ hơn nhiều so với cảm giác "tự nhiên hiệu ứng không chạy" mà không hiểu vì sao.
        /// </summary>
        private void TryBind()
        {
            if (_health != null)
                return;

            _player = PlayerActor.Current;
            if (_player == null || _player.Health == null)
                return;

            _health = _player.Health;
            _health.OnDamaged += HandleDamaged;
        }

        private void Unbind()
        {
            if (_health == null)
                return;

            _health.OnDamaged -= HandleDamaged;
            _health = null;
        }

        private void HandleDamaged(Health target, float appliedDamage, in DamageInfo info)
        {
            // Giáp đỡ hết thì không loé. Nhờ vậy người chơi phân biệt được "đỡ được" với "ăn đủ".
            if (appliedDamage <= 0f)
                return;

            // Độc thì để hiệu ứng báo độc lo, xem chú thích đầu lớp.
            if (info.Source == EDamageSource.Poison)
                return;

            _alpha = _peakAlpha;
            SetAlpha(_alpha);

            CameraShakeService.I?.ShakeOnPlayerHit();
            Survival.Audio.GameAudioService.PlayPlayerHurt();
        }

        private void Update()
        {
            if (_health == null)
                TryBind();

            if (_alpha <= 0f || _vignette == null)
                return;

            _alpha -= _peakAlpha * Time.deltaTime / _fadeDuration;
            SetAlpha(Mathf.Max(_alpha, 0f));
        }

        private void SetAlpha(float alpha)
        {
            if (_vignette == null)
                return;

            // Tắt hẳn component khi trong suốt: một Image phủ kín màn hình vẫn tốn một lượt vẽ
            // dù alpha bằng 0, mà phần lớn thời gian chơi thì nó vô hình.
            _vignette.enabled = alpha > 0.001f;
            _vignette.color = new Color(_tint.r, _tint.g, _tint.b, alpha);
        }
    }
}
