using NFramework;
using Survival.Manager;
using Survival.Pooling;
using Survival.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Bảng kết thúc ván chơi — dùng chung cho CẢ THUA LẪN THẮNG.
    ///
    /// Một bảng thay vì hai vì hai kết cục cần đúng những thứ giống nhau: một dòng tiêu đề,
    /// một bảng tổng kết, một nút chơi lại. Chỉ khác chữ và màu. Dựng hai bảng riêng sẽ đẻ ra
    /// hai chỗ phải nhớ sửa mỗi lần đổi bố cục, và sớm muộn chúng sẽ lệch nhau.
    ///
    /// Thiếu bảng này thì cái chết của player trông y hệt game bị treo: không đi được, không
    /// bắn được, quái đứng im, và không một dòng chữ nào giải thích.
    ///
    /// AI QUYẾT ĐỊNH LÚC NÀO BẢNG NÀY HIỆN RA — ĐÃ ĐẢO CHIỀU SO VỚI BẢN CŨ.
    /// Trước đây bảng tự đăng ký nghe sự kiện của ván chơi rồi tự bật mình lên. Cách đó buộc nó
    /// phải TỒN TẠI SẴN trong scene suốt cả trận chỉ để chờ một sự kiện, và phải tự tắt mình đi
    /// lúc mới vào. Bây giờ <see cref="GameplayManager"/> chủ động mở nó qua UIManager và truyền
    /// luôn kết cục vào <see cref="Show"/>, nên bảng chỉ tồn tại đúng lúc nó có nghĩa.
    /// </summary>
    public class ResultPopup : Popup
    {
        [SerializeField] private Button _restartButton;

        [SerializeField, Tooltip(
            "Nút quay về màn hình chính. Không có nó thì hết ván người chơi chỉ còn đúng một lựa " +
            "chọn là chơi tiếp — muốn dừng lại phải tắt hẳn ứng dụng.")]
        private Button _homeButton;

        [SerializeField, Tooltip("Dòng tiêu đề lớn: THUA CUỘC hoặc CHIẾN THẮNG.")]
        private TextMeshProUGUI _titleText;

        [SerializeField, Tooltip("Bảng tổng kết ván chơi.")]
        private TextMeshProUGUI _summaryText;

        [Header("Chữ và màu theo kết cục")]
        [SerializeField] private string _defeatTitle = "THUA CUỘC";
        [SerializeField] private Color _defeatColor = new Color(1f, 0.35f, 0.3f);
        [SerializeField] private string _victoryTitle = "CHIẾN THẮNG";
        [SerializeField] private Color _victoryColor = new Color(1f, 0.83f, 0.25f);

        [Header("Ăn mừng khi thắng")]
        [SerializeField, Tooltip("Hiệu ứng pháo hoa nổ ra quanh người chơi khi thắng. Để trống thì bỏ qua.")]
        private PooledObject _victoryVfx;

        [SerializeField, Min(1), Tooltip("Nổ mấy chùm pháo hoa.")]
        private int _victoryVfxCount = 5;

        [SerializeField, Min(0f), Tooltip("Bán kính rải pháo hoa quanh người chơi, tính bằng unit.")]
        private float _victoryVfxRadius = 4f;

        protected override void Awake()
        {
            base.Awake();

            if (_restartButton != null) _restartButton.onClick.AddListener(HandleRestartPressed);
            if (_homeButton != null) _homeButton.onClick.AddListener(HandleHomePressed);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_restartButton != null) _restartButton.onClick.RemoveListener(HandleRestartPressed);
            if (_homeButton != null) _homeButton.onClick.RemoveListener(HandleHomePressed);
        }

        /// <summary>
        /// Điền kết cục vào bảng. <see cref="GameplayManager"/> gọi hàm này ngay lúc mở popup.
        /// </summary>
        public void Show(bool won)
        {
            if (_titleText != null)
            {
                _titleText.text = won ? _victoryTitle : _defeatTitle;
                _titleText.color = won ? _victoryColor : _defeatColor;
            }

            if (_summaryText != null)
                _summaryText.text = BuildSummary();

            if (won)
            {
                Audio.GameAudioService.PlayLevelUp();
                SpawnFireworks();
            }
            else
            {
                Audio.GameAudioService.PlayPlayerDeath();
            }
        }

        /// <summary>
        /// Ván đã kết thúc — bấm Back không được phép đóng bảng này rồi bỏ người chơi đứng giữa
        /// một trận đã xong mà không còn lối ra nào. Phải chọn Chơi lại hoặc Về Home.
        /// Vì lý do đó, ô <c>_closeButton</c> của lớp nền cũng cố tình để trống.
        /// </summary>
        public override void HandleOnKeyBack() { }

        /// <summary>
        /// Bảng tổng kết. Cùng một bảng cho cả hai kết cục — người chơi thua cũng muốn biết
        /// mình đã đi được tới đâu, không chỉ người thắng.
        /// </summary>
        private string BuildSummary()
        {
            var session = GameplayManager.I;

            int wave = Waves.WaveManager.I != null ? Waves.WaveManager.I.CurrentWave : 0;
            int level = ExperienceSystem.I != null ? ExperienceSystem.I.Level.Value : 1;
            int kills = session != null ? session.Kills : 0;
            float seconds = session != null ? session.ElapsedSeconds : 0f;

            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);

            return $"Wave {wave}   ·   Cấp {level}\n" +
                   $"Hạ {kills} quái   ·   {minutes:0}:{secs:00}";
        }

        /// <summary>
        /// Nổ vài chùm pháo hoa quanh người chơi.
        ///
        /// Rải quanh chứ không dồn một chỗ, và mỗi chùm một độ cao khác nhau — dồn vào đúng một
        /// điểm thì chúng chồng khít lên nhau và nhìn ra thành đúng MỘT vụ nổ to chứ không phải
        /// nhiều chùm pháo.
        /// </summary>
        private void SpawnFireworks()
        {
            if (_victoryVfx == null || PoolService.I == null)
                return;

            var player = Player.PlayerActor.Current;
            Vector3 center = player != null ? player.transform.position : Vector3.zero;

            for (int i = 0; i < _victoryVfxCount; i++)
            {
                float angle = i * Mathf.PI * 2f / _victoryVfxCount + Random.Range(-0.3f, 0.3f);
                float radius = Random.Range(_victoryVfxRadius * 0.5f, _victoryVfxRadius);

                var spot = center + new Vector3(Mathf.Cos(angle) * radius, Random.Range(1.5f, 3.5f), Mathf.Sin(angle) * radius);
                PoolService.I.Spawn(_victoryVfx, spot, Quaternion.identity);
            }
        }

        private void HandleRestartPressed()
        {
            Audio.GameAudioService.PlayUiClick();
            GameManager.I.EnterReset();
        }

        private void HandleHomePressed()
        {
            Audio.GameAudioService.PlayUiClick();
            GameManager.I.EnterHome();
        }
    }
}
