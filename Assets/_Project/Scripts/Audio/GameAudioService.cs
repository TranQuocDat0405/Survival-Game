using NFramework;
using UnityEngine;

namespace Survival.Audio
{
    /// <summary>
    /// Cửa duy nhất để phần còn lại của game phát âm thanh.
    ///
    /// Mọi nơi gọi qua đây bằng một dòng, ví dụ <c>GameAudioService.PlayShoot()</c>. Nhờ vậy:
    ///
    ///   - Không chỗ nào trong gameplay phải giữ tham chiếu tới file âm thanh. Xoá hẳn service
    ///     này đi thì game vẫn chạy đủ luật, chỉ là im lặng — đúng nguyên tắc đã theo từ đầu
    ///     là phần trang trí không bao giờ được nắm quyền quyết định gì của luật chơi.
    ///   - Đổi một tiếng chỉ cần sửa một ô trong asset GameAudio, không phải lần theo code.
    ///
    /// CÁC HÀM TĨNH ĐỀU AN TOÀN KHI KHÔNG CÓ SERVICE. Chưa đặt service vào scene, hoặc đang chạy
    /// một scene thử nghiệm riêng, thì chúng lặng lẽ không làm gì thay vì ném lỗi. Âm thanh hỏng
    /// không được phép làm hỏng ván chơi.
    /// </summary>
    public class GameAudioService : SingletonMono<GameAudioService>
    {
        [SerializeField, Tooltip("Asset khai báo toàn bộ dàn âm thanh. Thiếu thì game chạy im lặng.")]
        private GameAudioSO _audio;

        /// <summary>Cấu hình âm thanh đang dùng, hoặc null nếu chưa gắn service vào scene.</summary>
        private static GameAudioSO Config => IsSingletonAlive ? I._audio : null;

        public static void PlayShoot() => Config?.Shoot.Play();
        public static void PlayBombExplode() => Config?.BombExplode.Play();
        public static void PlayDashExplode() => Config?.DashExplode.Play();
        public static void PlayEnemyAttack() => Config?.EnemyAttack.Play();
        public static void PlayEnemyRangedAttack() => Config?.EnemyRangedAttack.Play();
        public static void PlayEnemyHurt() => Config?.EnemyHurt.Play();
        public static void PlayEnemyDeath() => Config?.EnemyDeath.Play();
        public static void PlayPlayerHurt() => Config?.PlayerHurt.Play();
        public static void PlayPlayerDeath() => Config?.PlayerDeath.Play();
        public static void PlayLevelUp() => Config?.LevelUp.Play();
        public static void PlayPickup() => Config?.Pickup.Play();
        public static void PlayUiClick() => Config?.UiClick.Play();

        #region Nhạc nền

        /// <summary>
        /// Nhạc nền do <c>GameManager</c> phát theo trạng thái ứng dụng, không phải do scene tự phát.
        ///
        /// VÌ SAO ĐỔI CÁCH LÀM: trước đây mỗi scene có một <c>SceneMusicPlayer</c> tự khai báo bản
        /// nhạc của mình. Cách đó chỉ chạy được khi mỗi màn hình là một scene riêng. Sau refactor,
        /// màn hình chính và màn chơi dùng CHUNG một scene nền (Main) nên không còn "scene của màn
        /// hình chính" để gắn component vào nữa — thứ đổi khi người chơi đi lại giữa hai màn hình
        /// là TRẠNG THÁI, và trạng thái thì thuộc về GameManager.
        ///
        /// Vẫn không dùng chuỗi đường dẫn kiểu <c>PlayMusicResource("Audio/Music/menu")</c>: tham
        /// chiếu thẳng tới asset thì gõ sai là lỗi biên dịch, còn gõ sai một chuỗi thì im lặng.
        /// </summary>
        public static void PlayHomeMusic() => PlayMusic(Config?.MusicHome);

        public static void PlayIngameMusic() => PlayMusic(Config?.MusicIngame);

        public static void StopMusic()
        {
            if (SoundManager.I != null)
                SoundManager.I.StopMusic(0.3f);
        }

        private static void PlayMusic(SoundSO music)
        {
            if (music == null || music.clip == null || SoundManager.I == null)
                return;

            SoundManager.I.PlayMusic(music, loop: true);
        }

        #endregion
    }
}
