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
        public static void PlayUiClick() => Config?.UiClick.Play();
    }
}
