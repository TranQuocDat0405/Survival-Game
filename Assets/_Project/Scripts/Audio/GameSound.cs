using System;
using NFramework;
using UnityEngine;

namespace Survival.Audio
{
    /// <summary>
    /// Một tiếng động trong game, kèm hai thứ mà bản thân clip âm thanh không có:
    /// mức âm lượng riêng, và một khoảng nghỉ tối thiểu giữa hai lần phát.
    ///
    /// VÌ SAO CẦN KHOẢNG NGHỈ — đây là phần quan trọng nhất của lớp này.
    /// Nhiều sự kiện trong game xảy ra thành CHÙM chứ không lẻ tẻ:
    ///   - đánh thường bắn ba viên CÙNG MỘT LÚC
    ///   - một quả bom nổ trúng năm con quái, tức năm tiếng trúng đòn cùng khoảnh khắc
    ///   - cú lướt nổ bốn điểm liền nhau
    /// Phát đủ từng tiếng thì chúng chồng lên nhau, biên độ cộng dồn gây vỡ tiếng, và tai người
    /// nghe ra thành một tiếng "bụp" méo mó chứ không phải năm cú đánh. Chặn lại còn một tiếng
    /// trong mỗi khoảng nghỉ thì nghe rõ và đúng hơn hẳn.
    ///
    /// Ngưỡng để riêng cho từng tiếng chứ không dùng chung một con số, vì mỗi tiếng có nhịp
    /// tự nhiên khác nhau: tiếng bắn cần lặp nhanh, tiếng nổ thì không bao giờ nên chồng nhau.
    /// </summary>
    [Serializable]
    public class GameSound
    {
        [SerializeField, Tooltip("File âm thanh. Để trống thì tiếng này im lặng — tiện khi muốn tắt riêng một tiếng.")]
        private SoundSO _sound;

        [SerializeField, Range(0f, 1f), Tooltip("Chỉnh riêng âm lượng của tiếng này so với các tiếng khác.")]
        private float _volume = 1f;

        [SerializeField, Min(0f), Tooltip(
            "Hai lần phát phải cách nhau ít nhất bấy nhiêu giây.\n\n" +
            "Đặt 0 là cho phép chồng thoải mái. Với tiếng nổ nên để cao (0.15 trở lên) vì nhiều " +
            "vụ nổ cùng lúc sẽ cộng biên độ và vỡ tiếng; với tiếng bắn thì để thấp cho nhịp bắn mượt.")]
        private float _minInterval = 0.05f;

        /// <summary>
        /// Lần phát gần nhất. KHÔNG được serialize — đây là trạng thái lúc chạy, không phải cấu hình.
        /// Khởi tạo bằng số âm rất lớn để lần phát đầu tiên không bao giờ bị chặn.
        /// </summary>
        [NonSerialized] private float _lastPlayTime = -999f;

        public void Play()
        {
            if (_sound == null || _sound.clip == null)
                return;

            // Dùng unscaledTime chứ không phải time: lúc mở bảng thua cuộc, game có thể dừng
            // bằng cách đặt timeScale về 0, mà tiếng bấm nút trên bảng đó vẫn phải kêu.
            if (Time.unscaledTime - _lastPlayTime < _minInterval)
                return;

            // SoundManager là singleton nằm trong scene. Thiếu nó thì bỏ qua trong im lặng
            // thay vì ném lỗi — âm thanh là phần trang trí, không được phép làm hỏng ván chơi.
            if (SoundManager.I == null)
                return;

            _lastPlayTime = Time.unscaledTime;
            _sound.PlaySFX(volumeScale: _volume);
        }
    }
}
