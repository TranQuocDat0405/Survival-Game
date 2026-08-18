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
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField, Tooltip("Nút gốc chứa toàn bộ bảng. Bị tắt trong lúc đang chơi.")]
        private GameObject _panel;

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

        private GameplayManager _session;

        private void Start()
        {
            _session = GameplayManager.I;

            if (_panel != null)
                _panel.SetActive(false);

            if (_session != null)
            {
                _session.OnGameOver += HandleGameOver;
                _session.OnVictory += HandleVictory;
                _session.OnRestarted += HandleRestarted;
            }

            if (_restartButton != null)
                _restartButton.onClick.AddListener(HandleRestartPressed);

            if (_homeButton != null)
                _homeButton.onClick.AddListener(HandleHomePressed);
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.OnGameOver -= HandleGameOver;
                _session.OnVictory -= HandleVictory;
                _session.OnRestarted -= HandleRestarted;
            }

            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(HandleRestartPressed);

            if (_homeButton != null)
                _homeButton.onClick.RemoveListener(HandleHomePressed);
        }

        private void HandleGameOver()
        {
            Show(_defeatTitle, _defeatColor);
            Audio.GameAudioService.PlayPlayerDeath();
        }

        private void HandleVictory()
        {
            Show(_victoryTitle, _victoryColor);
            Audio.GameAudioService.PlayLevelUp();
            SpawnFireworks();
        }

        private void Show(string title, Color color)
        {
            if (_panel != null)
                _panel.SetActive(true);

            if (_titleText != null)
            {
                _titleText.text = title;
                _titleText.color = color;
            }

            if (_summaryText != null)
                _summaryText.text = BuildSummary();
        }

        /// <summary>
        /// Bảng tổng kết. Cùng một bảng cho cả hai kết cục — người chơi thua cũng muốn biết
        /// mình đã đi được tới đâu, không chỉ người thắng.
        /// </summary>
        private string BuildSummary()
        {
            int wave = Waves.WaveManager.I != null ? Waves.WaveManager.I.CurrentWave : 0;
            int level = ExperienceSystem.I != null ? ExperienceSystem.I.Level.Value : 1;
            int kills = _session != null ? _session.Kills : 0;
            float seconds = _session != null ? _session.ElapsedSeconds : 0f;

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

        private void HandleRestarted()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        private void HandleRestartPressed()
        {
            Audio.GameAudioService.PlayUiClick();
            _session?.Restart();
        }

        private void HandleHomePressed()
        {
            Audio.GameAudioService.PlayUiClick();
            Core.SceneFlow.I.GoToHome();
        }
    }
}
