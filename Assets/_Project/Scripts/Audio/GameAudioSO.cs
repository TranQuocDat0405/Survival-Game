using NFramework;
using UnityEngine;

namespace Survival.Audio
{
    /// <summary>
    /// Toàn bộ dàn âm thanh của game, gom vào MỘT chỗ duy nhất.
    ///
    /// VÌ SAO GOM CHUNG THAY VÌ RẢI VÀO TỪNG CONFIG:
    /// Âm thanh không thuộc về riêng một hệ thống nào — nó là lớp phản hồi cắt ngang mọi thứ,
    /// và điều quan trọng nhất khi chỉnh âm là NGHE CHÚNG CẠNH NHAU. Tiếng nổ to hay nhỏ chỉ có
    /// nghĩa khi so với tiếng bắn; tiếng trúng đòn chỉ chìm khi đặt cạnh tiếng nổ.
    /// Nếu rải mỗi tiếng vào config của hệ thống sinh ra nó thì phải mở sáu bảy file mới chỉnh
    /// được cân bằng chung, và gần như chắc chắn sẽ lệch.
    ///
    /// Đây cũng đúng cách đã làm với các mức rung camera: mọi mức rung nằm cạnh nhau trong
    /// <c>CameraShakeService</c> thay vì rải khắp các prefab.
    ///
    /// Đổi lại, thêm một LOẠI QUÁI mới thì nó dùng chung tiếng với các loại cũ. Nếu về sau cần
    /// mỗi loại một tiếng riêng thì thêm một trường <c>GameSound</c> vào <c>EnemyConfigSO</c>
    /// và cho nó quyền ghi đè — không phải đập đi làm lại.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Game Audio", fileName = "GameAudio")]
    public class GameAudioSO : ScriptableObject
    {
        [Header("Kỹ năng của người chơi")]
        [SerializeField, Tooltip("Mỗi lần bắn. Khoảng nghỉ nên để thấp vì spec cho bắn mỗi 0.5 giây.")]
        private GameSound _shoot = new GameSound();

        [SerializeField, Tooltip("Bom phát nổ. Đây là tiếng to nhất game — bom gây 50 sát thương, bán kính 5.")]
        private GameSound _bombExplode = new GameSound();

        [SerializeField, Tooltip(
            "Vụ nổ cuối cú lướt. NÊN ĐỂ KHOẢNG NGHỈ CAO: cú lướt nổ bốn điểm gần như cùng lúc, " +
            "phát đủ bốn tiếng thì biên độ cộng dồn và vỡ tiếng.")]
        private GameSound _dashExplode = new GameSound();

        [Header("Kẻ địch")]
        [SerializeField, Tooltip("Quái cận chiến vung vũ khí. Phát đúng lúc ra đòn nên nó cũng là tín hiệu báo sắp ăn đòn.")]
        private GameSound _enemyAttack = new GameSound();

        [SerializeField, Tooltip("Quái đánh xa nhả đạn độc.")]
        private GameSound _enemyRangedAttack = new GameSound();

        [SerializeField, Tooltip(
            "Quái trúng đòn. Khoảng nghỉ phải đủ lớn: một quả bom trúng năm con là năm tiếng " +
            "cùng khoảnh khắc, nghe ra thành một tiếng bụp méo chứ không phải năm cú đánh.")]
        private GameSound _enemyHurt = new GameSound();

        [SerializeField, Tooltip("Quái gục xuống.")]
        private GameSound _enemyDeath = new GameSound();

        [Header("Người chơi")]
        [SerializeField, Tooltip("Người chơi ăn đòn. Đi kèm viền đỏ loé ở rìa màn hình và rung camera.")]
        private GameSound _playerHurt = new GameSound();

        [SerializeField, Tooltip("Người chơi gục.")]
        private GameSound _playerDeath = new GameSound();

        [SerializeField, Tooltip("Lên cấp.")]
        private GameSound _levelUp = new GameSound();

        [SerializeField, Tooltip("Nhặt được bình hồi máu.")]
        private GameSound _pickup = new GameSound();

        [Header("Giao diện")]
        [SerializeField, Tooltip("Bấm nút bất kỳ trên màn hình.")]
        private GameSound _uiClick = new GameSound();

        [Header("Nhạc nền")]
        // Nhạc nền dùng thẳng SoundSO chứ KHÔNG bọc trong GameSound như mọi tiếng ở trên.
        // GameSound tồn tại để chặn nhiều tiếng giống nhau nổ chồng lên nhau trong cùng một
        // khoảnh khắc; nhạc nền thì chỉ có đúng một bản chạy tại một thời điểm và nó chạy liên
        // tục, nên khoảng nghỉ tối thiểu không có nghĩa gì. Nó cũng đi qua kênh Music của mixer
        // chứ không phải kênh SFX, tức là thanh trượt "Nhạc" trong phần cài đặt điều khiển nó.
        [SerializeField, Tooltip("Nhạc ở màn hình chính.")]
        private SoundSO _musicHome;

        [SerializeField, Tooltip("Nhạc trong lúc chơi.")]
        private SoundSO _musicIngame;

        public SoundSO MusicHome => _musicHome;
        public SoundSO MusicIngame => _musicIngame;

        public GameSound Shoot => _shoot;
        public GameSound BombExplode => _bombExplode;
        public GameSound DashExplode => _dashExplode;
        public GameSound EnemyAttack => _enemyAttack;
        public GameSound EnemyRangedAttack => _enemyRangedAttack;
        public GameSound EnemyHurt => _enemyHurt;
        public GameSound EnemyDeath => _enemyDeath;
        public GameSound PlayerHurt => _playerHurt;
        public GameSound PlayerDeath => _playerDeath;
        public GameSound LevelUp => _levelUp;
        public GameSound Pickup => _pickup;
        public GameSound UiClick => _uiClick;
    }
}
