using Survival.Core;
using Survival.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Survival.UI
{
    /// <summary>
    /// Bảng tạm dừng trong lúc chơi: Tiếp tục · Chơi lại · chỉnh âm lượng · Về màn hình chính.
    ///
    /// VÌ SAO PHẢI THẬT SỰ DỪNG GAME:
    /// Không dừng thì mở bảng ra chỉnh âm lượng là bị quái vây đánh chết. Bảng cài đặt mà giết
    /// người chơi thì thà không có còn hơn.
    ///
    /// DỪNG BẰNG <c>Time.timeScale = 0</c> LÀ CHƯA ĐỦ — và đây là chỗ dễ sai nhất:
    /// <c>timeScale = 0</c> chỉ làm <c>Time.deltaTime</c> bằng 0, nó KHÔNG chặn <c>Update</c>.
    /// Nghĩa là <c>Input.GetKeyDown</c> vẫn chạy bình thường, và người chơi bấm chuột trái trong
    /// lúc đang mở bảng thì nhân vật vẫn bắn ra một phát. Nên phải tắt hẳn phần nhận phím.
    /// Còn phía cảm ứng thì tấm nền của bảng đã chặn chạm xuyên xuống các nút bên dưới.
    /// </summary>
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField, Tooltip("Nút gốc của cả bảng. Bị tắt trong lúc đang chơi.")]
        private GameObject _panel;

        [SerializeField, Tooltip("Nút bánh răng trên màn chơi, bấm vào để mở bảng này.")]
        private Button _openButton;

        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _homeButton;

        [SerializeField, Tooltip(
            "Cụm chỉnh âm lượng nhúng thẳng trong bảng. Để trống thì bảng chỉ còn ba nút.")]
        private VolumeSettingsView _volumeSettings;

        [SerializeField, Tooltip(
            "Phím mở và đóng bảng trên máy tính. Đây là phím mà ai cũng thử đầu tiên.")]
        private KeyCode _toggleKey = KeyCode.Escape;

        private GameplayManager _session;
        private Player.KeyboardSkillInput _keyboardInput;

        public bool IsPaused { get; private set; }

        private void Start()
        {
            _session = GameplayManager.I;

            var player = Player.PlayerActor.Current;
            if (player != null)
                _keyboardInput = player.GetComponentInChildren<Player.KeyboardSkillInput>(true);

            if (_panel != null) _panel.SetActive(false);

            if (_openButton != null) _openButton.onClick.AddListener(Open);
            if (_resumeButton != null) _resumeButton.onClick.AddListener(Close);
            if (_retryButton != null) _retryButton.onClick.AddListener(HandleRetry);
            if (_homeButton != null) _homeButton.onClick.AddListener(HandleHome);

            if (_session != null)
            {
                // Hết ván thì giấu nút bánh răng đi. Bảng tạm dừng chồng lên bảng kết thúc
                // sẽ ra hai lớp nút chồng nhau, và "Tiếp tục" lúc đó cũng chẳng có nghĩa gì.
                _session.OnGameOver += HandleRunEnded;
                _session.OnVictory += HandleRunEnded;
                _session.OnRestarted += HandleRestarted;
            }
        }

        private void OnDestroy()
        {
            if (_openButton != null) _openButton.onClick.RemoveListener(Open);
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(Close);
            if (_retryButton != null) _retryButton.onClick.RemoveListener(HandleRetry);
            if (_homeButton != null) _homeButton.onClick.RemoveListener(HandleHome);

            if (_session != null)
            {
                _session.OnGameOver -= HandleRunEnded;
                _session.OnVictory -= HandleRunEnded;
                _session.OnRestarted -= HandleRestarted;
            }

            // Phòng xa: nếu object này bị huỷ trong lúc đang tạm dừng — ví dụ vì đổi scene —
            // mà không trả lại nhịp thời gian thì scene sau mở ra sẽ đứng hình hoàn toàn.
            if (IsPaused)
                Time.timeScale = 1f;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(_toggleKey))
                return;

            // Hết ván rồi thì phím này không còn tác dụng.
            if (_session != null && _session.State != EGameplayState.Playing)
                return;

            if (IsPaused) Close();
            else Open();
        }

        public void Open()
        {
            if (IsPaused)
                return;

            if (_session != null && _session.State != EGameplayState.Playing)
                return;

            IsPaused = true;
            Time.timeScale = 0f;
            SetGameplayInputEnabled(false);

            if (_panel != null) _panel.SetActive(true);
            if (_volumeSettings != null) _volumeSettings.Refresh();

            Audio.GameAudioService.PlayUiClick();
        }

        public void Close()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            Time.timeScale = 1f;
            SetGameplayInputEnabled(true);

            if (_panel != null) _panel.SetActive(false);

            Audio.GameAudioService.PlayUiClick();
        }

        private void HandleRetry()
        {
            // Trả nhịp thời gian TRƯỚC khi bắt đầu ván mới. Restart có chạy coroutine bên trong,
            // mà coroutine đợi theo thời gian sẽ đứng im vĩnh viễn nếu timeScale vẫn là 0.
            Close();
            _session?.Restart();
        }

        private void HandleHome()
        {
            // Không tự trả timeScale ở đây: SceneFlow làm việc đó sau khi tấm màn đã che kín,
            // để người chơi không kịp thấy một nhịp game chạy tiếp trước lúc chuyển cảnh.
            SetGameplayInputEnabled(true);
            IsPaused = false;

            Audio.GameAudioService.PlayUiClick();
            SceneFlow.I.GoToHome();
        }

        /// <summary>Hết ván: đóng bảng nếu đang mở, và giấu nút bánh răng đi.</summary>
        private void HandleRunEnded()
        {
            if (IsPaused)
                Close();

            if (_openButton != null)
                _openButton.gameObject.SetActive(false);
        }

        private void HandleRestarted()
        {
            if (_openButton != null)
                _openButton.gameObject.SetActive(true);
        }

        private void SetGameplayInputEnabled(bool enabled)
        {
            if (_keyboardInput != null)
                _keyboardInput.enabled = enabled;
        }
    }
}
