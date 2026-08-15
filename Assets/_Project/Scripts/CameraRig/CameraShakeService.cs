using Cinemachine;
using NFramework;
using UnityEngine;

namespace Survival.CameraRig
{
    /// <summary>
    /// Rung camera. Bonus mục 8 của spec.
    ///
    /// CHỈ RUNG Ở HAI KHOẢNH KHẮC: bom nổ, và người chơi ăn đòn.
    /// Cố tình KHÔNG rung khi bắn và khi dash, dù spec có gợi ý. Lý do là nhịp chơi:
    /// đánh thường bắn liên tục ba viên một lần, mỗi 0.5 giây một loạt — rung theo từng phát
    /// thì màn hình gần như không lúc nào đứng yên, và người chơi mất khả năng đọc vị trí quái.
    /// Rung phải là thứ hiếm thì nó mới còn nghĩa. Cả hai đều bật lại được trên Inspector.
    ///
    /// VÌ SAO DÙNG CINEMACHINE IMPULSE CHỨ KHÔNG TỰ LẮC TRANSFORM:
    /// Camera do Cinemachine điều khiển — tự ghi đè vị trí camera mỗi khung hình sẽ đánh nhau
    /// với nó và sinh ra giật hình. Impulse là đường chính thức: nguồn phát ra một xung,
    /// bộ lắng nghe trên virtual camera pha xung đó vào kết quả cuối. Nó cũng tự lo việc
    /// nhiều xung chồng lên nhau, thứ mà tự viết tay rất dễ sai khi hai quả bom nổ cùng lúc.
    ///
    /// LƯU Ý VỀ BỘ CARTOON FX: bản thân nó cũng có cơ chế rung camera riêng, và mặc định là BẬT.
    /// Để nguyên thì mỗi hiệu ứng lại tự rung một kiểu, kể cả những hiệu ứng ta cố ý không muốn
    /// rung. Cơ chế đó bị tắt trong <see cref="Survival.Vfx.CartoonFxGlobalSettings"/>, nhờ vậy
    /// mọi rung của game đều đi qua đúng một nơi — chính là lớp này.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShakeService : SingletonMono<CameraShakeService>
    {
        [SerializeField, Tooltip("Tắt hẳn thì không còn rung ở đâu nữa. Tiện khi quay video cần khung hình đứng yên.")]
        private bool _enableShake = true;

        [SerializeField, Range(0f, 3f), Tooltip(
            "Độ rung khi bom nổ. Đây là cú mạnh nhất trong game nên để cao nhất.")]
        private float _bombExplosionForce = 0.9f;

        [SerializeField, Range(0f, 3f), Tooltip(
            "Độ rung khi người chơi ăn một đòn.\n\n" +
            "Nhỏ hơn bom khá nhiều, vì lúc bị vây thì đòn đến liên tục — để mạnh là màn hình " +
            "rung không ngớt và không đọc nổi trận đánh nữa.")]
        private float _playerHitForce = 0.35f;

        [SerializeField, Range(0f, 3f), Tooltip(
            "Độ rung khi nổ cuối cú lướt. Để 0 nghĩa là không rung — đây là mặc định đã chốt, " +
            "vì dash dùng rất thường xuyên để né đòn.")]
        private float _dashExplosionForce;

        [SerializeField, Range(0f, 3f), Tooltip(
            "Độ rung mỗi phát bắn. Để 0 nghĩa là không rung — mặc định đã chốt, vì bắn liên tục.")]
        private float _shootForce;

        private CinemachineImpulseSource _source;

        protected override void Awake()
        {
            base.Awake();
            _source = GetComponent<CinemachineImpulseSource>();
        }

        public void ShakeOnBombExplosion() => Shake(_bombExplosionForce);
        public void ShakeOnDashExplosion() => Shake(_dashExplosionForce);
        public void ShakeOnPlayerHit() => Shake(_playerHitForce);
        public void ShakeOnShoot() => Shake(_shootForce);

        /// <summary>Phát một xung rung. Lực bằng 0 thì bỏ qua, nên tắt một loại rung chỉ cần để số 0.</summary>
        public void Shake(float force)
        {
            if (!_enableShake || force <= 0f || _source == null)
                return;

            _source.GenerateImpulseWithForce(force);
        }

    }
}
