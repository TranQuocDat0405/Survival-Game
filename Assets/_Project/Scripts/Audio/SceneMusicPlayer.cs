using NFramework;
using UnityEngine;

namespace Survival.Audio
{
    /// <summary>
    /// Bật nhạc nền của scene hiện tại.
    ///
    /// Mỗi scene có một bản riêng và tự khai báo bản nhạc của mình, nên màn hình chính và màn
    /// chơi dùng hai bản khác nhau mà không cần ai điều phối.
    ///
    /// VÌ SAO ĐỘ TO CỦA NHẠC KHÔNG NẰM Ở ĐÂY MÀ NẰM TRONG ASSET <c>SoundSO</c>:
    /// Có hai thứ khác hẳn nhau, rất dễ nhầm thành một —
    ///
    ///   1. CÂN BẰNG PHỐI ÂM: nhạc phải nhỏ hơn hiệu ứng bao nhiêu. Đây là quyết định của người
    ///      làm game, người chơi không được đụng vào. Nó thuộc về file nhạc nên đặt ở SoundSO.
    ///
    ///   2. SỞ THÍCH NGƯỜI CHƠI: thanh trượt âm lượng nhạc trong phần cài đặt. Người chơi kéo
    ///      từ 0 tới 100%, và 100% ở đây nghĩa là "to hết mức mà bản phối cho phép".
    ///
    /// Nếu nhét cân bằng phối âm vào thanh trượt thì thanh kéo hết cỡ vẫn nghe bé xíu, và người
    /// chơi tưởng thanh trượt bị hỏng.
    ///
    /// Đo thực tế: hai bản nhạc có độ to trung bình ngang đúng tiếng bắn nỏ và to hơn cả tiếng
    /// nổ bom. Mà nhạc thì kêu liên tục còn hiệu ứng chỉ kêu một nhịp, nên để nguyên là nhạc
    /// nuốt sạch hiệu ứng.
    /// </summary>
    public class SceneMusicPlayer : MonoBehaviour
    {
        [SerializeField, Tooltip("Bản nhạc của scene này. Để trống thì scene chạy im lặng.")]
        private SoundSO _music;

        [SerializeField, Tooltip(
            "Chờ bao lâu rồi mới bật nhạc, tính bằng giây.\n\n" +
            "Để một nhịp nhỏ thì nhạc không nổi lên đúng lúc màn hình còn đang chuyển cảnh.")]
        private float _delay = 0.15f;

        private void Start()
        {
            if (_music == null || _music.clip == null)
                return;

            Invoke(nameof(PlayNow), _delay);
        }

        private void PlayNow()
        {
            if (SoundManager.I == null)
                return;

            SoundManager.I.PlayMusic(_music, loop: true);
        }
    }
}
